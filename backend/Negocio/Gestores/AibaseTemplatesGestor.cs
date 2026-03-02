using Backend.Data;
using Backend.Models.AiBase;

namespace Backend.Negocio.Gestores
{
    public static class AibaseTemplatesGestor
    {
        public static List<AibaseTemplateResponse> ObtenerActivos()
        {
            using var context = new SystemBaseContext();

            return context.AibaseTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new AibaseTemplateResponse
                {
                    Id = t.Id,
                    Key = t.Key,
                    Name = t.Name,
                    Description = t.Description,
                    IsActive = t.IsActive,
                    Version = t.Version
                })
                .ToList();
        }
    }
}
