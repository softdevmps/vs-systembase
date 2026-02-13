using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Backend.Utils
{
    public sealed class GeocodeResult
    {
        public decimal Lat { get; init; }
        public decimal Lng { get; init; }
        public string? DisplayName { get; init; }
    }

    public static class NominatimClient
    {
        public static async Task<GeocodeResult?> GeocodeAsync(string? query, HttpClient httpClient, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return null;

            var baseUrl = AppConfig.GEOCODER_URL;
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            var queryText = query.Trim();
            var structured = await TryStructuredGeocodeAsync(queryText, httpClient, ct, null);
            if (structured != null)
                return structured;

            var result = await TryGeocodeAsync(queryText, httpClient, ct);
            if (result != null)
                return result;

            var simplified = SimplifyQuery(queryText);
            if (!string.IsNullOrWhiteSpace(simplified) && !string.Equals(simplified, queryText, StringComparison.OrdinalIgnoreCase))
            {
                result = await TryGeocodeAsync(simplified, httpClient, ct);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static async Task<GeocodeResult?> GeocodeWithRequiredTokensAsync(
            string? query,
            IEnumerable<string> requiredTokens,
            HttpClient httpClient,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return null;

            var tokens = requiredTokens?
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => NormalizeToken(t))
                .Where(t => t.Length > 0)
                .Distinct()
                .ToList() ?? new List<string>();

            if (tokens.Count == 0)
                return await GeocodeAsync(query, httpClient, ct);

            var baseUrl = AppConfig.GEOCODER_URL;
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            var queryText = query.Trim();
            var structured = await TryStructuredGeocodeAsync(queryText, httpClient, ct, tokens);
            if (structured != null)
                return structured;

            var result = await TryGeocodeAsync(queryText, httpClient, ct, tokens, 5);
            if (result != null)
                return result;

            var simplified = SimplifyQuery(queryText);
            if (!string.IsNullOrWhiteSpace(simplified) && !string.Equals(simplified, queryText, StringComparison.OrdinalIgnoreCase))
            {
                result = await TryGeocodeAsync(simplified, httpClient, ct, tokens, 5);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static async Task<GeocodeResult?> TryGeocodeAsync(
            string queryText,
            HttpClient httpClient,
            CancellationToken ct,
            IReadOnlyList<string>? requiredTokens = null,
            int limit = 1)
        {
            var baseUrl = AppConfig.GEOCODER_URL;
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            var finalQuery = queryText.Trim();
            var requiredNumber = ExtractHouseNumber(finalQuery);
            var requiredBarrio = ExtractBarrioFromQuery(finalQuery);
            var suffix = AppConfig.GEOCODER_DEFAULT_SUFFIX;
            if (!string.IsNullOrWhiteSpace(suffix))
            {
                var normalizedQuery = finalQuery.ToLowerInvariant();
                var normalizedSuffix = suffix.ToLowerInvariant();
                if (!normalizedQuery.Contains(normalizedSuffix))
                    finalQuery = $"{finalQuery}, {suffix}";
            }

            var effectiveLimit = limit;
            if (!string.IsNullOrWhiteSpace(requiredNumber) || (requiredTokens != null && requiredTokens.Count > 0))
                effectiveLimit = Math.Max(limit, 5);

            var url = $"{baseUrl}?format=json&limit={effectiveLimit}&addressdetails=1&q={Uri.EscapeDataString(finalQuery)}";
            if (!string.IsNullOrWhiteSpace(AppConfig.GEOCODER_COUNTRY_CODES))
                url += $"&countrycodes={Uri.EscapeDataString(AppConfig.GEOCODER_COUNTRY_CODES)}";
            if (!string.IsNullOrWhiteSpace(AppConfig.GEOCODER_LANGUAGE))
                url += $"&accept-language={Uri.EscapeDataString(AppConfig.GEOCODER_LANGUAGE)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("SystemBase-Mapeo/1.0");

            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                return null;

            JsonElement item = default;
            var candidates = doc.RootElement.EnumerateArray().ToList();
            var scored = candidates
                .Select(c => new
                {
                    Candidate = c,
                    Score = ScoreCandidate(c, requiredTokens, requiredNumber, requiredBarrio)
                })
                .OrderByDescending(c => c.Score)
                .ToList();

            if (scored.Count == 0)
                return null;

            item = scored[0].Candidate;
            if (scored[0].Score <= 0 && candidates.Count > 0)
                item = candidates[0];

            if (!item.TryGetProperty("lat", out var latProp) || !item.TryGetProperty("lon", out var lonProp))
                return null;

            if (!decimal.TryParse(latProp.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat))
                return null;
            if (!decimal.TryParse(lonProp.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lng))
                return null;

            return new GeocodeResult
            {
                Lat = lat,
                Lng = lng,
                DisplayName = item.TryGetProperty("display_name", out var displayNameProp)
                    ? displayNameProp.GetString()
                    : null
            };
        }

        private static async Task<GeocodeResult?> TryStructuredGeocodeAsync(
            string queryText,
            HttpClient httpClient,
            CancellationToken ct,
            IReadOnlyList<string>? requiredTokens)
        {
            var baseUrl = AppConfig.GEOCODER_URL;
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            var (street, number) = ExtractStreetAndNumber(queryText);
            if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(number))
                return null;

            var requiredBarrio = ExtractBarrioFromQuery(queryText);
            var city = ExtractDefaultCity();
            var streetParam = $"{number} {street}".Trim();

            var url = $"{baseUrl}?format=json&addressdetails=1&limit=5&street={Uri.EscapeDataString(streetParam)}";
            if (!string.IsNullOrWhiteSpace(city))
                url += $"&city={Uri.EscapeDataString(city)}";
            if (!string.IsNullOrWhiteSpace(AppConfig.GEOCODER_COUNTRY_CODES))
                url += $"&countrycodes={Uri.EscapeDataString(AppConfig.GEOCODER_COUNTRY_CODES)}";
            if (!string.IsNullOrWhiteSpace(AppConfig.GEOCODER_LANGUAGE))
                url += $"&accept-language={Uri.EscapeDataString(AppConfig.GEOCODER_LANGUAGE)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("SystemBase-Mapeo/1.0");

            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                return null;

            var candidates = doc.RootElement.EnumerateArray().ToList();
            var scored = candidates
                .Select(c => new
                {
                    Candidate = c,
                    Score = ScoreCandidate(c, requiredTokens, number, requiredBarrio)
                })
                .OrderByDescending(c => c.Score)
                .ToList();

            if (scored.Count == 0)
                return null;

            var item = scored[0].Candidate;
            if (!item.TryGetProperty("lat", out var latProp) || !item.TryGetProperty("lon", out var lonProp))
                return null;

            if (!decimal.TryParse(latProp.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat))
                return null;
            if (!decimal.TryParse(lonProp.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lng))
                return null;

            return new GeocodeResult
            {
                Lat = lat,
                Lng = lng,
                DisplayName = item.TryGetProperty("display_name", out var displayNameProp)
                    ? displayNameProp.GetString()
                    : null
            };
        }

        private static string SimplifyQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return query;
            var cleaned = query;
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b(barrio|ciudad|provincia)\b", " ",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bno especificad[oa]\b", " ",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bno se especifica\b", " ",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bsin especificar\b", " ",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b\d{3,}\b", " ",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();
            cleaned = cleaned.Trim(',', '.', ';', ':');
            return cleaned;
        }

        private static string NormalizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var lower = value.ToLowerInvariant();
            var normalized = AppConfig.NormalizeKey(lower);
            return normalized;
        }

        private static int ScoreCandidate(
            JsonElement candidate,
            IReadOnlyList<string>? requiredTokens,
            string? requiredNumber,
            string? requiredBarrio)
        {
            var score = 0;
            var display = "";
            if (candidate.TryGetProperty("display_name", out var nameProp))
                display = NormalizeToken(nameProp.GetString() ?? "");

            if (requiredTokens != null && requiredTokens.Count > 0)
                score += requiredTokens.Count(token => display.Contains(token));

            if (!string.IsNullOrWhiteSpace(requiredNumber))
            {
                var houseNumber = GetAddressField(candidate, "house_number");
                if (!string.IsNullOrWhiteSpace(houseNumber) && NormalizeToken(houseNumber).Contains(requiredNumber))
                {
                    score += 5;
                }
                else if (!string.IsNullOrWhiteSpace(display) && display.Contains(requiredNumber))
                {
                    score += 3;
                }
                else
                {
                    score -= 1;
                }
            }

            if (!string.IsNullOrWhiteSpace(requiredBarrio))
            {
                var suburb = GetAddressField(candidate, "suburb")
                             ?? GetAddressField(candidate, "neighbourhood")
                             ?? GetAddressField(candidate, "city_district");
                var suburbNorm = NormalizeToken(suburb ?? "");
                if (!string.IsNullOrWhiteSpace(suburbNorm) && suburbNorm.Contains(requiredBarrio))
                    score += 2;
                else if (!string.IsNullOrWhiteSpace(display) && display.Contains(requiredBarrio))
                    score += 1;
            }

            if (candidate.TryGetProperty("addresstype", out var at))
            {
                var addresstype = at.GetString();
                if (string.Equals(addresstype, "house", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(addresstype, "building", StringComparison.OrdinalIgnoreCase))
                {
                    score += 1;
                }
            }

            return score;
        }

        private static string? GetAddressField(JsonElement candidate, string field)
        {
            if (!candidate.TryGetProperty("address", out var address) || address.ValueKind != JsonValueKind.Object)
                return null;
            return address.TryGetProperty(field, out var value) ? value.GetString() : null;
        }

        private static string? ExtractHouseNumber(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;
            var lowered = query.ToLowerInvariant();
            lowered = Regex.Replace(lowered, @"\b\d{1,2}\s+de\s+\p{L}+\b", " ");
            var match = Regex.Match(lowered, @"\b\d{1,5}\b");
            return match.Success ? match.Value : null;
        }

        private static string? ExtractBarrioFromQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;
            var match = Regex.Match(query, @"\bbarrio\s+([a-z0-9\s]{3,60})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success) return null;
            var barrio = match.Groups[1].Value;
            var normalized = NormalizeToken(barrio);
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static (string? street, string? number) ExtractStreetAndNumber(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return (null, null);
            var cleaned = Regex.Replace(query, @"\bbarrio\s+[a-z0-9\s]{3,60}", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bcordoba\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bargentina\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

            var pattern = @"\b(?<street>(?:avenida|av\.?|boulevard|bulevar|calle|pasaje|ruta|diagonal)\s+[\p{L}0-9\s\.]{3,80}?)\s+(?:al\s+)?(?<num>\d{1,5})\b";
            var match = Regex.Match(cleaned, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                var street = match.Groups["street"].Value.Trim();
                var number = match.Groups["num"].Value.Trim();
                street = Regex.Replace(street, @"\bav\.?\b", "avenida", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                return (street, number);
            }

            var altPattern = @"\b(?<street>[\p{L}0-9\s\.]{3,80}?)\s+(?:al|nro\.?|numero)\s+(?<num>\d{1,5})\b";
            match = Regex.Match(cleaned, altPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                var street = match.Groups["street"].Value.Trim();
                var number = match.Groups["num"].Value.Trim();
                return (street, number);
            }

            return (null, null);
        }

        private static string? ExtractDefaultCity()
        {
            var suffix = AppConfig.GEOCODER_DEFAULT_SUFFIX;
            if (string.IsNullOrWhiteSpace(suffix)) return null;
            var parts = suffix.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length > 0 ? parts[0] : null;
        }
    }
}
