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

            public static class Menu
            {
                public const string Obtener = "api/v1/menu";
                public const string Tree = "api/v1/menu/tree";
                public const string SidebarTree = "api/v1/menu/sidebar";
                public const string Crear = "api/v1/menu";
                public const string Editar = "api/v1/menu/{id}";
                public const string Desactivar = "api/v1/menu/desactivar/{id}";
            }

            public static class Roles
            {
                public const string Obtener = "api/v1/roles";
                public const string ObtenerPorId = "api/v1/roles/{id}";
                public const string Crear = "api/v1/roles";
                public const string Editar = "api/v1/roles/{id}";
                public const string Estado = "api/v1/roles/{id}/estado";
                public const string AsignarMenus = "api/v1/roles/{id}/menus";
            }

            public static class Usuarios
            {
                public const string Obtener = "api/v1/usuarios";
                public const string ObtenerPorId = "api/v1/usuarios/{id}";
                public const string Crear = "api/v1/usuarios";
                public const string Editar = "api/v1/usuarios/{id}";
                public const string Estado = "api/v1/usuarios/{id}/estado";
            }

            public static class Sistemas
            {
                public const string Obtener = "api/v1/sistemas";
                public const string ObtenerPorId = "api/v1/sistemas/{id}";
                public const string ObtenerPorSlug = "api/v1/sistemas/slug/{slug}";
                public const string Crear = "api/v1/sistemas";
                public const string Editar = "api/v1/sistemas/{id}";
                public const string Publicar = "api/v1/sistemas/{id}/publicar";
            }

            public static class Entidades
            {
                public const string Obtener = "api/v1/sistemas/{systemId}/entidades";
                public const string ObtenerPorId = "api/v1/sistemas/{systemId}/entidades/{id}";
                public const string ObtenerPorNombre = "api/v1/sistemas/{systemId}/entidades/by-name/{name}";
                public const string Crear = "api/v1/sistemas/{systemId}/entidades";
                public const string Editar = "api/v1/sistemas/{systemId}/entidades/{id}";
            }

            public static class Campos
            {
                public const string Obtener = "api/v1/sistemas/{systemId}/entidades/{entityId}/campos";
                public const string Crear = "api/v1/sistemas/{systemId}/entidades/{entityId}/campos";
                public const string Editar = "api/v1/sistemas/{systemId}/entidades/{entityId}/campos/{id}";
            }

            public static class Datos
            {
                public const string Obtener = "api/v1/sistemas/{systemId}/entidades/{entityId}/datos";
                public const string Crear = "api/v1/sistemas/{systemId}/entidades/{entityId}/datos";
                public const string Editar = "api/v1/sistemas/{systemId}/entidades/{entityId}/datos/{id}";
                public const string Eliminar = "api/v1/sistemas/{systemId}/entidades/{entityId}/datos/{id}";
            }

            public static class Relaciones
            {
                public const string Obtener = "api/v1/sistemas/{systemId}/relaciones";
                public const string Crear = "api/v1/sistemas/{systemId}/relaciones";
                public const string Editar = "api/v1/sistemas/{systemId}/relaciones/{id}";
            }

        }
    }
}
