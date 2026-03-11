using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Backend.Utils
{
    public sealed class ModelServiceSettings
    {
        public bool Enabled { get; set; } = true;
        public string Provider { get; set; } = "engine";
        public string BaseUrl { get; set; } = "";
        public string Path { get; set; } = "";
        public string Model { get; set; } = "";
        public string? ApiKey { get; set; }
        public string? ApiKeyEnv { get; set; }
        public string? SystemPrompt { get; set; }
        public string? Task { get; set; }
        public bool LocalFilesOnly { get; set; } = true;
        public double Temperature { get; set; } = 0.2;
        public int MaxTokens { get; set; } = 512;
        public double TopP { get; set; } = 0.95;
        public double RepetitionPenalty { get; set; } = 1.05;
        public bool QualityGateEnabled { get; set; } = true;
        public int QualityGateMinSamples { get; set; } = 3;
        public double QualityGateMinSuccessRate { get; set; } = 0.6;
        public double QualityGateMaxFallbackRate { get; set; } = 0.4;
        public int QualityGateMaxAvgLatencyMs { get; set; } = 25000;
    }

    public sealed class ModelInferResult
    {
        public bool Ok { get; set; }
        public string Provider { get; set; } = "";
        public string Model { get; set; } = "";
        public string Endpoint { get; set; } = "";
        public string? Output { get; set; }
        public string? OutputJson { get; set; }
        public string? Error { get; set; }
    }

    public sealed class ModelServiceClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ModelServiceClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ModelInferResult> InferAsync(
            ModelServiceSettings settings,
            string input,
            string? contextJson,
            CancellationToken cancellationToken = default)
        {
            var provider = NormalizeProvider(settings.Provider);
            if (provider == "ollama")
                return await InferWithOllamaAsync(settings, input, contextJson, cancellationToken);
            if (provider == "openai")
                return await InferWithOpenAiCompatAsync(settings, input, contextJson, cancellationToken);

            return new ModelInferResult
            {
                Ok = false,
                Provider = provider,
                Model = settings.Model,
                Endpoint = "",
                Error = $"Provider no soportado: {settings.Provider}"
            };
        }

        private async Task<ModelInferResult> InferWithOllamaAsync(
            ModelServiceSettings settings,
            string input,
            string? contextJson,
            CancellationToken cancellationToken)
        {
            var endpoints = BuildEndpointCandidates(settings.BaseUrl, settings.Path, "/api/chat", "ollama");
            if (endpoints.Count == 0)
            {
                return new ModelInferResult
                {
                    Ok = false,
                    Provider = "ollama",
                    Model = settings.Model,
                    Endpoint = "",
                    Error = "BaseUrl inválida para provider ollama."
                };
            }

            var client = _httpClientFactory.CreateClient("aibase-model");
            var messages = BuildMessages(settings, input, contextJson);
            var modelName = string.IsNullOrWhiteSpace(settings.Model) ? "llama3.2:3b" : settings.Model.Trim();
            var optionsPayload = new
            {
                temperature = settings.Temperature,
                num_predict = Math.Clamp(settings.MaxTokens, 64, 4096),
                top_p = Math.Clamp(settings.TopP, 0.05, 1.0),
                repeat_penalty = Math.Clamp(settings.RepetitionPenalty, 0.8, 2.5)
            };

            var errors = new List<string>();
            foreach (var endpoint in endpoints)
            {
                try
                {
                    var useGenerateApi = endpoint.Contains("/api/generate", StringComparison.OrdinalIgnoreCase);
                    object payload;
                    if (useGenerateApi)
                    {
                        var systemPrompt = messages.FirstOrDefault(m => m.Role == "system").Content ?? "";
                        payload = new
                        {
                            model = modelName,
                            stream = false,
                            system = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt,
                            prompt = BuildGeneratePrompt(messages),
                            options = optionsPayload
                        };
                    }
                    else
                    {
                        payload = new
                        {
                            model = modelName,
                            stream = false,
                            messages = messages.Select(x => new { role = x.Role, content = x.Content }).ToArray(),
                            options = optionsPayload
                        };
                    }

                    using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                    request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

                    using var response = await client.SendAsync(request, cancellationToken);
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        errors.Add($"{endpoint} => HTTP {(int)response.StatusCode}: {TrimResponse(responseJson)}");
                        continue;
                    }

                    var output = TryExtractOllamaOutput(responseJson);
                    if (string.IsNullOrWhiteSpace(output) && IsOllamaLoadOnly(responseJson))
                    {
                        using var warmRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
                        warmRequest.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
                        using var warmResponse = await client.SendAsync(warmRequest, cancellationToken);
                        var warmJson = await warmResponse.Content.ReadAsStringAsync(cancellationToken);
                        if (warmResponse.IsSuccessStatusCode)
                        {
                            responseJson = warmJson;
                            output = TryExtractOllamaOutput(responseJson);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(output))
                        output = responseJson;

                    return new ModelInferResult
                    {
                        Ok = true,
                        Provider = "ollama",
                        Model = modelName,
                        Endpoint = endpoint,
                        Output = output,
                        OutputJson = responseJson
                    };
                }
                catch (Exception ex)
                {
                    errors.Add($"{endpoint} => {TrimResponse(ex.Message)}");
                }
            }

            return new ModelInferResult
            {
                Ok = false,
                Provider = "ollama",
                Model = modelName,
                Endpoint = endpoints[0],
                Error = errors.Count > 0
                    ? $"No se pudo conectar a Ollama. Intentos: {string.Join(" | ", errors.Take(3))}"
                    : "No se pudo conectar a Ollama."
            };
        }

        private async Task<ModelInferResult> InferWithOpenAiCompatAsync(
            ModelServiceSettings settings,
            string input,
            string? contextJson,
            CancellationToken cancellationToken)
        {
            var endpoint = BuildEndpoint(settings.BaseUrl, settings.Path, "/v1/chat/completions");
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return new ModelInferResult
                {
                    Ok = false,
                    Provider = "openai",
                    Model = settings.Model,
                    Endpoint = "",
                    Error = "BaseUrl inválida para provider openai."
                };
            }

            var apiKey = ResolveApiKey(settings);
            var client = _httpClientFactory.CreateClient("aibase-model");
            var messages = BuildMessages(settings, input, contextJson);
            var payload = new
            {
                model = string.IsNullOrWhiteSpace(settings.Model) ? "gpt-4o-mini" : settings.Model.Trim(),
                messages = messages.Select(x => new { role = x.Role, content = x.Content }).ToArray(),
                temperature = Math.Clamp(settings.Temperature, 0, 2),
                max_tokens = Math.Clamp(settings.MaxTokens, 64, 4096),
                top_p = Math.Clamp(settings.TopP, 0.05, 1.0)
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            using var response = await client.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new ModelInferResult
                {
                    Ok = false,
                    Provider = "openai",
                    Model = payload.model,
                    Endpoint = endpoint,
                    Error = $"OpenAI-compatible devolvió {(int)response.StatusCode}: {TrimResponse(responseJson)}"
                };
            }

            var output = TryExtractOpenAiOutput(responseJson);
            if (string.IsNullOrWhiteSpace(output))
                output = responseJson;

            return new ModelInferResult
            {
                Ok = true,
                Provider = "openai",
                Model = payload.model,
                Endpoint = endpoint,
                Output = output,
                OutputJson = responseJson
            };
        }

        private static string ResolveApiKey(ModelServiceSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                return settings.ApiKey.Trim();

            var envKey = string.IsNullOrWhiteSpace(settings.ApiKeyEnv)
                ? "OPENAI_API_KEY"
                : settings.ApiKeyEnv.Trim();

            return Environment.GetEnvironmentVariable(envKey) ?? "";
        }

        private static List<ModelMessage> BuildMessages(ModelServiceSettings settings, string input, string? contextJson)
        {
            var messages = new List<ModelMessage>();
            if (!string.IsNullOrWhiteSpace(settings.SystemPrompt))
            {
                messages.Add(new ModelMessage("system", settings.SystemPrompt.Trim()));
            }

            AppendContextMessages(messages, contextJson);
            messages.Add(new ModelMessage("user", input.Trim()));
            return messages;
        }

        private static void AppendContextMessages(List<ModelMessage> messages, string? contextJson)
        {
            if (string.IsNullOrWhiteSpace(contextJson)) return;

            try
            {
                using var doc = JsonDocument.Parse(contextJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return;

                if (TryGetPropertyIgnoreCase(doc.RootElement, "messages", out var messagesEl)
                    && messagesEl.ValueKind == JsonValueKind.Array)
                {
                    AppendMessagesArray(messages, messagesEl);
                    return;
                }

                if (TryGetPropertyIgnoreCase(doc.RootElement, "chatHistory", out var chatEl)
                    && chatEl.ValueKind == JsonValueKind.Array)
                {
                    AppendMessagesArray(messages, chatEl);
                }
            }
            catch
            {
                // El contexto inválido no debe romper inferencia.
            }
        }

        private static void AppendMessagesArray(List<ModelMessage> messages, JsonElement items)
        {
            foreach (var item in items.EnumerateArray().TakeLast(20))
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var role = "user";
                var content = "";

                if (TryGetPropertyIgnoreCase(item, "role", out var roleEl))
                {
                    role = NormalizeRole(roleEl.GetString());
                }

                if (TryGetPropertyIgnoreCase(item, "content", out var contentEl))
                    content = contentEl.GetString() ?? "";
                else if (TryGetPropertyIgnoreCase(item, "text", out var textEl))
                    content = textEl.GetString() ?? "";

                if (!string.IsNullOrWhiteSpace(content))
                {
                    messages.Add(new ModelMessage(role, content.Trim()));
                }
            }
        }

        private static string TryExtractOllamaOutput(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;
                if (TryGetPropertyIgnoreCase(root, "message", out var message)
                    && message.ValueKind == JsonValueKind.Object
                    && TryGetPropertyIgnoreCase(message, "content", out var content))
                {
                    return content.GetString() ?? "";
                }

                if (TryGetPropertyIgnoreCase(root, "response", out var response))
                {
                    return response.GetString() ?? "";
                }
            }
            catch
            {
                // no-op
            }
            return "";
        }

        private static bool IsOllamaLoadOnly(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;
                var done = TryGetPropertyIgnoreCase(root, "done", out var doneEl)
                    && doneEl.ValueKind == JsonValueKind.True;
                var doneReason = TryGetPropertyIgnoreCase(root, "done_reason", out var reasonEl)
                    ? (reasonEl.GetString() ?? "")
                    : "";
                var responseText = TryExtractOllamaOutput(responseJson);
                return done
                    && string.Equals(doneReason, "load", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(responseText);
            }
            catch
            {
                return false;
            }
        }

        private static string TryExtractOpenAiOutput(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;
                if (TryGetPropertyIgnoreCase(root, "choices", out var choices)
                    && choices.ValueKind == JsonValueKind.Array
                    && choices.GetArrayLength() > 0)
                {
                    var first = choices[0];
                    if (TryGetPropertyIgnoreCase(first, "message", out var message)
                        && message.ValueKind == JsonValueKind.Object
                        && TryGetPropertyIgnoreCase(message, "content", out var content))
                    {
                        return content.GetString() ?? "";
                    }
                }
            }
            catch
            {
                // no-op
            }
            return "";
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string key, out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
                    {
                        value = prop.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static string NormalizeProvider(string? provider)
        {
            var key = (provider ?? "").Trim().ToLowerInvariant();
            return key switch
            {
                "openai" => "openai",
                "openai_compatible" => "openai",
                "openai-compatible" => "openai",
                "chatgpt" => "openai",
                "ollama" => "ollama",
                _ => key
            };
        }

        private static string NormalizeRole(string? role)
        {
            var key = (role ?? "").Trim().ToLowerInvariant();
            return key switch
            {
                "assistant" => "assistant",
                "system" => "system",
                _ => "user"
            };
        }

        private static string BuildEndpoint(string? baseUrl, string? path, string fallbackPath)
        {
            var rawBase = (baseUrl ?? "").Trim();
            if (string.IsNullOrWhiteSpace(rawBase)) return "";
            if (!rawBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !rawBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                rawBase = $"http://{rawBase}";
            }

            var cleanBase = rawBase.TrimEnd('/');
            var cleanPath = string.IsNullOrWhiteSpace(path) ? fallbackPath : path.Trim();
            if (!cleanPath.StartsWith('/')) cleanPath = "/" + cleanPath;
            return cleanBase + cleanPath;
        }

        private static List<string> BuildEndpointCandidates(string? baseUrl, string? path, string fallbackPath, string provider)
        {
            var endpoints = new List<string>();

            void AddBase(string? baseCandidate)
            {
                var endpoint = BuildEndpoint(baseCandidate, path, fallbackPath);
                if (string.IsNullOrWhiteSpace(endpoint)) return;
                if (endpoints.Any(x => string.Equals(x, endpoint, StringComparison.OrdinalIgnoreCase))) return;
                endpoints.Add(endpoint);
            }

            var rawBase = (baseUrl ?? "").Trim();
            AddBase(rawBase);

            if (provider == "ollama")
            {
                if (string.IsNullOrWhiteSpace(rawBase))
                {
                    AddBase("http://localhost:11434");
                    AddBase("http://127.0.0.1:11434");
                    AddBase("http://host.docker.internal:11434");
                    return endpoints;
                }

                var baseWithScheme = rawBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || rawBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? rawBase
                    : $"http://{rawBase}";

                if (Uri.TryCreate(baseWithScheme, UriKind.Absolute, out var parsed))
                {
                    var host = parsed.Host.Trim().ToLowerInvariant();
                    if (host == "host.docker.internal")
                    {
                        AddBase(ReplaceHost(baseWithScheme, "localhost"));
                        AddBase(ReplaceHost(baseWithScheme, "127.0.0.1"));
                    }
                    else if (host == "localhost" || host == "127.0.0.1")
                    {
                        AddBase(ReplaceHost(baseWithScheme, "host.docker.internal"));
                    }
                }
            }

            return endpoints;
        }

        private static string ReplaceHost(string baseUrl, string newHost)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)) return baseUrl;
            var builder = new UriBuilder(uri) { Host = newHost };
            return builder.Uri.GetLeftPart(UriPartial.Authority);
        }

        private static string BuildGeneratePrompt(List<ModelMessage> messages)
        {
            var usable = messages
                .Where(x => x.Role is "user" or "assistant")
                .TakeLast(16)
                .ToList();

            if (usable.Count == 0)
                return "";

            var lines = new List<string>();
            foreach (var item in usable)
            {
                var role = item.Role == "assistant" ? "Asistente" : "Usuario";
                lines.Add($"{role}: {item.Content}");
            }
            lines.Add("Asistente:");
            return string.Join("\n", lines);
        }

        private static string TrimResponse(string? responseJson)
        {
            var raw = (responseJson ?? "").Trim();
            if (raw.Length <= 400) return raw;
            return raw[..400] + "...";
        }
    }

    internal readonly struct ModelMessage
    {
        public ModelMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public string Role { get; }
        public string Content { get; }
    }
}
