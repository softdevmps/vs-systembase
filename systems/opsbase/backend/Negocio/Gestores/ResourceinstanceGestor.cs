using Backend.Data;
using Backend.Models.Resourceinstance;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class ResourceinstanceGestor
    {
        public static List<ResourceinstanceResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [ResourceDefinitionId], [CodigoInterno], [Estado], [Serie], [Lote], [Vencimiento], [IsActive], [CreatedAt], [UpdatedAt] FROM [sys_opsbase].[ResourceInstance]");
            sql.Append(" WHERE [IsActive] = 1");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<ResourceinstanceResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static ResourceinstanceResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [ResourceDefinitionId], [CodigoInterno], [Estado], [Serie], [Lote], [Vencimiento], [IsActive], [CreatedAt], [UpdatedAt] FROM [sys_opsbase].[ResourceInstance] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(ResourceinstanceCreateRequest request)
        {
            using var conn = Db.Open();
            if (!ExistsByValue(conn, "sys_opsbase", "ResourceDefinition", "Id", request.Resourcedefinitionid, null, null)) return (false, "ResourceDefinition inexistente (ResourceDefinitionId)");
            if (ExistsByValue(conn, "sys_opsbase", "ResourceInstance", "ResourceDefinitionId", request.Resourcedefinitionid, null, null)) return (false, "Valor duplicado en ResourceDefinitionId");
            if (string.IsNullOrWhiteSpace(request.Codigointerno)) return (false, "Campo requerido: CodigoInterno");
            if (request.Codigointerno != null && request.Codigointerno.Length > 120) return (false, "MaxLength excedido: CodigoInterno");
            if (!string.IsNullOrWhiteSpace(request.Codigointerno) && ExistsByValue(conn, "sys_opsbase", "ResourceInstance", "CodigoInterno", request.Codigointerno!, null, null)) return (false, "Valor duplicado en CodigoInterno");
            if (string.IsNullOrWhiteSpace(request.Estado)) return (false, "Campo requerido: Estado");
            if (request.Estado != null && request.Estado.Length > 30) return (false, "MaxLength excedido: Estado");
            if (request.Serie != null && request.Serie.Length > 120) return (false, "MaxLength excedido: Serie");
            if (request.Lote != null && request.Lote.Length > 120) return (false, "MaxLength excedido: Lote");

            var sql = "INSERT INTO [sys_opsbase].[ResourceInstance] ([ResourceDefinitionId], [CodigoInterno], [Estado], [Serie], [Lote], [Vencimiento], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (@ResourceDefinitionId, @CodigoInterno, @Estado, @Serie, @Lote, @Vencimiento, @IsActive, @CreatedAt, @UpdatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ResourceDefinitionId", request.Resourcedefinitionid);
            cmd.Parameters.AddWithValue("@CodigoInterno", request.Codigointerno ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", request.Estado ?? "'activo'");
            cmd.Parameters.AddWithValue("@Serie", request.Serie ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lote", request.Lote ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Vencimiento", request.Vencimiento ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", request.Isactive);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, ResourceinstanceUpdateRequest request)
        {
            using var conn = Db.Open();
            if (!ExistsByValue(conn, "sys_opsbase", "ResourceDefinition", "Id", request.Resourcedefinitionid, null, null)) return (false, "ResourceDefinition inexistente (ResourceDefinitionId)");
            if (ExistsByValue(conn, "sys_opsbase", "ResourceInstance", "ResourceDefinitionId", request.Resourcedefinitionid, "Id", id)) return (false, "Valor duplicado en ResourceDefinitionId");
            if (string.IsNullOrWhiteSpace(request.Codigointerno)) return (false, "Campo requerido: CodigoInterno");
            if (request.Codigointerno != null && request.Codigointerno.Length > 120) return (false, "MaxLength excedido: CodigoInterno");
            if (!string.IsNullOrWhiteSpace(request.Codigointerno) && ExistsByValue(conn, "sys_opsbase", "ResourceInstance", "CodigoInterno", request.Codigointerno!, "Id", id)) return (false, "Valor duplicado en CodigoInterno");
            if (string.IsNullOrWhiteSpace(request.Estado)) return (false, "Campo requerido: Estado");
            if (request.Estado != null && request.Estado.Length > 30) return (false, "MaxLength excedido: Estado");
            if (request.Serie != null && request.Serie.Length > 120) return (false, "MaxLength excedido: Serie");
            if (request.Lote != null && request.Lote.Length > 120) return (false, "MaxLength excedido: Lote");
            var sql = "UPDATE [sys_opsbase].[ResourceInstance] SET [ResourceDefinitionId] = @ResourceDefinitionId, [CodigoInterno] = @CodigoInterno, [Estado] = @Estado, [Serie] = @Serie, [Lote] = @Lote, [Vencimiento] = @Vencimiento, [IsActive] = @IsActive, [CreatedAt] = @CreatedAt, [UpdatedAt] = @UpdatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ResourceDefinitionId", request.Resourcedefinitionid);
            cmd.Parameters.AddWithValue("@CodigoInterno", request.Codigointerno ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", request.Estado ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Serie", request.Serie ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Lote", request.Lote ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Vencimiento", request.Vencimiento ?? (object)DBNull.Value);
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
            var sql = "UPDATE [sys_opsbase].[ResourceInstance] SET [IsActive] = 0 WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static ResourceinstanceResponse MapToResponse(SqlDataReader reader)
        {
            return new ResourceinstanceResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Resourcedefinitionid = reader["ResourceDefinitionId"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["ResourceDefinitionId"], typeof(int)),
                Codigointerno = reader["CodigoInterno"] == DBNull.Value ? null : reader["CodigoInterno"].ToString(),
                Estado = reader["Estado"] == DBNull.Value ? null : reader["Estado"].ToString(),
                Serie = reader["Serie"] == DBNull.Value ? null : reader["Serie"].ToString(),
                Lote = reader["Lote"] == DBNull.Value ? null : reader["Lote"].ToString(),
                Vencimiento = reader["Vencimiento"] == DBNull.Value ? null : (DateTime)Convert.ChangeType(reader["Vencimiento"], typeof(DateTime)),
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
