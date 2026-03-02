using Backend.Data;
using Backend.Models.AiBase;
using Backend.Models.Entidades;
using Backend.Utils;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Backend.Negocio.Gestores
{
    public static class AibaseRunsGestor
    {
        public static List<AibaseRunResponse> ObtenerPorProyecto(int projectId)
        {
            using var context = new SystemBaseContext();

            return context.AibaseRuns
                .Where(r => r.ProjectId == projectId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(MapToResponse())
                .ToList();
        }

        public static AibaseRunResponse? ObtenerPorId(int id)
        {
            using var context = new SystemBaseContext();

            return context.AibaseRuns
                .Where(r => r.Id == id)
                .Select(MapToResponse())
                .FirstOrDefault();
        }

        public static async Task<(int? runId, string? error)> CrearYDespacharAsync(
            int projectId,
            AibaseRunCreateRequest request,
            int requestedByUserId,
            ILogger logger)
        {
            using var context = new SystemBaseContext();

            var project = context.AibaseProjects
                .Include(p => p.Template)
                .FirstOrDefault(p => p.Id == projectId);

            if (project == null)
                return (null, "Proyecto AIBase no encontrado.");

            var runType = NormalizeRunType(request.RunType);
            if (string.IsNullOrWhiteSpace(runType))
                return (null, "RunType no soportado.");

            var run = new AibaseRuns
            {
                ProjectId = projectId,
                RunType = runType,
                Status = "queued",
                ProgressPct = 0,
                RequestedByUserId = requestedByUserId,
                TriggerSource = "manual",
                InputJson = request.InputJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.AibaseRuns.Add(run);
            context.SaveChanges();

            var engineStart = await AibaseEngineClient.StartRunAsync(
                runType,
                projectId,
                run.Id,
                project.Template.Key,
                request.InputJson,
                logger);

            if (!engineStart.Ok)
            {
                run.Status = "failed";
                run.ProgressPct = 0;
                run.LastError = engineStart.Error;
                run.FinishedAt = DateTime.UtcNow;
                run.UpdatedAt = DateTime.UtcNow;
                context.SaveChanges();
                return (run.Id, null);
            }

            run.EngineRunId = engineStart.EngineRunId;
            run.Status = engineStart.Status;
            run.ProgressPct = engineStart.ProgressPct;
            run.OutputJson = engineStart.OutputJson;
            run.StartedAt = run.Status == "queued" ? null : DateTime.UtcNow;
            run.FinishedAt = run.Status is "completed" or "failed" or "canceled" ? DateTime.UtcNow : null;
            run.UpdatedAt = DateTime.UtcNow;

            context.SaveChanges();
            return (run.Id, null);
        }

        public static async Task<(AibaseRunResponse? run, string? error)> SincronizarAsync(int id, ILogger logger)
        {
            using var context = new SystemBaseContext();

            var run = context.AibaseRuns.FirstOrDefault(r => r.Id == id);
            if (run == null)
                return (null, "Run no encontrado.");

            if (run.Status is "completed" or "failed" or "canceled")
            {
                return (MapEntity(run), null);
            }

            if (string.IsNullOrWhiteSpace(run.EngineRunId))
            {
                return (MapEntity(run), null);
            }

            var sync = await AibaseEngineClient.SyncRunAsync(run.EngineRunId, logger);
            if (!sync.Ok)
            {
                run.LastError = sync.Error;
                run.UpdatedAt = DateTime.UtcNow;
                context.SaveChanges();
                return (MapEntity(run), sync.Error);
            }

            run.Status = sync.Status;
            run.ProgressPct = sync.ProgressPct;
            run.OutputJson = sync.OutputJson;
            run.UpdatedAt = DateTime.UtcNow;

            if (run.StartedAt == null && run.Status is "running" or "completed")
                run.StartedAt = DateTime.UtcNow;

            if (run.Status is "completed" or "failed" or "canceled")
                run.FinishedAt = DateTime.UtcNow;

            context.SaveChanges();
            return (MapEntity(run), null);
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

        private static Expression<Func<AibaseRuns, AibaseRunResponse>> MapToResponse()
        {
            return r => new AibaseRunResponse
            {
                Id = r.Id,
                ProjectId = r.ProjectId,
                RunType = r.RunType,
                Status = r.Status,
                EngineRunId = r.EngineRunId,
                ProgressPct = r.ProgressPct,
                RequestedByUserId = r.RequestedByUserId,
                TriggerSource = r.TriggerSource,
                InputJson = r.InputJson,
                OutputJson = r.OutputJson,
                LastError = r.LastError,
                CreatedAt = r.CreatedAt,
                StartedAt = r.StartedAt,
                FinishedAt = r.FinishedAt,
                UpdatedAt = r.UpdatedAt
            };
        }

        private static AibaseRunResponse MapEntity(AibaseRuns r)
        {
            return new AibaseRunResponse
            {
                Id = r.Id,
                ProjectId = r.ProjectId,
                RunType = r.RunType,
                Status = r.Status,
                EngineRunId = r.EngineRunId,
                ProgressPct = r.ProgressPct,
                RequestedByUserId = r.RequestedByUserId,
                TriggerSource = r.TriggerSource,
                InputJson = r.InputJson,
                OutputJson = r.OutputJson,
                LastError = r.LastError,
                CreatedAt = r.CreatedAt,
                StartedAt = r.StartedAt,
                FinishedAt = r.FinishedAt,
                UpdatedAt = r.UpdatedAt
            };
        }
    }
}
