using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Backend.Utils
{
    public sealed class LocationSignals
    {
        public string? Barrio { get; init; }
        public string? Poi { get; init; }
        public string? Calle1 { get; init; }
        public string? Calle2 { get; init; }
        public string? Calle1Core { get; init; }
        public string? Calle2Core { get; init; }
        public bool HasIntersection => !string.IsNullOrWhiteSpace(Calle1) && !string.IsNullOrWhiteSpace(Calle2);
    }

    public sealed class LocationParseResult
    {
        public string? Raw { get; init; }
        public string? Normalized { get; init; }
        public string? DisplayText { get; init; }
        public LocationSignals Signals { get; init; } = new();
        public List<string> Candidates { get; init; } = new();
    }

    public static class LocationNormalizer
    {
        private static readonly IReadOnlyDictionary<string, string> CommonReplacements =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["yurigoyen"] = "yrigoyen",
                ["yirigoyen"] = "yrigoyen",
                ["yrigollen"] = "yrigoyen",
                ["velez sarfield"] = "velez sarsfield",
                ["vele sarfield"] = "velez sarsfield",
                ["velez arfield"] = "velez sarsfield",
                ["velez arfiel"] = "velez sarsfield",
                ["belsarfield"] = "velez sarsfield",
                ["belsarfiel"] = "velez sarsfield",
                ["belsarfil"] = "velez sarsfield",
                ["esbelzarfil"] = "velez sarsfield",
                ["esbelzarfield"] = "velez sarsfield",
                ["avenilla"] = "avenida",
                ["avanilla"] = "avenida",
                ["huemes"] = "guemes",
                ["uemes"] = "guemes",
                ["wemes"] = "guemes",
                ["nueva corda"] = "nueva cordoba",
                ["espana"] = "espana"
            };

        private static readonly string[] StreetTypes =
        {
            "avenida", "av", "av.", "boulevard", "bulevar", "blvd", "calle", "pasaje", "ruta", "diagonal"
        };

        private static readonly string[] PoiKeywords =
        {
            "plaza", "parque", "hospital", "sanatorio", "terminal", "estacion",
            "banco", "comisaria", "escuela", "universidad", "shopping", "mercado",
            "catedral", "municipalidad"
        };

        public static LocationParseResult Build(string? rawText, string? extracted, string? llmLocation = null)
        {
            var seed = FirstNonEmpty(extracted, llmLocation, rawText);
            if (string.IsNullOrWhiteSpace(seed))
            {
                return new LocationParseResult
                {
                    Raw = rawText,
                    Normalized = null,
                    DisplayText = null,
                    Signals = new LocationSignals(),
                    Candidates = new List<string>()
                };
            }

            var searchText = string.Join(" ", new[]
            {
                extracted,
                llmLocation,
                rawText
            }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

            var normalizedSeed = NormalizeForMatch(seed);
            var normalizedSearch = NormalizeForMatch(searchText);

            var barrio = ExtractBarrio(normalizedSearch);
            var poi = ExtractPoi(normalizedSearch);
            var searchForIntersection = RemoveBarrioPhrase(normalizedSearch);
            var (calle1, calle2) = ExtractIntersection(searchForIntersection, barrio);
            var calle1Core = StripStreetType(calle1 ?? "");
            var calle2Core = StripStreetType(calle2 ?? "");

            var signals = new LocationSignals
            {
                Barrio = barrio,
                Poi = poi,
                Calle1 = calle1,
                Calle2 = calle2,
                Calle1Core = string.IsNullOrWhiteSpace(calle1Core) ? null : calle1Core,
                Calle2Core = string.IsNullOrWhiteSpace(calle2Core) ? null : calle2Core
            };

            var candidates = BuildCandidates(normalizedSeed, signals);
            var display = BuildDisplayText(normalizedSeed, signals);

            return new LocationParseResult
            {
                Raw = seed,
                Normalized = normalizedSeed,
                DisplayText = display,
                Signals = signals,
                Candidates = candidates
            };
        }

        public static decimal ScoreConfidence(decimal? baseConfidence, LocationParseResult location, GeocodeResult? geocode)
        {
            var score = baseConfidence ?? 0.5m;
            if (geocode != null)
                score = Math.Max(score, 0.78m);

            if (location.Signals.HasIntersection) score += 0.12m;
            if (!string.IsNullOrWhiteSpace(location.Signals.Poi)) score += 0.08m;
            if (!string.IsNullOrWhiteSpace(location.Signals.Barrio)) score += 0.05m;

            if (!string.IsNullOrWhiteSpace(geocode?.DisplayName))
            {
                var display = NormalizeForMatch(geocode.DisplayName ?? "");
                if (!string.IsNullOrWhiteSpace(location.Signals.Barrio) &&
                    display.Contains(NormalizeForMatch(location.Signals.Barrio)))
                {
                    score += 0.05m;
                }

                if (location.Signals.HasIntersection)
                {
                    var s1 = NormalizeForMatch(location.Signals.Calle1 ?? "");
                    var s2 = NormalizeForMatch(location.Signals.Calle2 ?? "");
                    if (!string.IsNullOrWhiteSpace(s1) && display.Contains(s1)) score += 0.05m;
                    if (!string.IsNullOrWhiteSpace(s2) && display.Contains(s2)) score += 0.05m;
                }
            }

            return Clamp(score, 0.4m, 0.97m);
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return null;
        }

        private static List<string> BuildCandidates(string normalizedSeed, LocationSignals signals)
        {
            var candidates = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string? value)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                var cleaned = value.Trim().Trim(',', '.', ';', ':');
                if (cleaned.Length == 0) return;
                if (seen.Add(cleaned))
                    candidates.Add(cleaned);
            }

            if (signals.HasIntersection)
            {
                var intersection = $"{signals.Calle1} y {signals.Calle2}".Trim();
                var swapped = $"{signals.Calle2} y {signals.Calle1}".Trim();
                var intersectionAmp = $"{signals.Calle1} & {signals.Calle2}".Trim();
                var swappedAmp = $"{signals.Calle2} & {signals.Calle1}".Trim();
                var intersectionEsquina = $"esquina de {signals.Calle1} y {signals.Calle2}".Trim();
                var swappedEsquina = $"esquina de {signals.Calle2} y {signals.Calle1}".Trim();
                var intersectionCon = $"{signals.Calle1} con {signals.Calle2}".Trim();
                var swappedCon = $"{signals.Calle2} con {signals.Calle1}".Trim();
                var name1 = StripStreetType(signals.Calle1 ?? "");
                var name2 = StripStreetType(signals.Calle2 ?? "");
                var namesOnly = $"{name1} y {name2}".Trim();
                var namesOnlySwapped = $"{name2} y {name1}".Trim();

                var hasType1 = HasStreetType(signals.Calle1);
                var hasType2 = HasStreetType(signals.Calle2);
                var calleVariant = $"calle {name1} y calle {name2}".Trim();
                var avenidaVariant = $"avenida {name1} y avenida {name2}".Trim();
                var mixVariant1 = $"avenida {name1} y calle {name2}".Trim();
                var mixVariant2 = $"calle {name1} y avenida {name2}".Trim();

                if (!string.IsNullOrWhiteSpace(signals.Barrio))
                {
                    Add($"{intersection}, barrio {signals.Barrio}");
                    Add($"{swapped}, barrio {signals.Barrio}");
                    Add($"{intersectionAmp}, barrio {signals.Barrio}");
                    Add($"{swappedAmp}, barrio {signals.Barrio}");
                    Add($"{intersectionEsquina}, barrio {signals.Barrio}");
                    Add($"{swappedEsquina}, barrio {signals.Barrio}");
                    Add($"{intersectionCon}, barrio {signals.Barrio}");
                    Add($"{swappedCon}, barrio {signals.Barrio}");
                    if (!hasType1 && !hasType2)
                    {
                        Add($"{calleVariant}, barrio {signals.Barrio}");
                        Add($"{avenidaVariant}, barrio {signals.Barrio}");
                        Add($"{mixVariant1}, barrio {signals.Barrio}");
                        Add($"{mixVariant2}, barrio {signals.Barrio}");
                    }
                    if (!string.IsNullOrWhiteSpace(name1) && !string.IsNullOrWhiteSpace(name2))
                    {
                        Add($"{namesOnly}, barrio {signals.Barrio}");
                        Add($"{namesOnlySwapped}, barrio {signals.Barrio}");
                    }
                }

                Add(intersection);
                Add(swapped);
                Add(intersectionAmp);
                Add(swappedAmp);
                Add(intersectionEsquina);
                Add(swappedEsquina);
                Add(intersectionCon);
                Add(swappedCon);
                if (!hasType1 && !hasType2)
                {
                    Add(calleVariant);
                    Add(avenidaVariant);
                    Add(mixVariant1);
                    Add(mixVariant2);
                }
                if (!string.IsNullOrWhiteSpace(name1) && !string.IsNullOrWhiteSpace(name2))
                {
                    Add(namesOnly);
                    Add(namesOnlySwapped);
                }
            }

            if (!string.IsNullOrWhiteSpace(signals.Poi))
            {
                if (!string.IsNullOrWhiteSpace(signals.Barrio))
                    Add($"{signals.Poi}, barrio {signals.Barrio}");
                Add(signals.Poi);
            }

            Add(normalizedSeed);

            if (!string.IsNullOrWhiteSpace(signals.Barrio) &&
                !normalizedSeed.Contains(signals.Barrio, StringComparison.OrdinalIgnoreCase))
            {
                Add($"{normalizedSeed}, barrio {signals.Barrio}");
            }

            return candidates;
        }

        private static string BuildDisplayText(string normalizedSeed, LocationSignals signals)
        {
            string baseText = normalizedSeed;
            if (signals.HasIntersection)
            {
                baseText = $"{signals.Calle1} y {signals.Calle2}".Trim();
                if (!string.IsNullOrWhiteSpace(signals.Barrio))
                    baseText = $"{baseText}, barrio {signals.Barrio}";
            }
            else if (!string.IsNullOrWhiteSpace(signals.Poi))
            {
                baseText = signals.Poi!;
                if (!string.IsNullOrWhiteSpace(signals.Barrio))
                    baseText = $"{baseText}, barrio {signals.Barrio}";
            }
            return ToTitleCase(baseText);
        }

        private static string NormalizeForMatch(string text)
        {
            var lower = text.ToLowerInvariant();
            var withoutAccents = RemoveDiacritics(lower);
            var cleaned = Regex.Replace(withoutAccents, @"[^\p{L}\p{N}\s,\.]", " ");
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            cleaned = ApplyReplacements(cleaned);
            cleaned = NormalizeImplicitIntersection(cleaned);
            return cleaned;
        }

        private static string ApplyReplacements(string text)
        {
            var fixedText = text;
            foreach (var kv in CommonReplacements)
            {
                var key = kv.Key.ToLowerInvariant();
                var value = kv.Value.ToLowerInvariant();
                if (key.Contains(' '))
                {
                    fixedText = fixedText.Replace(key, value, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    fixedText = Regex.Replace(fixedText, $@"\b{Regex.Escape(key)}\b", value,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
            }
            return fixedText;
        }

        private static string? ExtractBarrio(string text)
        {
            var match = Regex.Match(text, @"\bbarrio\s+([a-z0-9\s]{3,60})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success) return null;
            var value = match.Groups[1].Value;
            value = CutAtDelimiters(value);
            return value;
        }

        private static string? ExtractPoi(string text)
        {
            foreach (var keyword in PoiKeywords)
            {
                var match = Regex.Match(text, $@"\b{Regex.Escape(keyword)}\s+([a-z0-9\s]{{2,60}})",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (match.Success)
                {
                    var name = $"{keyword} {match.Groups[1].Value}";
                    return CutAtDelimiters(name);
                }
            }
            return null;
        }

        private static (string? calle1, string? calle2) ExtractIntersection(string text, string? barrio)
        {
            var streetTypeGroup = string.Join("|", StreetTypes.Select(Regex.Escape));
            var pattern = $@"\b(?:esquina\s+de\s+|entre\s+)?(?:(?<t1>{streetTypeGroup})\s+)?(?<n1>[a-z0-9\s]{{3,60}}?)\s+(?:y|e|&)\s+(?:(?<t2>{streetTypeGroup})\s+)?(?<n2>[a-z0-9\s]{{3,60}})";
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success) return (null, null);

            var t1 = NormalizeStreetType(match.Groups["t1"].Value);
            var t2 = NormalizeStreetType(match.Groups["t2"].Value);
            var n1 = CutAtDelimiters(match.Groups["n1"].Value);
            var n2 = CutAtDelimiters(match.Groups["n2"].Value);

            if (string.IsNullOrWhiteSpace(n1) || string.IsNullOrWhiteSpace(n2))
                return (null, null);

            if (IsBarrioLike(n1, barrio) || IsBarrioLike(n2, barrio))
                return (null, null);

            if (string.IsNullOrWhiteSpace(t1) && string.IsNullOrWhiteSpace(t2))
            {
                var numericStreetPattern = @"\b\d{1,2}\s+de\s+[a-z]{3,20}\b";
                var hasNumericStreet = Regex.IsMatch(n1, numericStreetPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                    || Regex.IsMatch(n2, numericStreetPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (!hasNumericStreet)
                    return (null, null);
            }

            var calle1 = string.IsNullOrWhiteSpace(t1) ? n1 : $"{t1} {n1}";
            var calle2 = string.IsNullOrWhiteSpace(t2) ? n2 : $"{t2} {n2}";
            return (calle1.Trim(), calle2.Trim());
        }

        private static bool IsBarrioLike(string? value, string? barrio)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(barrio))
                return false;

            var valueNorm = NormalizeForMatch(value);
            var barrioNorm = NormalizeForMatch(barrio);
            barrioNorm = Regex.Replace(barrioNorm, @"^barrio\s+", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Trim();
            if (string.IsNullOrWhiteSpace(barrioNorm))
                return false;

            if (valueNorm.StartsWith("barrio ", StringComparison.OrdinalIgnoreCase))
                return true;

            return string.Equals(valueNorm, barrioNorm, StringComparison.OrdinalIgnoreCase);
        }

        private static string RemoveBarrioPhrase(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            return Regex.Replace(text, @"\bbarrio\s+[a-z0-9\s]{3,60}", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static string NormalizeImplicitIntersection(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var streetTypeGroup = string.Join("|", StreetTypes.Select(Regex.Escape));
            var pattern = $@"\b(?<t1>{streetTypeGroup})\s+(?<n1>[a-z0-9\s]{{2,60}}?)\s+(?<t2>{streetTypeGroup})\s+(?<n2>[a-z0-9\s]{{2,60}})";
            var updated = Regex.Replace(
                text,
                pattern,
                m =>
                {
                    var t1 = NormalizeStreetType(m.Groups["t1"].Value);
                    var n1 = CutAtDelimiters(m.Groups["n1"].Value);
                    var t2 = NormalizeStreetType(m.Groups["t2"].Value);
                    var n2 = CutAtDelimiters(m.Groups["n2"].Value);
                    if (string.IsNullOrWhiteSpace(n1) || string.IsNullOrWhiteSpace(n2))
                        return m.Value;
                    return $"{t1} {n1} y {t2} {n2}".Trim();
                },
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );

            var numericStreetPattern = @"\b(?<n1>[a-z\s]{3,60}?)\s+(?<n2>\d{1,2}\s+de\s+[a-z]{3,20})\b";
            updated = Regex.Replace(
                updated,
                numericStreetPattern,
                m =>
                {
                    var n1 = CutAtDelimiters(m.Groups["n1"].Value);
                    var n2 = CutAtDelimiters(m.Groups["n2"].Value);
                    if (string.IsNullOrWhiteSpace(n1) || string.IsNullOrWhiteSpace(n2))
                        return m.Value;
                    return $"{n1} y {n2}".Trim();
                },
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );

            return updated;
        }

        public static string StripStreetType(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            var trimmed = value.Trim();
            foreach (var type in StreetTypes.OrderByDescending(t => t.Length))
            {
                var pattern = $"^{Regex.Escape(type)}\\s+";
                trimmed = Regex.Replace(trimmed, pattern, "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            return trimmed.Trim();
        }

        private static string NormalizeStreetType(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            var lower = value.Trim().ToLowerInvariant();
            return lower switch
            {
                "av" => "avenida",
                "av." => "avenida",
                "bulevar" => "boulevard",
                "blvd" => "boulevard",
                _ => lower
            };
        }

        private static bool HasStreetType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            foreach (var type in StreetTypes)
            {
                if (value.StartsWith(type, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string CutAtDelimiters(string value)
        {
            var trimmed = value.Trim();
            var tokens = new[] { ",", " cordoba", " córdoba", " capital", " provincia" };
            foreach (var token in tokens)
            {
                var idx = trimmed.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (idx > 0)
                {
                    trimmed = trimmed.Substring(0, idx);
                    break;
                }
            }
            trimmed = Regex.Replace(trimmed, @"\s+", " ").Trim();
            return trimmed;
        }

        private static string ToTitleCase(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var culture = new CultureInfo("es-AR");
            var lower = text.ToLower(culture);
            var titled = culture.TextInfo.ToTitleCase(lower);
            var minor = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "de", "del", "la", "el", "los", "las", "y", "e", "en", "al"
            };
            var parts = titled.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                if (i > 0 && minor.Contains(parts[i]))
                    parts[i] = parts[i].ToLower(culture);
            }
            return string.Join(" ", parts);
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static decimal Clamp(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
