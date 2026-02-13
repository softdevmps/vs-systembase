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

            public static class Incidentes
            {
                public const string Obtener = "api/v1/incidentes";
                public const string ObtenerPorId = "api/v1/incidentes/{id}";
                public const string Crear = "api/v1/incidentes";
                public const string Editar = "api/v1/incidentes/{id}";
                public const string Eliminar = "api/v1/incidentes/{id}";
                public const string UploadAudio = "api/v1/incidentes/audio";
            }

            public static class Incidenteaudio
            {
                public const string Obtener = "api/v1/incidente-audio";
                public const string ObtenerPorId = "api/v1/incidente-audio/{id}";
                public const string Descargar = "api/v1/incidente-audio/{id}/file";
                public const string Stream = "api/v1/incidente-audio/{id}/stream";
                public const string Crear = "api/v1/incidente-audio";
                public const string Editar = "api/v1/incidente-audio/{id}";
                public const string Eliminar = "api/v1/incidente-audio/{id}";
            }

            public static class Incidenteextraccion
            {
                public const string Obtener = "api/v1/incidente-extraccion";
                public const string ObtenerPorId = "api/v1/incidente-extraccion/{id}";
                public const string Crear = "api/v1/incidente-extraccion";
                public const string Editar = "api/v1/incidente-extraccion/{id}";
                public const string Eliminar = "api/v1/incidente-extraccion/{id}";
            }

            public static class Incidenteubicacion
            {
                public const string Obtener = "api/v1/incidente-ubicacion";
                public const string ObtenerPorId = "api/v1/incidente-ubicacion/{id}";
                public const string Crear = "api/v1/incidente-ubicacion";
                public const string Editar = "api/v1/incidente-ubicacion/{id}";
                public const string Eliminar = "api/v1/incidente-ubicacion/{id}";
            }

            public static class Catalogohechos
            {
                public const string Obtener = "api/v1/catalogo-hechos";
                public const string ObtenerPorId = "api/v1/catalogo-hechos/{id}";
                public const string Crear = "api/v1/catalogo-hechos";
                public const string Editar = "api/v1/catalogo-hechos/{id}";
                public const string Eliminar = "api/v1/catalogo-hechos/{id}";
            }

            public static class Incidentejobs
            {
                public const string Obtener = "api/v1/incidente-jobs";
                public const string ObtenerPorId = "api/v1/incidente-jobs/{id}";
                public const string Crear = "api/v1/incidente-jobs";
                public const string Editar = "api/v1/incidente-jobs/{id}";
                public const string Eliminar = "api/v1/incidente-jobs/{id}";
                public const string Reintentar = "api/v1/incidente-jobs/{id}/retry";
            }

        }
    }
}
