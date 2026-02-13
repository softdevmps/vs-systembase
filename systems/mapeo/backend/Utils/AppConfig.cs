namespace Backend.Utils
{
    public static class AppConfig
    {
        private static readonly Lazy<IReadOnlyDictionary<string, string>> _whisperFixMap =
            new(() => ParseFixMap(Environment.GetEnvironmentVariable("WHISPER_FIX_MAP")));
        private static readonly Lazy<IReadOnlyList<string>> _whisperLocationLabels =
            new(() => ParseList(Environment.GetEnvironmentVariable("WHISPER_LOCATION_LABELS"), DefaultLocationLabels));
        private static readonly Lazy<IReadOnlyList<string>> _whisperLocationPrepositions =
            new(() => ParseList(Environment.GetEnvironmentVariable("WHISPER_LOCATION_PREPOSITIONS"), DefaultLocationPrepositions));
        private static readonly Lazy<IReadOnlyList<string>> _whisperLocationStopWords =
            new(() => ParseList(Environment.GetEnvironmentVariable("WHISPER_LOCATION_STOP_WORDS"), DefaultLocationStopWords));

        private static readonly IReadOnlyDictionary<string, string> DefaultFixMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["cast"] = "calle",
                ["casa de"] = "calle",
                ["urto"] = "hurto",
                ["av"] = "avenida",
                ["av."] = "avenida",
                ["nueva corda"] = "nueva cordoba",
                ["varido"] = "barrio",
                ["mocho y arroja"] = "mochila roja"
            };

        private static readonly IReadOnlyList<string> DefaultLocationLabels = new[]
        {
            "lugar",
            "ubicacion",
            "direccion",
            "domicilio",
            "barrio",
            "zona"
        };

        private static readonly IReadOnlyList<string> DefaultLocationPrepositions = new[]
        {
            "en",
            "sobre",
            "frente a",
            "cerca de",
            "altura de",
            "a la altura de",
            "entre"
        };

        private static readonly IReadOnlyList<string> DefaultLocationStopWords = new[]
        {
            "hubo",
            "ocurrio",
            "se produjo",
            "se reporto",
            "a las",
            "hora",
            "fecha",
            "hecho",
            "delito",
            "incidente",
            "suceso",
            "evento"
        };

        public static string DB_SERVER => Environment.GetEnvironmentVariable("DB_SERVER") ?? "";
        public static string DB_NAME => Environment.GetEnvironmentVariable("DB_NAME") ?? "";
        public static string DB_USER => Environment.GetEnvironmentVariable("DB_USER") ?? "";
        public static string DB_PASSWORD => Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

        public static string JWT_SECRET => Environment.GetEnvironmentVariable("JWT_SECRET") ?? "secret";
        public static string JWT_ISSUER => Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "systembase";
        public static string JWT_AUDIENCE => Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "systembase";
        public static int JWT_EXPIRE_MINUTES =>
            int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES"), out var minutes)
                ? minutes
                : 120;

        public static string AUDIO_STORAGE_PROVIDER =>
            Environment.GetEnvironmentVariable("AUDIO_STORAGE_PROVIDER") ?? "local";
        public static string AUDIO_STORAGE_ROOT => Environment.GetEnvironmentVariable("AUDIO_STORAGE_ROOT") ?? "storage/audio";
        public static string AUDIO_ALLOWED_EXT =>
            Environment.GetEnvironmentVariable("AUDIO_ALLOWED_EXT") ?? "mp3,wav,m4a,ogg,opus,webm,aac";
        public static int AUDIO_MAX_MB =>
            int.TryParse(Environment.GetEnvironmentVariable("AUDIO_MAX_MB"), out var maxMb)
                ? maxMb
                : 50;

        public static bool AUDIO_TRANSCODE_ENABLED =>
            bool.TryParse(Environment.GetEnvironmentVariable("AUDIO_TRANSCODE_ENABLED"), out var enabled) && enabled;
        public static string AUDIO_TRANSCODE_FORMAT =>
            Environment.GetEnvironmentVariable("AUDIO_TRANSCODE_FORMAT") ?? "opus";
        public static string AUDIO_TRANSCODE_BITRATE =>
            Environment.GetEnvironmentVariable("AUDIO_TRANSCODE_BITRATE") ?? "32k";
        public static string AUDIO_TRANSCODE_FILTER =>
            Environment.GetEnvironmentVariable("AUDIO_TRANSCODE_FILTER") ?? "";
        public static string AUDIO_TRANSCODE_SAMPLE_RATE =>
            Environment.GetEnvironmentVariable("AUDIO_TRANSCODE_SAMPLE_RATE") ?? "";
        public static bool AUDIO_TRANSCODE_DELETE_ORIGINAL =>
            bool.TryParse(Environment.GetEnvironmentVariable("AUDIO_TRANSCODE_DELETE_ORIGINAL"), out var enabled) && enabled;
        public static string FFMPEG_PATH => Environment.GetEnvironmentVariable("FFMPEG_PATH") ?? "ffmpeg";

        public static int AUDIO_RETENTION_SOFT_DAYS =>
            int.TryParse(Environment.GetEnvironmentVariable("AUDIO_RETENTION_SOFT_DAYS"), out var days)
                ? days
                : 0;
        public static int AUDIO_RETENTION_PURGE_DAYS =>
            int.TryParse(Environment.GetEnvironmentVariable("AUDIO_RETENTION_PURGE_DAYS"), out var days)
                ? days
                : 0;
        public static int AUDIO_RETENTION_RUN_MINUTES =>
            int.TryParse(Environment.GetEnvironmentVariable("AUDIO_RETENTION_RUN_MINUTES"), out var minutes)
                ? minutes
                : 60;
        public static int AUDIO_JOB_TIMEOUT_SECONDS =>
            int.TryParse(Environment.GetEnvironmentVariable("AUDIO_JOB_TIMEOUT_SECONDS"), out var seconds)
                ? seconds
                : 60;
        public static int AUDIO_JOB_STUCK_MINUTES =>
            int.TryParse(Environment.GetEnvironmentVariable("AUDIO_JOB_STUCK_MINUTES"), out var minutes)
                ? minutes
                : 10;

        public static string WHISPER_URL => Environment.GetEnvironmentVariable("WHISPER_URL") ?? "";
        public static string WHISPER_STUB_TEXT => Environment.GetEnvironmentVariable("WHISPER_STUB_TEXT") ?? "";
        public static bool WHISPER_AUTOSTART =>
            bool.TryParse(Environment.GetEnvironmentVariable("WHISPER_AUTOSTART"), out var enabled) && enabled;
        public static string WHISPER_START_COMMAND => Environment.GetEnvironmentVariable("WHISPER_START_COMMAND") ?? "";
        public static string WHISPER_START_WORKDIR => Environment.GetEnvironmentVariable("WHISPER_START_WORKDIR") ?? "";
        public static bool WHISPER_START_USE_SHELL =>
            !bool.TryParse(Environment.GetEnvironmentVariable("WHISPER_START_USE_SHELL"), out var enabled) || enabled;
        public static int WHISPER_START_WAIT_SECONDS =>
            int.TryParse(Environment.GetEnvironmentVariable("WHISPER_START_WAIT_SECONDS"), out var seconds)
                ? seconds
                : 30;
        public static bool WHISPER_KILL_ON_STOP =>
            bool.TryParse(Environment.GetEnvironmentVariable("WHISPER_KILL_ON_STOP"), out var enabled) && enabled;

        public static IReadOnlyDictionary<string, string> WHISPER_FIX_MAP => _whisperFixMap.Value;
        public static IReadOnlyList<string> WHISPER_LOCATION_LABELS => _whisperLocationLabels.Value;
        public static IReadOnlyList<string> WHISPER_LOCATION_PREPOSITIONS => _whisperLocationPrepositions.Value;
        public static IReadOnlyList<string> WHISPER_LOCATION_STOP_WORDS => _whisperLocationStopWords.Value;

        public static bool LLM_ENABLED =>
            bool.TryParse(Environment.GetEnvironmentVariable("LLM_ENABLED"), out var enabled) && enabled;
        public static string LLM_URL => Environment.GetEnvironmentVariable("LLM_URL") ?? "";
        public static string LLM_MODEL => Environment.GetEnvironmentVariable("LLM_MODEL") ?? "llama3.2:3b";
        public static string LLM_MODE => Environment.GetEnvironmentVariable("LLM_MODE") ?? "generate";
        public static string LLM_API_KEY => Environment.GetEnvironmentVariable("LLM_API_KEY") ?? "";
        public static string LLM_FORMAT => Environment.GetEnvironmentVariable("LLM_FORMAT") ?? "";
        public static int LLM_TIMEOUT_SECONDS =>
            int.TryParse(Environment.GetEnvironmentVariable("LLM_TIMEOUT_SECONDS"), out var seconds)
                ? seconds
                : 45;

        public static string GEOCODER_URL => Environment.GetEnvironmentVariable("GEOCODER_URL") ?? "http://localhost:8080/search";
        public static string GEOCODER_DEFAULT_SUFFIX => Environment.GetEnvironmentVariable("GEOCODER_DEFAULT_SUFFIX") ?? "";
        public static string GEOCODER_COUNTRY_CODES => Environment.GetEnvironmentVariable("GEOCODER_COUNTRY_CODES") ?? "";
        public static string GEOCODER_LANGUAGE => Environment.GetEnvironmentVariable("GEOCODER_LANGUAGE") ?? "";

        public static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";
            var lower = value.ToLowerInvariant();
            var withoutAccents = RemoveDiacritics(lower);
            return withoutAccents.Trim();
        }

        private static IReadOnlyDictionary<string, string> ParseFixMap(string? raw)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw))
            {
                foreach (var kv in DefaultFixMap)
                    map[kv.Key] = kv.Value;
                return map;
            }

            var pairs = raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2) continue;
                if (string.IsNullOrWhiteSpace(parts[0])) continue;
                map[parts[0]] = parts[1];
            }

            if (map.Count == 0)
            {
                foreach (var kv in DefaultFixMap)
                    map[kv.Key] = kv.Value;
            }

            return map;
        }

        private static IReadOnlyList<string> ParseList(string? raw, IReadOnlyList<string> defaults)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return defaults.Select(NormalizeKey).Where(x => x.Length > 0).ToList();

            var items = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeKey)
                .Where(x => x.Length > 0)
                .ToList();

            return items.Count > 0 ? items : defaults.Select(NormalizeKey).Where(x => x.Length > 0).ToList();
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }
    }
}
