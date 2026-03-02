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

        public static bool AIBASE_ENGINE_ENABLED =>
            bool.TryParse(Environment.GetEnvironmentVariable("AIBASE_ENGINE_ENABLED"), out var enabled)
                ? enabled
                : false;

        public static string AIBASE_ENGINE_BASE_URL =>
            Environment.GetEnvironmentVariable("AIBASE_ENGINE_BASE_URL") ?? "http://localhost:8090/engine/v1";

        public static int AIBASE_ENGINE_TIMEOUT_SECONDS =>
            int.TryParse(Environment.GetEnvironmentVariable("AIBASE_ENGINE_TIMEOUT_SECONDS"), out var seconds)
                ? Math.Clamp(seconds, 3, 300)
                : 30;
    }
}
