using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Backend.Utils
{
    public static class AibaseEngineClient
    {
        private static readonly HttpClient Http = new();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static async Task<AibaseEngineStartResult> StartRunAsync(
            string runType,
            int projectId,
            int runId,
            string templateKey,
            string? inputJson,
            ILogger logger)
        {
            if (!AppConfig.AIBASE_ENGINE_ENABLED)
            {
                return new AibaseEngineStartResult
                {
                    Ok = true,
                    Status = "completed",
                    ProgressPct = 100,
                    OutputJson = "{\"mode\":\"stub\",\"message\":\"AIBASE_ENGINE_ENABLED=false\"}"
                };
            }

            var endpoint = MapEndpoint(runType);
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return new AibaseEngineStartResult
                {
                    Ok = false,
                    Error = $"Tipo de run no soportado: {runType}"
                };
            }

            var requestBody = new
            {
                projectId,
                runId,
                runType,
                templateKey,
                inputJson
            };

            try
            {
                Http.Timeout = TimeSpan.FromSeconds(AppConfig.AIBASE_ENGINE_TIMEOUT_SECONDS);

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody, JsonOptions),
                    Encoding.UTF8,
                    "application/json");

                var response = await Http.PostAsync($"{AppConfig.AIBASE_ENGINE_BASE_URL}{endpoint}", content);
                var raw = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new AibaseEngineStartResult
                    {
                        Ok = false,
                        Error = $"Engine devolvio {(int)response.StatusCode}: {raw}"
                    };
                }

                string? engineRunId = null;
                string status = "running";
                int progress = 0;

                if (!string.IsNullOrWhiteSpace(raw))
                {
                    using var doc = JsonDocument.Parse(raw);
                    var root = doc.RootElement;
                    engineRunId = GetString(root, "runId", "engineRunId", "id");
                    status = GetString(root, "status") ?? status;
                    progress = GetInt(root, "progressPct", "progress") ?? progress;
                }

                return new AibaseEngineStartResult
                {
                    Ok = true,
                    EngineRunId = engineRunId,
                    Status = NormalizeStatus(status),
                    ProgressPct = Math.Clamp(progress, 0, 100),
                    OutputJson = raw
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AibaseEngineClient.StartRunAsync: fallo al iniciar run en engine.");
                return new AibaseEngineStartResult
                {
                    Ok = false,
                    Error = ex.Message
                };
            }
        }

        public static async Task<AibaseEngineSyncResult> SyncRunAsync(string engineRunId, ILogger logger)
        {
            if (!AppConfig.AIBASE_ENGINE_ENABLED)
            {
                return new AibaseEngineSyncResult
                {
                    Ok = true,
                    Status = "completed",
                    ProgressPct = 100,
                    OutputJson = "{\"mode\":\"stub\",\"message\":\"AIBASE_ENGINE_ENABLED=false\"}"
                };
            }

            try
            {
                Http.Timeout = TimeSpan.FromSeconds(AppConfig.AIBASE_ENGINE_TIMEOUT_SECONDS);
                var response = await Http.GetAsync($"{AppConfig.AIBASE_ENGINE_BASE_URL}/runs/{Uri.EscapeDataString(engineRunId)}");
                var raw = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new AibaseEngineSyncResult
                    {
                        Ok = false,
                        Error = $"Engine devolvio {(int)response.StatusCode}: {raw}"
                    };
                }

                var status = "running";
                var progress = 0;

                if (!string.IsNullOrWhiteSpace(raw))
                {
                    using var doc = JsonDocument.Parse(raw);
                    var root = doc.RootElement;
                    status = GetString(root, "status") ?? status;
                    progress = GetInt(root, "progressPct", "progress") ?? progress;
                }

                return new AibaseEngineSyncResult
                {
                    Ok = true,
                    Status = NormalizeStatus(status),
                    ProgressPct = Math.Clamp(progress, 0, 100),
                    OutputJson = raw
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AibaseEngineClient.SyncRunAsync: fallo al sincronizar run en engine.");
                return new AibaseEngineSyncResult
                {
                    Ok = false,
                    Error = ex.Message
                };
            }
        }

        private static string MapEndpoint(string runType)
        {
            var key = (runType ?? string.Empty).Trim().ToLowerInvariant();
            return key switch
            {
                "dataset_build" => "/dataset/build",
                "rag_index" => "/rag/index",
                "train_lora" => "/train/lora",
                "eval_run" => "/eval/run",
                "infer" => "/infer",
                _ => string.Empty
            };
        }

        private static string NormalizeStatus(string status)
        {
            var value = (status ?? string.Empty).Trim().ToLowerInvariant();
            return value switch
            {
                "queued" => "queued",
                "running" => "running",
                "completed" => "completed",
                "failed" => "failed",
                "canceled" => "canceled",
                _ => "running"
            };
        }

        private static string? GetString(JsonElement element, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                    return prop.GetString();
            }

            return null;
        }

        private static int? GetInt(JsonElement element, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!element.TryGetProperty(key, out var prop))
                    continue;

                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var n))
                    return n;

                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var parsed))
                    return parsed;
            }

            return null;
        }
    }

    public class AibaseEngineStartResult
    {
        public bool Ok { get; set; }
        public string? EngineRunId { get; set; }
        public string Status { get; set; } = "running";
        public int ProgressPct { get; set; }
        public string? OutputJson { get; set; }
        public string? Error { get; set; }
    }

    public class AibaseEngineSyncResult
    {
        public bool Ok { get; set; }
        public string Status { get; set; } = "running";
        public int ProgressPct { get; set; }
        public string? OutputJson { get; set; }
        public string? Error { get; set; }
    }
}
