using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Backend.Utils
{
    internal sealed class CalleDatasetRow
    {
        public string CalleKey { get; init; } = "";
        public string CalleOriginal { get; init; } = "";
        public int Altura { get; init; }
        public decimal Lat { get; init; }
        public decimal Lng { get; init; }
    }

    public static class CalleDatasetGeocoder
    {
        private static readonly object Sync = new();
        private static readonly TimeSpan ReloadTtl = TimeSpan.FromSeconds(30);
        private static readonly HashSet<string> StreetNoiseTokens =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "avenida", "av", "boulevard", "bulevar", "calle", "pasaje", "ruta", "diagonal",
                "barrio", "cordoba", "argentina", "de", "del", "la", "el", "los", "las",
                "y", "e", "en", "al", "a", "con", "nro", "numero", "altura"
            };

        private static DateTime _lastLoadAttemptUtc = DateTime.MinValue;
        private static DateTime _lastDatasetWriteUtc = DateTime.MinValue;
        private static string? _loadedPath;
        private static IReadOnlyDictionary<string, List<CalleDatasetRow>> _rowsByStreet =
            new Dictionary<string, List<CalleDatasetRow>>(StringComparer.OrdinalIgnoreCase);

        public static GeocodeResult? Geocode(string? query, IReadOnlyList<string>? requiredTokens = null)
        {
            if (!AppConfig.LOCAL_GEOCODER_ENABLED || string.IsNullOrWhiteSpace(query))
                return null;

            if (!EnsureLoaded())
                return null;

            var cleaned = CleanQuery(query);
            if (string.IsNullOrWhiteSpace(cleaned))
                return null;

            var barrioHint = TryExtractBarrioHint(cleaned);

            GeocodeResult? result = null;
            if (TryParseAddress(cleaned, out var addressStreet, out var altura))
            {
                result = GeocodeByStreetAndNumber(addressStreet, altura, barrioHint);
            }

            if (result == null && TryParseIntersection(cleaned, out var street1, out var street2))
            {
                result = GeocodeByIntersection(street1, street2, barrioHint);
            }

            if (result == null)
                return null;

            if (!MatchesRequiredTokens(result, requiredTokens))
                return null;

            return result;
        }

        private static GeocodeResult? GeocodeByStreetAndNumber(string streetText, int targetAltura, string? barrioHint)
        {
            var entries = ResolveStreetEntries(streetText);
            if (entries == null || entries.Count == 0)
                return null;

            var best = Interpolate(entries, targetAltura);
            if (best == null)
                return null;

            if (best.Value.Distance > AppConfig.LOCAL_GEOCODER_MAX_NUMBER_DELTA)
                return null;

            var streetDisplay = string.IsNullOrWhiteSpace(best.Value.StreetDisplay)
                ? ToTitleCase(streetText)
                : best.Value.StreetDisplay;
            var barrioSuffix = string.IsNullOrWhiteSpace(barrioHint) ? "" : $", barrio {barrioHint}";

            return new GeocodeResult
            {
                Lat = best.Value.Lat,
                Lng = best.Value.Lng,
                DisplayName = $"{streetDisplay} {targetAltura}{barrioSuffix}, Cordoba, Argentina"
            };
        }

        private static GeocodeResult? GeocodeByIntersection(string street1Text, string street2Text, string? barrioHint)
        {
            var entries1 = ResolveStreetEntries(street1Text);
            var entries2 = ResolveStreetEntries(street2Text);
            if (entries1 == null || entries1.Count == 0 || entries2 == null || entries2.Count == 0)
                return null;

            var sample1 = Sample(entries1, 260);
            var sample2 = Sample(entries2, 260);
            if (sample1.Count == 0 || sample2.Count == 0)
                return null;

            CalleDatasetRow? bestA = null;
            CalleDatasetRow? bestB = null;
            decimal bestDistance = decimal.MaxValue;

            foreach (var a in sample1)
            {
                foreach (var b in sample2)
                {
                    var dist = HaversineMeters(a.Lat, a.Lng, b.Lat, b.Lng);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestA = a;
                        bestB = b;
                    }
                }
            }

            if (bestA == null || bestB == null)
                return null;

            if (bestDistance > AppConfig.LOCAL_GEOCODER_MAX_INTERSECTION_DISTANCE_METERS)
                return null;

            var lat = Math.Round((bestA.Lat + bestB.Lat) / 2m, 7);
            var lng = Math.Round((bestA.Lng + bestB.Lng) / 2m, 7);
            var s1 = !string.IsNullOrWhiteSpace(bestA.CalleOriginal) ? bestA.CalleOriginal : ToTitleCase(street1Text);
            var s2 = !string.IsNullOrWhiteSpace(bestB.CalleOriginal) ? bestB.CalleOriginal : ToTitleCase(street2Text);
            var barrioSuffix = string.IsNullOrWhiteSpace(barrioHint) ? "" : $", barrio {barrioHint}";

            return new GeocodeResult
            {
                Lat = lat,
                Lng = lng,
                DisplayName = $"Interseccion aproximada: {s1} y {s2}{barrioSuffix}, Cordoba, Argentina"
            };
        }

        private static (decimal Lat, decimal Lng, decimal Distance, string StreetDisplay)? Interpolate(
            List<CalleDatasetRow> entries,
            int targetAltura)
        {
            if (entries.Count == 0)
                return null;

            var index = entries.BinarySearch(
                new CalleDatasetRow { Altura = targetAltura },
                Comparer<CalleDatasetRow>.Create((a, b) => a.Altura.CompareTo(b.Altura))
            );

            if (index >= 0)
            {
                var exact = entries[index];
                return (exact.Lat, exact.Lng, 0m, exact.CalleOriginal);
            }

            index = ~index;
            CalleDatasetRow? lower = index > 0 ? entries[index - 1] : null;
            CalleDatasetRow? upper = index < entries.Count ? entries[index] : null;

            if (lower == null && upper == null)
                return null;

            if (lower == null)
                return (upper!.Lat, upper.Lng, Math.Abs(upper.Altura - targetAltura), upper.CalleOriginal);

            if (upper == null)
                return (lower.Lat, lower.Lng, Math.Abs(targetAltura - lower.Altura), lower.CalleOriginal);

            var lowerDiff = Math.Abs(targetAltura - lower.Altura);
            var upperDiff = Math.Abs(upper.Altura - targetAltura);
            var distance = (decimal)Math.Min(lowerDiff, upperDiff);

            if (upper.Altura == lower.Altura)
            {
                return lowerDiff <= upperDiff
                    ? (lower.Lat, lower.Lng, distance, lower.CalleOriginal)
                    : (upper.Lat, upper.Lng, distance, upper.CalleOriginal);
            }

            var ratio = (targetAltura - lower.Altura) / (double)(upper.Altura - lower.Altura);
            var lat = lower.Lat + (upper.Lat - lower.Lat) * (decimal)ratio;
            var lng = lower.Lng + (upper.Lng - lower.Lng) * (decimal)ratio;
            var display = lowerDiff <= upperDiff ? lower.CalleOriginal : upper.CalleOriginal;
            return (Math.Round(lat, 7), Math.Round(lng, 7), distance, display);
        }

        private static List<CalleDatasetRow>? ResolveStreetEntries(string streetText)
        {
            var key = NormalizeStreetKey(streetText);
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (_rowsByStreet.TryGetValue(key, out var exact))
                return exact;

            // Intento rapido removiendo cola de tokens ("en", "cordoba", etc).
            var quickTrim = TrimTrailingNoiseTokens(key, maxRemovals: 3);
            if (!string.IsNullOrWhiteSpace(quickTrim) && _rowsByStreet.TryGetValue(quickTrim, out var trimmed))
                return trimmed;

            var candidates = FindSimilarStreetKeys(key);
            foreach (var candidate in candidates)
            {
                if (_rowsByStreet.TryGetValue(candidate, out var rows) && rows.Count > 0)
                    return rows;
            }

            return null;
        }

        private static List<string> FindSimilarStreetKeys(string key)
        {
            var tokens = MeaningfulTokens(key);
            var scored = new List<(string Key, int Hits, int Distance)>();
            foreach (var candidate in _rowsByStreet.Keys)
            {
                var hits = tokens.Count == 0
                    ? 0
                    : tokens.Count(t => candidate.Contains(t, StringComparison.OrdinalIgnoreCase));

                var distance = Levenshtein(key, candidate);
                scored.Add((candidate, hits, distance));
            }

            if (scored.Count == 0)
                return new List<string>();

            var strict = scored
                .OrderByDescending(x => x.Hits)
                .ThenBy(x => x.Distance)
                .Take(3)
                .Where(x =>
                {
                    var maxDistance = Math.Max(3, key.Length / 4);
                    return x.Distance <= maxDistance || x.Hits >= Math.Min(2, tokens.Count);
                })
                .Select(x => x.Key)
                .ToList();

            if (strict.Count > 0)
                return strict;

            // Fallback puramente por distancia para typos cortos (ej: "rondo" -> "rondeau").
            var relaxedMaxDistance = Math.Max(2, key.Length <= 8 ? 3 : key.Length / 5);
            return scored
                .OrderBy(x => x.Distance)
                .ThenByDescending(x => x.Hits)
                .Take(5)
                .Where(x => x.Distance <= relaxedMaxDistance)
                .Select(x => x.Key)
                .ToList();
        }

        private static bool MatchesRequiredTokens(GeocodeResult result, IReadOnlyList<string>? requiredTokens)
        {
            if (requiredTokens == null || requiredTokens.Count == 0)
                return true;

            var normalizedDisplay = AppConfig.NormalizeKey(result.DisplayName ?? "");
            if (string.IsNullOrWhiteSpace(normalizedDisplay))
                return false;
            var displayTokens = normalizedDisplay
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var token in requiredTokens)
            {
                var normalizedToken = AppConfig.NormalizeKey(token);
                if (string.IsNullOrWhiteSpace(normalizedToken))
                    continue;
                if (normalizedToken.Length <= 2)
                    continue;
                if (!ContainsTokenLoosely(normalizedDisplay, normalizedToken, displayTokens))
                    return false;
            }

            return true;
        }

        private static bool ContainsTokenLoosely(string normalizedDisplay, string requiredToken, IReadOnlyList<string> displayTokens)
        {
            if (normalizedDisplay.Contains(requiredToken, StringComparison.OrdinalIgnoreCase))
                return true;

            var requiredParts = requiredToken
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(p => p.Length >= 3 && !StreetNoiseTokens.Contains(p))
                .ToList();

            if (requiredParts.Count == 0)
                return true;

            foreach (var part in requiredParts)
            {
                var maxDistance = part.Length >= 8 ? 2 : 1;
                var matched = displayTokens.Any(displayToken =>
                {
                    if (string.IsNullOrWhiteSpace(displayToken) || displayToken.Length < 3)
                        return false;
                    if (StreetNoiseTokens.Contains(displayToken))
                        return false;
                    if (displayToken.Equals(part, StringComparison.OrdinalIgnoreCase))
                        return true;
                    return Levenshtein(part, displayToken) <= maxDistance;
                });

                if (!matched)
                    return false;
            }

            return true;
        }

        private static bool TryParseAddress(string text, out string street, out int altura)
        {
            street = "";
            altura = 0;

            var withConnector = Regex.Match(
                text,
                @"\b(?<street>[a-z0-9\s]{4,120}?)\s+(?:al|altura|numero|nro|n)\s*(?<num>\d{1,5})\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (withConnector.Success)
            {
                street = withConnector.Groups["street"].Value.Trim();
                return int.TryParse(withConnector.Groups["num"].Value, out altura);
            }

            var simple = Regex.Match(
                text,
                @"\b(?<street>(?:avenida|av|boulevard|bulevar|calle|pasaje|ruta|diagonal)\s+[a-z0-9\s]{3,120}?)\s+(?<num>\d{1,5})\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (simple.Success)
            {
                street = simple.Groups["street"].Value.Trim();
                return int.TryParse(simple.Groups["num"].Value, out altura);
            }

            return false;
        }

        private static bool TryParseIntersection(string text, out string street1, out string street2)
        {
            street1 = "";
            street2 = "";
            var match = Regex.Match(
                text,
                @"\b(?<s1>[a-z0-9\s]{3,90}?)\s+(?:y|e|&|con)\s+(?<s2>[a-z0-9\s]{3,90}?)(?=\s*(?:,|$|\bbarrio\b))",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (!match.Success)
                return false;

            street1 = match.Groups["s1"].Value.Trim();
            street2 = match.Groups["s2"].Value.Trim();
            if (street1.Length < 3 || street2.Length < 3)
                return false;
            return true;
        }

        private static string CleanQuery(string query)
        {
            var normalized = AppConfig.NormalizeKey(query);
            normalized = Regex.Replace(
                normalized,
                @"\b(hoy|ayer|anteayer|a las|siendo las|hora|ocurrio|ocurrio un|hubo|robo|arrebato|hurto|se produjo|se reporto)\b.*$",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            return normalized;
        }

        private static string? TryExtractBarrioHint(string normalizedQuery)
        {
            if (string.IsNullOrWhiteSpace(normalizedQuery))
                return null;

            var match = Regex.Match(
                normalizedQuery,
                @"\bbarrio\s+(?<name>[a-z0-9\s]{3,60})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (!match.Success)
                return null;

            var value = match.Groups["name"].Value;
            value = Regex.Replace(
                value,
                @"\b(cordoba|argentina|hoy|ayer|a las|siendo las|hora|ocurrio|hubo|robo|arrebato|hurto|se produjo|se reporto)\b.*$",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            value = Regex.Replace(value, @"\s+", " ").Trim().Trim(',', '.', ';', ':');

            if (string.IsNullOrWhiteSpace(value))
                return null;

            return ToTitleCase(value);
        }

        private static string NormalizeStreetKey(string input)
        {
            var key = AppConfig.NormalizeKey(input ?? "");
            key = Regex.Replace(
                key,
                @"^(avenida|av|boulevard|bulevar|calle|pasaje|ruta|diagonal)\s+",
                "",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            key = Regex.Replace(
                key,
                @"\b(barrio|cordoba|argentina|frente|hospital|plaza|parque|interseccion|aproximada)\b.*$",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            key = Regex.Replace(key, @"\s+", " ").Trim();
            return TrimTrailingNoiseTokens(key, maxRemovals: 4);
        }

        private static string TrimTrailingNoiseTokens(string key, int maxRemovals)
        {
            var tokens = key.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            var removed = 0;
            while (tokens.Count > 0 && removed < maxRemovals && StreetNoiseTokens.Contains(tokens[^1]))
            {
                tokens.RemoveAt(tokens.Count - 1);
                removed++;
            }
            return string.Join(' ', tokens).Trim();
        }

        private static List<string> MeaningfulTokens(string key)
        {
            return key.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 3 && !StreetNoiseTokens.Contains(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<CalleDatasetRow> Sample(List<CalleDatasetRow> entries, int maxPoints)
        {
            if (entries.Count <= maxPoints)
                return entries;

            var result = new List<CalleDatasetRow>(maxPoints);
            var step = (double)entries.Count / maxPoints;
            for (var i = 0; i < maxPoints; i++)
            {
                var idx = (int)Math.Round(i * step);
                if (idx >= entries.Count) idx = entries.Count - 1;
                result.Add(entries[idx]);
            }
            return result;
        }

        private static decimal HaversineMeters(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const double r = 6371000d;
            var dLat = ToRadians((double)(lat2 - lat1));
            var dLon = ToRadians((double)(lon2 - lon1));
            var a = Math.Sin(dLat / 2d) * Math.Sin(dLat / 2d)
                    + Math.Cos(ToRadians((double)lat1))
                    * Math.Cos(ToRadians((double)lat2))
                    * Math.Sin(dLon / 2d)
                    * Math.Sin(dLon / 2d);
            var c = 2d * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1d - a));
            return (decimal)(r * c);
        }

        private static double ToRadians(double value) => value * Math.PI / 180d;

        private static bool EnsureLoaded()
        {
            var path = ResolveDatasetPath();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            var writeUtc = File.GetLastWriteTimeUtc(path);
            var now = DateTime.UtcNow;

            if (_rowsByStreet.Count > 0 &&
                string.Equals(path, _loadedPath, StringComparison.OrdinalIgnoreCase) &&
                writeUtc == _lastDatasetWriteUtc &&
                (now - _lastLoadAttemptUtc) < ReloadTtl)
            {
                return true;
            }

            lock (Sync)
            {
                path = ResolveDatasetPath();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return false;

                writeUtc = File.GetLastWriteTimeUtc(path);
                now = DateTime.UtcNow;
                if (_rowsByStreet.Count > 0 &&
                    string.Equals(path, _loadedPath, StringComparison.OrdinalIgnoreCase) &&
                    writeUtc == _lastDatasetWriteUtc &&
                    (now - _lastLoadAttemptUtc) < ReloadTtl)
                {
                    return true;
                }

                _lastLoadAttemptUtc = now;
                var loaded = LoadDataset(path);
                if (loaded.Count == 0)
                    return false;

                _rowsByStreet = loaded;
                _loadedPath = path;
                _lastDatasetWriteUtc = writeUtc;
                Console.WriteLine($"[LocalGeocoder] Dataset cargado: {_rowsByStreet.Count} calles ({path})");
                return true;
            }
        }

        private static IReadOnlyDictionary<string, List<CalleDatasetRow>> LoadDataset(string path)
        {
            var byStreet = new Dictionary<string, List<CalleDatasetRow>>(StringComparer.OrdinalIgnoreCase);
            using var reader = new StreamReader(path, Encoding.UTF8);
            var header = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(header))
                return byStreet;

            var columns = ParseCsvLine(header);
            var idxStreet = columns.FindIndex(c => string.Equals(c, "calle_normalizada", StringComparison.OrdinalIgnoreCase));
            var idxAltura = columns.FindIndex(c => string.Equals(c, "altura", StringComparison.OrdinalIgnoreCase));
            var idxOriginal = columns.FindIndex(c => string.Equals(c, "calle_original", StringComparison.OrdinalIgnoreCase));
            var idxLat = columns.FindIndex(c => string.Equals(c, "lat", StringComparison.OrdinalIgnoreCase));
            var idxLng = columns.FindIndex(c => string.Equals(c, "lng", StringComparison.OrdinalIgnoreCase));

            if (idxStreet < 0 || idxAltura < 0 || idxLat < 0 || idxLng < 0)
                return byStreet;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                var row = ParseCsvLine(line);
                if (row.Count <= Math.Max(Math.Max(idxStreet, idxAltura), Math.Max(idxLat, idxLng)))
                    continue;

                if (!int.TryParse(row[idxAltura], NumberStyles.Integer, CultureInfo.InvariantCulture, out var altura))
                    continue;
                if (!decimal.TryParse(row[idxLat], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                    continue;
                if (!decimal.TryParse(row[idxLng], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                    continue;

                var streetRaw = row[idxStreet];
                var streetKey = NormalizeStreetKey(streetRaw);
                if (string.IsNullOrWhiteSpace(streetKey))
                    continue;

                var streetOriginal = idxOriginal >= 0 && idxOriginal < row.Count
                    ? row[idxOriginal]
                    : streetRaw;

                var item = new CalleDatasetRow
                {
                    CalleKey = streetKey,
                    CalleOriginal = ToTitleCase(streetOriginal),
                    Altura = altura,
                    Lat = lat,
                    Lng = lng
                };

                if (!byStreet.TryGetValue(streetKey, out var list))
                {
                    list = new List<CalleDatasetRow>();
                    byStreet[streetKey] = list;
                }
                list.Add(item);
            }

            foreach (var pair in byStreet)
                pair.Value.Sort((a, b) => a.Altura.CompareTo(b.Altura));

            return byStreet;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null)
                return result;

            var sb = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString().Trim());
                    sb.Clear();
                    continue;
                }

                sb.Append(c);
            }

            result.Add(sb.ToString().Trim());
            return result;
        }

        private static int Levenshtein(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
                return b?.Length ?? 0;
            if (string.IsNullOrEmpty(b))
                return a.Length;

            var d = new int[a.Length + 1, b.Length + 1];
            for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (var j = 0; j <= b.Length; j++) d[0, j] = j;

            for (var i = 1; i <= a.Length; i++)
            {
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[a.Length, b.Length];
        }

        private static string? ResolveDatasetPath()
        {
            var raw = AppConfig.LOCAL_GEOCODER_CSV_PATH?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            if (Path.IsPathRooted(raw))
                return raw;

            var cwdPath = Path.GetFullPath(raw, Directory.GetCurrentDirectory());
            if (File.Exists(cwdPath))
                return cwdPath;

            var appBasePath = Path.GetFullPath(raw, AppContext.BaseDirectory);
            if (File.Exists(appBasePath))
                return appBasePath;

            // Fallbacks habituales cuando el backend se ejecuta desde la raiz del repo.
            var repoCandidateA = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "systems/mapeo/cordoba-dataset/out/direcciones_mapeo.csv"));
            if (File.Exists(repoCandidateA))
                return repoCandidateA;

            var repoCandidateB = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "cordoba-dataset/out/direcciones_mapeo.csv"));
            if (File.Exists(repoCandidateB))
                return repoCandidateB;

            return cwdPath;
        }

        private static string ToTitleCase(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            var cleaned = Regex.Replace(value, @"\s+", " ").Trim();
            return CultureInfo.GetCultureInfo("es-AR").TextInfo.ToTitleCase(cleaned.ToLowerInvariant());
        }
    }
}
