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
                lugar = !string.IsNullOrWhiteSpace(cleanedLugar) ? cleanedLugar : llm.LugarTexto;
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
            var match = Regex.Match(text, @"\b(?:a las|hora)\s*(\d{1,2})\s*([:\.\s])\s*(\d{2})\b", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                match = Regex.Match(text, @"\b(?:a las|hora)\s*(\d{1,2})\s*(?:hs|h)\b", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    match = Regex.Match(text, @"\b(?:a las|hora)\s*(\d{3,4})\b", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var raw = match.Groups[1].Value;
                        if (raw.Length == 3)
                        {
                            var h = int.Parse(raw.Substring(0, 1));
                            var m = int.Parse(raw.Substring(1, 2));
                            if (h > 23 || m > 59) return null;
                            return new TimeSpan(h, m, 0);
                        }
                        if (raw.Length == 4)
                        {
                            var h = int.Parse(raw.Substring(0, 2));
                            var m = int.Parse(raw.Substring(2, 2));
                            if (h > 23 || m > 59) return null;
                            return new TimeSpan(h, m, 0);
                        }
                    }

                    match = Regex.Match(text, @"\b(\d{1,2})[:\.](\d{2})\b");
                    if (!match.Success) return null;
                    var fallbackHour = int.Parse(match.Groups[1].Value);
                    var fallbackMinute = int.Parse(match.Groups[2].Value);
                    if (fallbackHour > 23 || fallbackMinute > 59) return null;
                    return new TimeSpan(fallbackHour, fallbackMinute, 0);
                }

                var onlyHour = int.Parse(match.Groups[1].Value);
                if (onlyHour > 23) return null;
                return new TimeSpan(onlyHour, 0, 0);
            }

            var hour = int.Parse(match.Groups[1].Value);
            var minute = int.Parse(match.Groups[3].Value);
            if (hour > 23 || minute > 59) return null;
            return new TimeSpan(hour, minute, 0);
        }

        private static string? TryExtractLocation(string text)
        {
            var labeled = TryExtractLabeledLocation(text);
            var preposition = TryExtractPrepositionLocation(text);

            if (!string.IsNullOrWhiteSpace(labeled) && !string.IsNullOrWhiteSpace(preposition))
            {
                return preposition.Length >= labeled.Length ? preposition : labeled;
            }

            if (!string.IsNullOrWhiteSpace(labeled))
                return labeled;

            if (!string.IsNullOrWhiteSpace(preposition))
                return preposition;

            return null;
        }

        private static string? TryExtractPrepositionLocation(string text)
        {
            var match = Regex.Match(
                text,
                $@"\b({BuildAlternatives(AppConfig.WHISPER_LOCATION_PREPOSITIONS)})\s+([\p{{L}}0-9\s\.\-\,º°]{{4,160}})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            );
            if (!match.Success) return null;
            return TrimAtStopWords(match.Groups[2].Value);
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
                @"\b\d{1,2}\s+de\s+(enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre)\s+de\s+\d{2,4}\b",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned,
                @"\by\s+\d{1,2}\s+de\s+(enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|setiembre|octubre|noviembre|diciembre)\s+de\s+\d{2,4}\b",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\([^)]*\)", " ");
            cleaned = Regex.Replace(cleaned, @"\bno se menciona.*$", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bno especificad[oa]\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bno se especifica\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bsin especificar\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bsin precisar\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bnull\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\s*:\s*$", " ");
            cleaned = Regex.Replace(cleaned, @"\s*:\s*null\b", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\bciudad\b(?!\s+de\b)", " ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            cleaned = Regex.Replace(cleaned, @"\s*,\s*", ", ");
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            cleaned = cleaned.Trim(',', '.', ';', ':');
            return cleaned;
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
            if (TimeSpan.TryParse(trimmed, out var ts))
                return ts;

            var digits = Regex.Replace(trimmed, @"\D", "");
            if (digits.Length == 3)
            {
                var h = int.Parse(digits.Substring(0, 1));
                var m = int.Parse(digits.Substring(1, 2));
                if (h <= 23 && m <= 59) return new TimeSpan(h, m, 0);
            }
            if (digits.Length == 4)
            {
                var h = int.Parse(digits.Substring(0, 2));
                var m = int.Parse(digits.Substring(2, 2));
                if (h <= 23 && m <= 59) return new TimeSpan(h, m, 0);
            }

            return null;
        }
    }
}
