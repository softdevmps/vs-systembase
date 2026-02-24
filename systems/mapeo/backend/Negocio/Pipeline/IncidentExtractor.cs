using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;
using System.Text;
using Backend.Utils;

namespace Backend.Negocio.Pipeline
{
    public sealed class ExtractResult
    {
        public string? LugarTexto { get; init; }
        public DateTime? FechaHora { get; init; }
        public int? TipoHechoId { get; init; }
        public string? Descripcion { get; init; }
        public decimal? Confidence { get; init; }
        public string? MatchedCodigo { get; init; }
        public string JsonExtract { get; init; } = "{}";
        public string ScoresJson { get; init; } = "{}";
    }

    public static class IncidentExtractor
    {
        public static string? CleanLocationText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;
            return StripLocationNoise(text);
        }

        public static string? NormalizeLocationForGeocode(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var normalized = Normalize(text);
            return StripLocationNoise(normalized);
        }

        public static ExtractResult Extract(string rawText, List<CatalogoHechoItem> catalogo)
        {
            var text = Normalize(rawText);
            var displayText = CleanForDisplay(rawText);

            var fecha = TryParseDate(text);
            var hora = TryParseTime(text);
            DateTime? fechaHora = null;
            if (fecha != null)
            {
                fechaHora = hora != null
                    ? new DateTime(fecha.Value.Year, fecha.Value.Month, fecha.Value.Day, hora.Value.Hours, hora.Value.Minutes, 0)
                    : new DateTime(fecha.Value.Year, fecha.Value.Month, fecha.Value.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
            }

            var lugar = TryExtractLocation(text);
            if (!string.IsNullOrWhiteSpace(lugar))
            {
                lugar = StripLocationNoise(lugar);
            }
            var match = MatchCatalogo(text, catalogo);

            var scores = new Dictionary<string, object?>
            {
                ["fecha"] = fecha != null ? 0.9 : 0.2,
                ["hora"] = hora != null ? 0.9 : 0.3,
                ["lugar"] = !string.IsNullOrWhiteSpace(lugar) ? 0.8 : 0.2,
                ["tipo"] = match != null ? 0.85 : 0.2
            };

            var jsonExtract = JsonSerializer.Serialize(new
            {
                lugarTexto = lugar,
                fecha = fecha?.ToString("yyyy-MM-dd"),
                hora = hora?.ToString(@"hh\:mm"),
                tipoCodigo = match?.Codigo,
                tipoNombre = match?.Nombre
            });

            var scoresJson = JsonSerializer.Serialize(scores);

            return new ExtractResult
            {
                LugarTexto = lugar,
                FechaHora = fechaHora,
                TipoHechoId = match?.Id,
                Descripcion = displayText,
                Confidence = match != null ? 0.75m : 0.4m,
                MatchedCodigo = match?.Codigo,
                JsonExtract = jsonExtract,
                ScoresJson = scoresJson
            };
        }

        public static ExtractResult MergeWithLlm(
            ExtractResult baseExtract,
            LlmExtractResult llm,
            List<CatalogoHechoItem> catalogo)
        {
            var fechaHora = baseExtract.FechaHora;
            var llmFecha = TryParseIsoDate(llm.Fecha);
            var llmHora = TryParseHourMinuteValue(llm.Hora);

            if (llmFecha != null)
            {
                var hour = llmHora?.Hours ?? (fechaHora?.Hour ?? DateTime.Now.Hour);
                var minute = llmHora?.Minutes ?? (fechaHora?.Minute ?? DateTime.Now.Minute);
                fechaHora = new DateTime(llmFecha.Value.Year, llmFecha.Value.Month, llmFecha.Value.Day, hour, minute, 0);
            }
            else if (llmHora != null && fechaHora != null)
            {
                fechaHora = new DateTime(fechaHora.Value.Year, fechaHora.Value.Month, fechaHora.Value.Day, llmHora.Value.Hours, llmHora.Value.Minutes, 0);
            }

            var lugar = baseExtract.LugarTexto;
            if (!string.IsNullOrWhiteSpace(llm.LugarTexto))
            {
                var cleanedLugar = StripLocationNoise(llm.LugarTexto);
                var llmLugar = !string.IsNullOrWhiteSpace(cleanedLugar) ? cleanedLugar : llm.LugarTexto;
                var currentScore = ScoreLocationCandidate(lugar);
                var llmScore = ScoreLocationCandidate(llmLugar);
                var currentNorm = Normalize(lugar ?? "");
                var llmNorm = Normalize(llmLugar ?? "");
                var currentHasNumber = Regex.IsMatch(currentNorm, @"\b\d{1,5}\b", RegexOptions.CultureInvariant);
                var llmHasNumber = Regex.IsMatch(llmNorm, @"\b\d{1,5}\b", RegexOptions.CultureInvariant);
                var currentHasAddress = Regex.IsMatch(currentNorm,
                    @"\b(avenida|boulevard|bulevar|calle|pasaje|ruta|diagonal)\b.*\b\d{1,5}\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                var llmHasAddress = Regex.IsMatch(llmNorm,
                    @"\b(avenida|boulevard|bulevar|calle|pasaje|ruta|diagonal)\b.*\b\d{1,5}\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                var llmLooksPoi = Regex.IsMatch(llmNorm, @"\b(plaza|parque|hospital|terminal|banco|comisaria)\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                // Preferimos el LLM solo si realmente mejora la calidad de la ubicacion.
                if (string.IsNullOrWhiteSpace(lugar))
                {
                    lugar = llmLugar;
                }
                else if (currentHasAddress && !llmHasAddress)
                {
                    // Si ya tenemos direccion con altura, no degradamos a POI/interseccion.
                }
                else if (currentHasNumber && !llmHasNumber)
                {
                }
                else if (currentHasAddress && llmLooksPoi)
                {
                }
                else if (llmScore >= currentScore + 1)
                {
                    lugar = llmLugar;
                }
            }
            var descripcion = !string.IsNullOrWhiteSpace(llm.Descripcion) ? llm.Descripcion : baseExtract.Descripcion;

            var tipoHechoId = baseExtract.TipoHechoId;
            var matchedCodigo = baseExtract.MatchedCodigo;
            if (!string.IsNullOrWhiteSpace(llm.TipoCodigo))
            {
                var normalized = llm.TipoCodigo.Trim();
                var match = catalogo.FirstOrDefault(c => string.Equals(c.Codigo, normalized, StringComparison.OrdinalIgnoreCase))
                            ?? catalogo.FirstOrDefault(c => string.Equals(c.Nombre, normalized, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    tipoHechoId = match.Id;
                    matchedCodigo = match.Codigo;
                }
            }

            var confidence = llm.Confianza ?? baseExtract.Confidence;
            var jsonExtract = JsonSerializer.Serialize(new
            {
                source = "llm",
                fecha = llm.Fecha,
                hora = llm.Hora,
                lugarTexto = lugar,
                tipoCodigo = matchedCodigo,
                descripcion
            });

            return new ExtractResult
            {
                LugarTexto = lugar,
                FechaHora = fechaHora,
                TipoHechoId = tipoHechoId,
                Descripcion = descripcion,
                Confidence = confidence,
                MatchedCodigo = matchedCodigo,
                JsonExtract = jsonExtract,
                ScoresJson = baseExtract.ScoresJson
            };
        }

        private static CatalogoHechoItem? MatchCatalogo(string text, List<CatalogoHechoItem> catalogo)
        {
            CatalogoHechoItem? best = null;
            var bestScore = 0;

            foreach (var item in catalogo)
            {
                if (string.IsNullOrWhiteSpace(item.PalabrasClave))
                    continue;

                var keywords = item.PalabrasClave
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(k => k.ToLowerInvariant())
                    .ToList();

                var score = 0;
                foreach (var keyword in keywords)
                {
                    if (keyword.Length < 3) continue;
                    if (text.Contains(keyword))
                        score++;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = item;
                }
            }

            return best;
        }

        private static DateTime? TryParseDate(string text)
        {
            var match = Regex.Match(text, @"\b(\d{1,2})[\/\-](\d{1,2})[\/\-](\d{2,4})\b");
            if (match.Success)
            {
                var day = int.Parse(match.Groups[1].Value);
                var month = int.Parse(match.Groups[2].Value);
                var year = int.Parse(match.Groups[3].Value);
                if (year < 100) year += 2000;

                try
                {
                    return new DateTime(year, month, day);
                }
                catch
                {
                    return null;
                }
            }

            var monthMatch = Regex.Match(
                text,
                @"\b(\d{1,2})\s+de(l)?\s+(enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre)\s+de(l)?\s+(\d{2,4}|\d{2}\s+\d{2})\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (monthMatch.Success)
            {
                var day = int.Parse(monthMatch.Groups[1].Value);
                var monthName = monthMatch.Groups[3].Value.ToLowerInvariant();
                var yearRaw = monthMatch.Groups[5].Value;
                var yearDigits = Regex.Replace(yearRaw, @"\D", "");
                if (string.IsNullOrWhiteSpace(yearDigits)) return null;
                var year = int.Parse(yearDigits);
                if (yearDigits.Length == 2) year += 2000;

                var month = monthName switch
                {
                    "enero" => 1,
                    "febrero" => 2,
                    "marzo" => 3,
                    "abril" => 4,
                    "mayo" => 5,
                    "junio" => 6,
                    "julio" => 7,
                    "agosto" => 8,
                    "septiembre" => 9,
                    "setiembre" => 9,
                    "octubre" => 10,
                    "noviembre" => 11,
                    "diciembre" => 12,
                    _ => 0
                };
                if (month == 0) return null;

                try
                {
                    return new DateTime(year, month, day);
                }
                catch
                {
                    return null;
                }
            }

            if (Regex.IsMatch(text, @"\banteayer\b", RegexOptions.IgnoreCase))
                return DateTime.Today.AddDays(-2);
            if (Regex.IsMatch(text, @"\bayer\b", RegexOptions.IgnoreCase))
                return DateTime.Today.AddDays(-1);
            if (Regex.IsMatch(text, @"\bhoy\b", RegexOptions.IgnoreCase))
                return DateTime.Today;

            return null;
        }

        private static TimeSpan? TryParseTime(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var withMinutes = Regex.Match(
                text,
                @"\b(?:a\s+las?|hora|siendo\s+las?)\s*(?<h>\d{1,2})\s*[:\.]\s*(?<m>\d{1,2})\s*(?:hs|h)?\s*(?<mer>a\.?\s*m\.?|p\.?\s*m\.?|am|pm|de\s+la\s+manana|de\s+la\s+tarde|de\s+la\s+noche|de\s+la\s+madrugada)?\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (withMinutes.Success)
            {
                var hour = int.Parse(withMinutes.Groups["h"].Value);
                var minute = int.Parse(withMinutes.Groups["m"].Value);
                var meridiem = ResolveMeridiemHint(text, withMinutes.Index, withMinutes.Length, withMinutes.Groups["mer"].Value);
                return BuildTime(hour, minute, meridiem);
            }

            var hourAndWordMinutes = Regex.Match(
                text,
                @"\b(?:a\s+las?|hora|siendo\s+las?)\s*(?<h>\d{1,2})\s+y\s+(?<mw>cuarto|media|quince|treinta|veinte|veinticinco|\d{1,2})\s*(?<mer>a\.?\s*m\.?|p\.?\s*m\.?|am|pm|de\s+la\s+manana|de\s+la\s+tarde|de\s+la\s+noche|de\s+la\s+madrugada)?\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (hourAndWordMinutes.Success)
            {
                var hour = int.Parse(hourAndWordMinutes.Groups["h"].Value);
                var minuteToken = hourAndWordMinutes.Groups["mw"].Value.Trim().ToLowerInvariant();
                var minute = minuteToken switch
                {
                    "cuarto" => 15,
                    "media" => 30,
                    "quince" => 15,
                    "treinta" => 30,
                    "veinte" => 20,
                    "veinticinco" => 25,
                    _ => int.TryParse(minuteToken, out var parsedMinute) ? parsedMinute : -1
                };

                var meridiem = ResolveMeridiemHint(text, hourAndWordMinutes.Index, hourAndWordMinutes.Length, hourAndWordMinutes.Groups["mer"].Value);
                return BuildTime(hour, minute, meridiem);
            }

            var onlyHour = Regex.Match(
                text,
                @"\b(?:a\s+las?|hora|siendo\s+las?)\s*(?<h>\d{1,2})\s*(?:hs|h|horas?)?\s*(?<mer>a\.?\s*m\.?|p\.?\s*m\.?|am|pm|de\s+la\s+manana|de\s+la\s+tarde|de\s+la\s+noche|de\s+la\s+madrugada)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (onlyHour.Success)
            {
                var hour = int.Parse(onlyHour.Groups["h"].Value);
                var meridiem = ResolveMeridiemHint(text, onlyHour.Index, onlyHour.Length, onlyHour.Groups["mer"].Value);
                return BuildTime(hour, 0, meridiem);
            }

            var compact = Regex.Match(text, @"\b(?:a\s+las?|hora)\s*(?<raw>\d{3,4})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (compact.Success)
            {
                var raw = compact.Groups["raw"].Value;
                if (raw.Length == 3)
                {
                    var h = int.Parse(raw[..1]);
                    var m = int.Parse(raw.Substring(1, 2));
                    var meridiem = ResolveMeridiemHint(text, compact.Index, compact.Length, null);
                    return BuildTime(h, m, meridiem);
                }
                if (raw.Length == 4)
                {
                    var h = int.Parse(raw[..2]);
                    var m = int.Parse(raw.Substring(2, 2));
                    var meridiem = ResolveMeridiemHint(text, compact.Index, compact.Length, null);
                    return BuildTime(h, m, meridiem);
                }
            }

            var fallback = Regex.Match(
                text,
                @"\b(?<h>\d{1,2})[:\.](?<m>\d{1,2})\s*(?<mer>a\.?\s*m\.?|p\.?\s*m\.?|am|pm|de\s+la\s+manana|de\s+la\s+tarde|de\s+la\s+noche|de\s+la\s+madrugada)?\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (fallback.Success)
            {
                var fallbackHour = int.Parse(fallback.Groups["h"].Value);
                var fallbackMinute = int.Parse(fallback.Groups["m"].Value);
                var meridiem = ResolveMeridiemHint(text, fallback.Index, fallback.Length, fallback.Groups["mer"].Value);
                return BuildTime(fallbackHour, fallbackMinute, meridiem);
            }

            return null;
        }

        private static string? TryExtractLocation(string text)
        {
            var addressWithNumber = TryExtractAddressWithNumber(text);
            var labeled = TryExtractLabeledLocation(text);
            var preposition = TryExtractPrepositionLocation(text);
            var candidates = new[] { addressWithNumber, labeled, preposition }
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (candidates.Count == 0)
                return null;

            return candidates
                .OrderByDescending(ScoreLocationCandidate)
                .ThenByDescending(c => c!.Length)
                .FirstOrDefault();
        }

        private static string? TryExtractAddressWithNumber(string text)
        {
            const string monthPattern = "enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre";
            var match = Regex.Match(
                text,
                $@"\b(?:en|de|sobre|frente a|cerca de|altura de|a la altura de)\s+(?<loc>(?!(?:{monthPattern}|hoy|siendo|las)\b)[\p{{L}}0-9\s\.\-]{{3,80}}?\s+(?:al|en|nro\.?|numero)\s*\d{{1,5}}(?:\s*(?:,|de)?\s*barrio\s+[\p{{L}}0-9\s]{{3,40}})?)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (!match.Success)
            {
                match = Regex.Match(
                    text,
                    @"\b(?<loc>(?:avenida|boulevard|bulevar|calle|pasaje|ruta|diagonal)\s+[\p{L}0-9\s\.\-]{3,90}\s+\d{1,5}(?:\s*,?\s*barrio\s+[\p{L}0-9\s]{3,60})?)",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                );
            }
            if (!match.Success)
            {
                match = Regex.Match(
                    text,
                    @"\b(?<loc>(?:avenida|boulevard|bulevar|calle|pasaje|ruta|diagonal)\s+[\p{L}0-9\s\.\-]{3,90}\s+(?:en|al)\s+\d{1,5}(?:\s*,?\s*barrio\s+[\p{L}0-9\s]{3,60})?)",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                );
            }

            if (!match.Success) return null;
            var candidate = TrimAtStopWords(match.Groups["loc"].Value);
            return IsLikelyAddressWithNumber(candidate) ? candidate : null;
        }

        private static bool IsLikelyAddressWithNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalized = Normalize(value);
            if (!Regex.IsMatch(normalized, @"\b(?:al|nro\.?|numero)\s*\d{1,5}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                && !Regex.IsMatch(normalized, @"\b(?:avenida|boulevard|bulevar|calle|pasaje|ruta|diagonal)\b.*\b\d{1,5}\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                if (!Regex.IsMatch(normalized, @"\ben\s+\d{1,5}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    return false;
            }

            if (Regex.IsMatch(normalized, @"\b(hoy|siendo|fecha|hora)\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return false;

            if (Regex.IsMatch(normalized, @"\b(enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre)\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return false;

            return true;
        }

        private static string? TryExtractPrepositionLocation(string text)
        {
            var match = Regex.Match(
                text,
                $@"\b({BuildAlternatives(AppConfig.WHISPER_LOCATION_PREPOSITIONS)})\s+([\p{{L}}0-9\s\.\-\,º°]{{4,160}})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (!match.Success) return null;
            var candidate = TrimAtStopWords(match.Groups[2].Value);
            if (string.IsNullOrWhiteSpace(candidate))
                return null;

            var normalized = Normalize(candidate);
            if (Regex.IsMatch(normalized, @"^(moto|en moto|auto|camion|camión)\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return null;

            return candidate;
        }

        private static string? TryExtractLabeledLocation(string text)
        {
            var labelsPattern = BuildAlternatives(AppConfig.WHISPER_LOCATION_LABELS);
            if (string.IsNullOrWhiteSpace(labelsPattern))
                return null;

            var match = Regex.Match(
                text,
                $@"\b({labelsPattern})\s*(es|:|-)?\s+([\p{{L}}0-9\s\.\-\,º°]{{4,160}})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (!match.Success) return null;
            return TrimAtStopWords(match.Groups[3].Value);
        }

        private static string BuildAlternatives(IReadOnlyList<string> options)
        {
            if (options == null || options.Count == 0) return "";
            return string.Join("|", options
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .OrderByDescending(o => o.Length)
                .Select(Regex.Escape));
        }

        private static string TrimAtStopWords(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            var lower = value.ToLowerInvariant();
            var stop = AppConfig.WHISPER_LOCATION_STOP_WORDS;
            var cutIndex = stop
                .Select(s => lower.IndexOf(s, StringComparison.Ordinal))
                .Where(i => i >= 0)
                .DefaultIfEmpty(-1)
                .Min();

            var trimmed = cutIndex > 0 ? value.Substring(0, cutIndex) : value;
            return trimmed.Trim().Trim(',', '.', ';', ':');
        }

        private static string Normalize(string text)
        {
            var lower = text.ToLowerInvariant();
            var withoutAccents = RemoveDiacritics(lower);
            var compact = Regex.Replace(withoutAccents, @"\s+", " ").Trim();
            compact = InsertMissingSeparators(compact);
            return FixCommonMishears(compact);
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

        private static string FixCommonMishears(string text)
        {
            var fixedText = text;
            foreach (var kv in AppConfig.WHISPER_FIX_MAP)
            {
                var key = AppConfig.NormalizeKey(kv.Key);
                if (string.IsNullOrWhiteSpace(key)) continue;
                var value = AppConfig.NormalizeKey(kv.Value);
                if (key.Contains(' '))
                {
                    fixedText = fixedText.Replace(key, value);
                }
                else
                {
                    fixedText = Regex.Replace(fixedText, $@"\b{Regex.Escape(key)}\b", value);
                }
            }
            return fixedText;
        }

        private static string CleanForDisplay(string text)
        {
            var cleaned = text;
            foreach (var kv in AppConfig.WHISPER_FIX_MAP)
            {
                var key = kv.Key?.Trim();
                if (string.IsNullOrWhiteSpace(key)) continue;
                var value = kv.Value ?? "";
                var pattern = key.Contains(' ')
                    ? Regex.Escape(key)
                    : $@"\b{Regex.Escape(key)}\b";
                cleaned = Regex.Replace(cleaned, pattern, _ => value, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            cleaned = InsertMissingSeparators(cleaned);
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            return cleaned;
        }

        private static string StripLocationNoise(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var cleaned = text;
            cleaned = Regex.Replace(cleaned, @"\bfecha\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bhora\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bhoy\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\b(lugar|ubicacion|ubicación)\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned,
                @"\b(hacia|rumbo\s+a|direccion\s+a|direccion\s+al|dirección\s+a|dirección\s+al)\b.*$",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\b(a\s+las?|a\s+la)\s+\d{1,2}\s*(y\s*(cuarto|media|quince|treinta))?\b", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\b\d{1,2}\s+y\s*(cuarto|media|quince|treinta)\b", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\b\d{1,2}[:\.]\d{2}\b", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\b\d{1,2}\s+horas?\b", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned,
                @"^\s*(calle|avenida|boulevard|bulevar)\s+y\s+",
                "",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned,
                @"\b\d{1,2}\s+de\s+(enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre)\s+de(?:l)?\s+\d{2,4}\b",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned,
                @"\by\s+\d{1,2}\s+de\s+(enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre)\s+de(?:l)?\s+\d{2,4}\b",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\([^)]*\)", " ");
            cleaned = Regex.Replace(cleaned, @"\bno se menciona.*$", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bno especificad[oa]\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bno se especifica\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bsin especificar\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bsin precisar\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bnull\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\ben moto\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bmoto\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\s*:\s*$", " ");
            cleaned = Regex.Replace(cleaned, @"\s*:\s*null\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bciudad\b(?!\s+de\b)", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\b(hubo|ocurrio|ocurrió|se produjo|se reporto|se reportó|arrebato|robo|hurto)\b.*$",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\b(autores?|huyeron|escaparon|le quitaron|le robaron|sustrajeron|sustra)\b.*$",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bno\s+(hubo|se reportan|se registran)\b.*$",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\b(y|e)\s+\1\b", "$1",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\b(y|e|con|en)\s*$", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"^\s*(y|e|con|en)\b", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = InsertMissingSeparators(cleaned);
            cleaned = Regex.Replace(cleaned, @"\s*,\s*", ", ");
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            cleaned = cleaned.Trim(',', '.', ';', ':');
            return cleaned;
        }

        private static int ScoreLocationCandidate(string? location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return -10;

            var score = 0;
            var normalized = Normalize(location);

            if (Regex.IsMatch(normalized, @"\b\d{1,5}\b", RegexOptions.CultureInvariant))
                score += 2;

            if (Regex.IsMatch(normalized, @"\b(avenida|boulevard|bulevar|calle|pasaje|ruta|diagonal)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                score += 2;

            if (Regex.IsMatch(normalized, @"\b(y|e|con)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                score += 1;

            if (Regex.IsMatch(normalized, @"\bbarrio\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                score += 1;

            if (Regex.IsMatch(normalized, @"\b(hubo|robo|hurto|arrebato|huyeron|escaparon|autores?|heridos)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                score -= 4;

            if (Regex.IsMatch(normalized, @"\b(hoy|siendo|fecha|hora)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                score -= 3;

            if (Regex.IsMatch(normalized, @"\b(hacia|rumbo|direccion|dirección)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                score -= 2;

            if (Regex.IsMatch(normalized, @"\b(enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                score -= 3;

            if (normalized.Length > 120)
                score -= 3;

            return score;
        }

        private static string InsertMissingSeparators(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var updated = text;
            updated = Regex.Replace(updated, @"(?<=\d)(?=[\p{L}])", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            updated = Regex.Replace(updated, @"(?<=[\p{L}])(?=\d)", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            updated = Regex.Replace(updated, @"\b(\d{1,5})\s*(barrio)\b", "$1, barrio",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            updated = Regex.Replace(updated, @"\b(avenida|boulevard|bulevar|calle|pasaje)([\p{L}])", "$1 $2",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            updated = Regex.Replace(updated, @"\s+", " ").Trim();
            return updated;
        }

        private static DateTime? TryParseIsoDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed;
            return null;
        }

        private static TimeSpan? TryParseHourMinuteValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var trimmed = value.Trim();
            if (TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out var ts))
                return ts;

            var spoken = TryParseTime($"a las {trimmed}");
            if (spoken.HasValue)
                return spoken;

            var digits = Regex.Replace(trimmed, @"\D", "");
            if (digits.Length == 3)
            {
                var h = int.Parse(digits.Substring(0, 1));
                var m = int.Parse(digits.Substring(1, 2));
                var built = BuildTime(h, m, trimmed);
                if (built.HasValue) return built;
            }
            if (digits.Length == 4)
            {
                var h = int.Parse(digits.Substring(0, 2));
                var m = int.Parse(digits.Substring(2, 2));
                var built = BuildTime(h, m, trimmed);
                if (built.HasValue) return built;
            }

            return null;
        }

        private static string ResolveMeridiemHint(string text, int index, int length, string? inlineHint)
        {
            if (!string.IsNullOrWhiteSpace(inlineHint))
                return inlineHint;

            var start = Math.Max(0, index - 12);
            var end = Math.Min(text.Length, index + length + 24);
            var window = text[start..end];
            return window;
        }

        private static TimeSpan? BuildTime(int hour, int minute, string? meridiemHint)
        {
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59)
                return null;

            var adjustedHour = AdjustHourByMeridiem(hour, meridiemHint);
            if (adjustedHour < 0 || adjustedHour > 23)
                return null;

            return new TimeSpan(adjustedHour, minute, 0);
        }

        private static int AdjustHourByMeridiem(int hour, string? hint)
        {
            if (string.IsNullOrWhiteSpace(hint))
                return hour;

            var normalized = hint.ToLowerInvariant();
            normalized = RemoveDiacritics(normalized);

            var isPm = Regex.IsMatch(
                normalized,
                @"\b(pm|p\.m|de la tarde|de la noche)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            var isAm = Regex.IsMatch(
                normalized,
                @"\b(am|a\.m|de la manana|de la madrugada)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );

            if (isPm && hour < 12)
                return hour + 12;
            if (isAm && hour == 12)
                return 0;

            return hour;
        }
    }
}
