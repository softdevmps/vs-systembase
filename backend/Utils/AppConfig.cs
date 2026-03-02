using DotNetEnv;

namespace Backend.Utils
{
    public static class AppConfig
    {
        static AppConfig()
        {
            Env.Load();
        }

        public static string JWT_SECRET =>
            Environment.GetEnvironmentVariable("JWT_SECRET");

        public static string JWT_ISSUER =>
            Environment.GetEnvironmentVariable("JWT_ISSUER");

        public static string JWT_AUDIENCE =>
            Environment.GetEnvironmentVariable("JWT_AUDIENCE");

        public static int JWT_EXPIRE_MINUTES =>
            int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES"));

        public static bool AIBASE_ENGINE_ENABLED =>
            bool.TryParse(Environment.GetEnvironmentVariable("AIBASE_ENGINE_ENABLED"), out var enabled)
                ? enabled
                : false;

        public static string AIBASE_ENGINE_BASE_URL =>
            Environment.GetEnvironmentVariable("AIBASE_ENGINE_BASE_URL")?.Trim().TrimEnd('/')
            ?? "http://localhost:8090/engine/v1";

        public static int AIBASE_ENGINE_TIMEOUT_SECONDS =>
            int.TryParse(Environment.GetEnvironmentVariable("AIBASE_ENGINE_TIMEOUT_SECONDS"), out var seconds)
                ? Math.Clamp(seconds, 5, 300)
                : 30;
    }
}
