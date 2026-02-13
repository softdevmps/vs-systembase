using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Backend.Negocio.Pipeline;

namespace Backend.Utils
{
    public sealed class LlmExtractResult
    {
        public string? Fecha { get; init; }
        public string? Hora { get; init; }
        public string? LugarTexto { get; init; }
        public string? Descripcion { get; init; }
        public string? TipoCodigo { get; init; }
        public decimal? Confianza { get; init; }
        public string? RawJson { get; init; }
    }

    public sealed class LlmLocationParts
    {
        public string? Calle { get; init; }
        public string? Numero { get; init; }
        public string? Interseccion { get; init; }
        public string? Barrio { get; init; }
        public string? Ciudad { get; init; }
        public string? Poi { get; init; }
        public decimal? Confianza { get; init; }
        public string? RawJson { get; init; }
    }

    public static class LlmClient
    {
        public static async Task<string?> NormalizeLocationAsync(string rawText, HttpClient httpClient, CancellationToken ct = default)
        {
            if (!AppConfig.LLM_ENABLED)
                return null;

            var url = AppConfig.LLM_URL;
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var prompt = BuildLocationPrompt(rawText);
            var payload = BuildPayload(prompt);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(AppConfig.LLM_API_KEY))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppConfig.LLM_API_KEY);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (AppConfig.LLM_TIMEOUT_SECONDS > 0)
                cts.CancelAfter(TimeSpan.FromSeconds(AppConfig.LLM_TIMEOUT_SECONDS));

            using var response = await httpClient.SendAsync(request, cts.Token);
            var body = await response.Content.ReadAsStringAsync(cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[LLM] HTTP {(int)response.StatusCode}: {Trim(body)}");
                return null;
            }

            var content = ExtractContent(body);
            var json = ExtractJson(content);
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine($"[LLM] Respuesta sin JSON (location): {Trim(content)}");
                return null;
            }

            try
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var lugar = root.TryGetProperty("lugar_texto", out var l) ? l.GetString() : null;
                if (string.IsNullOrWhiteSpace(lugar))
                    return null;
                var trimmed = lugar.Trim();
                var lowered = trimmed.ToLowerInvariant();
                if (lowered == "null" || lowered == "n/a" || lowered == "ninguno" || lowered.Contains("no especific"))
                    return null;
                return trimmed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LLM] Error parseando JSON (location): {ex.Message}. Body: {Trim(json)}");
                return null;
            }
        }

        public static async Task<LlmLocationParts?> ExtractLocationPartsAsync(string rawText, HttpClient httpClient, CancellationToken ct = default)
        {
            if (!AppConfig.LLM_ENABLED)
                return null;

            var url = AppConfig.LLM_URL;
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var prompt = BuildLocationPartsPrompt(rawText);
            var payload = BuildPayload(prompt);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(AppConfig.LLM_API_KEY))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppConfig.LLM_API_KEY);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (AppConfig.LLM_TIMEOUT_SECONDS > 0)
                cts.CancelAfter(TimeSpan.FromSeconds(AppConfig.LLM_TIMEOUT_SECONDS));

            using var response = await httpClient.SendAsync(request, cts.Token);
            var body = await response.Content.ReadAsStringAsync(cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[LLM] HTTP {(int)response.StatusCode}: {Trim(body)}");
                return null;
            }

            var content = ExtractContent(body);
            var json = ExtractJson(content);
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine($"[LLM] Respuesta sin JSON (location parts): {Trim(content)}");
                return null;
            }

            try
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                return new LlmLocationParts
                {
                    Calle = root.TryGetProperty("calle", out var c) ? c.GetString() : null,
                    Numero = root.TryGetProperty("numero", out var n) ? n.GetString() : null,
                    Interseccion = root.TryGetProperty("interseccion", out var i) ? i.GetString() : null,
                    Barrio = root.TryGetProperty("barrio", out var b) ? b.GetString() : null,
                    Ciudad = root.TryGetProperty("ciudad", out var ci) ? ci.GetString() : null,
                    Poi = root.TryGetProperty("poi", out var p) ? p.GetString() : null,
                    Confianza = root.TryGetProperty("confianza", out var conf) && conf.TryGetDecimal(out var confValue) ? confValue : null,
                    RawJson = json
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LLM] Error parseando JSON (location parts): {ex.Message}. Body: {Trim(json)}");
                return null;
            }
        }

        public static string? ComposeLocationText(LlmLocationParts? parts)
        {
            if (parts == null) return null;
            var inter = NormalizeField(parts.Interseccion);
            var calle = NormalizeField(parts.Calle);
            var numero = NormalizeNumber(parts.Numero);
            var poi = NormalizeField(parts.Poi);
            var barrio = NormalizeField(parts.Barrio);
            var ciudad = NormalizeField(parts.Ciudad);

            string? baseText = null;
            if (!string.IsNullOrWhiteSpace(inter))
                baseText = inter;
            else if (!string.IsNullOrWhiteSpace(calle))
                baseText = !string.IsNullOrWhiteSpace(numero) ? $"{calle} {numero}" : calle;
            else if (!string.IsNullOrWhiteSpace(poi))
                baseText = poi;

            if (string.IsNullOrWhiteSpace(baseText))
                return null;

            var partsList = new List<string> { baseText };
            if (!string.IsNullOrWhiteSpace(barrio))
                partsList.Add($"barrio {barrio}");
            if (!string.IsNullOrWhiteSpace(ciudad))
                partsList.Add(ciudad);
            return string.Join(", ", partsList);
        }

        public static async Task<LlmExtractResult?> ExtractIncidentAsync(
            string rawText,
            List<CatalogoHechoItem> catalogo,
            HttpClient httpClient,
            CancellationToken ct = default)
        {
            if (!AppConfig.LLM_ENABLED)
                return null;

            var url = AppConfig.LLM_URL;
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var prompt = BuildPrompt(rawText, catalogo);
            var payload = BuildPayload(prompt);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(AppConfig.LLM_API_KEY))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AppConfig.LLM_API_KEY);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (AppConfig.LLM_TIMEOUT_SECONDS > 0)
                cts.CancelAfter(TimeSpan.FromSeconds(AppConfig.LLM_TIMEOUT_SECONDS));

            using var response = await httpClient.SendAsync(request, cts.Token);
            var body = await response.Content.ReadAsStringAsync(cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[LLM] HTTP {(int)response.StatusCode}: {Trim(body)}");
                return null;
            }

            var content = ExtractContent(body);
            var json = ExtractJson(content);
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine($"[LLM] Respuesta sin JSON: {Trim(content)}");
                return null;
            }

            try
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                return new LlmExtractResult
                {
                    Fecha = root.TryGetProperty("fecha", out var f) ? f.GetString() : null,
                    Hora = root.TryGetProperty("hora", out var h) ? h.GetString() : null,
                    LugarTexto = root.TryGetProperty("lugar_texto", out var l) ? l.GetString() : null,
                    Descripcion = root.TryGetProperty("descripcion", out var d) ? d.GetString() : null,
                    TipoCodigo = root.TryGetProperty("tipo_codigo", out var t) ? t.GetString() : null,
                    Confianza = root.TryGetProperty("confianza", out var c) && c.TryGetDecimal(out var conf) ? conf : null,
                    RawJson = json
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LLM] Error parseando JSON: {ex.Message}. Body: {Trim(json)}");
                return null;
            }
        }

        private static string BuildPrompt(string text, List<CatalogoHechoItem> catalogo)
        {
            var catalogLines = catalogo
                .Where(c => !string.IsNullOrWhiteSpace(c.Codigo))
                .Select(c => $"{c.Codigo}|{c.Nombre}|{c.PalabrasClave}")
                .ToList();

            var catalog = string.Join("\n", catalogLines);

            return $$"""
Eres un extractor de incidentes. Devuelve SOLO un JSON valido, sin texto extra.
Reglas:
- Usa solo informacion presente en el texto, no inventes.
- Si un campo no aparece, usa null.
- No agregues comentarios ni aclaraciones extra.
- fecha en formato YYYY-MM-DD.
- hora en formato HH:MM.
- lugar_texto debe incluir calle/avenida + numero si aparece, y barrio si aparece.
- lugar_texto NO debe contener palabras "null", "no especificada" ni aclaraciones.
- descripcion debe ser corta (1 linea), sin palabras "fecha/hora/lugar/descripcion".
- tipo_codigo debe ser un CODIGO del catalogo (o null).

JSON esperado:
{"fecha":"YYYY-MM-DD","hora":"HH:MM","lugar_texto":"...","descripcion":"...","tipo_codigo":"CODIGO","confianza":0.0}

Catalogo (CODIGO|NOMBRE|PALABRAS_CLAVE):
{{catalog}}

Texto:
\"\"\"{{text}}\"\"\"
""";
        }

        private static string BuildLocationPrompt(string text)
        {
            return $$"""
Extrae SOLO la ubicacion completa del texto y devuelve JSON valido.
Reglas:
- Solo usa informacion presente en el texto, no inventes.
- Normaliza abreviaturas (av => avenida, etc.).
- No agregues comentarios ni aclaraciones extra.
- Si no hay ciudad, omitila. No escribas "null".
- Si no hay ubicacion clara, devuelve null.

JSON esperado:
{"lugar_texto":"calle/avenida + numero, barrio, ciudad"} o {"lugar_texto":null}

Texto:
\"\"\"{{text}}\"\"\"
""";
        }

        private static string BuildLocationPartsPrompt(string text)
        {
            return $$"""
Extrae SOLO la ubicacion del texto en campos separados y devuelve JSON valido.
Reglas:
- No inventes datos.
- No incluir fecha u hora en ningun campo (ignora palabras como "fecha", "hora", "hoy").
- Si aparece "lugar ..." usa lo que sigue a "lugar" como referencia principal.
- Corrige errores foneticos simples (yurigoyen->yrigoyen, velez sarfield->velez sarsfield, huemes/uemes/wemes->guemes).
- Si un campo no existe, usa null.
- interseccion si aparecen dos calles con "y", "e", "con", "entre", "esquina de" o dos vias seguidas.
- numero solo si aparece numero.

JSON esperado:
{"calle":"...","numero":"...","interseccion":"...","barrio":"...","ciudad":"...","poi":"...","confianza":0.0}

Texto:
\"\"\"{{text}}\"\"\"
""";
        }

        private static string BuildPayload(string prompt)
        {
            var format = string.IsNullOrWhiteSpace(AppConfig.LLM_FORMAT) ? null : AppConfig.LLM_FORMAT;
            if (string.Equals(AppConfig.LLM_MODE, "chat", StringComparison.OrdinalIgnoreCase))
            {
                return JsonSerializer.Serialize(new
                {
                    model = AppConfig.LLM_MODEL,
                    stream = false,
                    format,
                    options = new { temperature = 0.1 },
                    messages = new[]
                    {
                        new { role = "system", content = "Responde solo con JSON valido y completo." },
                        new { role = "user", content = prompt }
                    }
                });
            }

            return JsonSerializer.Serialize(new
            {
                model = AppConfig.LLM_MODEL,
                prompt,
                stream = false,
                format,
                options = new { temperature = 0.1 }
            });
        }

        private static string NormalizeField(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var trimmed = value.Trim();
            if (trimmed.Equals("null", StringComparison.OrdinalIgnoreCase)) return "";
            return trimmed;
        }

        private static string NormalizeNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var digits = new string(value.Where(char.IsDigit).ToArray());
            return digits;
        }

        private static string ExtractContent(string body)
        {
            try
            {
                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("response", out var response))
                    return response.GetString() ?? body;

                if (root.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
                    return content.GetString() ?? body;

                if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
                {
                    var first = choices[0];
                    if (first.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var msgContent))
                        return msgContent.GetString() ?? body;
                    if (first.TryGetProperty("text", out var text))
                        return text.GetString() ?? body;
                }

                return body;
            }
            catch
            {
                return body;
            }
        }

        private static string? ExtractJson(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');
            if (start < 0 || end <= start)
                return null;

            return content.Substring(start, end - start + 1);
        }

        private static string Trim(string? value, int max = 500)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "(vacio)";
            return value!.Length <= max ? value! : value!.Substring(0, max) + "...";
        }
    }
}
