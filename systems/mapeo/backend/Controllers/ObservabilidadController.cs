using Backend.Data;
using Backend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Backend.Controllers
{
    public sealed class LocationLearningItem
    {
        public int Id { get; init; }
        public int? IncidenteId { get; init; }
        public string? PredLugarTexto { get; init; }
        public string? CorrectLugarTexto { get; init; }
        public string? Descripcion { get; init; }
        public string? Estado { get; init; }
        public string? GateReason { get; init; }
        public DateTime? CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }

    public sealed class LocationLearningCorrectRetryRequest
    {
        public int IncidenteId { get; init; }
        public string? CorrectLugarTexto { get; init; }
        public string? CorrectLugarNormalizado { get; init; }
        public decimal? CorrectLat { get; init; }
        public decimal? CorrectLng { get; init; }
    }

    public sealed class LocationLearningCorrectRetryResponse
    {
        public bool Ok { get; init; }
        public int IncidenteId { get; init; }
        public int? JobId { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    public sealed class LocationLearningMetricsResponse
    {
        public int IncidentesTotal { get; init; }
        public int ConCoords { get; init; }
        public int SinCoords { get; init; }
        public decimal? AvgConfidence { get; init; }

        public int JobsDone { get; init; }
        public int JobsError { get; init; }
        public int JobsPending { get; init; }
        public int JobsProcessing { get; init; }
        public int JobsRetry { get; init; }

        public int FeedbackPending { get; init; }
        public int FeedbackCorrected { get; init; }
        public int FeedbackRejected { get; init; }
        public int FeedbackAccepted { get; init; }

        public int RulesActive { get; init; }
        public int RulesManual { get; init; }
        public int RulesAuto { get; init; }

        public List<LocationLearningItem> PendingItems { get; init; } = new();
        public List<LocationLearningItem> RecentCorrections { get; init; } = new();
        public List<LocationLearningItem> NoCoordsItems { get; init; } = new();
    }

    [ApiController]
    public class ObservabilidadController : AppController
    {
        [Authorize]
        [HttpGet(Routes.v1.Observabilidad.LocationLearning)]
        public IActionResult LocationLearning([FromQuery] int take = 8)
        {
            if (take <= 0) take = 8;
            if (take > 40) take = 40;

            using var conn = Db.Open();

            var response = new LocationLearningMetricsResponse
            {
                IncidentesTotal = GetIntScalar(conn, @"
SELECT COUNT(1)
FROM [sys_mapeo].[Incidentes];"),
                ConCoords = GetIntScalar(conn, @"
SELECT COUNT(1)
FROM [sys_mapeo].[Incidentes]
WHERE [Lat] IS NOT NULL AND [Lng] IS NOT NULL;"),
                SinCoords = GetIntScalar(conn, @"
SELECT COUNT(1)
FROM [sys_mapeo].[Incidentes]
WHERE [Lat] IS NULL OR [Lng] IS NULL;"),
                AvgConfidence = GetDecimalScalar(conn, @"
SELECT AVG(CAST([Confidence] AS DECIMAL(10,4)))
FROM [sys_mapeo].[Incidentes]
WHERE [Confidence] IS NOT NULL;"),

                JobsDone = GetIntScalar(conn, @"
SELECT COUNT(1) FROM [sys_mapeo].[IncidenteJobs] WHERE [Status] = 'done';"),
                JobsError = GetIntScalar(conn, @"
SELECT COUNT(1) FROM [sys_mapeo].[IncidenteJobs] WHERE [Status] = 'error';"),
                JobsPending = GetIntScalar(conn, @"
SELECT COUNT(1) FROM [sys_mapeo].[IncidenteJobs] WHERE [Status] = 'pending';"),
                JobsProcessing = GetIntScalar(conn, @"
SELECT COUNT(1) FROM [sys_mapeo].[IncidenteJobs] WHERE [Status] = 'processing';"),
                JobsRetry = GetIntScalar(conn, @"
SELECT COUNT(1) FROM [sys_mapeo].[IncidenteJobs] WHERE [Status] = 'retry';"),

                FeedbackPending = GetIntScalar(conn, @"
SELECT COUNT(1) FROM [sys_mapeo].[LocationNormalizationFeedback] WHERE [Verdict] = 'pending';"),
                FeedbackCorrected = GetIntScalar(conn, @"
SELECT COUNT(1) FROM [sys_mapeo].[LocationNormalizationFeedback] WHERE [Verdict] = 'corrected';"),
                FeedbackRejected = GetIntScalar(conn, @"
SELECT COUNT(1) FROM [sys_mapeo].[LocationNormalizationFeedback] WHERE [Verdict] = 'rejected';"),
                FeedbackAccepted = GetIntScalar(conn, @"
SELECT COUNT(1) FROM [sys_mapeo].[LocationNormalizationFeedback] WHERE [Verdict] = 'accepted';"),

                RulesActive = GetIntScalar(conn, @"
SELECT COUNT(1) FROM [sys_mapeo].[LocationNormalizationRules] WHERE [IsActive] = 1;"),
                RulesManual = GetIntScalar(conn, @"
SELECT COUNT(1)
FROM [sys_mapeo].[LocationNormalizationRules]
WHERE [IsActive] = 1 AND [Source] = 'manual';"),
                RulesAuto = GetIntScalar(conn, @"
SELECT COUNT(1)
FROM [sys_mapeo].[LocationNormalizationRules]
WHERE [IsActive] = 1 AND [Source] LIKE 'auto%';")
            };

            response.PendingItems.AddRange(GetLearningItems(
                conn,
                @"
SELECT TOP (@Take)
    f.[Id],
    f.[IncidenteId],
    f.[PredLugarTexto],
    CAST(NULL AS NVARCHAR(500)) AS [CorrectLugarTexto],
    CAST(NULL AS NVARCHAR(200)) AS [GateReason],
    i.[Descripcion],
    i.[Estado],
    f.[CreatedAt],
    f.[UpdatedAt]
FROM [sys_mapeo].[LocationNormalizationFeedback] f
LEFT JOIN [sys_mapeo].[Incidentes] i ON i.[Id] = f.[IncidenteId]
WHERE f.[Verdict] = 'pending'
ORDER BY f.[CreatedAt] DESC;",
                take
            ));

            response.RecentCorrections.AddRange(GetLearningItems(
                conn,
                @"
SELECT TOP (@Take)
    f.[Id],
    f.[IncidenteId],
    f.[PredLugarTexto],
    f.[CorrectLugarTexto],
    CAST(NULL AS NVARCHAR(200)) AS [GateReason],
    i.[Descripcion],
    i.[Estado],
    f.[CreatedAt],
    f.[UpdatedAt]
FROM [sys_mapeo].[LocationNormalizationFeedback] f
LEFT JOIN [sys_mapeo].[Incidentes] i ON i.[Id] = f.[IncidenteId]
WHERE f.[Verdict] = 'corrected'
ORDER BY ISNULL(f.[UpdatedAt], f.[CreatedAt]) DESC;",
                take
            ));

            response.NoCoordsItems.AddRange(GetLearningItems(
                conn,
                @"
SELECT TOP (@Take)
    i.[Id],
    i.[Id] AS [IncidenteId],
    COALESCE(f.[PredLugarTexto], i.[LugarTexto]) AS [PredLugarTexto],
    COALESCE(f.[CorrectLugarTexto], i.[LugarNormalizado]) AS [CorrectLugarTexto],
    COALESCE(NULLIF(f.[Notes], ''), NULLIF(j.[LastError], ''), 'sin_detalle') AS [GateReason],
    i.[Descripcion],
    j.[Status] AS [Estado],
    i.[CreatedAt],
    i.[CreatedAt] AS [UpdatedAt]
FROM [sys_mapeo].[Incidentes] i
OUTER APPLY (
    SELECT TOP 1 jj.[Status]
        ,jj.[LastError]
    FROM [sys_mapeo].[IncidenteJobs] jj
    WHERE jj.[IncidenteId] = i.[Id]
    ORDER BY jj.[UpdateAt] DESC, jj.[CreatedAt] DESC
) j
OUTER APPLY (
    SELECT TOP 1
        ff.[PredLugarTexto],
        ff.[CorrectLugarTexto],
        ff.[Notes]
    FROM [sys_mapeo].[LocationNormalizationFeedback] ff
    WHERE ff.[IncidenteId] = i.[Id]
    ORDER BY ISNULL(ff.[UpdatedAt], ff.[CreatedAt]) DESC, ff.[Id] DESC
) f
WHERE i.[Lat] IS NULL OR i.[Lng] IS NULL
ORDER BY i.[CreatedAt] DESC;",
                take
            ));

            return Ok(response);
        }

        [Authorize]
        [HttpPost(Routes.v1.Observabilidad.LocationLearningCorrectRetry)]
        public IActionResult CorrectRetry([FromBody] LocationLearningCorrectRetryRequest request)
        {
            if (request.IncidenteId <= 0)
                return BadRequest("IncidenteId invalido.");

            var correctLugarTexto = (request.CorrectLugarTexto ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(correctLugarTexto))
                return BadRequest("CorrectLugarTexto es obligatorio.");

            var correctLugarNormalizado = string.IsNullOrWhiteSpace(request.CorrectLugarNormalizado)
                ? null
                : request.CorrectLugarNormalizado!.Trim();

            using var conn = Db.Open();
            using var tx = conn.BeginTransaction();

            var incidenteExiste = GetIntScalar(conn, tx, @"
SELECT COUNT(1)
FROM [sys_mapeo].[Incidentes]
WHERE [Id] = @IncidenteId;", new SqlParameter("@IncidenteId", request.IncidenteId));
            if (incidenteExiste <= 0)
            {
                tx.Rollback();
                return NotFound($"Incidente {request.IncidenteId} no encontrado.");
            }

            using (var updateIncidente = new SqlCommand(@"
UPDATE [sys_mapeo].[Incidentes]
SET [LugarTexto] = @LugarTexto,
    [LugarNormalizado] = @LugarNormalizado,
    [Lat] = @Lat,
    [Lng] = @Lng,
    [Estado] = CASE WHEN [Estado] IS NULL OR [Estado] = '' THEN 'procesado' ELSE [Estado] END
WHERE [Id] = @IncidenteId;", conn, tx))
            {
                updateIncidente.Parameters.AddWithValue("@IncidenteId", request.IncidenteId);
                updateIncidente.Parameters.AddWithValue("@LugarTexto", correctLugarTexto);
                updateIncidente.Parameters.AddWithValue("@LugarNormalizado", (object?)correctLugarNormalizado ?? DBNull.Value);
                updateIncidente.Parameters.AddWithValue("@Lat", request.CorrectLat ?? (object)DBNull.Value);
                updateIncidente.Parameters.AddWithValue("@Lng", request.CorrectLng ?? (object)DBNull.Value);
                updateIncidente.ExecuteNonQuery();
            }

            var latestJobId = GetNullableIntScalar(conn, tx, @"
SELECT TOP 1 [Id]
FROM [sys_mapeo].[IncidenteJobs]
WHERE [IncidenteId] = @IncidenteId
ORDER BY ISNULL([UpdateAt], [CreatedAt]) DESC, [Id] DESC;", new SqlParameter("@IncidenteId", request.IncidenteId));

            if (latestJobId == null)
            {
                using var insertJob = new SqlCommand(@"
INSERT INTO [sys_mapeo].[IncidenteJobs]
([IncidenteId], [Status], [Step], [Attempts], [LastError], [CreatedAt], [UpdateAt])
VALUES
(@IncidenteId, 'retry', 'manual-corrected', 0, NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);
                insertJob.Parameters.AddWithValue("@IncidenteId", request.IncidenteId);
                latestJobId = Convert.ToInt32(insertJob.ExecuteScalar());
            }
            else
            {
                using var markRetry = new SqlCommand(@"
UPDATE [sys_mapeo].[IncidenteJobs]
SET [Status] = 'retry',
    [Step] = 'manual-corrected',
    [Attempts] = 0,
    [LastError] = NULL,
    [UpdateAt] = SYSUTCDATETIME()
WHERE [Id] = @Id;", conn, tx);
                markRetry.Parameters.AddWithValue("@Id", latestJobId.Value);
                markRetry.ExecuteNonQuery();
            }

            tx.Commit();

            LocationLearningService.ResolvePendingFeedback(
                request.IncidenteId,
                correctLugarTexto,
                correctLugarNormalizado,
                request.CorrectLat,
                request.CorrectLng,
                reviewer: "ui-correct-retry"
            );

            return Ok(new LocationLearningCorrectRetryResponse
            {
                Ok = true,
                IncidenteId = request.IncidenteId,
                JobId = latestJobId,
                Message = "Correccion aplicada y job marcado para retry."
            });
        }

        private static int GetIntScalar(SqlConnection conn, string sql)
        {
            using var cmd = new SqlCommand(sql, conn);
            var value = cmd.ExecuteScalar();
            if (value == null || value == DBNull.Value)
                return 0;
            return Convert.ToInt32(value);
        }

        private static int GetIntScalar(SqlConnection conn, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using var cmd = new SqlCommand(sql, conn, tx);
            if (parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);
            var value = cmd.ExecuteScalar();
            if (value == null || value == DBNull.Value)
                return 0;
            return Convert.ToInt32(value);
        }

        private static int? GetNullableIntScalar(SqlConnection conn, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using var cmd = new SqlCommand(sql, conn, tx);
            if (parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);
            var value = cmd.ExecuteScalar();
            if (value == null || value == DBNull.Value)
                return null;
            return Convert.ToInt32(value);
        }

        private static decimal? GetDecimalScalar(SqlConnection conn, string sql)
        {
            using var cmd = new SqlCommand(sql, conn);
            var value = cmd.ExecuteScalar();
            if (value == null || value == DBNull.Value)
                return null;
            return Convert.ToDecimal(value);
        }

        private static List<LocationLearningItem> GetLearningItems(SqlConnection conn, string sql, int take)
        {
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Take", take);
            using var reader = cmd.ExecuteReader();

            var list = new List<LocationLearningItem>();
            while (reader.Read())
            {
                list.Add(new LocationLearningItem
                {
                    Id = reader["Id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Id"]),
                    IncidenteId = reader["IncidenteId"] == DBNull.Value ? null : Convert.ToInt32(reader["IncidenteId"]),
                    PredLugarTexto = reader["PredLugarTexto"] == DBNull.Value ? null : reader["PredLugarTexto"].ToString(),
                    CorrectLugarTexto = reader["CorrectLugarTexto"] == DBNull.Value ? null : reader["CorrectLugarTexto"].ToString(),
                    GateReason = reader["GateReason"] == DBNull.Value ? null : reader["GateReason"].ToString(),
                    Descripcion = reader["Descripcion"] == DBNull.Value ? null : reader["Descripcion"].ToString(),
                    Estado = reader["Estado"] == DBNull.Value ? null : reader["Estado"].ToString(),
                    CreatedAt = reader["CreatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["CreatedAt"]),
                    UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
                });
            }

            return list;
        }
    }
}
