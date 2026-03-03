using Backend.Data;
using Backend.Models.Projects;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class ProjectsGestor
    {
        public static List<ProjectsResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [Slug], [Name], [Description], [Language], [Tone], [Status], [IsActive], [TemplateId], [TenantId], [CreatedByUserId], [CreatedAt], [UpdatedAt] FROM [sys_aibase].[Projects]");
            sql.Append(" WHERE [IsActive] = 1");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<ProjectsResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static ProjectsResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [Slug], [Name], [Description], [Language], [Tone], [Status], [IsActive], [TemplateId], [TenantId], [CreatedByUserId], [CreatedAt], [UpdatedAt] FROM [sys_aibase].[Projects] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(ProjectsCreateRequest request)
        {
            using var conn = Db.Open();
            if (string.IsNullOrWhiteSpace(request.Slug)) return (false, "Campo requerido: Slug");
            if (request.Slug != null && request.Slug.Length > 80) return (false, "MaxLength excedido: Slug");
            if (!string.IsNullOrWhiteSpace(request.Slug) && ExistsByValue(conn, "sys_aibase", "Projects", "Slug", request.Slug!, null, null)) return (false, "Valor duplicado en Slug");
            if (string.IsNullOrWhiteSpace(request.Name)) return (false, "Campo requerido: Name");
            if (request.Name != null && request.Name.Length > 200) return (false, "MaxLength excedido: Name");
            if (request.Description != null && request.Description.Length > 500) return (false, "MaxLength excedido: Description");
            if (string.IsNullOrWhiteSpace(request.Language)) return (false, "Campo requerido: Language");
            if (request.Language != null && request.Language.Length > 10) return (false, "MaxLength excedido: Language");
            if (request.Tone != null && request.Tone.Length > 100) return (false, "MaxLength excedido: Tone");
            if (string.IsNullOrWhiteSpace(request.Status)) return (false, "Campo requerido: Status");
            if (request.Status != null && request.Status.Length > 30) return (false, "MaxLength excedido: Status");
            if (!ExistsByValue(conn, "sys_aibase", "Templates", "Id", request.Templateid, null, null)) return (false, "Templates inexistente (TemplateId)");

            var sql = "INSERT INTO [sys_aibase].[Projects] ([Slug], [Name], [Description], [Language], [Tone], [Status], [IsActive], [TemplateId], [TenantId], [CreatedByUserId], [CreatedAt], [UpdatedAt]) VALUES (@Slug, @Name, @Description, @Language, @Tone, @Status, @IsActive, @TemplateId, @TenantId, @CreatedByUserId, @CreatedAt, @UpdatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Slug", request.Slug ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Name", request.Name ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Language", request.Language ?? "es");
            cmd.Parameters.AddWithValue("@Tone", request.Tone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", request.Status ?? "draft");
            cmd.Parameters.AddWithValue("@IsActive", request.Isactive);
            cmd.Parameters.AddWithValue("@TemplateId", request.Templateid);
            cmd.Parameters.AddWithValue("@TenantId", request.Tenantid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedByUserId", request.Createdbyuserid);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, ProjectsUpdateRequest request)
        {
            using var conn = Db.Open();
            if (string.IsNullOrWhiteSpace(request.Slug)) return (false, "Campo requerido: Slug");
            if (request.Slug != null && request.Slug.Length > 80) return (false, "MaxLength excedido: Slug");
            if (!string.IsNullOrWhiteSpace(request.Slug) && ExistsByValue(conn, "sys_aibase", "Projects", "Slug", request.Slug!, "Id", id)) return (false, "Valor duplicado en Slug");
            if (string.IsNullOrWhiteSpace(request.Name)) return (false, "Campo requerido: Name");
            if (request.Name != null && request.Name.Length > 200) return (false, "MaxLength excedido: Name");
            if (request.Description != null && request.Description.Length > 500) return (false, "MaxLength excedido: Description");
            if (string.IsNullOrWhiteSpace(request.Language)) return (false, "Campo requerido: Language");
            if (request.Language != null && request.Language.Length > 10) return (false, "MaxLength excedido: Language");
            if (request.Tone != null && request.Tone.Length > 100) return (false, "MaxLength excedido: Tone");
            if (string.IsNullOrWhiteSpace(request.Status)) return (false, "Campo requerido: Status");
            if (request.Status != null && request.Status.Length > 30) return (false, "MaxLength excedido: Status");
            if (!ExistsByValue(conn, "sys_aibase", "Templates", "Id", request.Templateid, null, null)) return (false, "Templates inexistente (TemplateId)");
            var sql = "UPDATE [sys_aibase].[Projects] SET [Slug] = @Slug, [Name] = @Name, [Description] = @Description, [Language] = @Language, [Tone] = @Tone, [Status] = @Status, [IsActive] = @IsActive, [TemplateId] = @TemplateId, [TenantId] = @TenantId, [CreatedByUserId] = @CreatedByUserId, [CreatedAt] = @CreatedAt, [UpdatedAt] = @UpdatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Slug", request.Slug ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Name", request.Name ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Language", request.Language ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Tone", request.Tone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", request.Status ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", request.Isactive);
            cmd.Parameters.AddWithValue("@TemplateId", request.Templateid);
            cmd.Parameters.AddWithValue("@TenantId", request.Tenantid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedByUserId", request.Createdbyuserid);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "UPDATE [sys_aibase].[Projects] SET [IsActive] = 0 WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static ProjectsResponse MapToResponse(SqlDataReader reader)
        {
            return new ProjectsResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Slug = reader["Slug"] == DBNull.Value ? null : reader["Slug"].ToString(),
                Name = reader["Name"] == DBNull.Value ? null : reader["Name"].ToString(),
                Description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
                Language = reader["Language"] == DBNull.Value ? null : reader["Language"].ToString(),
                Tone = reader["Tone"] == DBNull.Value ? null : reader["Tone"].ToString(),
                Status = reader["Status"] == DBNull.Value ? null : reader["Status"].ToString(),
                Isactive = reader["IsActive"] == DBNull.Value ? default(bool) : (bool)Convert.ChangeType(reader["IsActive"], typeof(bool)),
                Templateid = reader["TemplateId"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["TemplateId"], typeof(int)),
                Tenantid = reader["TenantId"] == DBNull.Value ? null : (int)Convert.ChangeType(reader["TenantId"], typeof(int)),
                Createdbyuserid = reader["CreatedByUserId"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["CreatedByUserId"], typeof(int)),
                Createdat = reader["CreatedAt"] == DBNull.Value ? default(DateTime) : (DateTime)Convert.ChangeType(reader["CreatedAt"], typeof(DateTime)),
                Updatedat = reader["UpdatedAt"] == DBNull.Value ? null : (DateTime)Convert.ChangeType(reader["UpdatedAt"], typeof(DateTime)),
            };
        }

        private static bool ExistsByValue(SqlConnection conn, string schema, string table, string column, object value, string? idColumn, object? idValue)
        {
            var sql = $"SELECT COUNT(1) FROM [{schema}].[{table}] WHERE [{column}] = @val";
            if (!string.IsNullOrWhiteSpace(idColumn))
                sql += $" AND [{idColumn}] <> @id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@val", value);
            if (!string.IsNullOrWhiteSpace(idColumn))
                cmd.Parameters.AddWithValue("@id", idValue!);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
    }
}
