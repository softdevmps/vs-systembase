using System.Text.Json;
using System.Linq;
using System.Text.RegularExpressions;
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
                    if (!HasStrongLocation(locationContext))
                    {
                        var rawContext = LocationNormalizer.Build(whisper.Text, whisper.Text, null);
                        if (HasStrongLocation(rawContext))
                            locationContext = rawContext;
                    }
                    if (!HasStrongLocation(locationContext))
                    {
                        var forcedIntersection = TryExtractIntersectionHint(whisper.Text)
                            ?? TryExtractIntersectionHint(extract.Descripcion);
                        if (!string.IsNullOrWhiteSpace(forcedIntersection))
                        {
                            var forcedContext = LocationNormalizer.Build(whisper.Text, forcedIntersection, forcedIntersection);
                            if (HasStrongLocation(forcedContext))
                                locationContext = forcedContext;
                        }
                    }
                    var lugarTexto = locationContext.DisplayText ?? extractedLocation ?? "Pendiente";

                    try
                    {
                        foreach (var candidate in locationContext.Candidates)
                        {
                            var requiredTokens = BuildRequiredLocationTokens(locationContext, candidate);
                            geocode = requiredTokens.Count > 0
                                ? await NominatimClient.GeocodeWithRequiredTokensAsync(candidate, requiredTokens, _httpClient, jobToken)
                                : await NominatimClient.GeocodeAsync(candidate, _httpClient, jobToken);
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
                                    var requiredTokens = BuildRequiredLocationTokens(llmContext, candidate);
                                    geocode = requiredTokens.Count > 0
                                        ? await NominatimClient.GeocodeWithRequiredTokensAsync(candidate, requiredTokens, _httpClient, jobToken)
                                        : await NominatimClient.GeocodeAsync(candidate, _httpClient, jobToken);
                                    if (geocode != null)
                                    {
                                        locationContext = llmContext;
                                        break;
                                    }
                                }
                                if (!string.IsNullOrWhiteSpace(llmContext.DisplayText) &&
                                    ShouldReplaceLugarTexto(locationContext, llmContext, geocode))
                                {
                                    lugarTexto = llmContext.DisplayText!;
                                }
                            }

                            if (geocode == null)
                            {
                                var normalizedLocation = await LlmClient.NormalizeLocationAsync(whisper.Text, _httpClient, jobToken);
                                if (!string.IsNullOrWhiteSpace(normalizedLocation))
                                {
                                    var llmContext = LocationNormalizer.Build(whisper.Text, normalizedLocation, normalizedLocation);
                                    foreach (var candidate in llmContext.Candidates)
                                    {
                                        var requiredTokens = BuildRequiredLocationTokens(llmContext, candidate);
                                        geocode = requiredTokens.Count > 0
                                            ? await NominatimClient.GeocodeWithRequiredTokensAsync(candidate, requiredTokens, _httpClient, jobToken)
                                            : await NominatimClient.GeocodeAsync(candidate, _httpClient, jobToken);
                                        if (geocode != null)
                                        {
                                            locationContext = llmContext;
                                            break;
                                        }
                                    }
                                    if (!string.IsNullOrWhiteSpace(llmContext.DisplayText) &&
                                        ShouldReplaceLugarTexto(locationContext, llmContext, geocode))
                                    {
                                        lugarTexto = llmContext.DisplayText!;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[LLM] Error normalizando ubicacion: {ex.Message}");
                        }
                    }

                    if (geocode == null)
                    {
                        var candidatesPreview = string.Join(" | ", locationContext.Candidates.Take(6));
                        Console.WriteLine($"[Geocode] Sin resultado incidente={job.IncidenteId} lugar='{lugarTexto}' candidatos='{candidatesPreview}'");
                    }
                    else if (!IsReliableGeocode(locationContext, geocode))
                    {
                        Console.WriteLine($"[Geocode] Resultado descartado por baja coherencia incidente={job.IncidenteId} lugar='{lugarTexto}' display='{geocode.DisplayName}'");
                        geocode = null;
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

                    if (geocode == null)
                    {
                        LocationLearningService.RecordPredictionFailure(
                            job.IncidenteId,
                            whisper.Text,
                            extract.LugarTexto,
                            cleanedLugarTexto,
                            null,
                            null,
                            null
                        );
                    }
                    else
                    {
                        LocationLearningService.ResolvePendingFeedback(
                            job.IncidenteId,
                            cleanedLugarTexto,
                            geocode.DisplayName,
                            geocode.Lat,
                            geocode.Lng
                        );
                        LocationLearningService.LearnFromSuccessfulGeocode(cleanedLugarTexto, geocode.DisplayName);
                    }

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

        private static IReadOnlyList<string> BuildRequiredLocationTokens(LocationParseResult context, string candidate)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddTokens(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                var normalized = AppConfig.NormalizeKey(value);
                if (string.IsNullOrWhiteSpace(normalized))
                    return;

                foreach (var token in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (token.Length < 4)
                        continue;
                    if (token == "barrio" || token == "cordoba" || token == "argentina")
                        continue;
                    set.Add(token);
                }
            }

            AddTokens(context.Signals.Barrio);
            AddTokens(context.Signals.AddressWithNumber);
            AddTokens(context.Signals.Calle1Core);
            AddTokens(context.Signals.Calle2Core);

            var number = ExtractFirstNumber(candidate);
            if (!string.IsNullOrWhiteSpace(number))
                set.Add(number);

            return set.ToList();
        }

        private static string? ExtractFirstNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var match = System.Text.RegularExpressions.Regex.Match(value, @"\d{1,5}");
            return match.Success ? match.Value : null;
        }

        private static bool IsReliableGeocode(LocationParseResult context, GeocodeResult geocode)
        {
            var display = AppConfig.NormalizeKey(geocode.DisplayName ?? "");
            if (string.IsNullOrWhiteSpace(display))
                return false;

            var requiredStreetTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void AddStreetTokens(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;
                foreach (var token in AppConfig.NormalizeKey(value).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (token.Length < 4)
                        continue;
                    if (token == "avenida" || token == "boulevard" || token == "bulevar" || token == "calle" ||
                        token == "pasaje" || token == "ruta" || token == "diagonal" || token == "barrio" ||
                        token == "cordoba" || token == "argentina")
                        continue;
                    if (Regex.IsMatch(token, @"\d", RegexOptions.CultureInvariant))
                        continue;
                    requiredStreetTokens.Add(token);
                }
            }

            AddStreetTokens(context.Signals.AddressWithNumber);
            AddStreetTokens(context.Signals.Calle1Core);
            AddStreetTokens(context.Signals.Calle2Core);
            if (requiredStreetTokens.Count == 0)
                AddStreetTokens(context.DisplayText);

            if (requiredStreetTokens.Count > 0)
            {
                var hits = requiredStreetTokens.Count(token => display.Contains(token));
                if (hits == 0)
                    return false;
            }

            var expectedNumber = ExtractFirstNumber(context.Signals.AddressWithNumber ?? context.DisplayText);
            var candidateNumber = ExtractFirstStreetNumber(geocode.DisplayName);
            if (!string.IsNullOrWhiteSpace(expectedNumber) &&
                !string.IsNullOrWhiteSpace(candidateNumber) &&
                int.TryParse(expectedNumber, out var exp) &&
                int.TryParse(candidateNumber, out var got))
            {
                if (Math.Abs(exp - got) > 450)
                    return false;
            }

            return true;
        }

        private static string? ExtractFirstStreetNumber(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            // Tomamos solo el primer tramo (antes de la primera coma):
            // evita confundir codigo postal (X5000) con altura de calle.
            var firstSegment = displayName.Split(',', 2, StringSplitOptions.TrimEntries)[0];
            return ExtractFirstNumber(firstSegment);
        }

        private static bool HasStrongLocation(LocationParseResult context)
        {
            return !string.IsNullOrWhiteSpace(context.Signals.AddressWithNumber) || context.Signals.HasIntersection;
        }

        private static string? TryExtractIntersectionHint(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var normalized = AppConfig.NormalizeKey(text);
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            var inter = Regex.Match(
                normalized,
                @"\b(?:la\s+)?esquina\s+de\s+(?<s1>(?:avenida|boulevard|bulevar|calle)\s+[a-z0-9\s]{2,60}?)\s+(?:y|e|con|&)\s+(?<s2>[a-z0-9\s]{2,60}?)(?=\s*(?:,|\bbarrio\b|\bfrente\b|\bhubo\b|\brobo\b|\barrebato\b|\bhurto\b|\bhuy|\ba\s+las\b|$))",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (!inter.Success)
                return null;

            var s1 = inter.Groups["s1"].Value.Trim();
            var s2 = inter.Groups["s2"].Value.Trim();
            if (string.IsNullOrWhiteSpace(s1) || string.IsNullOrWhiteSpace(s2))
                return null;

            var barrioMatch = Regex.Match(
                normalized,
                @"\bbarrio\s+(?<b>[a-z0-9\s]{3,40})(?=\s*(?:,|\bfrente\b|\bhubo\b|\brobo\b|\barrebato\b|\bhurto\b|\bhuy|\ba\s+las\b|$))",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            var barrio = barrioMatch.Success ? barrioMatch.Groups["b"].Value.Trim() : null;

            return string.IsNullOrWhiteSpace(barrio)
                ? $"{s1} y {s2}"
                : $"{s1} y {s2}, barrio {barrio}";
        }

        private static bool ShouldReplaceLugarTexto(
            LocationParseResult current,
            LocationParseResult candidate,
            GeocodeResult? candidateGeocode)
        {
            // Si el candidato logro geocode valido, usamos su display.
            if (candidateGeocode != null)
                return true;

            // Si ya tenemos calle+altura o interseccion, no degradamos a POI.
            var currentStrong = !string.IsNullOrWhiteSpace(current.Signals.AddressWithNumber) || current.Signals.HasIntersection;
            var candidateStrong = !string.IsNullOrWhiteSpace(candidate.Signals.AddressWithNumber) || candidate.Signals.HasIntersection;
            if (currentStrong && !candidateStrong)
                return false;

            // Si el actual no tiene ubicacion fuerte y el candidato si, mejoramos.
            if (!currentStrong && candidateStrong)
                return true;

            if (string.IsNullOrWhiteSpace(current.DisplayText))
                return true;

            return false;
        }

    }
}
