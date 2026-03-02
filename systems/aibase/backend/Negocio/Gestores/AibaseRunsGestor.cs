using Backend.Data;
using Backend.Models.AiBase;
using Backend.Utils;
using Microsoft.Data.SqlClient;

namespace Backend.Negocio.Gestores
{
    public static class AibaseRunsGestor
    {
        public static List<AibaseRunResponse> ObtenerPorProyecto(int projectId)
        {
            using var conn = Db.Open();
            const string sql = @"
SELECT Id, ProjectId, RunType, [Status], EngineRunId, ProgressPct, RequestedByUserId, TriggerSource,
       InputJson, OutputJson, LastError, CreatedAt, StartedAt, FinishedAt, UpdatedAt
FROM sb_ai.Runs
WHERE ProjectId = @projectId
ORDER BY CreatedAt DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@projectId", projectId);
            using var rd = cmd.ExecuteReader();

            var result = new List<AibaseRunResponse>();
            while (rd.Read())
            {
                result.Add(Map(rd));
            }

            return result;
        }

        public static AibaseRunResponse? ObtenerPorId(int id)
        {
            using var conn = Db.Open();
            using var cmd = new SqlCommand(@"
SELECT Id, ProjectId, RunType, [Status], EngineRunId, ProgressPct, RequestedByUserId, TriggerSource,
       InputJson, OutputJson, LastError, CreatedAt, StartedAt, FinishedAt, UpdatedAt
FROM sb_ai.Runs
WHERE Id = @id", conn);

            cmd.Parameters.AddWithValue("@id", id);
            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return null;
            return Map(rd);
        }

        public static async Task<(int? runId, string? error)> CrearYDespacharAsync(
            int projectId,
            AibaseRunCreateRequest request,
            int requestedByUserId,
            ILogger logger)
        {
            var runType = NormalizeRunType(request.RunType);
            if (string.IsNullOrWhiteSpace(runType))
                return (null, "RunType no soportado.");

            string templateKey;
            using (var conn = Db.Open())
            {
                const string sqlProject = @"
SELECT t.[Key]
FROM sb_ai.Projects p
INNER JOIN sb_ai.Templates t ON t.Id = p.TemplateId
WHERE p.Id = @projectId";

                using var cmdProject = new SqlCommand(sqlProject, conn);
                cmdProject.Parameters.AddWithValue("@projectId", projectId);
                var key = cmdProject.ExecuteScalar();
                if (key == null)
                    return (null, "Proyecto AIBase no encontrado.");

                templateKey = Convert.ToString(key) ?? string.Empty;

                const string sqlInsert = @"
INSERT INTO sb_ai.Runs
    (ProjectId, RunType, [Status], ProgressPct, RequestedByUserId, TriggerSource, InputJson, CreatedAt, UpdatedAt)
VALUES
    (@projectId, @runType, 'queued', 0, @requestedByUserId, 'manual', @inputJson, SYSUTCDATETIME(), SYSUTCDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using var cmdInsert = new SqlCommand(sqlInsert, conn);
                cmdInsert.Parameters.AddWithValue("@projectId", projectId);
                cmdInsert.Parameters.AddWithValue("@runType", runType);
                cmdInsert.Parameters.AddWithValue("@requestedByUserId", requestedByUserId);
                cmdInsert.Parameters.AddWithValue("@inputJson", (object?)request.InputJson ?? DBNull.Value);

                var id = cmdInsert.ExecuteScalar();
                var runId = id is int i ? i : Convert.ToInt32(id);

                var engineStart = await AibaseEngineClient.StartRunAsync(
                    runType,
                    projectId,
                    runId,
                    templateKey,
                    request.InputJson,
                    logger);

                if (!engineStart.Ok)
                {
                    using var cmdFail = new SqlCommand(@"
UPDATE sb_ai.Runs
SET [Status] = 'failed', ProgressPct = 0, LastError = @error, FinishedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME()
WHERE Id = @id", conn);
                    cmdFail.Parameters.AddWithValue("@error", (object?)engineStart.Error ?? DBNull.Value);
                    cmdFail.Parameters.AddWithValue("@id", runId);
                    cmdFail.ExecuteNonQuery();
                    return (runId, null);
                }

                using var cmdUpdate = new SqlCommand(@"
UPDATE sb_ai.Runs
SET EngineRunId = @engineRunId,
    [Status] = @status,
    ProgressPct = @progress,
    OutputJson = @output,
    StartedAt = CASE WHEN @status = 'queued' THEN NULL ELSE SYSUTCDATETIME() END,
    FinishedAt = CASE WHEN @status IN ('completed','failed','canceled') THEN SYSUTCDATETIME() ELSE NULL END,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @id", conn);

                cmdUpdate.Parameters.AddWithValue("@engineRunId", (object?)engineStart.EngineRunId ?? DBNull.Value);
                cmdUpdate.Parameters.AddWithValue("@status", engineStart.Status);
                cmdUpdate.Parameters.AddWithValue("@progress", Math.Clamp(engineStart.ProgressPct, 0, 100));
                cmdUpdate.Parameters.AddWithValue("@output", (object?)engineStart.OutputJson ?? DBNull.Value);
                cmdUpdate.Parameters.AddWithValue("@id", runId);
                cmdUpdate.ExecuteNonQuery();

                return (runId, null);
            }
        }

        public static async Task<(AibaseRunResponse? run, string? error)> SincronizarAsync(int id, ILogger logger)
        {
            var run = ObtenerPorId(id);
            if (run == null)
                return (null, "Run no encontrado.");

            if (run.Status is "completed" or "failed" or "canceled")
                return (run, null);

            if (string.IsNullOrWhiteSpace(run.EngineRunId))
                return (run, null);

            var sync = await AibaseEngineClient.SyncRunAsync(run.EngineRunId, logger);
            using var conn = Db.Open();

            if (!sync.Ok)
            {
                using var cmdErr = new SqlCommand(@"
UPDATE sb_ai.Runs
SET LastError = @error, UpdatedAt = SYSUTCDATETIME()
WHERE Id = @id", conn);
                cmdErr.Parameters.AddWithValue("@error", (object?)sync.Error ?? DBNull.Value);
                cmdErr.Parameters.AddWithValue("@id", id);
                cmdErr.ExecuteNonQuery();

                return (ObtenerPorId(id), sync.Error);
            }

            using (var cmdUpdate = new SqlCommand(@"
UPDATE sb_ai.Runs
SET [Status] = @status,
    ProgressPct = @progress,
    OutputJson = @output,
    StartedAt = CASE WHEN StartedAt IS NULL AND @status IN ('running','completed') THEN SYSUTCDATETIME() ELSE StartedAt END,
    FinishedAt = CASE WHEN @status IN ('completed','failed','canceled') THEN COALESCE(FinishedAt, SYSUTCDATETIME()) ELSE FinishedAt END,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @id", conn))
            {
                cmdUpdate.Parameters.AddWithValue("@status", sync.Status);
                cmdUpdate.Parameters.AddWithValue("@progress", Math.Clamp(sync.ProgressPct, 0, 100));
                cmdUpdate.Parameters.AddWithValue("@output", (object?)sync.OutputJson ?? DBNull.Value);
                cmdUpdate.Parameters.AddWithValue("@id", id);
                cmdUpdate.ExecuteNonQuery();
            }

            return (ObtenerPorId(id), null);
        }

        private static string NormalizeRunType(string runType)
        {
            var key = (runType ?? string.Empty).Trim().ToLowerInvariant();
            return key switch
            {
                "dataset_build" => "dataset_build",
                "rag_index" => "rag_index",
                "train_lora" => "train_lora",
                "eval_run" => "eval_run",
                "infer" => "infer",
                _ => string.Empty
            };
        }

        private static AibaseRunResponse Map(SqlDataReader rd)
        {
            return new AibaseRunResponse
            {
                Id = rd.GetInt32(0),
                ProjectId = rd.GetInt32(1),
                RunType = rd.GetString(2),
                Status = rd.GetString(3),
                EngineRunId = rd.IsDBNull(4) ? null : rd.GetString(4),
                ProgressPct = rd.IsDBNull(5) ? 0 : rd.GetInt32(5),
                RequestedByUserId = rd.GetInt32(6),
                TriggerSource = rd.IsDBNull(7) ? "manual" : rd.GetString(7),
                InputJson = rd.IsDBNull(8) ? null : rd.GetString(8),
                OutputJson = rd.IsDBNull(9) ? null : rd.GetString(9),
                LastError = rd.IsDBNull(10) ? null : rd.GetString(10),
                CreatedAt = rd.GetDateTime(11),
                StartedAt = rd.IsDBNull(12) ? null : rd.GetDateTime(12),
                FinishedAt = rd.IsDBNull(13) ? null : rd.GetDateTime(13),
                UpdatedAt = rd.IsDBNull(14) ? null : rd.GetDateTime(14)
            };
        }
    }
}
