using Backend.Data;
using Backend.Models.AiBase;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class AibaseTemplatesGestor
    {
        public static List<AibaseTemplateResponse> ObtenerActivos()
        {
            using var conn = Db.Open();
            const string sql = @"
SELECT Id, [Key], [Name], [Description], IsActive, [Version]
FROM sb_ai.Templates
WHERE IsActive = 1
ORDER BY [Name] ASC";

            using var cmd = new SqlCommand(sql, conn);
            using var rd = cmd.ExecuteReader();

            var result = new List<AibaseTemplateResponse>();
            while (rd.Read())
            {
                result.Add(new AibaseTemplateResponse
                {
                    Id = rd.GetInt32(0),
                    Key = rd.GetString(1),
                    Name = rd.GetString(2),
                    Description = rd.IsDBNull(3) ? null : rd.GetString(3),
                    IsActive = rd.GetBoolean(4),
                    Version = rd.IsDBNull(5) ? "1.0" : rd.GetString(5)
                });
            }

            return result;
        }
    }
}
