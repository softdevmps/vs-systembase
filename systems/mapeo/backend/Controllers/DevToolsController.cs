using Backend.Negocio.Pipeline;
using Backend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Globalization;
using System.Text;

namespace Backend.Controllers
{
    public sealed class EvalCaseRequest
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
        public string? ExpectedTipoCodigo { get; set; }
        public string? ExpectedLugarContains { get; set; }
        public string? ExpectedFecha { get; set; }
        public string? ExpectedHora { get; set; }
        public decimal? ExpectedLat { get; set; }
        public decimal? ExpectedLng { get; set; }
    }

    public sealed class EvalBatchRequest
    {
        public decimal DistanceToleranceMeters { get; set; } = 250m;
        public List<EvalCaseRequest> Cases { get; set; } = new();
    }

    internal sealed class EvalCaseResult
    {
        public string Id { get; init; } = "";
        public string Text { get; init; } = "";
        public string? PredTipoCodigo { get; init; }
        public string? PredLugarTexto { get; init; }
        public string? PredLugarNormalizado { get; init; }
        public string? PredFecha { get; init; }
        public string? PredHora { get; init; }
        public decimal? PredLat { get; init; }
        public decimal? PredLng { get; init; }
        public decimal? PredConfidence { get; init; }
        public bool LlmEnabled { get; init; }
        public bool LlmExtractUsed { get; init; }
        public bool LlmLocationPartsUsed { get; init; }
        public bool LlmLocationNormalizeUsed { get; init; }
        public bool? TipoOk { get; init; }
        public bool? LugarOk { get; init; }
        public bool? FechaOk { get; init; }
        public bool? HoraOk { get; init; }
        public bool? CoordsOk { get; init; }
        public decimal? DistanceMeters { get; init; }
        public int DurationMs { get; init; }
    }

    [ApiController]
    public class DevToolsController : AppController
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IWebHostEnvironment _env;
        private readonly IncidentePipelineRepository _repo = new();
        private readonly HttpClient _httpClient = new();

        public DevToolsController(IHostApplicationLifetime lifetime, IWebHostEnvironment env)
        {
            _lifetime = lifetime;
            _env = env;
        }

        [HttpPost(Routes.v1.DevTools.Restart)]
        [AllowAnonymous]
        public IActionResult Restart()
        {
            if (!_env.IsDevelopment())
                return Forbid();

            var remote = HttpContext.Connection.RemoteIpAddress;
            if (remote == null || !IPAddress.IsLoopback(remote))
                return Forbid();

            TouchRestartSignal();
            _ = Task.Run(() => _lifetime.StopApplication());

            return Ok(new { message = "Reiniciando backend..." });
        }

        [HttpGet(Routes.v1.DevTools.Ping)]
        [AllowAnonymous]
        public IActionResult Ping()
        {
            if (!_env.IsDevelopment())
                return Forbid();

            return Ok(new { status = "ok" });
        }

        [HttpPost(Routes.v1.DevTools.EvalIncident)]
        [AllowAnonymous]
        public async Task<IActionResult> EvalIncident([FromBody] EvalCaseRequest request, CancellationToken ct)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            if (!IsLocalRequest())
                return Forbid();

            if (request == null || string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Text es requerido.");

            var tolerance = 250m;
            var catalogo = _repo.ObtenerCatalogoHechos();
            var result = await EvaluateCaseAsync(request, catalogo, tolerance, ct);
            return Ok(result);
        }

        [HttpPost(Routes.v1.DevTools.EvalBatch)]
        [AllowAnonymous]
        public async Task<IActionResult> EvalBatch([FromBody] EvalBatchRequest request, CancellationToken ct)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            if (!IsLocalRequest())
                return Forbid();

            if (request?.Cases == null || request.Cases.Count == 0)
                return BadRequest("Cases es requerido y no puede estar vacio.");

            if (request.Cases.Count > 500)
                return BadRequest("Maximo permitido: 500 casos por corrida.");

            var tolerance = request.DistanceToleranceMeters <= 0 ? 250m : request.DistanceToleranceMeters;
            var catalogo = _repo.ObtenerCatalogoHechos();
            var results = new List<EvalCaseResult>(request.Cases.Count);

            foreach (var item in request.Cases)
            {
                if (string.IsNullOrWhiteSpace(item.Text))
                    continue;
                results.Add(await EvaluateCaseAsync(item, catalogo, tolerance, ct));
            }

            if (results.Count == 0)
                return BadRequest("No hay casos validos para evaluar.");

            var metrics = BuildMetrics(results, tolerance);
            return Ok(new
            {
                llm = new
                {
                    enabled = AppConfig.LLM_ENABLED,
                    model = AppConfig.LLM_MODEL,
                    mode = AppConfig.LLM_MODE,
                    jsonSchema = AppConfig.LLM_JSON_SCHEMA_ENABLED
                },
                geocoder = new
                {
                    url = AppConfig.GEOCODER_URL,
                    defaultSuffix = AppConfig.GEOCODER_DEFAULT_SUFFIX,
                    countryCodes = AppConfig.GEOCODER_COUNTRY_CODES
                },
                metrics,
                cases = results
            });
        }

        [HttpGet(Routes.v1.DevTools.EvalDatasetAuto)]
        [AllowAnonymous]
        public IActionResult EvalDatasetAuto(
            [FromQuery] int take = 200,
            [FromQuery] decimal distanceToleranceMeters = 250m,
            [FromQuery] bool includeWithoutCoords = false)
        {
            if (!_env.IsDevelopment())
                return Forbid();

            if (!IsLocalRequest())
                return Forbid();

            if (take <= 0)
                take = 200;
            if (take > 1000)
                take = 1000;

            var tolerance = distanceToleranceMeters <= 0 ? 250m : distanceToleranceMeters;
            var catalogoById = _repo.ObtenerCatalogoHechos().ToDictionary(x => x.Id, x => x.Codigo);
            var source = _repo.ObtenerDatasetAuto(take);

            var cases = new List<EvalCaseRequest>(source.Count);
            foreach (var item in source)
            {
                var text = BuildSeedText(item);
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var hasCoords = item.Lat.HasValue && item.Lng.HasValue;
                if (!includeWithoutCoords && !hasCoords)
                    continue;

                string? tipoCodigo = null;
                if (item.TipoHechoId.HasValue && catalogoById.TryGetValue(item.TipoHechoId.Value, out var value))
                    tipoCodigo = value;

                cases.Add(new EvalCaseRequest
                {
                    Id = $"INC-{item.IncidenteId}",
                    Text = text,
                    ExpectedTipoCodigo = tipoCodigo,
                    ExpectedLugarContains = !string.IsNullOrWhiteSpace(item.LugarNormalizado)
                        ? item.LugarNormalizado
                        : item.LugarTexto,
                    ExpectedFecha = item.FechaHora?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ExpectedHora = item.FechaHora?.ToString("HH:mm", CultureInfo.InvariantCulture),
                    ExpectedLat = item.Lat,
                    ExpectedLng = item.Lng
                });
            }

            return Ok(new EvalBatchRequest
            {
                DistanceToleranceMeters = tolerance,
                Cases = cases
            });
        }

        private async Task<EvalCaseResult> EvaluateCaseAsync(
            EvalCaseRequest item,
            List<CatalogoHechoItem> catalogo,
            decimal toleranceMeters,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var text = item.Text!.Trim();

            var extract = IncidentExtractor.Extract(text, catalogo);
            var llmExtractUsed = false;
            var llmPartsUsed = false;
            var llmNormalizeUsed = false;

            if (AppConfig.LLM_ENABLED)
            {
                try
                {
                    var llmExtract = await LlmClient.ExtractIncidentAsync(text, catalogo, _httpClient, ct);
                    if (llmExtract != null)
                    {
                        extract = IncidentExtractor.MergeWithLlm(extract, llmExtract, catalogo);
                        llmExtractUsed = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Eval] LLM extract error: {ex.Message}");
                }
            }

            GeocodeResult? geocode = null;
            var extractedLocation = IncidentExtractor.NormalizeLocationForGeocode(extract.LugarTexto) ?? extract.LugarTexto;
            var locationContext = LocationNormalizer.Build(text, extractedLocation, null);
            var lugarTexto = locationContext.DisplayText ?? extractedLocation ?? "Pendiente";

            try
            {
                foreach (var candidate in locationContext.Candidates)
                {
                    geocode = await NominatimClient.GeocodeAsync(candidate, _httpClient, ct);
                    if (geocode != null)
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Eval] Geocoder error: {ex.Message}");
            }

            if (geocode == null && AppConfig.LLM_ENABLED)
            {
                try
                {
                    var locationHint = extractedLocation ?? text;
                    var parts = await LlmClient.ExtractLocationPartsAsync(locationHint, _httpClient, ct);
                    var partsText = LlmClient.ComposeLocationText(parts);
                    if (!string.IsNullOrWhiteSpace(partsText))
                    {
                        llmPartsUsed = true;
                        var normalizedParts = IncidentExtractor.NormalizeLocationForGeocode(partsText) ?? partsText;
                        var llmContext = LocationNormalizer.Build(text, normalizedParts, normalizedParts);
                        foreach (var candidate in llmContext.Candidates)
                        {
                            geocode = await NominatimClient.GeocodeAsync(candidate, _httpClient, ct);
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
                        var normalizedLocation = await LlmClient.NormalizeLocationAsync(text, _httpClient, ct);
                        if (!string.IsNullOrWhiteSpace(normalizedLocation))
                        {
                            llmNormalizeUsed = true;
                            var llmContext = LocationNormalizer.Build(text, normalizedLocation, normalizedLocation);
                            foreach (var candidate in llmContext.Candidates)
                            {
                                geocode = await NominatimClient.GeocodeAsync(candidate, _httpClient, ct);
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
                    Console.WriteLine($"[Eval] LLM location error: {ex.Message}");
                }
            }

            var finalConfidence = LocationNormalizer.ScoreConfidence(extract.Confidence, locationContext, geocode);
            var cleanedLugarTexto = IncidentExtractor.CleanLocationText(lugarTexto) ?? lugarTexto;
            var predTipo = extract.MatchedCodigo ?? catalogo.FirstOrDefault(c => c.Id == extract.TipoHechoId)?.Codigo;

            var predFecha = extract.FechaHora?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var predHora = extract.FechaHora?.ToString("HH:mm", CultureInfo.InvariantCulture);

            var tipoOk = CompareEquals(item.ExpectedTipoCodigo, predTipo);
            var fechaOk = CompareEquals(item.ExpectedFecha, predFecha);
            var horaOk = CompareEquals(item.ExpectedHora, predHora);
            var lugarOk = CompareContains(item.ExpectedLugarContains, geocode?.DisplayName ?? cleanedLugarTexto);

            decimal? distanceMeters = null;
            bool? coordsOk = null;
            if (item.ExpectedLat.HasValue && item.ExpectedLng.HasValue)
            {
                if (geocode != null)
                {
                    distanceMeters = HaversineMeters(item.ExpectedLat.Value, item.ExpectedLng.Value, geocode.Lat, geocode.Lng);
                    coordsOk = distanceMeters <= toleranceMeters;
                }
                else
                {
                    coordsOk = false;
                }
            }

            sw.Stop();
            return new EvalCaseResult
            {
                Id = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("N")[..8] : item.Id.Trim(),
                Text = text,
                PredTipoCodigo = predTipo,
                PredLugarTexto = cleanedLugarTexto,
                PredLugarNormalizado = geocode?.DisplayName,
                PredFecha = predFecha,
                PredHora = predHora,
                PredLat = geocode?.Lat,
                PredLng = geocode?.Lng,
                PredConfidence = finalConfidence,
                LlmEnabled = AppConfig.LLM_ENABLED,
                LlmExtractUsed = llmExtractUsed,
                LlmLocationPartsUsed = llmPartsUsed,
                LlmLocationNormalizeUsed = llmNormalizeUsed,
                TipoOk = tipoOk,
                LugarOk = lugarOk,
                FechaOk = fechaOk,
                HoraOk = horaOk,
                CoordsOk = coordsOk,
                DistanceMeters = distanceMeters,
                DurationMs = (int)sw.ElapsedMilliseconds
            };
        }

        private static object BuildMetrics(List<EvalCaseResult> results, decimal toleranceMeters)
        {
            var typeChecked = results.Count(r => r.TipoOk.HasValue);
            var typeHit = results.Count(r => r.TipoOk == true);

            var lugarChecked = results.Count(r => r.LugarOk.HasValue);
            var lugarHit = results.Count(r => r.LugarOk == true);

            var fechaChecked = results.Count(r => r.FechaOk.HasValue);
            var fechaHit = results.Count(r => r.FechaOk == true);

            var horaChecked = results.Count(r => r.HoraOk.HasValue);
            var horaHit = results.Count(r => r.HoraOk == true);

            var coordsChecked = results.Count(r => r.CoordsOk.HasValue);
            var coordsHit = results.Count(r => r.CoordsOk == true);
            var withCoords = results.Count(r => r.PredLat.HasValue && r.PredLng.HasValue);

            var distances = results.Where(r => r.DistanceMeters.HasValue).Select(r => r.DistanceMeters!.Value).OrderBy(v => v).ToList();
            var avgDistance = distances.Count == 0 ? (decimal?)null : Math.Round(distances.Average(), 2);
            decimal? p50Distance = null;
            if (distances.Count > 0)
            {
                var mid = distances.Count / 2;
                p50Distance = distances.Count % 2 == 0
                    ? Math.Round((distances[mid - 1] + distances[mid]) / 2m, 2)
                    : Math.Round(distances[mid], 2);
            }

            var avgConfidence = results.Count == 0
                ? (decimal?)null
                : Math.Round(results.Where(r => r.PredConfidence.HasValue).DefaultIfEmpty()
                    .Average(r => r?.PredConfidence ?? 0m), 3);

            var avgDuration = results.Count == 0
                ? 0
                : (int)Math.Round(results.Average(r => r.DurationMs));

            return new
            {
                total = results.Count,
                predictionsWithCoords = withCoords,
                avgConfidence,
                avgDurationMs = avgDuration,
                toleranceMeters,
                type = BuildMetric(typeChecked, typeHit),
                location = BuildMetric(lugarChecked, lugarHit),
                date = BuildMetric(fechaChecked, fechaHit),
                hour = BuildMetric(horaChecked, horaHit),
                coords = BuildMetric(coordsChecked, coordsHit),
                geocodeDistanceMeters = new
                {
                    avg = avgDistance,
                    p50 = p50Distance
                }
            };
        }

        private static object BuildMetric(int checkedCount, int hitCount)
        {
            var accuracy = checkedCount == 0
                ? (decimal?)null
                : Math.Round((decimal)hitCount / checkedCount * 100m, 2);

            return new
            {
                checkedCount,
                hitCount,
                accuracyPct = accuracy
            };
        }

        private static bool? CompareEquals(string? expected, string? predicted)
        {
            if (string.IsNullOrWhiteSpace(expected))
                return null;
            return Normalize(expected) == Normalize(predicted);
        }

        private static bool? CompareContains(string? expected, string? predicted)
        {
            if (string.IsNullOrWhiteSpace(expected))
                return null;
            var expectedNormalized = Normalize(expected);
            if (string.IsNullOrWhiteSpace(expectedNormalized))
                return null;

            var predictedNormalized = Normalize(predicted);
            return !string.IsNullOrWhiteSpace(predictedNormalized) && predictedNormalized.Contains(expectedNormalized);
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            var lower = value.ToLowerInvariant();
            var normalized = lower.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            var noAccents = sb.ToString().Normalize(NormalizationForm.FormC);
            return string.Join(" ", noAccents.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static decimal HaversineMeters(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const double earthRadius = 6371000d;
            var dLat = ToRadians((double)(lat2 - lat1));
            var dLon = ToRadians((double)(lon2 - lon1));

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                    + Math.Cos(ToRadians((double)lat1))
                    * Math.Cos(ToRadians((double)lat2))
                    * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return Math.Round((decimal)(earthRadius * c), 2);
        }

        private static double ToRadians(double degree) => degree * Math.PI / 180d;

        private static string? BuildSeedText(EvalDatasetSeedItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.RawText))
                return item.RawText.Trim();

            var fragments = new List<string>();
            if (item.FechaHora.HasValue)
                fragments.Add($"Fecha {item.FechaHora.Value:yyyy-MM-dd HH:mm}");
            if (!string.IsNullOrWhiteSpace(item.LugarTexto))
                fragments.Add($"Lugar {item.LugarTexto}");
            if (!string.IsNullOrWhiteSpace(item.Descripcion))
                fragments.Add($"Descripcion {item.Descripcion}");

            if (fragments.Count == 0)
                return null;

            return string.Join(". ", fragments) + ".";
        }

        private bool IsLocalRequest()
        {
            var remote = HttpContext.Connection.RemoteIpAddress;
            return remote != null && IPAddress.IsLoopback(remote);
        }

        private void TouchRestartSignal()
        {
            try
            {
                var path = Path.Combine(_env.ContentRootPath, ".restart");
                System.IO.File.WriteAllText(path, DateTime.UtcNow.ToString("O"));
            }
            catch
            {
                // no-op
            }
        }
    }
}
