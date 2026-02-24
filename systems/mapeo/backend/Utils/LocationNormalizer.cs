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
        public string? AddressWithNumber { get; init; }
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
                ["bolivar san juan"] = "boulevard san juan",
                ["la esquina de"] = "esquina de",
                ["aavenida"] = "avenida",
                ["venida"] = "avenida",
                ["avenilla"] = "avenida",
                ["avanilla"] = "avenida",
                ["urigoyen"] = "yrigoyen",
                ["hirigoyen"] = "yrigoyen",
                ["huemes"] = "guemes",
                ["uemes"] = "guemes",
                ["wemes"] = "guemes",
                ["nueva corda"] = "nueva cordoba",
                ["hipolitohirigoyen"] = "hipolito yrigoyen",
                ["polityrigoyen"] = "hipolito yrigoyen",
                ["politoyrigoyen"] = "hipolito yrigoyen",
                ["chacauco"] = "chacabuco",
                ["chacabulco"] = "chacabuco",
                ["chocabuco"] = "chacabuco",
                ["chacabucoo"] = "chacabuco",
                ["bulevar ilia"] = "boulevard arturo illia",
                ["boulevard ilia"] = "boulevard arturo illia",
                ["oulevard"] = "boulevard",
                ["boulevar"] = "boulevard",
                ["navegania"] = "avenida sabattini",
                ["madeo"] = "amadeo",
                ["sabatine"] = "sabattini",
                ["general pas"] = "general paz",
                ["sabatini"] = "sabattini",
                ["nuevaas"] = "nueva",
                ["espana"] = "espana",
                ["alverdi"] = "alberdi",
                ["fuerza area"] = "fuerza aerea",
                ["rondeo"] = "rondeau",
                ["aveania"] = "avenida",
                ["avenia"] = "avenida",
                ["roodriguez"] = "rodriguez",
                ["ridriguez"] = "rodriguez",
                ["rodrigues"] = "rodriguez",
                ["rodriguez pena"] = "rodriguez peña"
            };

        private static readonly string[] KnownBarrios =
        {
            "nueva cordoba",
            "general paz",
            "alto alberdi",
            "cofico",
            "arguello",
            "guemes",
            "centro",
            "alberdi",
            "don bosco",
            "cerro de las rosas",
            "los platanos",
            "pueyrredon",
            "jardin",
            "observatorio"
        };

        private static readonly string[] KnownStreetNames =
        {
            "velez sarsfield",
            "boulevard san juan",
            "boulevard ilia",
            "bulevar ilia",
            "ilia",
            "arturo illia",
            "boulevard arturo illia",
            "hipolito yrigoyen",
            "duarte quiros",
            "general paz",
            "chacabuco",
            "colon",
            "san martin",
            "9 de julio",
            "ambrosio olmos",
            "dean funes",
            "santa rosa",
            "pueyrredon",
            "lugones",
            "arturo m bas",
            "maipu",
            "caseros",
            "crisol",
            "sabattini",
            "amadeo sabattini",
            "fuerza aerea",
            "rondeau",
            "juan b justo",
            "rodriguez peña",
            "don bosco",
            "rafael nuñez",
            "sagrada familia"
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
            var addressWithNumber = ExtractAddressWithNumber(normalizedSearch);
            var searchForIntersection = RemoveBarrioPhrase(normalizedSearch);
            var (calle1, calle2) = ExtractIntersection(searchForIntersection, barrio);
            var calle1Core = StripStreetType(calle1 ?? "");
            var calle2Core = StripStreetType(calle2 ?? "");

            var signals = new LocationSignals
            {
                Barrio = barrio,
                Poi = poi,
                AddressWithNumber = addressWithNumber,
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

            if (!string.IsNullOrWhiteSpace(location.Signals.AddressWithNumber)) score += 0.1m;
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

            if (!string.IsNullOrWhiteSpace(signals.AddressWithNumber))
            {
                if (!string.IsNullOrWhiteSpace(signals.Barrio) &&
                    !signals.AddressWithNumber.Contains("barrio", StringComparison.OrdinalIgnoreCase))
                {
                    Add($"{signals.AddressWithNumber}, barrio {signals.Barrio}");
                }
                Add(signals.AddressWithNumber);
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
            if (!string.IsNullOrWhiteSpace(signals.AddressWithNumber))
            {
                baseText = signals.AddressWithNumber!;
                if (!string.IsNullOrWhiteSpace(signals.Barrio) &&
                    !baseText.Contains("barrio", StringComparison.OrdinalIgnoreCase))
                {
                    baseText = $"{baseText}, barrio {signals.Barrio}";
                }
            }
            else if (signals.HasIntersection)
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
            cleaned = ExpandGluedTokens(cleaned);
            cleaned = ApplyReplacements(cleaned);
            cleaned = ExpandGluedTokens(cleaned);
            cleaned = NormalizeImplicitIntersection(cleaned);
            cleaned = Regex.Replace(cleaned, @"\b(y|e)\s+\1\b", "$1",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\b(y|e|con|en)\s*$", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            return cleaned;
        }

        private static string ExpandGluedTokens(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var updated = text;
            updated = Regex.Replace(updated, @"(?<=\d)(?=[a-z])", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            updated = Regex.Replace(updated, @"(?<=[a-z])(?=\d)", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            updated = Regex.Replace(updated, @"\b(\d{1,5})\s*(barrio)\b", "$1 barrio",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            updated = Regex.Replace(updated, @"\b(avenida|boulevard|bulevar|calle|pasaje)([a-z])", "$1 $2",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            updated = Regex.Replace(updated, @"\s+", " ").Trim();
            return updated;
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
            fixedText = LocationNormalizationRuleStore.ApplyRules(fixedText, "location");
            return fixedText;
        }

        private static string? ExtractBarrio(string text)
        {
            var match = Regex.Match(text, @"\bbarrio\s*([a-z0-9\s]{3,60})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                var value = match.Groups[1].Value;
                value = Regex.Replace(value,
                    @"\b(hubo|ocurrio|ocurrió|robo|hurto|arrebato|autores?|huyeron|escaparon|heridos?|a\s+las)\b.*$",
                    " ",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                value = Regex.Replace(value,
                    @"\b(en|de|del|la|el)\s*$",
                    " ",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                value = Regex.Replace(value, @"\s+", " ").Trim().Trim(',', '.', ';', ':');
                return CanonicalizeBarrioName(value);
            }

            foreach (var barrio in KnownBarrios.OrderByDescending(x => x.Length))
            {
                if (Regex.IsMatch(text, $@"\b{Regex.Escape(barrio)}\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    return barrio;
            }

            return null;
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

        private static string? ExtractAddressWithNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var match = Regex.Match(
                text,
                @"\b(?<street>(?:avenida|boulevard|bulevar|calle|pasaje|ruta|diagonal)\s+[a-z0-9\s\.]{2,90}?)\s+(?:al|en|nro\.?|numero)?\s*(?<num>\d{1,5})\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );

            if (!match.Success)
                return null;

            var street = CutAtDelimiters(match.Groups["street"].Value);
            var number = match.Groups["num"].Value.Trim();
            if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(number))
                return null;

            street = CleanStreetName(street);
            street = CanonicalizeStreetName(street);

            if (ContainsDateOrTemporalNoise(street))
                return null;

            return $"{street} {number}".Trim();
        }

        private static (string? calle1, string? calle2) ExtractIntersection(string text, string? barrio)
        {
            var streetTypeGroup = string.Join("|", StreetTypes.Select(Regex.Escape));
            var cuePattern = $@"\b(?:esquina\s+de|entre)\s+(?:(?<t1>{streetTypeGroup})\s+)?(?<n1>[a-z0-9\s]{{3,60}}?)\s+(?:y|e|&|con)\s+(?:(?<t2>{streetTypeGroup})\s+)?(?<n2>[a-z0-9\s]{{3,60}}?)(?=\s*(?:,|\bbarrio\b|\bfrente\b|\bhubo\b|\brobo\b|\barrebato\b|\bhurto\b|\bhuy|\bno\b|\bherid|\bautor|\ba las\b|$))";
            var cueResult = TryIntersectionMatches(text, cuePattern, barrio);
            if (!string.IsNullOrWhiteSpace(cueResult.calle1) && !string.IsNullOrWhiteSpace(cueResult.calle2))
                return cueResult;

            var pattern = $@"\b(?:la\s+)?(?:esquina\s+de\s+|entre\s+)?(?:(?<t1>{streetTypeGroup})\s+)?(?<n1>[a-z0-9\s]{{3,60}}?)\s+(?:y|e|&|con)\s+(?:(?<t2>{streetTypeGroup})\s+)?(?<n2>[a-z0-9\s]{{3,60}}?)(?=\s*(?:,|\bbarrio\b|\bhubo\b|\brobo\b|\barrebato\b|\bhuy|\bno\b|\bherid|\bautor|\ba las\b|$))";
            return TryIntersectionMatches(text, pattern, barrio);
        }

        private static (string? calle1, string? calle2) TryIntersectionMatches(string text, string pattern, string? barrio)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var match = regex.Match(text);
            while (match.Success)
            {
                var t1 = NormalizeStreetType(match.Groups["t1"].Value);
                var t2 = NormalizeStreetType(match.Groups["t2"].Value);
                var n1 = CanonicalizeStreetName(CleanStreetName(CutAtDelimiters(match.Groups["n1"].Value)));
                var n2 = CanonicalizeStreetName(CleanStreetName(CutAtDelimiters(match.Groups["n2"].Value)));

                if (!string.IsNullOrWhiteSpace(n1) &&
                    !string.IsNullOrWhiteSpace(n2) &&
                    !ContainsDateOrTemporalNoise(n1) &&
                    !ContainsDateOrTemporalNoise(n2) &&
                    !IsBarrioLike(n1, barrio) &&
                    !IsBarrioLike(n2, barrio) &&
                    HasIntersectionStreetSignal(t1, t2, n1, n2))
                {
                    var calle1 = string.IsNullOrWhiteSpace(t1) ? n1 : $"{t1} {n1}";
                    var calle2 = string.IsNullOrWhiteSpace(t2) ? n2 : $"{t2} {n2}";
                    return (calle1.Trim(), calle2.Trim());
                }

                match = match.NextMatch();
            }

            return (null, null);
        }

        private static bool HasIntersectionStreetSignal(string? t1, string? t2, string n1, string n2)
        {
            if (!string.IsNullOrWhiteSpace(t1) || !string.IsNullOrWhiteSpace(t2))
                return true;

            var numericStreetPattern = @"\b\d{1,2}\s+de\s+[a-z]{3,20}\b";
            var hasNumericStreet = Regex.IsMatch(n1, numericStreetPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                || Regex.IsMatch(n2, numericStreetPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (hasNumericStreet)
                return true;

            var n1Core = AppConfig.NormalizeKey(StripStreetType(n1));
            var n2Core = AppConfig.NormalizeKey(StripStreetType(n2));
            if (string.IsNullOrWhiteSpace(n1Core) || string.IsNullOrWhiteSpace(n2Core))
                return false;

            var known = new HashSet<string>(KnownStreetNames.Select(AppConfig.NormalizeKey), StringComparer.OrdinalIgnoreCase);
            return known.Contains(n1Core) && known.Contains(n2Core);
        }

        private static bool ContainsDateOrTemporalNoise(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return Regex.IsMatch(value,
                @"\b(hoy|ayer|anteayer|fecha|hora|siendo|enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre|20\d{2})\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static string CleanStreetName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            var cleaned = Regex.Replace(value, @"\b(?:y|e|con)\s*$", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"^(?:y|e|de|la|el)\s+", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            return cleaned;
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

        public static string CanonicalizeStreetName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            var cleaned = Regex.Replace(value, @"\s+", " ").Trim();
            var normalized = AppConfig.NormalizeKey(cleaned);
            if (string.IsNullOrWhiteSpace(normalized))
                return cleaned;

            foreach (var known in KnownStreetNames)
            {
                var knownNorm = AppConfig.NormalizeKey(known);
                if (normalized.Equals(knownNorm, StringComparison.OrdinalIgnoreCase))
                    return known;
            }

            // match "hipolitohirigoyen" -> "hipolito yrigoyen" and similar glued names
            var compact = normalized.Replace(" ", "");
            var bestCompact = KnownStreetNames
                .Select(k => new { Value = k, Key = AppConfig.NormalizeKey(k).Replace(" ", "") })
                .Select(x => new
                {
                    x.Value,
                    Distance = LevenshteinDistance(compact, x.Key),
                    Ratio = SimilarityRatio(compact, x.Key)
                })
                .OrderBy(x => x.Ratio)
                .ThenBy(x => x.Distance)
                .FirstOrDefault();

            if (bestCompact != null && bestCompact.Distance <= 4 && bestCompact.Ratio <= 0.34m)
                return bestCompact.Value;

            return cleaned;
        }

        private static string CanonicalizeBarrioName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            var cleaned = Regex.Replace(value, @"\s+", " ").Trim();
            var normalized = AppConfig.NormalizeKey(cleaned);
            if (string.IsNullOrWhiteSpace(normalized))
                return cleaned;

            foreach (var known in KnownBarrios)
            {
                var knownNorm = AppConfig.NormalizeKey(known);
                if (normalized.Equals(knownNorm, StringComparison.OrdinalIgnoreCase))
                    return known;
            }

            var best = KnownBarrios
                .Select(k => new
                {
                    Value = k,
                    Distance = LevenshteinDistance(normalized, AppConfig.NormalizeKey(k)),
                    Ratio = SimilarityRatio(normalized, AppConfig.NormalizeKey(k))
                })
                .OrderBy(x => x.Ratio)
                .ThenBy(x => x.Distance)
                .FirstOrDefault();

            if (best != null && best.Distance <= 3 && best.Ratio <= 0.32m)
                return best.Value;

            return cleaned;
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
            var tokens = new[]
            {
                ",",
                " cordoba",
                " córdoba",
                " capital",
                " provincia",
                " hubo",
                " robo",
                " robaron",
                " arrebato",
                " huyeron",
                " no se",
                " no hubo",
                " heridos",
                " autores",
                " sustra",
                " danaron",
                " dañaron",
                " a las"
            };
            foreach (var token in tokens)
            {
                var idx = trimmed.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (idx > 0)
                {
                    trimmed = trimmed.Substring(0, idx);
                    break;
                }
            }
            trimmed = Regex.Replace(trimmed, @"\b(y|e|con)\s*$", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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

        private static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            var n = a.Length;
            var m = b.Length;
            var d = new int[n + 1, m + 1];

            for (var i = 0; i <= n; i++) d[i, 0] = i;
            for (var j = 0; j <= m; j++) d[0, j] = j;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[n, m];
        }

        private static decimal SimilarityRatio(string a, string b)
        {
            var max = Math.Max(a.Length, b.Length);
            if (max == 0) return 0m;
            return (decimal)LevenshteinDistance(a, b) / max;
        }

        private static decimal Clamp(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
