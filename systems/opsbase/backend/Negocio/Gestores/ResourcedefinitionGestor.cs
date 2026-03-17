using Backend.Data;
using Backend.Models.Resourcedefinition;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class ResourcedefinitionGestor
    {
        public static List<ResourcedefinitionResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [Codigo], [Nombre], [Descripcion], [TrackMode], [IsActive], [CreatedAt], [UpdatedAt] FROM [sys_opsbase].[ResourceDefinition]");
            sql.Append(" WHERE [IsActive] = 1");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<ResourcedefinitionResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static ResourcedefinitionResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [Codigo], [Nombre], [Descripcion], [TrackMode], [IsActive], [CreatedAt], [UpdatedAt] FROM [sys_opsbase].[ResourceDefinition] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(ResourcedefinitionCreateRequest request)
        {
            using var conn = Db.Open();
            if (string.IsNullOrWhiteSpace(request.Codigo)) return (false, "Campo requerido: Codigo");
            if (request.Codigo != null && request.Codigo.Length > 80) return (false, "MaxLength excedido: Codigo");
            if (!string.IsNullOrWhiteSpace(request.Codigo) && ExistsByValue(conn, "sys_opsbase", "ResourceDefinition", "Codigo", request.Codigo!, null, null)) return (false, "Valor duplicado en Codigo");
            if (string.IsNullOrWhiteSpace(request.Nombre)) return (false, "Campo requerido: Nombre");
            if (request.Nombre != null && request.Nombre.Length > 120) return (false, "MaxLength excedido: Nombre");
            if (request.Descripcion != null && request.Descripcion.Length > 300) return (false, "MaxLength excedido: Descripcion");
            if (string.IsNullOrWhiteSpace(request.Trackmode)) return (false, "Campo requerido: TrackMode");
            if (request.Trackmode != null && request.Trackmode.Length > 30) return (false, "MaxLength excedido: TrackMode");

            var sql = "INSERT INTO [sys_opsbase].[ResourceDefinition] ([Codigo], [Nombre], [Descripcion], [TrackMode], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (@Codigo, @Nombre, @Descripcion, @TrackMode, @IsActive, @CreatedAt, @UpdatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Codigo", request.Codigo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Nombre", request.Nombre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Descripcion", request.Descripcion ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TrackMode", request.Trackmode ?? "'none'");
            cmd.Parameters.AddWithValue("@IsActive", request.Isactive);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, ResourcedefinitionUpdateRequest request)
        {
            using var conn = Db.Open();
            if (string.IsNullOrWhiteSpace(request.Codigo)) return (false, "Campo requerido: Codigo");
            if (request.Codigo != null && request.Codigo.Length > 80) return (false, "MaxLength excedido: Codigo");
            if (!string.IsNullOrWhiteSpace(request.Codigo) && ExistsByValue(conn, "sys_opsbase", "ResourceDefinition", "Codigo", request.Codigo!, "Id", id)) return (false, "Valor duplicado en Codigo");
            if (string.IsNullOrWhiteSpace(request.Nombre)) return (false, "Campo requerido: Nombre");
            if (request.Nombre != null && request.Nombre.Length > 120) return (false, "MaxLength excedido: Nombre");
            if (request.Descripcion != null && request.Descripcion.Length > 300) return (false, "MaxLength excedido: Descripcion");
            if (string.IsNullOrWhiteSpace(request.Trackmode)) return (false, "Campo requerido: TrackMode");
            if (request.Trackmode != null && request.Trackmode.Length > 30) return (false, "MaxLength excedido: TrackMode");
            var sql = "UPDATE [sys_opsbase].[ResourceDefinition] SET [Codigo] = @Codigo, [Nombre] = @Nombre, [Descripcion] = @Descripcion, [TrackMode] = @TrackMode, [IsActive] = @IsActive, [CreatedAt] = @CreatedAt, [UpdatedAt] = @UpdatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Codigo", request.Codigo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Nombre", request.Nombre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Descripcion", request.Descripcion ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TrackMode", request.Trackmode ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", request.Isactive);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "UPDATE [sys_opsbase].[ResourceDefinition] SET [IsActive] = 0 WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static ResourcedefinitionResponse MapToResponse(SqlDataReader reader)
        {
            return new ResourcedefinitionResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Codigo = reader["Codigo"] == DBNull.Value ? null : reader["Codigo"].ToString(),
                Nombre = reader["Nombre"] == DBNull.Value ? null : reader["Nombre"].ToString(),
                Descripcion = reader["Descripcion"] == DBNull.Value ? null : reader["Descripcion"].ToString(),
                Trackmode = reader["TrackMode"] == DBNull.Value ? null : reader["TrackMode"].ToString(),
                Isactive = reader["IsActive"] == DBNull.Value ? default(bool) : (bool)Convert.ChangeType(reader["IsActive"], typeof(bool)),
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
