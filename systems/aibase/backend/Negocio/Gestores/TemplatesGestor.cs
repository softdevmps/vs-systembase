using Backend.Data;
using Backend.Models.Templates;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class TemplatesGestor
    {
        public static List<TemplatesResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [Key], [Name], [Description], [PipelineJson], [IsActive], [Version], [CreatedAt], [UpdatedAt] FROM [sys_aibase].[Templates]");
            sql.Append(" WHERE [IsActive] = 1");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<TemplatesResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static TemplatesResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [Key], [Name], [Description], [PipelineJson], [IsActive], [Version], [CreatedAt], [UpdatedAt] FROM [sys_aibase].[Templates] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(TemplatesCreateRequest request)
        {
            using var conn = Db.Open();
            if (string.IsNullOrWhiteSpace(request.Key)) return (false, "Campo requerido: Key");
            if (request.Key != null && request.Key.Length > 80) return (false, "MaxLength excedido: Key");
            if (!string.IsNullOrWhiteSpace(request.Key) && ExistsByValue(conn, "sys_aibase", "Templates", "Key", request.Key!, null, null)) return (false, "Valor duplicado en Key");
            if (string.IsNullOrWhiteSpace(request.Name)) return (false, "Campo requerido: Name");
            if (request.Name != null && request.Name.Length > 200) return (false, "MaxLength excedido: Name");
            if (request.Description != null && request.Description.Length > 500) return (false, "MaxLength excedido: Description");
            if (string.IsNullOrWhiteSpace(request.Pipelinejson)) return (false, "Campo requerido: PipelineJson");
            if (string.IsNullOrWhiteSpace(request.Version)) return (false, "Campo requerido: Version");
            if (request.Version != null && request.Version.Length > 20) return (false, "MaxLength excedido: Version");

            var sql = "INSERT INTO [sys_aibase].[Templates] ([Key], [Name], [Description], [PipelineJson], [IsActive], [Version], [CreatedAt], [UpdatedAt]) VALUES (@Key, @Name, @Description, @PipelineJson, @IsActive, @Version, @CreatedAt, @UpdatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Key", request.Key ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Name", request.Name ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@PipelineJson", request.Pipelinejson ?? "{}");
            cmd.Parameters.AddWithValue("@IsActive", request.Isactive);
            cmd.Parameters.AddWithValue("@Version", request.Version ?? "1.0");
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, TemplatesUpdateRequest request)
        {
            using var conn = Db.Open();
            if (string.IsNullOrWhiteSpace(request.Key)) return (false, "Campo requerido: Key");
            if (request.Key != null && request.Key.Length > 80) return (false, "MaxLength excedido: Key");
            if (!string.IsNullOrWhiteSpace(request.Key) && ExistsByValue(conn, "sys_aibase", "Templates", "Key", request.Key!, "Id", id)) return (false, "Valor duplicado en Key");
            if (string.IsNullOrWhiteSpace(request.Name)) return (false, "Campo requerido: Name");
            if (request.Name != null && request.Name.Length > 200) return (false, "MaxLength excedido: Name");
            if (request.Description != null && request.Description.Length > 500) return (false, "MaxLength excedido: Description");
            if (string.IsNullOrWhiteSpace(request.Pipelinejson)) return (false, "Campo requerido: PipelineJson");
            if (string.IsNullOrWhiteSpace(request.Version)) return (false, "Campo requerido: Version");
            if (request.Version != null && request.Version.Length > 20) return (false, "MaxLength excedido: Version");
            var sql = "UPDATE [sys_aibase].[Templates] SET [Key] = @Key, [Name] = @Name, [Description] = @Description, [PipelineJson] = @PipelineJson, [IsActive] = @IsActive, [Version] = @Version, [CreatedAt] = @CreatedAt, [UpdatedAt] = @UpdatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Key", request.Key ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Name", request.Name ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@PipelineJson", request.Pipelinejson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", request.Isactive);
            cmd.Parameters.AddWithValue("@Version", request.Version ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "UPDATE [sys_aibase].[Templates] SET [IsActive] = 0 WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static TemplatesResponse MapToResponse(SqlDataReader reader)
        {
            return new TemplatesResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Key = reader["Key"] == DBNull.Value ? null : reader["Key"].ToString(),
                Name = reader["Name"] == DBNull.Value ? null : reader["Name"].ToString(),
                Description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
                Pipelinejson = reader["PipelineJson"] == DBNull.Value ? null : reader["PipelineJson"].ToString(),
                Isactive = reader["IsActive"] == DBNull.Value ? default(bool) : (bool)Convert.ChangeType(reader["IsActive"], typeof(bool)),
                Version = reader["Version"] == DBNull.Value ? null : reader["Version"].ToString(),
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
