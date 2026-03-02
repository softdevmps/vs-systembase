using Backend.Data;
using Backend.Models.AiBase;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class AibaseProjectsGestor
    {
        public static List<AibaseProjectResponse> ObtenerTodos()
        {
            using var conn = Db.Open();
            const string sql = @"
SELECT
    p.Id,
    p.Slug,
    p.[Name],
    p.[Description],
    p.[Language],
    p.Tone,
    p.[Status],
    p.IsActive,
    p.TemplateId,
    t.[Key] AS TemplateKey,
    t.[Name] AS TemplateName,
    p.CreatedByUserId,
    p.CreatedAt,
    p.UpdatedAt
FROM sb_ai.Projects p
INNER JOIN sb_ai.Templates t ON t.Id = p.TemplateId
ORDER BY p.CreatedAt DESC";

            using var cmd = new SqlCommand(sql, conn);
            using var rd = cmd.ExecuteReader();

            var result = new List<AibaseProjectResponse>();
            while (rd.Read())
            {
                result.Add(Map(rd));
            }

            return result;
        }

        public static AibaseProjectResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            const string sql = @"
SELECT
    p.Id,
    p.Slug,
    p.[Name],
    p.[Description],
    p.[Language],
    p.Tone,
    p.[Status],
    p.IsActive,
    p.TemplateId,
    t.[Key] AS TemplateKey,
    t.[Name] AS TemplateName,
    p.CreatedByUserId,
    p.CreatedAt,
    p.UpdatedAt
FROM sb_ai.Projects p
INNER JOIN sb_ai.Templates t ON t.Id = p.TemplateId
WHERE p.Id = @id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return null;
            return Map(rd);
        }

        public static (int? projectId, string? error) Crear(AibaseProjectCreateRequest request, int createdByUserId)
        {
            var slug = (request.Slug ?? string.Empty).Trim().ToLowerInvariant();
            if (!IsValidSlug(slug))
                return (null, "Slug invalido. Usa solo letras, numeros y guion bajo.");

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return (null, "Nombre requerido.");

            using var conn = Db.Open();

            const string sqlSlug = "SELECT COUNT(1) FROM sb_ai.Projects WHERE Slug = @slug";
            using (var cmdSlug = new SqlCommand(sqlSlug, conn))
            {
                cmdSlug.Parameters.AddWithValue("@slug", slug);
                var exists = Convert.ToInt32(cmdSlug.ExecuteScalar()) > 0;
                if (exists)
                    return (null, "El slug ya existe.");
            }

            const string sqlTemplate = "SELECT COUNT(1) FROM sb_ai.Templates WHERE Id = @id AND IsActive = 1";
            using (var cmdTpl = new SqlCommand(sqlTemplate, conn))
            {
                cmdTpl.Parameters.AddWithValue("@id", request.TemplateId);
                var exists = Convert.ToInt32(cmdTpl.ExecuteScalar()) > 0;
                if (!exists)
                    return (null, "Template no valido o inactivo.");
            }

            const string sqlInsert = @"
INSERT INTO sb_ai.Projects
    (Slug, [Name], [Description], [Language], Tone, [Status], IsActive, TemplateId, CreatedByUserId, CreatedAt)
VALUES
    (@slug, @name, @desc, @lang, @tone, 'draft', 1, @templateId, @createdBy, SYSUTCDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var cmdInsert = new SqlCommand(sqlInsert, conn);
            cmdInsert.Parameters.AddWithValue("@slug", slug);
            cmdInsert.Parameters.AddWithValue("@name", name);
            cmdInsert.Parameters.AddWithValue("@desc", (object?)request.Description?.Trim() ?? DBNull.Value);
            cmdInsert.Parameters.AddWithValue("@lang", string.IsNullOrWhiteSpace(request.Language) ? "es" : request.Language.Trim().ToLowerInvariant());
            cmdInsert.Parameters.AddWithValue("@tone", (object?)request.Tone?.Trim() ?? DBNull.Value);
            cmdInsert.Parameters.AddWithValue("@templateId", request.TemplateId);
            cmdInsert.Parameters.AddWithValue("@createdBy", createdByUserId);

            var id = cmdInsert.ExecuteScalar();
            return (id is int projectId ? projectId : Convert.ToInt32(id), null);
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

        private static AibaseProjectResponse Map(SqlDataReader rd)
        {
            return new AibaseProjectResponse
            {
                Id = rd.GetInt32(0),
                Slug = rd.GetString(1),
                Name = rd.GetString(2),
                Description = rd.IsDBNull(3) ? null : rd.GetString(3),
                Language = rd.IsDBNull(4) ? "es" : rd.GetString(4),
                Tone = rd.IsDBNull(5) ? null : rd.GetString(5),
                Status = rd.IsDBNull(6) ? "draft" : rd.GetString(6),
                IsActive = rd.GetBoolean(7),
                TemplateId = rd.GetInt32(8),
                TemplateKey = rd.IsDBNull(9) ? string.Empty : rd.GetString(9),
                TemplateName = rd.IsDBNull(10) ? string.Empty : rd.GetString(10),
                CreatedByUserId = rd.GetInt32(11),
                CreatedAt = rd.GetDateTime(12),
                UpdatedAt = rd.IsDBNull(13) ? null : rd.GetDateTime(13)
            };
        }
    }
}
