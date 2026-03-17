using Backend.Data;
using Backend.Models.Operationaudit;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class OperationauditGestor
    {
        public static List<OperationauditResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [OperationName], [EntityName], [EntityId], [Result], [Message], [Actor], [CorrelationId], [PayloadJson], [ExecutedAt] FROM [sys_opsbase].[OperationAudit]");
            sql.Append("");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<OperationauditResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static OperationauditResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [OperationName], [EntityName], [EntityId], [Result], [Message], [Actor], [CorrelationId], [PayloadJson], [ExecutedAt] FROM [sys_opsbase].[OperationAudit] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(OperationauditCreateRequest request)
        {
            using var conn = Db.Open();
            if (string.IsNullOrWhiteSpace(request.Operationname)) return (false, "Campo requerido: OperationName");
            if (request.Operationname != null && request.Operationname.Length > 80) return (false, "MaxLength excedido: OperationName");
            if (string.IsNullOrWhiteSpace(request.Entityname)) return (false, "Campo requerido: EntityName");
            if (request.Entityname != null && request.Entityname.Length > 80) return (false, "MaxLength excedido: EntityName");
            if (string.IsNullOrWhiteSpace(request.Result)) return (false, "Campo requerido: Result");
            if (request.Result != null && request.Result.Length > 20) return (false, "MaxLength excedido: Result");
            if (request.Message != null && request.Message.Length > 500) return (false, "MaxLength excedido: Message");
            if (request.Actor != null && request.Actor.Length > 100) return (false, "MaxLength excedido: Actor");

            var sql = "INSERT INTO [sys_opsbase].[OperationAudit] ([OperationName], [EntityName], [EntityId], [Result], [Message], [Actor], [CorrelationId], [PayloadJson], [ExecutedAt]) VALUES (@OperationName, @EntityName, @EntityId, @Result, @Message, @Actor, @CorrelationId, @PayloadJson, @ExecutedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@OperationName", request.Operationname ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EntityName", request.Entityname ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EntityId", request.Entityid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Result", request.Result ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Message", request.Message ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Actor", request.Actor ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CorrelationId", request.Correlationid);
            cmd.Parameters.AddWithValue("@PayloadJson", request.Payloadjson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ExecutedAt", request.Executedat);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, OperationauditUpdateRequest request)
        {
            using var conn = Db.Open();
            if (string.IsNullOrWhiteSpace(request.Operationname)) return (false, "Campo requerido: OperationName");
            if (request.Operationname != null && request.Operationname.Length > 80) return (false, "MaxLength excedido: OperationName");
            if (string.IsNullOrWhiteSpace(request.Entityname)) return (false, "Campo requerido: EntityName");
            if (request.Entityname != null && request.Entityname.Length > 80) return (false, "MaxLength excedido: EntityName");
            if (string.IsNullOrWhiteSpace(request.Result)) return (false, "Campo requerido: Result");
            if (request.Result != null && request.Result.Length > 20) return (false, "MaxLength excedido: Result");
            if (request.Message != null && request.Message.Length > 500) return (false, "MaxLength excedido: Message");
            if (request.Actor != null && request.Actor.Length > 100) return (false, "MaxLength excedido: Actor");
            var sql = "UPDATE [sys_opsbase].[OperationAudit] SET [OperationName] = @OperationName, [EntityName] = @EntityName, [EntityId] = @EntityId, [Result] = @Result, [Message] = @Message, [Actor] = @Actor, [CorrelationId] = @CorrelationId, [PayloadJson] = @PayloadJson, [ExecutedAt] = @ExecutedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@OperationName", request.Operationname ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EntityName", request.Entityname ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EntityId", request.Entityid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Result", request.Result ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Message", request.Message ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Actor", request.Actor ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CorrelationId", request.Correlationid);
            cmd.Parameters.AddWithValue("@PayloadJson", request.Payloadjson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ExecutedAt", request.Executedat);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "DELETE FROM [sys_opsbase].[OperationAudit] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static OperationauditResponse MapToResponse(SqlDataReader reader)
        {
            return new OperationauditResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Operationname = reader["OperationName"] == DBNull.Value ? null : reader["OperationName"].ToString(),
                Entityname = reader["EntityName"] == DBNull.Value ? null : reader["EntityName"].ToString(),
                Entityid = reader["EntityId"] == DBNull.Value ? null : (int)Convert.ChangeType(reader["EntityId"], typeof(int)),
                Result = reader["Result"] == DBNull.Value ? null : reader["Result"].ToString(),
                Message = reader["Message"] == DBNull.Value ? null : reader["Message"].ToString(),
                Actor = reader["Actor"] == DBNull.Value ? null : reader["Actor"].ToString(),
                Correlationid = reader["CorrelationId"] == DBNull.Value ? default(Guid) : (Guid)Convert.ChangeType(reader["CorrelationId"], typeof(Guid)),
                Payloadjson = reader["PayloadJson"] == DBNull.Value ? null : reader["PayloadJson"].ToString(),
                Executedat = reader["ExecutedAt"] == DBNull.Value ? default(DateTime) : (DateTime)Convert.ChangeType(reader["ExecutedAt"], typeof(DateTime)),
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
