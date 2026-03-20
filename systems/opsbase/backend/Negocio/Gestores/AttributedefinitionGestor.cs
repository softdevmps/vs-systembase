using Backend.Data;
using Backend.Models.Attributedefinition;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class AttributedefinitionGestor
    {
        public static List<AttributedefinitionResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [ResourceDefinitionId], [Codigo], [Nombre], [DataType], [IsRequired], [MaxLength], [SortOrder], [IsActive], [CreatedAt], [UpdatedAt] FROM [sys_opsbase].[AttributeDefinition]");
            sql.Append(" WHERE [IsActive] = 1");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<AttributedefinitionResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static AttributedefinitionResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [ResourceDefinitionId], [Codigo], [Nombre], [DataType], [IsRequired], [MaxLength], [SortOrder], [IsActive], [CreatedAt], [UpdatedAt] FROM [sys_opsbase].[AttributeDefinition] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(AttributedefinitionCreateRequest request)
        {
            using var conn = Db.Open();
            if (!ExistsByValue(conn, "sys_opsbase", "ResourceDefinition", "Id", request.Resourcedefinitionid, null, null)) return (false, "ResourceDefinition inexistente (ResourceDefinitionId)");
            if (ExistsByValue(conn, "sys_opsbase", "AttributeDefinition", "ResourceDefinitionId", request.Resourcedefinitionid, null, null)) return (false, "Valor duplicado en ResourceDefinitionId");
            if (string.IsNullOrWhiteSpace(request.Codigo)) return (false, "Campo requerido: Codigo");
            if (request.Codigo != null && request.Codigo.Length > 80) return (false, "MaxLength excedido: Codigo");
            if (!string.IsNullOrWhiteSpace(request.Codigo) && ExistsByValue(conn, "sys_opsbase", "AttributeDefinition", "Codigo", request.Codigo!, null, null)) return (false, "Valor duplicado en Codigo");
            if (string.IsNullOrWhiteSpace(request.Nombre)) return (false, "Campo requerido: Nombre");
            if (request.Nombre != null && request.Nombre.Length > 120) return (false, "MaxLength excedido: Nombre");
            if (string.IsNullOrWhiteSpace(request.Datatype)) return (false, "Campo requerido: DataType");
            if (request.Datatype != null && request.Datatype.Length > 30) return (false, "MaxLength excedido: DataType");

            var sql = "INSERT INTO [sys_opsbase].[AttributeDefinition] ([ResourceDefinitionId], [Codigo], [Nombre], [DataType], [IsRequired], [MaxLength], [SortOrder], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (@ResourceDefinitionId, @Codigo, @Nombre, @DataType, @IsRequired, @MaxLength, @SortOrder, @IsActive, @CreatedAt, @UpdatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ResourceDefinitionId", request.Resourcedefinitionid);
            cmd.Parameters.AddWithValue("@Codigo", request.Codigo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Nombre", request.Nombre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DataType", request.Datatype ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IsRequired", request.Isrequired);
            cmd.Parameters.AddWithValue("@MaxLength", request.Maxlength ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SortOrder", request.Sortorder);
            cmd.Parameters.AddWithValue("@IsActive", request.Isactive);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, AttributedefinitionUpdateRequest request)
        {
            using var conn = Db.Open();
            if (!ExistsByValue(conn, "sys_opsbase", "ResourceDefinition", "Id", request.Resourcedefinitionid, null, null)) return (false, "ResourceDefinition inexistente (ResourceDefinitionId)");
            if (ExistsByValue(conn, "sys_opsbase", "AttributeDefinition", "ResourceDefinitionId", request.Resourcedefinitionid, "Id", id)) return (false, "Valor duplicado en ResourceDefinitionId");
            if (string.IsNullOrWhiteSpace(request.Codigo)) return (false, "Campo requerido: Codigo");
            if (request.Codigo != null && request.Codigo.Length > 80) return (false, "MaxLength excedido: Codigo");
            if (!string.IsNullOrWhiteSpace(request.Codigo) && ExistsByValue(conn, "sys_opsbase", "AttributeDefinition", "Codigo", request.Codigo!, "Id", id)) return (false, "Valor duplicado en Codigo");
            if (string.IsNullOrWhiteSpace(request.Nombre)) return (false, "Campo requerido: Nombre");
            if (request.Nombre != null && request.Nombre.Length > 120) return (false, "MaxLength excedido: Nombre");
            if (string.IsNullOrWhiteSpace(request.Datatype)) return (false, "Campo requerido: DataType");
            if (request.Datatype != null && request.Datatype.Length > 30) return (false, "MaxLength excedido: DataType");
            var sql = "UPDATE [sys_opsbase].[AttributeDefinition] SET [ResourceDefinitionId] = @ResourceDefinitionId, [Codigo] = @Codigo, [Nombre] = @Nombre, [DataType] = @DataType, [IsRequired] = @IsRequired, [MaxLength] = @MaxLength, [SortOrder] = @SortOrder, [IsActive] = @IsActive, [CreatedAt] = @CreatedAt, [UpdatedAt] = @UpdatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ResourceDefinitionId", request.Resourcedefinitionid);
            cmd.Parameters.AddWithValue("@Codigo", request.Codigo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Nombre", request.Nombre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DataType", request.Datatype ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IsRequired", request.Isrequired);
            cmd.Parameters.AddWithValue("@MaxLength", request.Maxlength ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SortOrder", request.Sortorder);
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
            var sql = "UPDATE [sys_opsbase].[AttributeDefinition] SET [IsActive] = 0 WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static AttributedefinitionResponse MapToResponse(SqlDataReader reader)
        {
            return new AttributedefinitionResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Resourcedefinitionid = reader["ResourceDefinitionId"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["ResourceDefinitionId"], typeof(int)),
                Codigo = reader["Codigo"] == DBNull.Value ? null : reader["Codigo"].ToString(),
                Nombre = reader["Nombre"] == DBNull.Value ? null : reader["Nombre"].ToString(),
                Datatype = reader["DataType"] == DBNull.Value ? null : reader["DataType"].ToString(),
                Isrequired = reader["IsRequired"] == DBNull.Value ? default(bool) : (bool)Convert.ChangeType(reader["IsRequired"], typeof(bool)),
                Maxlength = reader["MaxLength"] == DBNull.Value ? null : (int)Convert.ChangeType(reader["MaxLength"], typeof(int)),
                Sortorder = reader["SortOrder"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["SortOrder"], typeof(int)),
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
