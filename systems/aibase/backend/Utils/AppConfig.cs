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

        public static bool AIBASE_ENGINE_MOCK =>
            bool.TryParse(Environment.GetEnvironmentVariable("AIBASE_ENGINE_MOCK"), out var enabled)
                ? enabled
                : true;

        public static int AIBASE_ENGINE_TIMEOUT_SECONDS =>
            int.TryParse(Environment.GetEnvironmentVariable("AIBASE_ENGINE_TIMEOUT_SECONDS"), out var seconds)
                ? seconds
                : 120;

        public static string AIBASE_DEPLOY_ENDPOINT =>
            Environment.GetEnvironmentVariable("AIBASE_DEPLOY_ENDPOINT") ?? "http://localhost:5177";

        public static string AIBASE_DEPLOY_HEALTH =>
            Environment.GetEnvironmentVariable("AIBASE_DEPLOY_HEALTH") ?? "http://localhost:5036/api/v1/dev/ping";
    }
}
