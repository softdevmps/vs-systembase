using System.Net.Http.Headers;
using System.Text.Json;

namespace Backend.Utils
{
    public sealed class WhisperResult
    {
        public string Text { get; init; } = "";
        public string Language { get; init; } = "es";
        public decimal? Confidence { get; init; }
        public string ModelVersion { get; init; } = "whisper-local";
    }

    public static class WhisperClient
    {
        public static async Task<WhisperResult?> TranscribeAsync(string filePath, HttpClient httpClient, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(AppConfig.WHISPER_STUB_TEXT))
            {
                return new WhisperResult
                {
                    Text = AppConfig.WHISPER_STUB_TEXT,
                    Language = "es"
                };
            }

            var url = AppConfig.WHISPER_URL;
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var (ok, json) = await PostWhisperAsync(url, "audio_file", filePath, httpClient, ct);
            if (!ok)
            {
                var (fallbackOk, fallbackJson) = await PostWhisperAsync(url, "file", filePath, httpClient, ct);
                if (!fallbackOk)
                    return null;

                json = fallbackJson;
            }

            try
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var text = root.TryGetProperty("text", out var t) ? t.GetString() : null;
                if (string.IsNullOrWhiteSpace(text))
                {
                    Console.WriteLine($"[Whisper] Respuesta JSON sin texto: {Trim(json)}");
                    return null;
                }

                var lang = root.TryGetProperty("language", out var l) ? l.GetString() : "es";
                decimal? confidence = null;
                if (root.TryGetProperty("confidence", out var c) && c.TryGetDecimal(out var conf))
                    confidence = conf;

                return new WhisperResult
                {
                    Text = text ?? "",
                    Language = lang ?? "es",
                    Confidence = confidence
                };
            }
            catch (Exception ex)
            {
                // Algunos servicios devuelven texto plano aunque se pida JSON.
                if (!string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine($"[Whisper] Respuesta no JSON, usando texto plano. Error: {ex.Message}");
                    return new WhisperResult
                    {
                        Text = json.Trim(),
                        Language = "es",
                        Confidence = null
                    };
                }

                Console.WriteLine($"[Whisper] Error parseando respuesta vacia: {ex.Message}");
                return null;
            }
        }

        private static async Task<(bool ok, string body)> PostWhisperAsync(string url, string fileField, string filePath, HttpClient httpClient, CancellationToken ct)
        {
            await using var stream = File.OpenRead(filePath);
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(new StringContent("transcribe"), "task");
            content.Add(new StringContent("json"), "output");
            content.Add(new StringContent("es"), "language");
            content.Add(fileContent, fileField, Path.GetFileName(filePath));

            using var response = await httpClient.PostAsync(url, content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Whisper] HTTP {(int)response.StatusCode} usando '{fileField}': {Trim(body)}");
                return (false, body);
            }

            return (true, body);
        }

        private static string Trim(string? value, int max = 400)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "(vacio)";
            return value!.Length <= max ? value! : value!.Substring(0, max) + "...";
        }
    }
}
