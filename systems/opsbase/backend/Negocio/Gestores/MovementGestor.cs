using Backend.Data;
using Backend.Models.Movement;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class MovementGestor
    {
        public static List<MovementResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [MovementType], [Status], [SourceLocationId], [TargetLocationId], [ReferenceNo], [Notes], [OperationAt], [CreatedBy], [CreatedAt] FROM [sys_opsbase].[Movement]");
            sql.Append("");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<MovementResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static MovementResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [MovementType], [Status], [SourceLocationId], [TargetLocationId], [ReferenceNo], [Notes], [OperationAt], [CreatedBy], [CreatedAt] FROM [sys_opsbase].[Movement] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(MovementCreateRequest request)
        {
            using var conn = Db.Open();
            if (string.IsNullOrWhiteSpace(request.Movementtype)) return (false, "Campo requerido: MovementType");
            if (request.Movementtype != null && request.Movementtype.Length > 30) return (false, "MaxLength excedido: MovementType");
            if (string.IsNullOrWhiteSpace(request.Status)) return (false, "Campo requerido: Status");
            if (request.Status != null && request.Status.Length > 30) return (false, "MaxLength excedido: Status");
            if (request.Sourcelocationid != null && !ExistsByValue(conn, "sys_opsbase", "Location", "Id", request.Sourcelocationid!, null, null)) return (false, "Location inexistente (SourceLocationId)");
            if (request.Targetlocationid != null && !ExistsByValue(conn, "sys_opsbase", "Location", "Id", request.Targetlocationid!, null, null)) return (false, "Location inexistente (TargetLocationId)");
            if (request.Referenceno != null && request.Referenceno.Length > 80) return (false, "MaxLength excedido: ReferenceNo");
            if (request.Notes != null && request.Notes.Length > 500) return (false, "MaxLength excedido: Notes");
            if (request.Createdby != null && request.Createdby.Length > 100) return (false, "MaxLength excedido: CreatedBy");

            var sql = "INSERT INTO [sys_opsbase].[Movement] ([MovementType], [Status], [SourceLocationId], [TargetLocationId], [ReferenceNo], [Notes], [OperationAt], [CreatedBy], [CreatedAt]) VALUES (@MovementType, @Status, @SourceLocationId, @TargetLocationId, @ReferenceNo, @Notes, @OperationAt, @CreatedBy, @CreatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MovementType", request.Movementtype ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", request.Status ?? "'confirmado'");
            cmd.Parameters.AddWithValue("@SourceLocationId", request.Sourcelocationid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TargetLocationId", request.Targetlocationid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ReferenceNo", request.Referenceno ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", request.Notes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@OperationAt", request.Operationat);
            cmd.Parameters.AddWithValue("@CreatedBy", request.Createdby ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, MovementUpdateRequest request)
        {
            using var conn = Db.Open();
            if (string.IsNullOrWhiteSpace(request.Movementtype)) return (false, "Campo requerido: MovementType");
            if (request.Movementtype != null && request.Movementtype.Length > 30) return (false, "MaxLength excedido: MovementType");
            if (string.IsNullOrWhiteSpace(request.Status)) return (false, "Campo requerido: Status");
            if (request.Status != null && request.Status.Length > 30) return (false, "MaxLength excedido: Status");
            if (request.Sourcelocationid != null && !ExistsByValue(conn, "sys_opsbase", "Location", "Id", request.Sourcelocationid!, null, null)) return (false, "Location inexistente (SourceLocationId)");
            if (request.Targetlocationid != null && !ExistsByValue(conn, "sys_opsbase", "Location", "Id", request.Targetlocationid!, null, null)) return (false, "Location inexistente (TargetLocationId)");
            if (request.Referenceno != null && request.Referenceno.Length > 80) return (false, "MaxLength excedido: ReferenceNo");
            if (request.Notes != null && request.Notes.Length > 500) return (false, "MaxLength excedido: Notes");
            if (request.Createdby != null && request.Createdby.Length > 100) return (false, "MaxLength excedido: CreatedBy");
            var sql = "UPDATE [sys_opsbase].[Movement] SET [MovementType] = @MovementType, [Status] = @Status, [SourceLocationId] = @SourceLocationId, [TargetLocationId] = @TargetLocationId, [ReferenceNo] = @ReferenceNo, [Notes] = @Notes, [OperationAt] = @OperationAt, [CreatedBy] = @CreatedBy, [CreatedAt] = @CreatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MovementType", request.Movementtype ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", request.Status ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SourceLocationId", request.Sourcelocationid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@TargetLocationId", request.Targetlocationid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ReferenceNo", request.Referenceno ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", request.Notes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@OperationAt", request.Operationat);
            cmd.Parameters.AddWithValue("@CreatedBy", request.Createdby ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "DELETE FROM [sys_opsbase].[Movement] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static MovementResponse MapToResponse(SqlDataReader reader)
        {
            return new MovementResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Movementtype = reader["MovementType"] == DBNull.Value ? null : reader["MovementType"].ToString(),
                Status = reader["Status"] == DBNull.Value ? null : reader["Status"].ToString(),
                Sourcelocationid = reader["SourceLocationId"] == DBNull.Value ? null : (int)Convert.ChangeType(reader["SourceLocationId"], typeof(int)),
                Targetlocationid = reader["TargetLocationId"] == DBNull.Value ? null : (int)Convert.ChangeType(reader["TargetLocationId"], typeof(int)),
                Referenceno = reader["ReferenceNo"] == DBNull.Value ? null : reader["ReferenceNo"].ToString(),
                Notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString(),
                Operationat = reader["OperationAt"] == DBNull.Value ? default(DateTime) : (DateTime)Convert.ChangeType(reader["OperationAt"], typeof(DateTime)),
                Createdby = reader["CreatedBy"] == DBNull.Value ? null : reader["CreatedBy"].ToString(),
                Createdat = reader["CreatedAt"] == DBNull.Value ? default(DateTime) : (DateTime)Convert.ChangeType(reader["CreatedAt"], typeof(DateTime)),
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
