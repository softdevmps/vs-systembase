using Backend.Data;
using Backend.Models.AiBase;
using Backend.Models.Entidades;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Backend.Negocio.Gestores
{
    public static class AibaseProjectsGestor
    {
        public static List<AibaseProjectResponse> ObtenerTodos()
        {
            using var context = new SystemBaseContext();

            return context.AibaseProjects
                .Include(p => p.Template)
                .OrderByDescending(p => p.CreatedAt)
                .Select(MapToResponse())
                .ToList();
        }

        public static AibaseProjectResponse? ObtenerPorId(int id)
        {
            using var context = new SystemBaseContext();

            return context.AibaseProjects
                .Include(p => p.Template)
                .Where(p => p.Id == id)
                .Select(MapToResponse())
                .FirstOrDefault();
        }

        public static (int? projectId, string? error) Crear(AibaseProjectCreateRequest request, int createdByUserId)
        {
            using var context = new SystemBaseContext();

            var slug = (request.Slug ?? string.Empty).Trim().ToLowerInvariant();
            if (!IsValidSlug(slug))
                return (null, "Slug invalido. Usa solo letras, numeros y guion bajo.");

            if (context.AibaseProjects.Any(p => p.Slug == slug))
                return (null, "El slug ya existe.");

            var template = context.AibaseTemplates.FirstOrDefault(t => t.Id == request.TemplateId && t.IsActive);
            if (template == null)
                return (null, "Template no valido o inactivo.");

            var project = new AibaseProjects
            {
                Slug = slug,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Language = string.IsNullOrWhiteSpace(request.Language) ? "es" : request.Language.Trim().ToLowerInvariant(),
                Tone = request.Tone?.Trim(),
                Status = "draft",
                IsActive = true,
                TemplateId = template.Id,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow
            };

            context.AibaseProjects.Add(project);
            context.SaveChanges();

            return (project.Id, null);
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

        private static Expression<Func<AibaseProjects, AibaseProjectResponse>> MapToResponse()
        {
            return p => new AibaseProjectResponse
            {
                Id = p.Id,
                Slug = p.Slug,
                Name = p.Name,
                Description = p.Description,
                Language = p.Language,
                Tone = p.Tone,
                Status = p.Status,
                IsActive = p.IsActive,
                TemplateId = p.TemplateId,
                TemplateKey = p.Template.Key,
                TemplateName = p.Template.Name,
                CreatedByUserId = p.CreatedByUserId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
        }
    }
}
