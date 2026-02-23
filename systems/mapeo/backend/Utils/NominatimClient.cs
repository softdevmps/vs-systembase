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

            result = await TryStreetOnlyFallbackAsync(queryText, httpClient, ct);
            if (result != null)
                return result;

            var simplified = SimplifyQuery(queryText);
            if (!string.IsNullOrWhiteSpace(simplified) && !string.Equals(simplified, queryText, StringComparison.OrdinalIgnoreCase))
            {
                result = await TryGeocodeAsync(simplified, httpClient, ct);
                if (result != null)
                    return result;

                result = await TryStreetOnlyFallbackAsync(simplified, httpClient, ct);
                if (result != null)
                    return result;
            }

            var intersectionFallback = await TryIntersectionFallbackAsync(queryText, httpClient, ct);
            if (intersectionFallback != null)
                return intersectionFallback;

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

            result = await TryStreetOnlyFallbackAsync(queryText, httpClient, ct, tokens);
            if (result != null)
                return result;

            var simplified = SimplifyQuery(queryText);
            if (!string.IsNullOrWhiteSpace(simplified) && !string.Equals(simplified, queryText, StringComparison.OrdinalIgnoreCase))
            {
                result = await TryGeocodeAsync(simplified, httpClient, ct, tokens, 5);
                if (result != null)
                    return result;

                result = await TryStreetOnlyFallbackAsync(simplified, httpClient, ct, tokens);
                if (result != null)
                    return result;
            }

            var intersectionFallback = await TryIntersectionFallbackAsync(queryText, httpClient, ct);
            if (intersectionFallback != null)
                return intersectionFallback;

            return null;
        }

        private static async Task<GeocodeResult?> TryIntersectionFallbackAsync(
            string queryText,
            HttpClient httpClient,
            CancellationToken ct)
        {
            var (street1, street2, barrio) = ParseIntersectionQuery(queryText);
            if (string.IsNullOrWhiteSpace(street1) || string.IsNullOrWhiteSpace(street2))
                return null;

            var c1 = await SearchRoadCandidatesAsync(street1, barrio, httpClient, ct);
            var c2 = await SearchRoadCandidatesAsync(street2, barrio, httpClient, ct);
            if (c1.Count == 0 || c2.Count == 0)
                return null;

            GeocodeResult? bestA = null;
            GeocodeResult? bestB = null;
            decimal bestDist = decimal.MaxValue;

            foreach (var a in c1)
            {
                foreach (var b in c2)
                {
                    var d = HaversineMeters(a.Lat, a.Lng, b.Lat, b.Lng);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestA = a;
                        bestB = b;
                    }
                }
            }

            if (bestA == null || bestB == null)
                return null;

            // Si las dos trazas estan demasiado lejos, no forzamos coordenada.
            if (bestDist > 2000m)
                return null;

            var lat = Math.Round((bestA.Lat + bestB.Lat) / 2m, 7);
            var lng = Math.Round((bestA.Lng + bestB.Lng) / 2m, 7);
            var barrioSuffix = string.IsNullOrWhiteSpace(barrio) ? "" : $", barrio {barrio}";
            return new GeocodeResult
            {
                Lat = lat,
                Lng = lng,
                DisplayName = $"Interseccion aproximada: {street1} y {street2}{barrioSuffix}, Cordoba, Argentina"
            };
        }

        private static async Task<List<GeocodeResult>> SearchRoadCandidatesAsync(
            string street,
            string? barrio,
            HttpClient httpClient,
            CancellationToken ct)
        {
            var baseUrl = AppConfig.GEOCODER_URL;
            if (string.IsNullOrWhiteSpace(baseUrl))
                return new List<GeocodeResult>();

            var normalizedStreet = NormalizeStreetForQuery(street);
            var city = ExtractDefaultCity();
            var query = string.IsNullOrWhiteSpace(barrio)
                ? $"{normalizedStreet}, {city}"
                : $"{normalizedStreet}, barrio {barrio}, {city}";

            var url = $"{baseUrl}?format=json&limit=8&addressdetails=1&q={Uri.EscapeDataString(query)}";
            if (!string.IsNullOrWhiteSpace(AppConfig.GEOCODER_COUNTRY_CODES))
                url += $"&countrycodes={Uri.EscapeDataString(AppConfig.GEOCODER_COUNTRY_CODES)}";
            if (!string.IsNullOrWhiteSpace(AppConfig.GEOCODER_LANGUAGE))
                url += $"&accept-language={Uri.EscapeDataString(AppConfig.GEOCODER_LANGUAGE)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("SystemBase-Mapeo/1.0");

            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return new List<GeocodeResult>();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return new List<GeocodeResult>();

            var list = new List<GeocodeResult>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("lat", out var latProp) || !item.TryGetProperty("lon", out var lonProp))
                    continue;

                if (!decimal.TryParse(latProp.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat))
                    continue;
                if (!decimal.TryParse(lonProp.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lng))
                    continue;

                var addresstype = item.TryGetProperty("addresstype", out var at) ? at.GetString() : null;
                if (!string.IsNullOrWhiteSpace(addresstype) &&
                    !string.Equals(addresstype, "road", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(addresstype, "residential", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                list.Add(new GeocodeResult
                {
                    Lat = lat,
                    Lng = lng,
                    DisplayName = item.TryGetProperty("display_name", out var nameProp) ? nameProp.GetString() : null
                });
            }

            return list;
        }

        private static (string? street1, string? street2, string? barrio) ParseIntersectionQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return (null, null, null);

            var cleaned = LocationNormalizer.CanonicalizeStreetName(query);
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            var match = Regex.Match(cleaned,
                @"(?<s1>(?:avenida|boulevard|bulevar|calle|pasaje|ruta|diagonal)\s+[\p{L}\p{N}\s\.]{3,80}|[\p{L}][\p{L}\p{N}\s\.]{3,80})\s+(?:y|e|&|con)\s+(?<s2>(?:avenida|boulevard|bulevar|calle|pasaje|ruta|diagonal)\s+[\p{L}\p{N}\s\.]{3,80}|[\p{L}][\p{L}\p{N}\s\.]{3,80})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (!match.Success)
                return (null, null, null);

            var street1 = match.Groups["s1"].Value.Trim();
            var street2 = match.Groups["s2"].Value.Trim();
            street1 = Regex.Replace(street1, @"\b(hubo|robo|arrebato|hurto|a las|hora)\b.*$", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Trim();
            street2 = Regex.Replace(street2, @"\b(hubo|robo|arrebato|hurto|a las|hora)\b.*$", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Trim();
            street1 = NormalizeStreetForQuery(street1);
            street2 = NormalizeStreetForQuery(street2);

            var barrioMatch = Regex.Match(cleaned, @"\bbarrio\s+([\p{L}\p{N}\s]{3,40})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var barrio = barrioMatch.Success ? barrioMatch.Groups[1].Value.Trim() : null;
            barrio = NormalizeBarrioForQuery(barrio);

            return (street1, street2, barrio);
        }

        private static async Task<GeocodeResult?> TryStreetOnlyFallbackAsync(
            string queryText,
            HttpClient httpClient,
            CancellationToken ct,
            IReadOnlyList<string>? requiredTokens = null)
        {
            var (street, _) = ExtractStreetAndNumber(queryText);
            if (string.IsNullOrWhiteSpace(street))
                return null;

            var barrio = NormalizeBarrioForQuery(ExtractBarrioFromQuery(queryText));
            var city = ExtractDefaultCity();
            var normalizedStreet = NormalizeStreetForQuery(street);

            var queries = new List<string>();
            if (!string.IsNullOrWhiteSpace(barrio))
                queries.Add($"{normalizedStreet}, barrio {barrio}, {city}");
            queries.Add($"{normalizedStreet}, {city}");

            var coreStreet = Regex.Replace(normalizedStreet, @"^(avenida|boulevard|bulevar|calle|pasaje|ruta|diagonal)\s+", "",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Trim();
            if (!string.IsNullOrWhiteSpace(coreStreet))
            {
                if (!string.IsNullOrWhiteSpace(barrio))
                    queries.Add($"{coreStreet}, barrio {barrio}, {city}");
                queries.Add($"{coreStreet}, {city}");
            }

            foreach (var query in queries.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var result = await TryGeocodeAsync(query, httpClient, ct, requiredTokens, 5);
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
            var displayRaw = "";
            if (candidate.TryGetProperty("display_name", out var nameProp))
            {
                displayRaw = nameProp.GetString() ?? "";
                display = NormalizeToken(nameProp.GetString() ?? "");
            }

            if (requiredTokens != null && requiredTokens.Count > 0)
                score += requiredTokens.Count(token => display.Contains(token));

            if (!string.IsNullOrWhiteSpace(requiredNumber))
            {
                var houseNumber = GetAddressField(candidate, "house_number");
                var expectedNumber = ParseFirstNumber(requiredNumber);
                var candidateNumber = ParseFirstNumber(houseNumber) ?? ParseFirstNumber(displayRaw);

                if (expectedNumber.HasValue && candidateNumber.HasValue)
                {
                    var diff = Math.Abs(expectedNumber.Value - candidateNumber.Value);
                    if (diff == 0) score += 6;
                    else if (diff <= 50) score += 4;
                    else if (diff <= 150) score += 2;
                    else if (diff <= 300) score += 1;
                    else score -= 2;
                }
                else if (!string.IsNullOrWhiteSpace(display) && display.Contains(requiredNumber))
                {
                    score += 2;
                }
                else
                {
                    score -= 3;
                }
            }

            if (!string.IsNullOrWhiteSpace(requiredBarrio))
            {
                var suburb = GetAddressField(candidate, "suburb")
                             ?? GetAddressField(candidate, "neighbourhood")
                             ?? GetAddressField(candidate, "city_district");
                var suburbNorm = NormalizeToken(suburb ?? "");
                if (!string.IsNullOrWhiteSpace(suburbNorm) && suburbNorm.Equals(requiredBarrio, StringComparison.OrdinalIgnoreCase))
                    score += 4;
                else if (!string.IsNullOrWhiteSpace(suburbNorm) &&
                    (suburbNorm.Contains(requiredBarrio) || requiredBarrio.Contains(suburbNorm)))
                    score += 2;
                else if (!string.IsNullOrWhiteSpace(display) && display.Contains(requiredBarrio))
                    score += 1;
                else
                    score -= 1;
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

        private static int? ParseFirstNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var match = Regex.Match(value, @"\d{1,5}");
            if (!match.Success)
                return null;

            if (int.TryParse(match.Value, out var result))
                return result;

            return null;
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
            var match = Regex.Match(query, @"\bbarrio\s+([\p{L}\p{N}\s]{3,60})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success) return null;
            var barrio = match.Groups[1].Value;
            var normalized = NormalizeToken(barrio);
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static (string? street, string? number) ExtractStreetAndNumber(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return (null, null);
            var cleaned = Regex.Replace(query, @"\bbarrio\s+[\p{L}\p{N}\s]{3,60}", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bcordoba\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bargentina\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\b(?:la\s+)?esquina\s+de\s+", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"(?<=\d)(?=[\p{L}])", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"(?<=[\p{L}])(?=\d)", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

            var pattern = @"\b(?<street>(?:avenida|av\.?|boulevard|bulevar|calle|pasaje|ruta|diagonal)\s+[\p{L}0-9\s\.]{3,80}?)\s*(?:,\s*)?(?:al\s+)?(?<num>\d{1,5})\b";
            var match = Regex.Match(cleaned, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                var street = match.Groups["street"].Value.Trim();
                var number = match.Groups["num"].Value.Trim();
                street = NormalizeStreetForQuery(street);
                return (street, number);
            }

            var altPattern = @"\b(?<street>[\p{L}0-9\s\.]{3,80}?)\s*(?:,\s*)?(?:al|nro\.?|numero)\s+(?<num>\d{1,5})\b";
            match = Regex.Match(cleaned, altPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                var street = match.Groups["street"].Value.Trim();
                var number = match.Groups["num"].Value.Trim();
                street = NormalizeStreetForQuery(street);
                return (street, number);
            }

            var directPattern = @"\b(?<street>(?:avenida|av\.?|boulevard|bulevar|calle|pasaje|ruta|diagonal)\s+[\p{L}0-9\s\.]{3,80}|[\p{L}][\p{L}\s\.]{2,80})\s+(?<num>\d{1,5})\b";
            match = Regex.Match(cleaned, directPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                var street = match.Groups["street"].Value.Trim();
                var number = match.Groups["num"].Value.Trim();
                if (!Regex.IsMatch(street, @"\b(enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre)\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    street = NormalizeStreetForQuery(street);
                    return (street, number);
                }
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

        private static string NormalizeStreetForQuery(string street)
        {
            if (string.IsNullOrWhiteSpace(street))
                return street;

            var cleaned = Regex.Replace(street, @"\s+", " ").Trim();
            cleaned = Regex.Replace(cleaned, @"\bav\.?\b", "avenida",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            var typeMatch = Regex.Match(cleaned,
                @"^(?<type>avenida|boulevard|bulevar|calle|pasaje|ruta|diagonal)\s+(?<name>.+)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (typeMatch.Success)
            {
                var type = typeMatch.Groups["type"].Value.Trim().ToLowerInvariant();
                if (type == "bulevar")
                    type = "boulevard";

                var name = typeMatch.Groups["name"].Value.Trim();
                name = LocationNormalizer.CanonicalizeStreetName(name);
                return $"{type} {name}".Trim();
            }

            return LocationNormalizer.CanonicalizeStreetName(cleaned);
        }

        private static string? NormalizeBarrioForQuery(string? barrio)
        {
            if (string.IsNullOrWhiteSpace(barrio))
                return null;

            var normalized = Regex.Replace(barrio, @"\s+", " ").Trim().ToLowerInvariant();
            if (normalized == "nueva")
                return "nueva cordoba";
            if (normalized == "general")
                return "general paz";
            if (normalized == "alverdi")
                return "alberdi";

            return normalized;
        }

        private static decimal HaversineMeters(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
        {
            const double earthRadiusMeters = 6371000d;

            var dLat = ToRadians((double)(lat2 - lat1));
            var dLng = ToRadians((double)(lng2 - lng1));
            var radLat1 = ToRadians((double)lat1);
            var radLat2 = ToRadians((double)lat2);

            var sinLat = Math.Sin(dLat / 2d);
            var sinLng = Math.Sin(dLng / 2d);
            var a = sinLat * sinLat + Math.Cos(radLat1) * Math.Cos(radLat2) * sinLng * sinLng;
            var c = 2d * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1d - a));
            return (decimal)(earthRadiusMeters * c);
        }

        private static double ToRadians(double value)
        {
            return value * (Math.PI / 180d);
        }
    }
}
