using System.Text.Json;
using Backend.Negocio.Pipeline;
using Backend.Utils;

namespace Backend.Services
{
    public sealed class IncidenteJobWorker : BackgroundService
    {
        private readonly IncidentePipelineRepository _repo = new();
        private readonly HttpClient _httpClient = new();
        private DateTime _lastStuckSweep = DateTime.MinValue;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                PipelineJob? job = null;
                try
                {
                    if ((DateTime.UtcNow - _lastStuckSweep) > TimeSpan.FromMinutes(1))
                    {
                        var reset = _repo.ResetStuckProcessingJobs(AppConfig.AUDIO_JOB_STUCK_MINUTES);
                        if (reset > 0)
                            Console.WriteLine($"[Pipeline] Reset {reset} jobs stuck in processing.");
                        _lastStuckSweep = DateTime.UtcNow;
                    }

                    job = _repo.ObtenerSiguienteJob();
                    if (job == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        continue;
                    }

                    var attempts = job.Attempts + 1;
                    _repo.MarcarJobProcesando(job.Id, attempts);

                    using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    jobCts.CancelAfter(TimeSpan.FromSeconds(AppConfig.AUDIO_JOB_TIMEOUT_SECONDS));
                    var jobToken = jobCts.Token;

                    var audio = _repo.ObtenerAudioPorIncidente(job.IncidenteId);
                    if (audio == null || string.IsNullOrWhiteSpace(audio.FilePath))
                    {
                        _repo.MarcarJobError(job.Id, "Audio no encontrado.");
                        continue;
                    }

                    try
                    {
                        var transcode = await AudioTranscoder.TranscodeIfEnabledAsync(audio.FilePath, audio.Format, jobToken);
                        if (transcode != null)
                        {
                            _repo.ActualizarAudioArchivo(audio.Id, transcode.RelativePath, transcode.Format, transcode.Hash);
                            audio = new IncidenteAudioInfo
                            {
                                Id = audio.Id,
                                IncidenteId = audio.IncidenteId,
                                FilePath = transcode.RelativePath,
                                Format = transcode.Format,
                                Hash = transcode.Hash
                            };
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            throw;
                        // si falla la transcodificacion por timeout, seguimos con el original
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Pipeline] Transcodificacion fallida: {ex.Message}");
                        // seguir con el audio original
                    }

                    var fullPath = AudioStorage.ResolveFullPath(audio.FilePath);
                    if (!File.Exists(fullPath))
                    {
                        _repo.MarcarJobError(job.Id, "Archivo de audio no encontrado.");
                        continue;
                    }

                    WhisperResult? whisper;
                    try
                    {
                        whisper = await WhisperClient.TranscribeAsync(fullPath, _httpClient, jobToken);
                    }
                    catch (OperationCanceledException)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            throw;
                        if (attempts < 3)
                            _repo.MarcarJobRetry(job.Id, "Timeout en transcripcion.");
                        else
                            _repo.MarcarJobError(job.Id, "Timeout en transcripcion.");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        var msg = $"Error en transcripcion: {ex.Message}";
                        if (attempts < 3)
                            _repo.MarcarJobRetry(job.Id, msg);
                        else
                            _repo.MarcarJobError(job.Id, msg);
                        continue;
                    }

                    if (whisper == null || string.IsNullOrWhiteSpace(whisper.Text))
                    {
                        if (attempts < 3)
                        {
                            _repo.MarcarJobRetry(job.Id, "Transcripcion vacia.");
                        }
                        else
                        {
                            _repo.MarcarJobError(job.Id, "No se pudo transcribir el audio.");
                        }
                        continue;
                    }

                    var catalogo = _repo.ObtenerCatalogoHechos();
                    var extract = IncidentExtractor.Extract(whisper.Text, catalogo);
                    if (AppConfig.LLM_ENABLED)
                    {
                        try
                        {
                            var llm = await LlmClient.ExtractIncidentAsync(whisper.Text, catalogo, _httpClient, jobToken);
                            if (llm != null)
                            {
                                Console.WriteLine($"[LLM] Ok fecha={llm.Fecha} hora={llm.Hora} lugar={llm.LugarTexto} tipo={llm.TipoCodigo}");
                                extract = IncidentExtractor.MergeWithLlm(extract, llm, catalogo);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[LLM] Error en extraccion: {ex.Message}");
                        }
                    }

                    GeocodeResult? geocode = null;
                    var extractedLocation = IncidentExtractor.NormalizeLocationForGeocode(extract.LugarTexto) ?? extract.LugarTexto;
                    var locationContext = LocationNormalizer.Build(whisper.Text, extractedLocation, null);
                    var lugarTexto = locationContext.DisplayText ?? extractedLocation ?? "Pendiente";

                    try
                    {
                        foreach (var candidate in locationContext.Candidates)
                        {
                            geocode = await NominatimClient.GeocodeAsync(candidate, _httpClient, jobToken);
                            if (geocode != null)
                                break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            throw;
                        Console.WriteLine("[Pipeline] Timeout en geocodificacion. Se continua sin coordenadas.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Pipeline] Error en geocodificacion: {ex.Message}");
                    }

                    if (geocode == null && AppConfig.LLM_ENABLED)
                    {
                        try
                        {
                            var locationHint = extractedLocation ?? extract.LugarTexto ?? whisper.Text;
                            var parts = await LlmClient.ExtractLocationPartsAsync(locationHint, _httpClient, jobToken);
                            if (parts != null)
                            {
                                Console.WriteLine($"[LLM] Parts calle={parts.Calle} numero={parts.Numero} inter={parts.Interseccion} barrio={parts.Barrio} ciudad={parts.Ciudad} poi={parts.Poi} conf={parts.Confianza}");
                            }
                            var partsText = LlmClient.ComposeLocationText(parts);
                            if (!string.IsNullOrWhiteSpace(partsText))
                            {
                                Console.WriteLine($"[LLM] Parts text: {partsText}");
                                var normalizedParts = IncidentExtractor.NormalizeLocationForGeocode(partsText) ?? partsText;
                                var llmContext = LocationNormalizer.Build(whisper.Text, normalizedParts, normalizedParts);
                                foreach (var candidate in llmContext.Candidates)
                                {
                                    geocode = await NominatimClient.GeocodeAsync(candidate, _httpClient, jobToken);
                                    if (geocode != null)
                                    {
                                        locationContext = llmContext;
                                        break;
                                    }
                                }
                                if (!string.IsNullOrWhiteSpace(llmContext.DisplayText))
                                    lugarTexto = llmContext.DisplayText!;
                            }

                            if (geocode == null)
                            {
                                var normalizedLocation = await LlmClient.NormalizeLocationAsync(whisper.Text, _httpClient, jobToken);
                                if (!string.IsNullOrWhiteSpace(normalizedLocation))
                                {
                                    var llmContext = LocationNormalizer.Build(whisper.Text, normalizedLocation, normalizedLocation);
                                    foreach (var candidate in llmContext.Candidates)
                                    {
                                        geocode = await NominatimClient.GeocodeAsync(candidate, _httpClient, jobToken);
                                        if (geocode != null)
                                        {
                                            locationContext = llmContext;
                                            break;
                                        }
                                    }
                                    if (!string.IsNullOrWhiteSpace(llmContext.DisplayText))
                                        lugarTexto = llmContext.DisplayText!;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[LLM] Error normalizando ubicacion: {ex.Message}");
                        }
                    }

                    if (geocode != null)
                    {
                        _repo.InsertarUbicacion(job.IncidenteId, geocode.Lat, geocode.Lng, "high", "nominatim", geocode.DisplayName);
                    }

                    _repo.InsertarExtraccion(
                        job.IncidenteId,
                        whisper.Text,
                        extract.JsonExtract,
                        extract.ScoresJson,
                        whisper.ModelVersion,
                        whisper.Language,
                        whisper.Confidence
                    );

                    var finalConfidence = LocationNormalizer.ScoreConfidence(extract.Confidence, locationContext, geocode);
                    var cleanedLugarTexto = IncidentExtractor.CleanLocationText(lugarTexto) ?? lugarTexto;

                    _repo.ActualizarIncidente(
                        job.IncidenteId,
                        extract.FechaHora,
                        cleanedLugarTexto ?? "Pendiente",
                        geocode?.DisplayName,
                        extract.TipoHechoId,
                        extract.Descripcion,
                        geocode?.Lat,
                        geocode?.Lng,
                        finalConfidence,
                        "procesado"
                    );

                    _repo.MarcarJobFinalizado(job.Id);
                }
                catch (Exception ex)
                {
                    var message = ex.Message;
                    if (string.IsNullOrWhiteSpace(message))
                        message = "Error inesperado en pipeline.";
                    Console.WriteLine($"[Pipeline] Error: {message}");
                    if (job != null)
                    {
                        try
                        {
                            _repo.MarcarJobError(job.Id, message);
                        }
                        catch
                        {
                            // si no se puede marcar error, evitamos romper el loop
                        }
                    }
                }
            }
        }

    }
}
