using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Requests.Roles;
using Backend.Models.Responses.Roles;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Gestores
{
    public static class RolesGestor
    {

        public static List<RolResponse> ObtenerTodos()
        {
            using var context = new SystemBaseContext();

            return context.Roles
            .OrderBy(r => r.Id)
            .Select(r => new RolResponse
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Activo = r.Activo
            })
            .ToList();
        }

        public static RolDetalleResponse? ObtenerPorId(int id)
        {
            using var context = new SystemBaseContext();

            var rol = context.Roles
                .Include(r => r.Menu)
                .FirstOrDefault(r => r.Id == id);

            if (rol == null)
                return null;

            var menusAsignadosIds = rol.Menu
                .Select(m => m.Id)
                .ToHashSet();

            var menus = context.Menus
                .Where(m => m.Activo)
                .Select(m => new RolMenuResponse
                {
                    Id = m.Id,
                    Titulo = m.Titulo,
                    Icono = m.Icono,
                    Ruta = m.Ruta,
                    Asignado = menusAsignadosIds.Contains(m.Id)
                })
                .ToList();

            return new RolDetalleResponse
            {
                Id = rol.Id,
                Nombre = rol.Nombre,
                Activo = rol.Activo,
                Menus = menus
            };
        }

        public static bool Crear(RolCreateRequest request)
        {
            using var context = new SystemBaseContext();

            var rol = new Roles
            {
                Nombre = request.Nombre,
                Activo = request.Activo
            };

            context.Roles.Add(rol);
            context.SaveChanges();

            return true;
        }

        public static bool Editar(int id, RolUpdateRequest request)
        {
            using var context = new SystemBaseContext();

            var rol = context.Roles.FirstOrDefault(r => r.Id == id);
            if (rol == null)
                return false;

            rol.Nombre = request.Nombre;
            rol.Activo = request.Activo;

            context.SaveChanges();
            return true;
        }

        public static bool CambiarEstado(int id, bool activo)
        {
            using var context = new SystemBaseContext();

            var rol = context.Roles.FirstOrDefault(r => r.Id == id);
            if (rol == null)
                return false;

            rol.Activo = activo;
            context.SaveChanges();

            return true;
        }

        public static bool AsignarMenus(int rolId, List<int> menusIds)
        {
            using var context = new SystemBaseContext();

            var rol = context.Roles
                .Include(r => r.Menu)
                .FirstOrDefault(r => r.Id == rolId);

            if (rol == null)
                return false;

            rol.Menu.Clear();

            var menus = context.Menus
                .Where(m => menusIds.Contains(m.Id))
                .ToList();

            foreach (var menu in menus)
                rol.Menu.Add(menu);

            context.SaveChanges();
            return true;
        }
    }
}
