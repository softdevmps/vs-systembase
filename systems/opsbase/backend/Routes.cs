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

            public static class Attributedefinition
            {
                public const string Obtener = "api/v1/attribute-definition";
                public const string ObtenerPorId = "api/v1/attribute-definition/{id}";
                public const string Crear = "api/v1/attribute-definition";
                public const string Editar = "api/v1/attribute-definition/{id}";
                public const string Eliminar = "api/v1/attribute-definition/{id}";
            }

            public static class Attributevalue
            {
                public const string Obtener = "api/v1/attribute-value";
                public const string ObtenerPorId = "api/v1/attribute-value/{id}";
                public const string Crear = "api/v1/attribute-value";
                public const string Editar = "api/v1/attribute-value/{id}";
                public const string Eliminar = "api/v1/attribute-value/{id}";
            }

            public static class Location
            {
                public const string Obtener = "api/v1/location";
                public const string ObtenerPorId = "api/v1/location/{id}";
                public const string Crear = "api/v1/location";
                public const string Editar = "api/v1/location/{id}";
                public const string Eliminar = "api/v1/location/{id}";
            }

            public static class Movement
            {
                public const string Obtener = "api/v1/movement";
                public const string ObtenerPorId = "api/v1/movement/{id}";
                public const string Crear = "api/v1/movement";
                public const string Editar = "api/v1/movement/{id}";
                public const string Eliminar = "api/v1/movement/{id}";
            }

            public static class Movementline
            {
                public const string Obtener = "api/v1/movement-line";
                public const string ObtenerPorId = "api/v1/movement-line/{id}";
                public const string Crear = "api/v1/movement-line";
                public const string Editar = "api/v1/movement-line/{id}";
                public const string Eliminar = "api/v1/movement-line/{id}";
            }

            public static class Operationaudit
            {
                public const string Obtener = "api/v1/operation-audit";
                public const string ObtenerPorId = "api/v1/operation-audit/{id}";
                public const string Crear = "api/v1/operation-audit";
                public const string Editar = "api/v1/operation-audit/{id}";
                public const string Eliminar = "api/v1/operation-audit/{id}";
            }

            public static class Resourcedefinition
            {
                public const string Obtener = "api/v1/resource-definition";
                public const string ObtenerPorId = "api/v1/resource-definition/{id}";
                public const string Crear = "api/v1/resource-definition";
                public const string Editar = "api/v1/resource-definition/{id}";
                public const string Eliminar = "api/v1/resource-definition/{id}";
            }

            public static class Resourceinstance
            {
                public const string Obtener = "api/v1/resource-instance";
                public const string ObtenerPorId = "api/v1/resource-instance/{id}";
                public const string Crear = "api/v1/resource-instance";
                public const string Editar = "api/v1/resource-instance/{id}";
                public const string Eliminar = "api/v1/resource-instance/{id}";
            }

            public static class Stockbalance
            {
                public const string Obtener = "api/v1/stock-balance";
                public const string ObtenerPorId = "api/v1/stock-balance/{id}";
                public const string Crear = "api/v1/stock-balance";
                public const string Editar = "api/v1/stock-balance/{id}";
                public const string Eliminar = "api/v1/stock-balance/{id}";
            }

        }
    }
}
