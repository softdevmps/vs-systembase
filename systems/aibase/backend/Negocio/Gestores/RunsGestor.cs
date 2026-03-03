using Backend.Data;
using Backend.Models.Runs;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class RunsGestor
    {
        public static List<RunsResponse> ObtenerTodos(string? search, int? take, int? skip)
        {
            using var conn = Db.Open();
            var sql = new System.Text.StringBuilder();
            sql.Append("SELECT [Id], [ProjectId], [RunType], [Status], [EngineRunId], [ProgressPct], [RequestedByUserId], [TriggerSource], [InputJson], [OutputJson], [LastError], [CreatedAt], [StartedAt], [FinishedAt], [UpdatedAt] FROM [sys_aibase].[Runs]");
            sql.Append("");
            sql.Append(" ORDER BY [Id] ASC");
            using var cmd = new SqlCommand(sql.ToString(), conn);
            using var reader = cmd.ExecuteReader();

            var list = new List<RunsResponse>();
            while (reader.Read())
            {
                list.Add(MapToResponse(reader));
            }

            return list;
        }

        public static RunsResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            var sql = "SELECT [Id], [ProjectId], [RunType], [Status], [EngineRunId], [ProgressPct], [RequestedByUserId], [TriggerSource], [InputJson], [OutputJson], [LastError], [CreatedAt], [StartedAt], [FinishedAt], [UpdatedAt] FROM [sys_aibase].[Runs] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return MapToResponse(reader);
        }

        public static (bool Ok, string? Error) Crear(RunsCreateRequest request)
        {
            using var conn = Db.Open();
            if (!ExistsByValue(conn, "sys_aibase", "Projects", "Id", request.Projectid, null, null)) return (false, "Projects inexistente (ProjectId)");
            if (string.IsNullOrWhiteSpace(request.Runtype)) return (false, "Campo requerido: RunType");
            if (request.Runtype != null && request.Runtype.Length > 50) return (false, "MaxLength excedido: RunType");
            if (string.IsNullOrWhiteSpace(request.Status)) return (false, "Campo requerido: Status");
            if (request.Status != null && request.Status.Length > 30) return (false, "MaxLength excedido: Status");
            if (request.Enginerunid != null && request.Enginerunid.Length > 120) return (false, "MaxLength excedido: EngineRunId");
            if (string.IsNullOrWhiteSpace(request.Triggersource)) return (false, "Campo requerido: TriggerSource");
            if (request.Triggersource != null && request.Triggersource.Length > 30) return (false, "MaxLength excedido: TriggerSource");
            if (request.Lasterror != null && request.Lasterror.Length > 1000) return (false, "MaxLength excedido: LastError");

            var sql = "INSERT INTO [sys_aibase].[Runs] ([ProjectId], [RunType], [Status], [EngineRunId], [ProgressPct], [RequestedByUserId], [TriggerSource], [InputJson], [OutputJson], [LastError], [CreatedAt], [StartedAt], [FinishedAt], [UpdatedAt]) VALUES (@ProjectId, @RunType, @Status, @EngineRunId, @ProgressPct, @RequestedByUserId, @TriggerSource, @InputJson, @OutputJson, @LastError, @CreatedAt, @StartedAt, @FinishedAt, @UpdatedAt);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProjectId", request.Projectid);
            cmd.Parameters.AddWithValue("@RunType", request.Runtype ?? "dataset_build");
            cmd.Parameters.AddWithValue("@Status", request.Status ?? "queued");
            cmd.Parameters.AddWithValue("@EngineRunId", request.Enginerunid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ProgressPct", request.Progresspct);
            cmd.Parameters.AddWithValue("@RequestedByUserId", request.Requestedbyuserid);
            cmd.Parameters.AddWithValue("@TriggerSource", request.Triggersource ?? "manual");
            cmd.Parameters.AddWithValue("@InputJson", request.Inputjson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@OutputJson", request.Outputjson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LastError", request.Lasterror ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@StartedAt", request.Startedat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FinishedAt", request.Finishedat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            return (true, null);
        }

        public static (bool Ok, string? Error) Editar(int id, RunsUpdateRequest request)
        {
            using var conn = Db.Open();
            if (!ExistsByValue(conn, "sys_aibase", "Projects", "Id", request.Projectid, null, null)) return (false, "Projects inexistente (ProjectId)");
            if (string.IsNullOrWhiteSpace(request.Runtype)) return (false, "Campo requerido: RunType");
            if (request.Runtype != null && request.Runtype.Length > 50) return (false, "MaxLength excedido: RunType");
            if (string.IsNullOrWhiteSpace(request.Status)) return (false, "Campo requerido: Status");
            if (request.Status != null && request.Status.Length > 30) return (false, "MaxLength excedido: Status");
            if (request.Enginerunid != null && request.Enginerunid.Length > 120) return (false, "MaxLength excedido: EngineRunId");
            if (string.IsNullOrWhiteSpace(request.Triggersource)) return (false, "Campo requerido: TriggerSource");
            if (request.Triggersource != null && request.Triggersource.Length > 30) return (false, "MaxLength excedido: TriggerSource");
            if (request.Lasterror != null && request.Lasterror.Length > 1000) return (false, "MaxLength excedido: LastError");
            var sql = "UPDATE [sys_aibase].[Runs] SET [ProjectId] = @ProjectId, [RunType] = @RunType, [Status] = @Status, [EngineRunId] = @EngineRunId, [ProgressPct] = @ProgressPct, [RequestedByUserId] = @RequestedByUserId, [TriggerSource] = @TriggerSource, [InputJson] = @InputJson, [OutputJson] = @OutputJson, [LastError] = @LastError, [CreatedAt] = @CreatedAt, [StartedAt] = @StartedAt, [FinishedAt] = @FinishedAt, [UpdatedAt] = @UpdatedAt WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ProjectId", request.Projectid);
            cmd.Parameters.AddWithValue("@RunType", request.Runtype ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", request.Status ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@EngineRunId", request.Enginerunid ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ProgressPct", request.Progresspct);
            cmd.Parameters.AddWithValue("@RequestedByUserId", request.Requestedbyuserid);
            cmd.Parameters.AddWithValue("@TriggerSource", request.Triggersource ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@InputJson", request.Inputjson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@OutputJson", request.Outputjson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@LastError", request.Lasterror ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", request.Createdat);
            cmd.Parameters.AddWithValue("@StartedAt", request.Startedat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FinishedAt", request.Finishedat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UpdatedAt", request.Updatedat ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0 ? (true, null) : (false, "No encontrado");
        }

        public static bool Eliminar(int id)
        {
            using var conn = Db.Open();
            var sql = "DELETE FROM [sys_aibase].[Runs] WHERE [Id] = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static RunsResponse MapToResponse(SqlDataReader reader)
        {
            return new RunsResponse
            {
                Id = reader["Id"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["Id"], typeof(int)),
                Projectid = reader["ProjectId"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["ProjectId"], typeof(int)),
                Runtype = reader["RunType"] == DBNull.Value ? null : reader["RunType"].ToString(),
                Status = reader["Status"] == DBNull.Value ? null : reader["Status"].ToString(),
                Enginerunid = reader["EngineRunId"] == DBNull.Value ? null : reader["EngineRunId"].ToString(),
                Progresspct = reader["ProgressPct"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["ProgressPct"], typeof(int)),
                Requestedbyuserid = reader["RequestedByUserId"] == DBNull.Value ? default(int) : (int)Convert.ChangeType(reader["RequestedByUserId"], typeof(int)),
                Triggersource = reader["TriggerSource"] == DBNull.Value ? null : reader["TriggerSource"].ToString(),
                Inputjson = reader["InputJson"] == DBNull.Value ? null : reader["InputJson"].ToString(),
                Outputjson = reader["OutputJson"] == DBNull.Value ? null : reader["OutputJson"].ToString(),
                Lasterror = reader["LastError"] == DBNull.Value ? null : reader["LastError"].ToString(),
                Createdat = reader["CreatedAt"] == DBNull.Value ? default(DateTime) : (DateTime)Convert.ChangeType(reader["CreatedAt"], typeof(DateTime)),
                Startedat = reader["StartedAt"] == DBNull.Value ? null : (DateTime)Convert.ChangeType(reader["StartedAt"], typeof(DateTime)),
                Finishedat = reader["FinishedAt"] == DBNull.Value ? null : (DateTime)Convert.ChangeType(reader["FinishedAt"], typeof(DateTime)),
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
