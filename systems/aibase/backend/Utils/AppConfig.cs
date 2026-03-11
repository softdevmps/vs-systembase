using System.Globalization;

namespace Backend.Utils
{
    public static class AppConfig
    {
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

        public static string AIBASE_ENGINE_URL =>
            Environment.GetEnvironmentVariable("AIBASE_ENGINE_URL") ?? "http://localhost:8010";

        public static string AIBASE_ENGINE_URLS =>
            Environment.GetEnvironmentVariable("AIBASE_ENGINE_URLS") ?? "";

        public static bool AIBASE_ENGINE_MOCK =>
            bool.TryParse(Environment.GetEnvironmentVariable("AIBASE_ENGINE_MOCK"), out var enabled)
                ? enabled
                : true;

        public static int AIBASE_ENGINE_TIMEOUT_SECONDS =>
            int.TryParse(Environment.GetEnvironmentVariable("AIBASE_ENGINE_TIMEOUT_SECONDS"), out var seconds)
                ? seconds
                : 120;

        public static string AIBASE_MODEL_PROVIDER =>
            Environment.GetEnvironmentVariable("AIBASE_MODEL_PROVIDER") ?? "engine";

        public static string AIBASE_MODEL_BASE_URL =>
            Environment.GetEnvironmentVariable("AIBASE_MODEL_BASE_URL") ?? "http://localhost:11434";

        public static string AIBASE_MODEL_PATH =>
            Environment.GetEnvironmentVariable("AIBASE_MODEL_PATH") ?? "/api/chat";

        public static string AIBASE_MODEL_NAME =>
            Environment.GetEnvironmentVariable("AIBASE_MODEL_NAME") ?? "llama3.2:3b";

        public static string AIBASE_MODEL_API_KEY =>
            Environment.GetEnvironmentVariable("AIBASE_MODEL_API_KEY") ?? "";

        public static string AIBASE_MODEL_API_KEY_ENV =>
            Environment.GetEnvironmentVariable("AIBASE_MODEL_API_KEY_ENV") ?? "OPENAI_API_KEY";

        public static string AIBASE_MODEL_SYSTEM_PROMPT =>
            Environment.GetEnvironmentVariable("AIBASE_MODEL_SYSTEM_PROMPT") ?? "";

        public static double AIBASE_MODEL_TEMPERATURE =>
            double.TryParse(Environment.GetEnvironmentVariable("AIBASE_MODEL_TEMPERATURE"), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? Math.Clamp(value, 0, 2)
                : 0.2;

        public static int AIBASE_MODEL_MAX_TOKENS =>
            int.TryParse(Environment.GetEnvironmentVariable("AIBASE_MODEL_MAX_TOKENS"), out var value)
                ? Math.Clamp(value, 64, 4096)
                : 512;

        public static string AIBASE_MODEL_TASK =>
            Environment.GetEnvironmentVariable("AIBASE_MODEL_TASK") ?? "";

        public static bool AIBASE_MODEL_LOCAL_FILES_ONLY =>
            bool.TryParse(Environment.GetEnvironmentVariable("AIBASE_MODEL_LOCAL_FILES_ONLY"), out var enabled)
                ? enabled
                : true;

        public static double AIBASE_MODEL_TOP_P =>
            double.TryParse(Environment.GetEnvironmentVariable("AIBASE_MODEL_TOP_P"), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? Math.Clamp(value, 0.05, 1.0)
                : 0.95;

        public static double AIBASE_MODEL_REPETITION_PENALTY =>
            double.TryParse(Environment.GetEnvironmentVariable("AIBASE_MODEL_REPETITION_PENALTY"), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? Math.Clamp(value, 0.8, 2.5)
                : 1.05;

        public static int AIBASE_MODEL_TIMEOUT_SECONDS =>
            int.TryParse(Environment.GetEnvironmentVariable("AIBASE_MODEL_TIMEOUT_SECONDS"), out var seconds)
                ? Math.Clamp(seconds, 5, 300)
                : 120;

        public static bool AIBASE_QUALITY_GATE_ENABLED =>
            bool.TryParse(Environment.GetEnvironmentVariable("AIBASE_QUALITY_GATE_ENABLED"), out var enabled)
                ? enabled
                : true;

        public static int AIBASE_QUALITY_GATE_MIN_SAMPLES =>
            int.TryParse(Environment.GetEnvironmentVariable("AIBASE_QUALITY_GATE_MIN_SAMPLES"), out var value)
                ? Math.Clamp(value, 1, 500)
                : 3;

        public static double AIBASE_QUALITY_GATE_MIN_SUCCESS_RATE =>
            double.TryParse(Environment.GetEnvironmentVariable("AIBASE_QUALITY_GATE_MIN_SUCCESS_RATE"), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? Math.Clamp(value, 0, 1)
                : 0.6;

        public static double AIBASE_QUALITY_GATE_MAX_FALLBACK_RATE =>
            double.TryParse(Environment.GetEnvironmentVariable("AIBASE_QUALITY_GATE_MAX_FALLBACK_RATE"), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? Math.Clamp(value, 0, 1)
                : 0.4;

        public static int AIBASE_QUALITY_GATE_MAX_AVG_LATENCY_MS =>
            int.TryParse(Environment.GetEnvironmentVariable("AIBASE_QUALITY_GATE_MAX_AVG_LATENCY_MS"), out var value)
                ? Math.Clamp(value, 0, 1800000)
                : 25000;

        public static string AIBASE_DEPLOY_ENDPOINT =>
            Environment.GetEnvironmentVariable("AIBASE_DEPLOY_ENDPOINT") ?? "http://localhost:5177";

        public static string AIBASE_DEPLOY_HEALTH =>
            Environment.GetEnvironmentVariable("AIBASE_DEPLOY_HEALTH") ?? "http://localhost:5036/api/v1/dev/ping";

        public static string AIBASE_DOCKER_PROJECT =>
            Environment.GetEnvironmentVariable("AIBASE_DOCKER_PROJECT") ?? "aibase-stack";

        public static string AIBASE_DOCKER_COMPOSE_FILE =>
            Environment.GetEnvironmentVariable("AIBASE_DOCKER_COMPOSE_FILE") ?? "";

        public static int AIBASE_DOCKER_COMMAND_TIMEOUT_SECONDS =>
            int.TryParse(Environment.GetEnvironmentVariable("AIBASE_DOCKER_COMMAND_TIMEOUT_SECONDS"), out var seconds)
                ? Math.Clamp(seconds, 5, 300)
                : 45;
    }
}
