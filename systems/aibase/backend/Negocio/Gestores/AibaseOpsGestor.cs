using Backend.Data;
using Backend.Models.Aibase;
using Backend.Models.Projects;
using Backend.Models.Runs;
using Backend.Models.Templates;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace Backend.Negocio.Gestores
{
    public static class AibaseOpsGestor
    {
        private static readonly IReadOnlyList<string> WorkflowOrder = new List<string>
        {
            "dataset_build",
            "rag_index",
            "train_lora",
            "eval_run",
            "deploy_service"
        };

        private static readonly IReadOnlyDictionary<string, string> RunLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["dataset_build"] = "Build Dataset",
            ["rag_index"] = "Construir Índice RAG",
            ["train_lora"] = "Entrenar LoRA",
            ["eval_run"] = "Evaluar Modelo",
            ["deploy_service"] = "Desplegar Servicio"
        };

        public static AibaseOverviewResponse GetOverview(int take = 12)
        {
            using var conn = Db.Open();
            using var cmd = new SqlCommand(@"
SELECT
  (SELECT COUNT(1) FROM [sys_aibase].[Templates] WHERE [IsActive] = 1) AS TemplatesCount,
  (SELECT COUNT(1) FROM [sys_aibase].[Projects] WHERE [IsActive] = 1) AS ProjectsCount,
  (SELECT COUNT(1) FROM [sys_aibase].[Runs]) AS RunsCount,
  (SELECT COUNT(1) FROM [sys_aibase].[Runs] WHERE [Status] IN ('queued','running')) AS RunningCount;
", conn);

            using var reader = cmd.ExecuteReader();
            var response = new AibaseOverviewResponse();
            if (reader.Read())
            {
                response.TemplatesCount = Convert.ToInt32(reader["TemplatesCount"]);
                response.ProjectsCount = Convert.ToInt32(reader["ProjectsCount"]);
                response.RunsCount = Convert.ToInt32(reader["RunsCount"]);
                response.RunningCount = Convert.ToInt32(reader["RunningCount"]);
            }
            reader.Close();

            using var runsCmd = new SqlCommand(@"
SELECT TOP (@take)
  r.[Id],
  r.[ProjectId],
  p.[Name] AS ProjectName,
  r.[RunType],
  r.[Status],
  r.[ProgressPct],
  r.[LastError],
  r.[CreatedAt],
  r.[UpdatedAt]
FROM [sys_aibase].[Runs] r
INNER JOIN [sys_aibase].[Projects] p ON p.[Id] = r.[ProjectId]
ORDER BY r.[Id] DESC;", conn);
            runsCmd.Parameters.AddWithValue("@take", Math.Clamp(take, 1, 50));

            using var runsReader = runsCmd.ExecuteReader();
            while (runsReader.Read())
            {
                response.LastRuns.Add(new AibaseRunListItem
                {
                    Id = Convert.ToInt32(runsReader["Id"]),
                    ProjectId = Convert.ToInt32(runsReader["ProjectId"]),
                    ProjectName = runsReader["ProjectName"]?.ToString() ?? "",
                    RunType = runsReader["RunType"]?.ToString() ?? "",
                    Status = runsReader["Status"]?.ToString() ?? "",
                    ProgressPct = Convert.ToInt32(runsReader["ProgressPct"]),
                    LastError = runsReader["LastError"] == DBNull.Value ? null : runsReader["LastError"]?.ToString(),
                    CreatedAt = Convert.ToDateTime(runsReader["CreatedAt"]),
                    UpdatedAt = runsReader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(runsReader["UpdatedAt"])
                });
            }

            response.LastRunAt = response.LastRuns
                .Select(x => x.UpdatedAt ?? x.CreatedAt)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            return response;
        }

        public static AibaseWorkflowResponse? GetProjectWorkflow(int projectId)
        {
            var project = GetProject(projectId);
            if (project == null) return null;

            var template = GetTemplate(project.Templateid);
            if (template == null) return null;

            var runs = GetRunsByProject(projectId, 100);
            var pipelineRunTypes = GetPipelineRunTypes(template.Pipelinejson);

            var steps = BuildWorkflowSteps(pipelineRunTypes, runs);
            var nextRunType = steps
                .Where(s => s.Enabled && s.Available && s.Status != "completed")
                .Select(s => s.RunType)
                .FirstOrDefault();

            var lastRunAt = runs
                .Select(r => r.Updatedat ?? r.Finishedat ?? r.Startedat ?? r.Createdat)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            return new AibaseWorkflowResponse
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                ProjectStatus = project.Status,
                TemplateKey = template.Key,
                TemplateName = template.Name,
                NextRunType = nextRunType,
                CanInfer = steps.Any(s => s.RunType == "deploy_service" && s.Status == "completed"),
                LastRunAt = lastRunAt == default ? null : lastRunAt,
                Steps = steps
            };
        }

        public static (bool Ok, string? Error) CanTriggerRun(int projectId, string runType)
        {
            var workflow = GetProjectWorkflow(projectId);
            if (workflow == null) return (false, "No se pudo recuperar workflow del proyecto.");

            var step = workflow.Steps.FirstOrDefault(s => string.Equals(s.RunType, runType, StringComparison.OrdinalIgnoreCase));
            if (step == null) return (false, "Etapa inexistente para este proyecto.");
            if (!step.Enabled) return (false, "La etapa no aplica al template de este proyecto.");
            if (!step.Available && step.Status != "completed")
                return (false, "La etapa está bloqueada. Ejecuta las etapas previas.");

            return (true, null);
        }

        public static List<RunsResponse> GetRunsByProject(int projectId, int take = 25)
        {
            using var conn = Db.Open();
            using var cmd = new SqlCommand(@"
SELECT TOP (@take)
  [Id], [ProjectId], [RunType], [Status], [EngineRunId], [ProgressPct], [RequestedByUserId],
  [TriggerSource], [InputJson], [OutputJson], [LastError], [CreatedAt], [StartedAt], [FinishedAt], [UpdatedAt]
FROM [sys_aibase].[Runs]
WHERE [ProjectId] = @projectId
ORDER BY [Id] DESC;", conn);
            cmd.Parameters.AddWithValue("@take", Math.Clamp(take, 1, 100));
            cmd.Parameters.AddWithValue("@projectId", projectId);

            using var reader = cmd.ExecuteReader();
            var list = new List<RunsResponse>();
            while (reader.Read())
            {
                list.Add(new RunsResponse
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Projectid = Convert.ToInt32(reader["ProjectId"]),
                    Runtype = reader["RunType"]?.ToString() ?? "",
                    Status = reader["Status"]?.ToString() ?? "",
                    Enginerunid = reader["EngineRunId"] == DBNull.Value ? null : reader["EngineRunId"]?.ToString(),
                    Progresspct = Convert.ToInt32(reader["ProgressPct"]),
                    Requestedbyuserid = Convert.ToInt32(reader["RequestedByUserId"]),
                    Triggersource = reader["TriggerSource"]?.ToString() ?? "",
                    Inputjson = reader["InputJson"] == DBNull.Value ? null : reader["InputJson"]?.ToString(),
                    Outputjson = reader["OutputJson"] == DBNull.Value ? null : reader["OutputJson"]?.ToString(),
                    Lasterror = reader["LastError"] == DBNull.Value ? null : reader["LastError"]?.ToString(),
                    Createdat = Convert.ToDateTime(reader["CreatedAt"]),
                    Startedat = reader["StartedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["StartedAt"]),
                    Finishedat = reader["FinishedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["FinishedAt"]),
                    Updatedat = reader["UpdatedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UpdatedAt"])
                });
            }
            return list;
        }

        public static int InsertRunningRun(int projectId, string runType, string? inputJson, int requestedByUserId)
        {
            using var conn = Db.Open();
            using var cmd = new SqlCommand(@"
INSERT INTO [sys_aibase].[Runs]
([ProjectId], [RunType], [Status], [EngineRunId], [ProgressPct], [RequestedByUserId], [TriggerSource],
 [InputJson], [OutputJson], [LastError], [CreatedAt], [StartedAt], [FinishedAt], [UpdatedAt])
VALUES
(@projectId, @runType, 'running', NULL, 10, @requestedByUserId, 'manual', @inputJson, NULL, NULL, @now, @now, NULL, @now);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);

            var now = DateTime.UtcNow;
            cmd.Parameters.AddWithValue("@projectId", projectId);
            cmd.Parameters.AddWithValue("@runType", runType);
            cmd.Parameters.AddWithValue("@requestedByUserId", requestedByUserId);
            cmd.Parameters.AddWithValue("@inputJson", (object?)inputJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@now", now);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static void CompleteRun(int runId, string? outputJson)
        {
            using var conn = Db.Open();
            using var cmd = new SqlCommand(@"
UPDATE [sys_aibase].[Runs]
SET [Status] = 'completed',
    [ProgressPct] = 100,
    [OutputJson] = @outputJson,
    [LastError] = NULL,
    [FinishedAt] = @now,
    [UpdatedAt] = @now
WHERE [Id] = @runId;", conn);
            cmd.Parameters.AddWithValue("@runId", runId);
            cmd.Parameters.AddWithValue("@outputJson", (object?)outputJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }

        public static void UpdateProjectStatus(int projectId, string status)
        {
            using var conn = Db.Open();
            using var cmd = new SqlCommand(@"
UPDATE [sys_aibase].[Projects]
SET [Status] = @status, [UpdatedAt] = @now
WHERE [Id] = @projectId;", conn);

            cmd.Parameters.AddWithValue("@projectId", projectId);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }

        public static void FailRun(int runId, string? error)
        {
            using var conn = Db.Open();
            using var cmd = new SqlCommand(@"
UPDATE [sys_aibase].[Runs]
SET [Status] = 'error',
    [ProgressPct] = 100,
    [LastError] = @error,
    [FinishedAt] = @now,
    [UpdatedAt] = @now
WHERE [Id] = @runId;", conn);
            cmd.Parameters.AddWithValue("@runId", runId);
            cmd.Parameters.AddWithValue("@error", (object?)error ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }

        public static ProjectsResponse? GetProject(int projectId) => ProjectsGestor.ObtenerPorId(projectId);

        public static TemplatesResponse? GetTemplate(int templateId) => TemplatesGestor.ObtenerPorId(templateId);

        public static RunsResponse? GetRun(int runId) => RunsGestor.ObtenerPorId(runId);

        public static string StatusForRunType(string runType)
        {
            return runType switch
            {
                "dataset_build" => "data_ready",
                "rag_index" => "index_ready",
                "train_lora" => "trained",
                "eval_run" => "evaluated",
                "deploy_service" => "deployed",
                _ => "in_progress"
            };
        }

        private static List<string> GetPipelineRunTypes(string? pipelineJson)
        {
            var steps = new List<string>();
            if (!string.IsNullOrWhiteSpace(pipelineJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(pipelineJson);
                    if (doc.RootElement.TryGetProperty("steps", out var stepsEl) &&
                        stepsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var step in stepsEl.EnumerateArray())
                        {
                            if (!step.TryGetProperty("name", out var nameEl)) continue;
                            var normalized = NormalizeRunType(nameEl.GetString());
                            if (!string.IsNullOrWhiteSpace(normalized))
                                steps.Add(normalized);
                        }
                    }
                }
                catch
                {
                    // ignore malformed pipeline json and fallback to defaults
                }
            }

            if (!steps.Contains("dataset_build")) steps.Insert(0, "dataset_build");
            if (!steps.Contains("eval_run")) steps.Add("eval_run");
            if (!steps.Contains("deploy_service")) steps.Add("deploy_service");

            return steps
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetRunOrder)
                .ToList();
        }

        private static List<AibaseWorkflowStep> BuildWorkflowSteps(List<string> pipelineRunTypes, List<RunsResponse> runs)
        {
            var byRunType = runs
                .GroupBy(r => (r.Runtype ?? "").Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).ToList());

            var requiredByTemplate = new HashSet<string>(pipelineRunTypes, StringComparer.OrdinalIgnoreCase);
            var steps = new List<AibaseWorkflowStep>();

            for (var i = 0; i < WorkflowOrder.Count; i++)
            {
                var runType = WorkflowOrder[i];
                byRunType.TryGetValue(runType, out var runList);
                runList ??= new List<RunsResponse>();
                var last = runList.FirstOrDefault();
                var lastStatus = (last?.Status ?? "").Trim().ToLowerInvariant();

                var status = "pending";
                if (!requiredByTemplate.Contains(runType))
                {
                    status = runList.Count > 0 ? (NormalizeStepStatus(lastStatus) ?? "completed") : "na";
                }
                else if (runList.Count == 0)
                {
                    status = "pending";
                }
                else
                {
                    status = NormalizeStepStatus(lastStatus) ?? "pending";
                }

                steps.Add(new AibaseWorkflowStep
                {
                    Order = i + 1,
                    RunType = runType,
                    Label = RunLabels.TryGetValue(runType, out var label) ? label : runType,
                    Enabled = requiredByTemplate.Contains(runType),
                    Required = requiredByTemplate.Contains(runType),
                    Available = false,
                    Status = status,
                    RunsCount = runList.Count,
                    CompletedCount = runList.Count(x => string.Equals(x.Status, "completed", StringComparison.OrdinalIgnoreCase)),
                    ErrorCount = runList.Count(x => string.Equals(x.Status, "error", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Status, "failed", StringComparison.OrdinalIgnoreCase)),
                    LastRunId = last?.Id,
                    LastRunAt = last?.Updatedat ?? last?.Finishedat ?? last?.Startedat ?? last?.Createdat,
                    LastError = last?.Lasterror
                });
            }

            var prerequisitesDone = true;
            foreach (var step in steps)
            {
                if (!step.Enabled)
                {
                    step.Available = false;
                    continue;
                }

                if (step.Status == "completed" || step.Status == "running")
                {
                    step.Available = true;
                    if (step.Status != "completed") prerequisitesDone = false;
                    continue;
                }

                if (prerequisitesDone)
                {
                    step.Available = true;
                    prerequisitesDone = false;
                }
                else if (step.Status == "pending")
                {
                    step.Status = "blocked";
                }
            }

            return steps;
        }

        private static string? NormalizeStepStatus(string? raw)
        {
            var value = (raw ?? "").Trim().ToLowerInvariant();
            return value switch
            {
                "completed" => "completed",
                "ready" => "completed",
                "running" => "running",
                "queued" => "running",
                "error" => "error",
                "failed" => "error",
                "pending" => "pending",
                _ => string.IsNullOrWhiteSpace(value) ? null : "pending"
            };
        }

        private static string NormalizeRunType(string? name)
        {
            var value = (name ?? "").Trim().ToLowerInvariant();
            return value switch
            {
                "dataset" => "dataset_build",
                "dataset_build" => "dataset_build",
                "rag" => "rag_index",
                "rag_index" => "rag_index",
                "train" => "train_lora",
                "train_lora" => "train_lora",
                "eval" => "eval_run",
                "evaluate" => "eval_run",
                "eval_run" => "eval_run",
                "deploy" => "deploy_service",
                "deploy_service" => "deploy_service",
                _ => value
            };
        }

        private static int GetRunOrder(string runType)
        {
            for (var i = 0; i < WorkflowOrder.Count; i++)
            {
                if (string.Equals(WorkflowOrder[i], runType, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return 999;
        }
    }
}
