using System.Text;
using System.Text.Json;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;

namespace Backend.Utils
{
    public sealed class EngineClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly object PreferredEndpointLock = new();
        private static string? _preferredEndpoint;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public EngineClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<EngineCallResult> ExecuteRunAsync(
            int projectId,
            string runType,
            string? inputJson,
            CancellationToken cancellationToken = default)
        {
            var body = JsonSerializer.Serialize(new
            {
                projectId,
                runType,
                inputJson
            }, JsonOptions);

            return await PostJsonWithFailoverAsync("/runs/execute", body, cancellationToken);
        }

        public async Task<EngineCallResult> InferAsync(
            int projectId,
            string input,
            string? contextJson,
            CancellationToken cancellationToken = default)
        {
            var body = JsonSerializer.Serialize(new
            {
                projectId,
                input,
                contextJson
            }, JsonOptions);

            return await PostJsonWithFailoverAsync("/infer", body, cancellationToken);
        }

        public async Task<EngineCallResult> ExportDockerBundleAsync(
            int projectId,
            string? serviceName,
            string? imageTag,
            int hostPort,
            int containerPort,
            IDictionary<string, string>? extraEnv,
            CancellationToken cancellationToken = default)
        {
            var body = JsonSerializer.Serialize(new
            {
                serviceName,
                imageTag,
                hostPort,
                containerPort,
                extraEnv = extraEnv ?? new Dictionary<string, string>()
            }, JsonOptions);

            return await PostJsonWithFailoverAsync($"/projects/{projectId}/export/docker", body, cancellationToken);
        }

        public async Task<EngineCallResult> UploadDatasetFileAsync(
            int projectId,
            Func<Stream> openReadStream,
            string fileName,
            string? contentType,
            string? sourceType,
            CancellationToken cancellationToken = default)
        {
            return await SendWithFailoverAsync(
                async (client, token) =>
                {
                    using var stream = openReadStream();
                    using var content = new MultipartFormDataContent();
                    using var streamContent = new StreamContent(stream);
                    if (!string.IsNullOrWhiteSpace(contentType))
                    {
                        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                    }
                    content.Add(streamContent, "file", fileName);
                    if (!string.IsNullOrWhiteSpace(sourceType))
                    {
                        content.Add(new StringContent(sourceType), "sourceType");
                    }
                    return await client.PostAsync($"/projects/{projectId}/dataset/upload", content, token);
                },
                cancellationToken);
        }

        public async Task<EngineCallResult> ListProjectDatasetSourcesAsync(
            int projectId,
            CancellationToken cancellationToken = default)
        {
            return await GetWithFailoverAsync($"/projects/{projectId}/dataset/sources", cancellationToken);
        }

        public async Task<EngineCallResult> GenerateProjectDatasetAsync(
            int projectId,
            string payloadJson,
            CancellationToken cancellationToken = default)
        {
            return await PostJsonWithFailoverAsync($"/projects/{projectId}/dataset/generate", payloadJson, cancellationToken);
        }

        public async Task<EngineCallResult> MergeProjectDatasetsAsync(
            int projectId,
            string payloadJson,
            CancellationToken cancellationToken = default)
        {
            return await PostJsonWithFailoverAsync($"/projects/{projectId}/dataset/merge", payloadJson, cancellationToken);
        }

        public async Task<EngineCallResult> GetInferMetricsAsync(
            int projectId,
            int take,
            bool gateEnabled,
            int minSamples,
            double minSuccessRate,
            double maxFallbackRate,
            int maxAvgLatencyMs,
            CancellationToken cancellationToken = default)
        {
            var query = new Dictionary<string, string>
            {
                ["take"] = Math.Clamp(take, 1, 200).ToString(),
                ["gateEnabled"] = gateEnabled ? "true" : "false",
                ["minSamples"] = Math.Clamp(minSamples, 1, 500).ToString(),
                ["minSuccessRate"] = Math.Clamp(minSuccessRate, 0, 1).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture),
                ["maxFallbackRate"] = Math.Clamp(maxFallbackRate, 0, 1).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture),
                ["maxAvgLatencyMs"] = Math.Max(0, maxAvgLatencyMs).ToString(),
            };
            var queryString = string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            var path = $"/projects/{projectId}/metrics/infer?{queryString}";
            return await GetWithFailoverAsync(path, cancellationToken);
        }

        private async Task<EngineCallResult> PostJsonWithFailoverAsync(
            string path,
            string body,
            CancellationToken cancellationToken)
        {
            return await SendWithFailoverAsync(
                async (client, token) =>
                {
                    using var content = new StringContent(body, Encoding.UTF8, "application/json");
                    return await client.PostAsync(path, content, token);
                },
                cancellationToken);
        }

        private async Task<EngineCallResult> GetWithFailoverAsync(string path, CancellationToken cancellationToken)
        {
            return await SendWithFailoverAsync(
                (client, token) => client.GetAsync(path, token),
                cancellationToken);
        }

        private async Task<EngineCallResult> SendWithFailoverAsync(
            Func<HttpClient, CancellationToken, Task<HttpResponseMessage>> sendAsync,
            CancellationToken cancellationToken)
        {
            var candidates = BuildCandidateEndpoints();
            if (candidates.Count == 0)
            {
                return EngineCallResult.Fail("No hay endpoints de engine configurados.");
            }

            var configuredEndpoint = NormalizeEndpoint(AppConfig.AIBASE_ENGINE_URL) ?? AppConfig.AIBASE_ENGINE_URL.Trim();
            var tried = new List<string>();
            var transportErrors = new List<string>();
            var sawRetryStatus = false;
            var sawNotFound = false;

            foreach (var endpoint in candidates)
            {
                tried.Add(endpoint);
                try
                {
                    using var client = _httpClientFactory.CreateClient("aibase-engine");
                    client.BaseAddress = new Uri(endpoint, UriKind.Absolute);

                    using var response = await sendAsync(client, cancellationToken);
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        RememberPreferredEndpoint(endpoint);
                        var notice = BuildFailoverNotice(configuredEndpoint, endpoint);
                        return EngineCallResult.Success(
                            responseJson,
                            endpoint,
                            notice,
                            tried,
                            usedFailover: !string.IsNullOrWhiteSpace(notice));
                    }

                    if (ShouldRetryOnStatus(response.StatusCode))
                    {
                        sawRetryStatus = true;
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            sawNotFound = true;
                        }
                        transportErrors.Add($"{endpoint}: Engine devolvio {(int)response.StatusCode}");
                        continue;
                    }

                    return EngineCallResult.Fail(
                        $"Engine devolvio {(int)response.StatusCode} ({response.StatusCode}) en {endpoint}.",
                        responseJson,
                        endpoint,
                        tried);
                }
                catch (Exception ex) when (IsTransportException(ex, cancellationToken))
                {
                    transportErrors.Add($"{endpoint}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    return EngineCallResult.Fail(
                        $"Error llamando engine en {endpoint}: {ex.Message}",
                        null,
                        endpoint,
                        tried);
                }
            }

            string error;
            if (sawNotFound)
            {
                error = transportErrors.Count > 0
                    ? $"El engine respondió pero no soporta este endpoint (404). Probable versión antigua del engine. {string.Join(" | ", transportErrors)}"
                    : "El engine respondió 404 para este endpoint. Probable versión antigua del engine.";
            }
            else if (sawRetryStatus)
            {
                error = transportErrors.Count > 0
                    ? $"Engine no disponible para esta operación en endpoints candidatos. {string.Join(" | ", transportErrors)}"
                    : "Engine no disponible para esta operación en endpoints candidatos.";
            }
            else
            {
                error = transportErrors.Count > 0
                    ? $"No se pudo contactar engine en ningún endpoint candidato. {string.Join(" | ", transportErrors)}"
                    : "No se pudo contactar engine en ningún endpoint candidato.";
            }

            return EngineCallResult.Fail(error, null, null, tried);
        }

        private static bool IsTransportException(Exception ex, CancellationToken cancellationToken)
        {
            if (ex is HttpRequestException) return true;
            if (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested) return true;
            return false;
        }

        private static bool ShouldRetryOnStatus(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.NotFound || statusCode == HttpStatusCode.MethodNotAllowed;
        }

        private static string? BuildFailoverNotice(string configuredEndpoint, string usedEndpoint)
        {
            if (string.IsNullOrWhiteSpace(usedEndpoint)) return null;
            if (string.Equals(
                NormalizeEndpoint(configuredEndpoint),
                NormalizeEndpoint(usedEndpoint),
                StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(configuredEndpoint))
            {
                return $"Engine detectado automáticamente en {usedEndpoint}.";
            }

            return $"Engine no disponible en {configuredEndpoint}; se usó {usedEndpoint} automáticamente.";
        }

        private static List<string> BuildCandidateEndpoints()
        {
            var candidates = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddCandidate(string? raw)
            {
                var normalized = NormalizeEndpoint(raw);
                if (string.IsNullOrWhiteSpace(normalized)) return;
                if (seen.Add(normalized)) candidates.Add(normalized);
            }

            AddCandidate(GetPreferredEndpoint());
            AddCandidate(AppConfig.AIBASE_ENGINE_URL);

            foreach (var raw in SplitCandidates(AppConfig.AIBASE_ENGINE_URLS))
            {
                AddCandidate(raw);
            }

            var configured = NormalizeEndpoint(AppConfig.AIBASE_ENGINE_URL);
            AddCandidate(SwapLocalPort(configured, 8010));
            AddCandidate(SwapLocalPort(configured, 8011));

            AddCandidate("http://localhost:8010");
            AddCandidate("http://localhost:8011");
            AddCandidate("http://127.0.0.1:8010");
            AddCandidate("http://127.0.0.1:8011");

            return candidates;
        }

        private static IEnumerable<string> SplitCandidates(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) yield break;
            var parts = raw.Split(new[] { ',', ';', '\n', '\r', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                yield return part.Trim();
            }
        }

        private static string? SwapLocalPort(string? endpoint, int newPort)
        {
            if (string.IsNullOrWhiteSpace(endpoint)) return null;
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)) return null;
            var host = (uri.Host ?? "").Trim().ToLowerInvariant();
            if (host != "localhost" && host != "127.0.0.1") return null;
            if (uri.Port == newPort) return endpoint.TrimEnd('/');
            var builder = new UriBuilder(uri)
            {
                Port = newPort
            };
            return builder.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        private static string? NormalizeEndpoint(string? raw)
        {
            var value = (raw ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (!value.Contains("://", StringComparison.Ordinal))
            {
                value = $"http://{value}";
            }
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null;
            return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        private static string? GetPreferredEndpoint()
        {
            lock (PreferredEndpointLock)
            {
                return _preferredEndpoint;
            }
        }

        private static void RememberPreferredEndpoint(string endpoint)
        {
            var normalized = NormalizeEndpoint(endpoint);
            if (string.IsNullOrWhiteSpace(normalized)) return;
            lock (PreferredEndpointLock)
            {
                _preferredEndpoint = normalized;
            }
        }

        public sealed class EngineCallResult
        {
            public bool Ok { get; private init; }
            public string? ResponseJson { get; private init; }
            public string? Error { get; private init; }
            public string? Endpoint { get; private init; }
            public bool UsedFailover { get; private init; }
            public string? Notice { get; private init; }
            public IReadOnlyList<string> TriedEndpoints { get; private init; } = Array.Empty<string>();

            public static EngineCallResult Success(
                string? responseJson,
                string? endpoint,
                string? notice,
                IReadOnlyList<string> triedEndpoints,
                bool usedFailover)
            {
                return new EngineCallResult
                {
                    Ok = true,
                    ResponseJson = responseJson,
                    Endpoint = endpoint,
                    Notice = notice,
                    UsedFailover = usedFailover,
                    TriedEndpoints = triedEndpoints
                };
            }

            public static EngineCallResult Fail(
                string error,
                string? responseJson = null,
                string? endpoint = null,
                IReadOnlyList<string>? triedEndpoints = null)
            {
                return new EngineCallResult
                {
                    Ok = false,
                    Error = error,
                    ResponseJson = responseJson,
                    Endpoint = endpoint,
                    TriedEndpoints = triedEndpoints ?? Array.Empty<string>()
                };
            }
        }
    }
}
