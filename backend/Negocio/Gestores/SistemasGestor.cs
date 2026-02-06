using Backend.Data;
using Backend.Models.Entidades;
using Backend.Models.Sistemas;
using Microsoft.EntityFrameworkCore;

namespace Backend.Negocio.Gestores
{
    public static class SistemasGestor
    {
        public static List<SistemaResponse> ObtenerTodos()
        {
            using var context = new SystemBaseContext();

            return context.Systems
                .OrderBy(s => s.Id)
                .Select(s => new SistemaResponse
                {
                    Id = s.Id,
                    Slug = s.Slug,
                    Name = s.Name,
                    Namespace = s.Namespace,
                    Status = s.Status,
                    IsActive = s.IsActive,
                    Version = s.Version,
                    Description = s.Description
                })
                .ToList();
        }

        public static SistemaDetalleResponse? ObtenerPorId(int id)
        {
            using var context = new SystemBaseContext();

            var sistema = context.Systems.FirstOrDefault(s => s.Id == id);
            if (sistema == null)
                return null;

            return new SistemaDetalleResponse
            {
                Id = sistema.Id,
                Slug = sistema.Slug,
                Name = sistema.Name,
                Namespace = sistema.Namespace,
                Status = sistema.Status,
                IsActive = sistema.IsActive,
                Version = sistema.Version,
                Description = sistema.Description,
                CreatedAt = sistema.CreatedAt,
                UpdatedAt = sistema.UpdatedAt,
                PublishedAt = sistema.PublishedAt
            };
        }

        public static SistemaDetalleResponse? ObtenerPorSlug(string slug)
        {
            using var context = new SystemBaseContext();

            var sistema = context.Systems.FirstOrDefault(s => s.Slug == slug);
            if (sistema == null)
                return null;

            return new SistemaDetalleResponse
            {
                Id = sistema.Id,
                Slug = sistema.Slug,
                Name = sistema.Name,
                Namespace = sistema.Namespace,
                Status = sistema.Status,
                IsActive = sistema.IsActive,
                Version = sistema.Version,
                Description = sistema.Description,
                CreatedAt = sistema.CreatedAt,
                UpdatedAt = sistema.UpdatedAt,
                PublishedAt = sistema.PublishedAt
            };
        }

        public static int? Crear(SistemaCreateRequest request)
        {
            using var context = new SystemBaseContext();

            var slug = request.Slug.Trim().ToLowerInvariant();
            if (!IsValidSlug(slug))
                return null;
            var exists = context.Systems.Any(s => s.Slug == slug);
            if (exists)
                return null;

            var sistema = new Systems
            {
                Slug = slug,
                Name = request.Name.Trim(),
                Namespace = request.Namespace.Trim(),
                Description = request.Description?.Trim(),
                Version = request.Version?.Trim(),
                Status = "draft",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Systems.Add(sistema);
            context.SaveChanges();

            return sistema.Id;
        }

        public static bool Editar(int id, SistemaUpdateRequest request)
        {
            using var context = new SystemBaseContext();

            var sistema = context.Systems.FirstOrDefault(s => s.Id == id);
            if (sistema == null)
                return false;

            sistema.Name = request.Name.Trim();
            sistema.Namespace = request.Namespace.Trim();
            sistema.Description = request.Description?.Trim();
            sistema.Version = request.Version?.Trim();
            sistema.IsActive = request.IsActive;
            sistema.UpdatedAt = DateTime.UtcNow;

            context.SaveChanges();
            return true;
        }

        private static bool IsValidSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            if (char.IsDigit(slug[0]))
                return false;

            foreach (var ch in slug)
            {
                if (!(char.IsLetterOrDigit(ch) || ch == '_'))
                    return false;
            }

            return true;
        }
    }
}
