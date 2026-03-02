namespace Backend
{
    public static class Routes
    {
        public static class v1
        {
            public static class Auth
            {
                public const string Login = "api/v1/auth/login";
                public const string Registrar = "api/v1/auth/registrar";
            }

            public static class DevTools
            {
                public const string Restart = "api/v1/dev/restart";
                public const string Ping = "api/v1/dev/ping";
            }

            public static class Aibase
            {
                public const string ObtenerTemplates = "api/v1/aibase/templates";
                public const string ObtenerProyectos = "api/v1/aibase/projects";
                public const string ObtenerProyectoPorId = "api/v1/aibase/projects/{id}";
                public const string CrearProyecto = "api/v1/aibase/projects";
                public const string ObtenerRunsProyecto = "api/v1/aibase/projects/{projectId}/runs";
                public const string CrearRunProyecto = "api/v1/aibase/projects/{projectId}/runs";
                public const string ObtenerRunPorId = "api/v1/aibase/runs/{id}";
                public const string SincronizarRunPorId = "api/v1/aibase/runs/{id}/sync";
            }
        }
    }
}
