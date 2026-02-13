using Backend.Data;
using Backend.Models.Incidentejobs;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class IncidentejobsGestor
    {
        public static List<IncidentejobsResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [IncidenteId], [Status], [Step], [Attempts], [LastError], [CreatedAt], [UpdateAt] FROM [sys_mapeo].[IncidenteJobs]");
            sql.Append("");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<IncidentejobsResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static IncidentejobsResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [IncidenteId], [Status], [Step], [Attempts], [LastError], [CreatedAt], [UpdateAt] FROM [sys_mapeo].[IncidenteJobs] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(IncidentejobsCreateRequest request)
        {
            using var conn = Db.Open();
            if (request.Incidenteid != null && !ExistsByValue(conn, "sys_mapeo", "Incidentes", "Id", request.Incidenteid!, null, null)) return (false, "Incidentes inexistente (IncidenteId)");
            if (request.Status != null && request.Status.Length > 50) return (false, "MaxLength excedido: Status");
            if (request.Step != null && request.Step.Length > 50) return (false, "MaxLength excedido: Step");
            if (request.Lasterror != null && request.Lasterror.Length > 4000) return (false, "MaxLength excedido: LastError");

            var sql = "INSERT INTO [sys_mapeo].[IncidenteJobs] ([IncidenteId], [Status], [Step], [Attempts], [LastError], [CreatedAt], [UpdateAt]) VALUES (@IncidenteId, @Status, @Step, @Attempts, @LastError, @CreatedAt, @UpdateAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IncidenteId", request.Incidenteid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", request.Status ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Step", request.Step ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Attempts", request.Attempts ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LastError", request.Lasterror ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UpdateAt", request.Updateat ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, IncidentejobsUpdateRequest request)
        {
            using var conn = Db.Open();
            if (request.Incidenteid != null && !ExistsByValue(conn, "sys_mapeo", "Incidentes", "Id", request.Incidenteid!, null, null)) return (false, "Incidentes inexistente (IncidenteId)");
            if (request.Status != null && request.Status.Length > 50) return (false, "MaxLength excedido: Status");
            if (request.Step != null && request.Step.Length > 50) return (false, "MaxLength excedido: Step");
            if (request.Lasterror != null && request.Lasterror.Length > 4000) return (false, "MaxLength excedido: LastError");
            var sql = "UPDATE [sys_mapeo].[IncidenteJobs] SET [IncidenteId] = @IncidenteId, [Status] = @Status, [Step] = @Step, [Attempts] = @Attempts, [LastError] = @LastError, [CreatedAt] = @CreatedAt, [UpdateAt] = @UpdateAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IncidenteId", request.Incidenteid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", request.Status ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Step", request.Step ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Attempts", request.Attempts ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LastError", request.Lasterror ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UpdateAt", request.Updateat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "DELETE FROM [sys_mapeo].[IncidenteJobs] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        public static bool Reintentar(int id)
        {
            using var conn = Db.Open();
            var sql = @"UPDATE [sys_mapeo].[IncidenteJobs]
SET [Status] = 'retry',
    [Step] = 'manual',
    [Attempts] = 0,
    [LastError] = NULL,
    [UpdateAt] = GETUTCDATE()
WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static IncidentejobsResponse MapToResponse(SqlDataReader reader)
        {
            return new IncidentejobsResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Incidenteid = reader["IncidenteId"] == DBNull.Value ? null : (int)Convert.ChangeType(reader["IncidenteId"], typeof(int)),
                Status = reader["Status"] == DBNull.Value ? null : reader["Status"].ToString(),
                Step = reader["Step"] == DBNull.Value ? null : reader["Step"].ToString(),
                Attempts = reader["Attempts"] == DBNull.Value ? null : (int)Convert.ChangeType(reader["Attempts"], typeof(int)),
                Lasterror = reader["LastError"] == DBNull.Value ? null : reader["LastError"].ToString(),
                Createdat = reader["CreatedAt"] == DBNull.Value ? null : (DateTime)Convert.ChangeType(reader["CreatedAt"], typeof(DateTime)),
                Updateat = reader["UpdateAt"] == DBNull.Value ? null : (DateTime)Convert.ChangeType(reader["UpdateAt"], typeof(DateTime)),
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
