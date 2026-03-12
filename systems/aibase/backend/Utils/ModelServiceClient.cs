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
            var media = ExtractMediaAttachments(contextJson);
            var messages = BuildMessages(settings, input, contextJson, media);
            var imageBase64 = media
                .Where(x => string.Equals(x.Type, "image", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Base64)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .Take(4)
                .ToArray();
            var wantsVision = imageBase64.Length > 0;
            var requestedModel = string.IsNullOrWhiteSpace(settings.Model) ? "llama3.2:3b" : settings.Model.Trim();
            var modelCandidates = BuildOllamaModelCandidates(requestedModel, wantsVision);

            var errors = new List<string>();
            foreach (var endpoint in endpoints)
            {
                foreach (var modelName in modelCandidates)
                {
                    try
                    {
                        var useGenerateApi = endpoint.Contains("/api/generate", StringComparison.OrdinalIgnoreCase);
                        var isFallbackModel = !string.Equals(modelName, requestedModel, StringComparison.OrdinalIgnoreCase);
                        var optionsPayload = new
                        {
                            temperature = settings.Temperature,
                            num_predict = isFallbackModel && wantsVision
                                ? Math.Min(Math.Clamp(settings.MaxTokens, 64, 4096), 192)
                                : Math.Clamp(settings.MaxTokens, 64, 4096),
                            top_p = Math.Clamp(settings.TopP, 0.05, 1.0),
                            repeat_penalty = Math.Clamp(settings.RepetitionPenalty, 0.8, 2.5)
                        };

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
                                images = imageBase64.Length > 0 ? imageBase64 : null,
                                options = optionsPayload
                            };
                        }
                        else
                        {
                            var chatMessages = BuildOllamaChatMessages(messages, imageBase64);
                            payload = new
                            {
                                model = modelName,
                                stream = false,
                                messages = chatMessages,
                                options = optionsPayload
                            };
                        }

                        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

                        using var response = await client.SendAsync(request, cancellationToken);
                        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

                        if (!response.IsSuccessStatusCode)
                        {
                            if (ShouldAutoPullOllamaModel((int)response.StatusCode, responseJson))
                            {
                                var pull = await TryPullOllamaModelAsync(endpoint, modelName, cancellationToken);
                                if (pull.Ok)
                                {
                                    using var retryRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
                                    retryRequest.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
                                    using var retryResponse = await client.SendAsync(retryRequest, cancellationToken);
                                    var retryJson = await retryResponse.Content.ReadAsStringAsync(cancellationToken);
                                    if (retryResponse.IsSuccessStatusCode)
                                    {
                                        responseJson = retryJson;
                                        var retryOutput = TryExtractOllamaOutput(responseJson);
                                        if (string.IsNullOrWhiteSpace(retryOutput))
                                            retryOutput = responseJson;

                                        return new ModelInferResult
                                        {
                                            Ok = true,
                                            Provider = "ollama",
                                            Model = modelName,
                                            Endpoint = endpoint,
                                            Output = retryOutput,
                                            OutputJson = responseJson
                                        };
                                    }

                                    if (ShouldAutoDowngradeOllamaModel((int)retryResponse.StatusCode, retryJson, wantsVision))
                                    {
                                        errors.Add($"{endpoint} [{modelName}] => auto-pull ok, pero sin memoria/runner ({(int)retryResponse.StatusCode}); probando modelo alternativo");
                                        continue;
                                    }

                                    errors.Add($"{endpoint} [{modelName}] => auto-pull ok, pero infer falló HTTP {(int)retryResponse.StatusCode}: {TrimResponse(retryJson)}");
                                    continue;
                                }

                                errors.Add($"{endpoint} [{modelName}] => modelo no descargado: {TrimResponse(pull.Error)}");
                                continue;
                            }

                            if (ShouldAutoDowngradeOllamaModel((int)response.StatusCode, responseJson, wantsVision))
                            {
                                errors.Add($"{endpoint} [{modelName}] => sin memoria/runner ({(int)response.StatusCode}); probando modelo alternativo");
                                continue;
                            }

                            errors.Add($"{endpoint} [{modelName}] => HTTP {(int)response.StatusCode}: {TrimResponse(responseJson)}");
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

                        if (string.IsNullOrWhiteSpace(output) && wantsVision && !useGenerateApi)
                        {
                            var generateEndpoint = ReplaceOllamaApiPath(endpoint, "/api/generate");
                            if (!string.IsNullOrWhiteSpace(generateEndpoint)
                                && !string.Equals(generateEndpoint, endpoint, StringComparison.OrdinalIgnoreCase))
                            {
                                var systemPrompt = messages.FirstOrDefault(m => m.Role == "system").Content ?? "";
                                var generatePayload = new
                                {
                                    model = modelName,
                                    stream = false,
                                    system = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt,
                                    prompt = BuildGeneratePrompt(messages),
                                    images = imageBase64.Length > 0 ? imageBase64 : null,
                                    options = optionsPayload
                                };

                                using var generateRequest = new HttpRequestMessage(HttpMethod.Post, generateEndpoint);
                                generateRequest.Content = new StringContent(JsonSerializer.Serialize(generatePayload, JsonOptions), Encoding.UTF8, "application/json");
                                using var generateResponse = await client.SendAsync(generateRequest, cancellationToken);
                                var generateJson = await generateResponse.Content.ReadAsStringAsync(cancellationToken);
                                if (generateResponse.IsSuccessStatusCode)
                                {
                                    var generateOutput = TryExtractOllamaOutput(generateJson);
                                    if (!string.IsNullOrWhiteSpace(generateOutput))
                                    {
                                        return new ModelInferResult
                                        {
                                            Ok = true,
                                            Provider = "ollama",
                                            Model = modelName,
                                            Endpoint = generateEndpoint,
                                            Output = generateOutput,
                                            OutputJson = generateJson
                                        };
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrWhiteSpace(output))
                        {
                            output = wantsVision
                                ? BuildVisionEmptyOutputFallback(modelName, responseJson)
                                : responseJson;
                        }

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
                        errors.Add($"{endpoint} [{modelName}] => {TrimResponse(ex.Message)}");
                    }
                }
            }

            return new ModelInferResult
            {
                Ok = false,
                Provider = "ollama",
                Model = requestedModel,
                Endpoint = endpoints[0],
                Error = BuildOllamaFailureError(errors)
            };
        }

        private async Task<(bool Ok, string Error)> TryPullOllamaModelAsync(
            string endpoint,
            string modelName,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
                    return (false, "endpoint inválido");

                var pullUrl = $"{endpointUri.Scheme}://{endpointUri.Authority}/api/pull";
                var pullClient = _httpClientFactory.CreateClient("aibase-model");
                pullClient.Timeout = TimeSpan.FromMinutes(30);

                var payload = new
                {
                    model = modelName,
                    stream = false
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, pullUrl);
                request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

                using var response = await pullClient.SendAsync(request, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"HTTP {(int)response.StatusCode}: {TrimResponse(responseJson)}");
                }

                if (!string.IsNullOrWhiteSpace(responseJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(responseJson);
                        if (TryGetPropertyIgnoreCase(doc.RootElement, "error", out var errorEl)
                            && errorEl.ValueKind == JsonValueKind.String
                            && !string.IsNullOrWhiteSpace(errorEl.GetString()))
                        {
                            return (false, errorEl.GetString() ?? "error al descargar modelo");
                        }
                    }
                    catch
                    {
                        // respuesta no JSON: tratamos como OK si HTTP fue exitoso
                    }
                }

                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private static bool ShouldAutoPullOllamaModel(int statusCode, string responseJson)
        {
            if (statusCode != 404) return false;

            var raw = (responseJson ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw)) return false;

            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (TryGetPropertyIgnoreCase(doc.RootElement, "error", out var errorEl)
                    && errorEl.ValueKind == JsonValueKind.String)
                {
                    var value = (errorEl.GetString() ?? "").Trim().ToLowerInvariant();
                    return value.Contains("model") && value.Contains("not found");
                }
            }
            catch
            {
                // fallback texto plano
            }

            var normalized = raw.ToLowerInvariant();
            return normalized.Contains("model") && normalized.Contains("not found");
        }

        private static bool ShouldAutoDowngradeOllamaModel(int statusCode, string responseJson, bool wantsVision)
        {
            if (!wantsVision) return false;
            if (statusCode < 500) return false;

            var normalized = (responseJson ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized)) return false;
            return normalized.Contains("signal: killed")
                   || normalized.Contains("runner process has terminated")
                   || normalized.Contains("out of memory")
                   || normalized.Contains("insufficient memory");
        }

        private static List<string> BuildOllamaModelCandidates(string configuredModel, bool wantsVision)
        {
            var first = string.IsNullOrWhiteSpace(configuredModel) ? "llama3.2:3b" : configuredModel.Trim();
            if (first.StartsWith("hf:", StringComparison.OrdinalIgnoreCase))
                first = first[3..].Trim();

            var candidates = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string value)
            {
                var v = (value ?? "").Trim();
                if (string.IsNullOrWhiteSpace(v)) return;
                if (seen.Add(v)) candidates.Add(v);
            }

            Add(first);

            var firstLower = first.ToLowerInvariant();
            if (wantsVision)
            {
                var isLikelyVision = firstLower.Contains("llava")
                                     || firstLower.Contains("vision")
                                     || firstLower.Contains("bakllava")
                                     || firstLower.Contains("moondream");
                if (!isLikelyVision)
                {
                    Add("llava:7b");
                    Add("llava");
                    Add("moondream:latest");
                    Add("moondream");
                }
                else
                {
                    if (firstLower.Contains("13b"))
                        Add("llava:7b");
                    Add("llava");
                    Add("moondream:latest");
                    Add("moondream");
                }
            }
            else
            {
                Add("llama3.2:3b");
            }

            return candidates;
        }

        private static string BuildOllamaFailureError(List<string> errors)
        {
            if (errors is null || errors.Count == 0)
                return "No se pudo conectar a Ollama.";

            if (errors.Any(x => x.Contains("sin memoria/runner", StringComparison.OrdinalIgnoreCase)))
            {
                return $"Ollama no tiene memoria suficiente para cargar modelos de visión en este equipo. Intentos: {string.Join(" | ", errors.Take(3))}";
            }

            return $"No se pudo conectar a Ollama. Intentos: {string.Join(" | ", errors.Take(3))}";
        }

        private static string BuildVisionEmptyOutputFallback(string modelName, string responseJson)
        {
            return JsonSerializer.Serialize(new
            {
                faces = Array.Empty<object>(),
                summary = "El modelo de visión devolvió salida vacía para esta imagen.",
                status = "empty_model_output",
                model = modelName,
                hint = "Reintenta con otra imagen o usa un modelo de visión más robusto.",
                raw = TrimResponse(responseJson)
            }, JsonOptions);
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
            var media = ExtractMediaAttachments(contextJson);
            var messages = BuildMessages(settings, input, contextJson, media);
            var modelName = string.IsNullOrWhiteSpace(settings.Model) ? "gpt-4o-mini" : settings.Model.Trim();

            var multimodalEnabled = media.Any(x => string.Equals(x.Type, "image", StringComparison.OrdinalIgnoreCase));
            var (ok, responseJson, statusCode) = await SendOpenAiCompatAsync(
                client,
                endpoint,
                apiKey,
                modelName,
                settings,
                messages,
                media,
                includeImages: multimodalEnabled,
                cancellationToken);

            if (!ok && multimodalEnabled)
            {
                (ok, responseJson, statusCode) = await SendOpenAiCompatAsync(
                    client,
                    endpoint,
                    apiKey,
                    modelName,
                    settings,
                    messages,
                    media,
                    includeImages: false,
                    cancellationToken);
            }

            if (!ok)
            {
                return new ModelInferResult
                {
                    Ok = false,
                    Provider = "openai",
                    Model = modelName,
                    Endpoint = endpoint,
                    Error = $"OpenAI-compatible devolvió {statusCode}: {TrimResponse(responseJson)}"
                };
            }

            var output = TryExtractOpenAiOutput(responseJson);
            if (string.IsNullOrWhiteSpace(output))
                output = responseJson;

            return new ModelInferResult
            {
                Ok = true,
                Provider = "openai",
                Model = modelName,
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

        private static List<ModelMessage> BuildMessages(
            ModelServiceSettings settings,
            string input,
            string? contextJson,
            IReadOnlyList<ModelMedia>? media = null)
        {
            var messages = new List<ModelMessage>();
            if (!string.IsNullOrWhiteSpace(settings.SystemPrompt))
            {
                messages.Add(new ModelMessage("system", settings.SystemPrompt.Trim()));
            }

            AppendContextMessages(messages, contextJson);
            var userContent = string.IsNullOrWhiteSpace(input) ? "" : input.Trim();
            if (media is { Count: > 0 })
            {
                var mediaHint = BuildMediaContextHint(media);
                if (!string.IsNullOrWhiteSpace(mediaHint))
                {
                    userContent = string.IsNullOrWhiteSpace(userContent)
                        ? mediaHint
                        : $"{userContent}\n\n{mediaHint}";
                }
            }
            messages.Add(new ModelMessage("user", userContent));
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

        private static List<Dictionary<string, object?>> BuildOllamaChatMessages(
            IReadOnlyList<ModelMessage> messages,
            IReadOnlyList<string> imageBase64)
        {
            var payload = messages
                .Select(x => new Dictionary<string, object?>
                {
                    ["role"] = x.Role,
                    ["content"] = x.Content
                })
                .ToList();

            if (imageBase64.Count <= 0)
                return payload;

            var userIndex = -1;
            for (var i = payload.Count - 1; i >= 0; i--)
            {
                if (string.Equals(Convert.ToString(payload[i]["role"]), "user", StringComparison.OrdinalIgnoreCase))
                {
                    userIndex = i;
                    break;
                }
            }

            if (userIndex < 0)
            {
                payload.Add(new Dictionary<string, object?>
                {
                    ["role"] = "user",
                    ["content"] = "Analiza la imagen adjunta.",
                    ["images"] = imageBase64.ToArray()
                });
                return payload;
            }

            payload[userIndex]["images"] = imageBase64.ToArray();
            return payload;
        }

        private static async Task<(bool Ok, string ResponseJson, int StatusCode)> SendOpenAiCompatAsync(
            HttpClient client,
            string endpoint,
            string? apiKey,
            string modelName,
            ModelServiceSettings settings,
            IReadOnlyList<ModelMessage> messages,
            IReadOnlyList<ModelMedia> media,
            bool includeImages,
            CancellationToken cancellationToken)
        {
            var payload = new Dictionary<string, object?>
            {
                ["model"] = modelName,
                ["messages"] = BuildOpenAiMessages(messages, media, includeImages),
                ["temperature"] = Math.Clamp(settings.Temperature, 0, 2),
                ["max_tokens"] = Math.Clamp(settings.MaxTokens, 64, 4096),
                ["top_p"] = Math.Clamp(settings.TopP, 0.05, 1.0)
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            using var response = await client.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return (response.IsSuccessStatusCode, responseJson, (int)response.StatusCode);
        }

        private static List<Dictionary<string, object?>> BuildOpenAiMessages(
            IReadOnlyList<ModelMessage> messages,
            IReadOnlyList<ModelMedia> media,
            bool includeImages)
        {
            var imageDataUrls = includeImages
                ? media.Where(x => string.Equals(x.Type, "image", StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.DataUrl)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.Ordinal)
                    .Take(4)
                    .ToArray()
                : Array.Empty<string>();

            var payload = new List<Dictionary<string, object?>>();
            for (var idx = 0; idx < messages.Count; idx++)
            {
                var item = messages[idx];
                var row = new Dictionary<string, object?>
                {
                    ["role"] = item.Role
                };

                var isLastUser = idx == messages.Count - 1
                                 && string.Equals(item.Role, "user", StringComparison.OrdinalIgnoreCase);

                if (isLastUser && imageDataUrls.Length > 0)
                {
                    var content = new List<Dictionary<string, object?>>
                    {
                        new()
                        {
                            ["type"] = "text",
                            ["text"] = item.Content
                        }
                    };
                    foreach (var dataUrl in imageDataUrls)
                    {
                        content.Add(new Dictionary<string, object?>
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new Dictionary<string, object?>
                            {
                                ["url"] = dataUrl
                            }
                        });
                    }

                    row["content"] = content;
                }
                else
                {
                    row["content"] = item.Content;
                }

                payload.Add(row);
            }

            return payload;
        }

        private static List<ModelMedia> ExtractMediaAttachments(string? contextJson)
        {
            var media = new List<ModelMedia>();
            if (string.IsNullOrWhiteSpace(contextJson))
                return media;

            try
            {
                using var doc = JsonDocument.Parse(contextJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return media;

                ExtractMediaFromObject(doc.RootElement, media);
                if (TryGetPropertyIgnoreCase(doc.RootElement, "userContext", out var userContext)
                    && userContext.ValueKind == JsonValueKind.Object)
                {
                    ExtractMediaFromObject(userContext, media);
                }
            }
            catch
            {
                // El contexto inválido no debe romper inferencia.
            }

            return media;
        }

        private static void ExtractMediaFromObject(JsonElement node, List<ModelMedia> target)
        {
            if (!TryGetPropertyIgnoreCase(node, "media", out var mediaNode) || mediaNode.ValueKind != JsonValueKind.Array)
                return;

            foreach (var item in mediaNode.EnumerateArray().Take(4))
            {
                if (item.ValueKind != JsonValueKind.Object) continue;

                var type = ReadString(item, "type")?.Trim().ToLowerInvariant() ?? "";
                var mime = ReadString(item, "mime")?.Trim() ?? "";
                var fileName = ReadString(item, "fileName")?.Trim() ?? "";
                var dataUrl = ReadString(item, "dataUrl")?.Trim() ?? "";
                var base64 = ReadString(item, "base64")?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(base64) && !string.IsNullOrWhiteSpace(dataUrl))
                {
                    base64 = ExtractBase64FromDataUrl(dataUrl);
                }

                if (string.IsNullOrWhiteSpace(type))
                {
                    if (mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) type = "image";
                    else if (mime.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) type = "audio";
                    else type = "file";
                }

                if (string.IsNullOrWhiteSpace(type) && string.IsNullOrWhiteSpace(dataUrl))
                    continue;

                target.Add(new ModelMedia
                {
                    Type = type,
                    Mime = mime,
                    FileName = fileName,
                    DataUrl = dataUrl,
                    Base64 = base64
                });
            }
        }

        private static string BuildMediaContextHint(IReadOnlyList<ModelMedia> media)
        {
            if (media.Count <= 0) return "";

            var lines = media
                .Take(4)
                .Select(item =>
                {
                    var type = string.IsNullOrWhiteSpace(item.Type) ? "file" : item.Type;
                    var fileName = string.IsNullOrWhiteSpace(item.FileName) ? "archivo" : item.FileName;
                    var mime = string.IsNullOrWhiteSpace(item.Mime) ? "mime/n-a" : item.Mime;
                    return $"- {type}: {fileName} ({mime})";
                });

            return "Archivos adjuntos:\n" + string.Join("\n", lines);
        }

        private static string ExtractBase64FromDataUrl(string dataUrl)
        {
            var raw = (dataUrl ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var marker = "base64,";
            var idx = raw.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return "";
            var encoded = raw[(idx + marker.Length)..].Trim();
            return encoded;
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
                        if (content.ValueKind == JsonValueKind.String)
                            return content.GetString() ?? "";
                        if (content.ValueKind == JsonValueKind.Array)
                        {
                            var fragments = new List<string>();
                            foreach (var part in content.EnumerateArray())
                            {
                                if (part.ValueKind != JsonValueKind.Object) continue;
                                if (TryGetPropertyIgnoreCase(part, "text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                                {
                                    var text = textEl.GetString();
                                    if (!string.IsNullOrWhiteSpace(text)) fragments.Add(text);
                                }
                            }
                            return string.Join("\n", fragments);
                        }
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

        private static string? ReadString(JsonElement element, string key)
        {
            if (!TryGetPropertyIgnoreCase(element, key, out var value)) return null;
            return value.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                JsonValueKind.String => value.GetString(),
                _ => value.ToString()
            };
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

        private static string ReplaceOllamaApiPath(string endpoint, string newPath)
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)) return "";
            var clean = string.IsNullOrWhiteSpace(newPath) ? "/api/generate" : newPath.Trim();
            if (!clean.StartsWith('/')) clean = "/" + clean;
            var builder = new UriBuilder(uri) { Path = clean };
            return builder.Uri.ToString();
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

    internal sealed class ModelMedia
    {
        public string Type { get; set; } = "";
        public string Mime { get; set; } = "";
        public string FileName { get; set; } = "";
        public string DataUrl { get; set; } = "";
        public string Base64 { get; set; } = "";
    }
}
