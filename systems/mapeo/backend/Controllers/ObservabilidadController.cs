using Backend.Data;
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
        public DateTime? CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
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
    i.[LugarTexto] AS [PredLugarTexto],
    i.[LugarNormalizado] AS [CorrectLugarTexto],
    i.[Descripcion],
    j.[Status] AS [Estado],
    i.[CreatedAt],
    i.[CreatedAt] AS [UpdatedAt]
FROM [sys_mapeo].[Incidentes] i
OUTER APPLY (
    SELECT TOP 1 jj.[Status]
    FROM [sys_mapeo].[IncidenteJobs] jj
    WHERE jj.[IncidenteId] = i.[Id]
    ORDER BY jj.[UpdateAt] DESC, jj.[CreatedAt] DESC
) j
WHERE i.[Lat] IS NULL OR i.[Lng] IS NULL
ORDER BY i.[CreatedAt] DESC;",
                take
            ));

            return Ok(response);
        }

        private static int GetIntScalar(SqlConnection conn, string sql)
        {
            using var cmd = new SqlCommand(sql, conn);
            var value = cmd.ExecuteScalar();
            if (value == null || value == DBNull.Value)
                return 0;
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
