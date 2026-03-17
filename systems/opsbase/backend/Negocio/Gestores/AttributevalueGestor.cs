using Backend.Data;
using Backend.Models.Attributevalue;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class AttributevalueGestor
    {
        public static List<AttributevalueResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [ResourceInstanceId], [AttributeDefinitionId], [ValueString], [ValueNumber], [ValueDateTime], [ValueBool], [ValueJson], [CreatedAt], [UpdatedAt] FROM [sys_opsbase].[AttributeValue]");
            sql.Append("");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<AttributevalueResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static AttributevalueResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [ResourceInstanceId], [AttributeDefinitionId], [ValueString], [ValueNumber], [ValueDateTime], [ValueBool], [ValueJson], [CreatedAt], [UpdatedAt] FROM [sys_opsbase].[AttributeValue] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(AttributevalueCreateRequest request)
        {
            using var conn = Db.Open();
            if (!ExistsByValue(conn, "sys_opsbase", "ResourceInstance", "Id", request.Resourceinstanceid, null, null)) return (false, "ResourceInstance inexistente (ResourceInstanceId)");
            if (ExistsByValue(conn, "sys_opsbase", "AttributeValue", "ResourceInstanceId", request.Resourceinstanceid, null, null)) return (false, "Valor duplicado en ResourceInstanceId");
            if (!ExistsByValue(conn, "sys_opsbase", "AttributeDefinition", "Id", request.Attributedefinitionid, null, null)) return (false, "AttributeDefinition inexistente (AttributeDefinitionId)");
            if (ExistsByValue(conn, "sys_opsbase", "AttributeValue", "AttributeDefinitionId", request.Attributedefinitionid, null, null)) return (false, "Valor duplicado en AttributeDefinitionId");
            if (request.Valuestring != null && request.Valuestring.Length > 500) return (false, "MaxLength excedido: ValueString");

            var sql = "INSERT INTO [sys_opsbase].[AttributeValue] ([ResourceInstanceId], [AttributeDefinitionId], [ValueString], [ValueNumber], [ValueDateTime], [ValueBool], [ValueJson], [CreatedAt], [UpdatedAt]) VALUES (@ResourceInstanceId, @AttributeDefinitionId, @ValueString, @ValueNumber, @ValueDateTime, @ValueBool, @ValueJson, @CreatedAt, @UpdatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ResourceInstanceId", request.Resourceinstanceid);
            cmd.Parameters.AddWithValue("@AttributeDefinitionId", request.Attributedefinitionid);
            cmd.Parameters.AddWithValue("@ValueString", request.Valuestring ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ValueNumber", request.Valuenumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ValueDateTime", request.Valuedatetime ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ValueBool", request.Valuebool ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ValueJson", request.Valuejson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, AttributevalueUpdateRequest request)
        {
            using var conn = Db.Open();
            if (!ExistsByValue(conn, "sys_opsbase", "ResourceInstance", "Id", request.Resourceinstanceid, null, null)) return (false, "ResourceInstance inexistente (ResourceInstanceId)");
            if (ExistsByValue(conn, "sys_opsbase", "AttributeValue", "ResourceInstanceId", request.Resourceinstanceid, "Id", id)) return (false, "Valor duplicado en ResourceInstanceId");
            if (!ExistsByValue(conn, "sys_opsbase", "AttributeDefinition", "Id", request.Attributedefinitionid, null, null)) return (false, "AttributeDefinition inexistente (AttributeDefinitionId)");
            if (ExistsByValue(conn, "sys_opsbase", "AttributeValue", "AttributeDefinitionId", request.Attributedefinitionid, "Id", id)) return (false, "Valor duplicado en AttributeDefinitionId");
            if (request.Valuestring != null && request.Valuestring.Length > 500) return (false, "MaxLength excedido: ValueString");
            var sql = "UPDATE [sys_opsbase].[AttributeValue] SET [ResourceInstanceId] = @ResourceInstanceId, [AttributeDefinitionId] = @AttributeDefinitionId, [ValueString] = @ValueString, [ValueNumber] = @ValueNumber, [ValueDateTime] = @ValueDateTime, [ValueBool] = @ValueBool, [ValueJson] = @ValueJson, [CreatedAt] = @CreatedAt, [UpdatedAt] = @UpdatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ResourceInstanceId", request.Resourceinstanceid);
            cmd.Parameters.AddWithValue("@AttributeDefinitionId", request.Attributedefinitionid);
            cmd.Parameters.AddWithValue("@ValueString", request.Valuestring ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ValueNumber", request.Valuenumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ValueDateTime", request.Valuedatetime ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ValueBool", request.Valuebool ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ValueJson", request.Valuejson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "DELETE FROM [sys_opsbase].[AttributeValue] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static AttributevalueResponse MapToResponse(SqlDataReader reader)
        {
            return new AttributevalueResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Resourceinstanceid = reader["ResourceInstanceId"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["ResourceInstanceId"], typeof(int)),
                Attributedefinitionid = reader["AttributeDefinitionId"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["AttributeDefinitionId"], typeof(int)),
                Valuestring = reader["ValueString"] == DBNull.Value ? null : reader["ValueString"].ToString(),
                Valuenumber = reader["ValueNumber"] == DBNull.Value ? null : (decimal)Convert.ChangeType(reader["ValueNumber"], typeof(decimal)),
                Valuedatetime = reader["ValueDateTime"] == DBNull.Value ? null : (DateTime)Convert.ChangeType(reader["ValueDateTime"], typeof(DateTime)),
                Valuebool = reader["ValueBool"] == DBNull.Value ? null : (bool)Convert.ChangeType(reader["ValueBool"], typeof(bool)),
                Valuejson = reader["ValueJson"] == DBNull.Value ? null : reader["ValueJson"].ToString(),
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
