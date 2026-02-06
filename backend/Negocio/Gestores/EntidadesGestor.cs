using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Sistemas;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Gestores
{
    public static class EntidadesGestor
    {
        public static List<EntidadResponse> ObtenerPorSistema(int systemId)
        {
            using var context = new SystemBaseContext();

            return context.Entities
                .Where(e => e.SystemId == systemId)
                .OrderBy(e => e.SortOrder)
                .ThenBy(e => e.Id)
                .Select(e => new EntidadResponse
                {
                    Id = e.Id,
                    SystemId = e.SystemId,
                    Name = e.Name,
                    TableName = e.TableName,
                    DisplayName = e.DisplayName,
                    Description = e.Description,
                    IsActive = e.IsActive,
                    SortOrder = e.SortOrder
                })
                .ToList();
        }

        public static EntidadResponse? ObtenerPorId(int systemId, int id)
        {
            using var context = new SystemBaseContext();

            var entity = context.Entities.FirstOrDefault(e => e.Id == id && e.SystemId == systemId);
            if (entity == null)
                return null;

            return new EntidadResponse
            {
                Id = entity.Id,
                SystemId = entity.SystemId,
                Name = entity.Name,
                TableName = entity.TableName,
                DisplayName = entity.DisplayName,
                Description = entity.Description,
                IsActive = entity.IsActive,
                SortOrder = entity.SortOrder
            };
        }

        public static EntidadResponse? ObtenerPorNombre(int systemId, string name)
        {
            using var context = new SystemBaseContext();

            var entity = context.Entities.FirstOrDefault(e => e.SystemId == systemId && e.Name == name);
            if (entity == null)
                return null;

            return new EntidadResponse
            {
                Id = entity.Id,
                SystemId = entity.SystemId,
                Name = entity.Name,
                TableName = entity.TableName,
                DisplayName = entity.DisplayName,
                Description = entity.Description,
                IsActive = entity.IsActive,
                SortOrder = entity.SortOrder
            };
        }

        public static int? Crear(int systemId, EntidadCreateRequest request)
        {
            using var context = new SystemBaseContext();

            var sistemaExiste = context.Systems.Any(s => s.Id == systemId);
            if (!sistemaExiste)
                return null;

            var name = request.Name.Trim();
            var tableName = request.TableName.Trim();

            var duplicated = context.Entities.Any(e =>
                e.SystemId == systemId &&
                (e.Name == name || e.TableName == tableName));

            if (duplicated)
                return null;

            var entidad = new Entities
            {
                SystemId = systemId,
                Name = name,
                TableName = tableName,
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? name : request.DisplayName.Trim(),
                Description = request.Description?.Trim(),
                SortOrder = request.SortOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Entities.Add(entidad);
            context.SaveChanges();

            return entidad.Id;
        }

        public static bool Editar(int systemId, int id, EntidadUpdateRequest request)
        {
            using var context = new SystemBaseContext();

            var entidad = context.Entities.FirstOrDefault(e => e.Id == id && e.SystemId == systemId);
            if (entidad == null)
                return false;

            var name = request.Name.Trim();
            var tableName = request.TableName.Trim();

            var duplicated = context.Entities.Any(e =>
                e.SystemId == systemId &&
                e.Id != id &&
                (e.Name == name || e.TableName == tableName));

            if (duplicated)
                return false;

            entidad.Name = name;
            entidad.TableName = tableName;
            entidad.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? name : request.DisplayName.Trim();
            entidad.Description = request.Description?.Trim();
            entidad.SortOrder = request.SortOrder;
            entidad.IsActive = request.IsActive;
            entidad.UpdatedAt = DateTime.UtcNow;

            context.SaveChanges();
            return true;
        }
    }
}
