using Backend.Data;
using Backend.Models.Rubro;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class RubroGestor
    {
        public static List<RubroResponse> ObtenerTodos()
        {
            using var conn = Db.Open();
            RubroSchemaHelper.EnsureSchema(conn);

            const string sql = @"SELECT [Id], [Codigo], [Nombre], [Descripcion], [ColorHex], [IsActive], [CreatedAt], [UpdatedAt]
FROM [sys_opsbase].[Rubro]
WHERE [IsActive] = 1
ORDER BY [Nombre] ASC, [Codigo] ASC;";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<RubroResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static RubroResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            RubroSchemaHelper.EnsureSchema(conn);

            const string sql = @"SELECT [Id], [Codigo], [Nombre], [Descripcion], [ColorHex], [IsActive], [CreatedAt], [UpdatedAt]
FROM [sys_opsbase].[Rubro]
WHERE [Id] = @id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(RubroCreateRequest request)
        {
            using var conn = Db.Open();
            RubroSchemaHelper.EnsureSchema(conn);

            if (string.IsNullOrWhiteSpace(request.Codigo)) return (false, "Campo requerido: Codigo");
            if (request.Codigo.Length > 60) return (false, "MaxLength excedido: Codigo");
            if (ExistsCodigo(conn, request.Codigo.Trim(), null)) return (false, "Valor duplicado en Codigo");
            if (string.IsNullOrWhiteSpace(request.Nombre)) return (false, "Campo requerido: Nombre");
            if (request.Nombre.Length > 120) return (false, "MaxLength excedido: Nombre");
            if (request.Descripcion != null && request.Descripcion.Length > 300) return (false, "MaxLength excedido: Descripcion");
            if (request.Colorhex != null && request.Colorhex.Length > 20) return (false, "MaxLength excedido: ColorHex");

            var createdAt = request.Createdat == default ? DateTime.UtcNow : request.Createdat;
            var updatedAt = request.Updatedat;

            const string sql = @"INSERT INTO [sys_opsbase].[Rubro]
([Codigo], [Nombre], [Descripcion], [ColorHex], [IsActive], [CreatedAt], [UpdatedAt])
VALUES
(@Codigo, @Nombre, @Descripcion, @ColorHex, @IsActive, @CreatedAt, @UpdatedAt);";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Codigo", request.Codigo.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
            cmd.Parameters.AddWithValue("@Descripcion", request.Descripcion ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ColorHex", request.Colorhex ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", request.Isactive);
            cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
            cmd.Parameters.AddWithValue("@UpdatedAt", updatedAt ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();

            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, RubroUpdateRequest request)
        {
            using var conn = Db.Open();
            RubroSchemaHelper.EnsureSchema(conn);

            if (string.IsNullOrWhiteSpace(request.Codigo)) return (false, "Campo requerido: Codigo");
            if (request.Codigo.Length > 60) return (false, "MaxLength excedido: Codigo");
            if (ExistsCodigo(conn, request.Codigo.Trim(), id)) return (false, "Valor duplicado en Codigo");
            if (string.IsNullOrWhiteSpace(request.Nombre)) return (false, "Campo requerido: Nombre");
            if (request.Nombre.Length > 120) return (false, "MaxLength excedido: Nombre");
            if (request.Descripcion != null && request.Descripcion.Length > 300) return (false, "MaxLength excedido: Descripcion");
            if (request.Colorhex != null && request.Colorhex.Length > 20) return (false, "MaxLength excedido: ColorHex");

            var createdAt = request.Createdat == default ? DateTime.UtcNow : request.Createdat;
            var updatedAt = request.Updatedat ?? DateTime.UtcNow;

            const string sql = @"UPDATE [sys_opsbase].[Rubro]
SET [Codigo] = @Codigo,
    [Nombre] = @Nombre,
    [Descripcion] = @Descripcion,
    [ColorHex] = @ColorHex,
    [IsActive] = @IsActive,
    [CreatedAt] = @CreatedAt,
    [UpdatedAt] = @UpdatedAt
WHERE [Id] = @id;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Codigo", request.Codigo.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
            cmd.Parameters.AddWithValue("@Descripcion", request.Descripcion ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ColorHex", request.Colorhex ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", request.Isactive);
            cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
            cmd.Parameters.AddWithValue("@UpdatedAt", updatedAt);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            RubroSchemaHelper.EnsureSchema(conn);

            const string sql = @"UPDATE [sys_opsbase].[Rubro]
SET [IsActive] = 0,
    [UpdatedAt] = @updatedAt
WHERE [Id] = @id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static RubroResponse MapToResponse(SqlDataReader reader)
        {
            return new RubroResponse
            {
                Id = reader["Id"] == DBNull.Value ? default : Convert.ToInt32(reader["Id"]),
                Codigo = reader["Codigo"] == DBNull.Value ? string.Empty : reader["Codigo"].ToString() ?? string.Empty,
                Nombre = reader["Nombre"] == DBNull.Value ? string.Empty : reader["Nombre"].ToString() ?? string.Empty,
                Descripcion = reader["Descripcion"] == DBNull.Value ? null : reader["Descripcion"].ToString(),
                Colorhex = reader["ColorHex"] == DBNull.Value ? null : reader["ColorHex"].ToString(),
                Isactive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
                Createdat = reader["CreatedAt"] == DBNull.Value ? default : Convert.ToDateTime(reader["CreatedAt"]),
                Updatedat = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
            };
        }

        private static bool ExistsCodigo(SqlConnection conn, string codigo, int? excludeId)
        {
            var sql = @"SELECT COUNT(1)
FROM [sys_opsbase].[Rubro]
WHERE UPPER(LTRIM(RTRIM([Codigo]))) = UPPER(LTRIM(RTRIM(@codigo)))";
            if (excludeId.HasValue)
                sql += " AND [Id] <> @id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@codigo", codigo);
            if (excludeId.HasValue)
                cmd.Parameters.AddWithValue("@id", excludeId.Value);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
    }
}
