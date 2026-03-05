using System.Text;
using System.Text.Json;

namespace Backend.Utils
{
    public sealed class EngineClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public EngineClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<(bool Ok, string? ResponseJson, string? Error)> ExecuteRunAsync(
            int projectId,
            string runType,
            string? inputJson,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("aibase-engine");
                var body = JsonSerializer.Serialize(new
                {
                    projectId,
                    runType,
                    inputJson
                }, JsonOptions);

                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync("/runs/execute", content, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return (false, responseJson, $"Engine devolvio {(int)response.StatusCode}");
                }

                return (true, responseJson, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Ok, string? ResponseJson, string? Error)> InferAsync(
            int projectId,
            string input,
            string? contextJson,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("aibase-engine");
                var body = JsonSerializer.Serialize(new
                {
                    projectId,
                    input,
                    contextJson
                }, JsonOptions);

                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync("/infer", content, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return (false, responseJson, $"Engine devolvio {(int)response.StatusCode}");
                }

                return (true, responseJson, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }
    }
}
