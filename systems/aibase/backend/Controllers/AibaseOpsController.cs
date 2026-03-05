using System.Text.Json;
using Backend.Models.Aibase;
using Backend.Negocio.Gestores;
using Backend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    public class AibaseOpsController : AppController
    {
        private static readonly HashSet<string> AllowedRunTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "dataset_build",
            "rag_index",
            "train_lora",
            "eval_run",
            "deploy_service"
        };

        private readonly EngineClient _engineClient;

        public AibaseOpsController(IHttpClientFactory httpClientFactory)
        {
            _engineClient = new EngineClient(httpClientFactory);
        }

        [Authorize]
        [HttpGet(Routes.v1.Aibase.Overview)]
        public IActionResult Overview([FromQuery] int take = 12)
        {
            var payload = AibaseOpsGestor.GetOverview(take);
            return Ok(payload);
        }

        [Authorize]
        [HttpGet(Routes.v1.Aibase.RunsByProject)]
        public IActionResult RunsByProject(int projectId, [FromQuery] int take = 25)
        {
            var project = AibaseOpsGestor.GetProject(projectId);
            if (project == null) return NotFound("Proyecto inexistente.");
            return Ok(AibaseOpsGestor.GetRunsByProject(projectId, take));
        }

        [Authorize]
        [HttpGet(Routes.v1.Aibase.WorkflowByProject)]
        public IActionResult WorkflowByProject(int projectId)
        {
            var workflow = AibaseOpsGestor.GetProjectWorkflow(projectId);
            if (workflow == null) return NotFound("Proyecto inexistente.");
            return Ok(workflow);
        }

        [Authorize]
        [HttpPost(Routes.v1.Aibase.TriggerRun)]
        public async Task<IActionResult> TriggerRun(int projectId, [FromBody] AibaseRunRequest? request)
        {
            var project = AibaseOpsGestor.GetProject(projectId);
            if (project == null) return NotFound("Proyecto inexistente.");

            var runType = (request?.RunType ?? "dataset_build").Trim().ToLowerInvariant();
            if (!AllowedRunTypes.Contains(runType))
            {
                return BadRequest($"RunType no soportado: {runType}");
            }

            var canRun = AibaseOpsGestor.CanTriggerRun(projectId, runType);
            if (!canRun.Ok)
            {
                return Conflict(canRun.Error);
            }

            var runId = AibaseOpsGestor.InsertRunningRun(
                projectId,
                runType,
                request?.InputJson,
                Math.Max(1, UsuarioToken().UsuarioId)
            );

            var useMock = AppConfig.AIBASE_ENGINE_MOCK;
            string? outputJson;
            string? error = null;

            if (useMock)
            {
                outputJson = BuildMockRunOutput(project, runType, request?.InputJson);
            }
            else
            {
                var result = await _engineClient.ExecuteRunAsync(projectId, runType, request?.InputJson);
                outputJson = result.ResponseJson;
                error = result.Ok ? null : (result.Error ?? "Error de engine");
            }

            if (string.IsNullOrWhiteSpace(error))
            {
                AibaseOpsGestor.CompleteRun(runId, outputJson);
                AibaseOpsGestor.UpdateProjectStatus(projectId, AibaseOpsGestor.StatusForRunType(runType));
            }
            else
            {
                AibaseOpsGestor.FailRun(runId, error);
                AibaseOpsGestor.UpdateProjectStatus(projectId, "error");
            }

            var run = AibaseOpsGestor.GetRun(runId);
            if (run == null) return StatusCode(500, "No se pudo recuperar la ejecución.");

            return Ok(MapRun(run, useMock));
        }

        [Authorize]
        [HttpPost(Routes.v1.Aibase.Infer)]
        public async Task<IActionResult> Infer(int projectId, [FromBody] AibaseInferRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Input))
                return BadRequest("Input requerido.");

            var project = AibaseOpsGestor.GetProject(projectId);
            if (project == null) return NotFound("Proyecto inexistente.");

            var template = AibaseOpsGestor.GetTemplate(project.Templateid);
            if (template == null) return BadRequest("Template asociado inexistente.");

            var useMock = AppConfig.AIBASE_ENGINE_MOCK;
            string output;
            string? outputJson = null;

            if (useMock)
            {
                var mock = BuildMockInferOutput(template.Key, request.Input, request.ContextJson);
                output = mock.Output;
                outputJson = mock.OutputJson;
            }
            else
            {
                var result = await _engineClient.InferAsync(projectId, request.Input, request.ContextJson);
                if (!result.Ok)
                {
                    return StatusCode(502, new
                    {
                        error = result.Error ?? "No se pudo inferir con engine.",
                        response = result.ResponseJson
                    });
                }

                output = result.ResponseJson ?? "";
                outputJson = result.ResponseJson;
            }

            return Ok(new AibaseInferResponse
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                TemplateKey = template.Key,
                Input = request.Input,
                Output = output,
                OutputJson = outputJson,
                IsMock = useMock,
                CreatedAt = DateTime.UtcNow
            });
        }

        private static AibaseRunResponse MapRun(Backend.Models.Runs.RunsResponse run, bool isMock)
        {
            return new AibaseRunResponse
            {
                RunId = run.Id,
                ProjectId = run.Projectid,
                RunType = run.Runtype ?? "",
                Status = run.Status ?? "",
                ProgressPct = run.Progresspct,
                OutputJson = run.Outputjson,
                LastError = run.Lasterror,
                CreatedAt = run.Createdat,
                StartedAt = run.Startedat,
                FinishedAt = run.Finishedat,
                UpdatedAt = run.Updatedat,
                IsMock = isMock
            };
        }

        private static string BuildMockRunOutput(Backend.Models.Projects.ProjectsResponse project, string runType, string? inputJson)
        {
            var payload = new
            {
                mode = "mock",
                runType,
                project = new { project.Id, project.Name, project.Slug },
                summary = runType switch
                {
                    "dataset_build" => "Dataset construido y validado.",
                    "rag_index" => "Índice vectorial generado y sincronizado.",
                    "train_lora" => "Adapter LoRA entrenado y versionado.",
                    "eval_run" => "Evaluación completada con suite por defecto.",
                    "deploy_service" => "Servicio desplegado y saludable.",
                    _ => "Ejecución completada."
                },
                deploy = runType == "deploy_service"
                    ? new
                    {
                        endpoint = AppConfig.AIBASE_DEPLOY_ENDPOINT,
                        health = AppConfig.AIBASE_DEPLOY_HEALTH
                    }
                    : null,
                inputJson,
                generatedAt = DateTime.UtcNow
            };
            return JsonSerializer.Serialize(payload);
        }

        private static (string Output, string? OutputJson) BuildMockInferOutput(string templateKey, string input, string? contextJson)
        {
            JsonElement? contextRoot = null;
            string? mediaType = null;
            string? mediaMime = null;
            string? mediaFile = null;

            if (!string.IsNullOrWhiteSpace(contextJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(contextJson);
                    contextRoot = doc.RootElement.Clone();
                    if (contextRoot.Value.TryGetProperty("media", out var mediaNode)
                        && mediaNode.ValueKind == JsonValueKind.Array
                        && mediaNode.GetArrayLength() > 0)
                    {
                        var first = mediaNode[0];
                        if (first.ValueKind == JsonValueKind.Object)
                        {
                            mediaType = first.TryGetProperty("type", out var mt) ? mt.GetString() : null;
                            mediaMime = first.TryGetProperty("mime", out var mm) ? mm.GetString() : null;
                            mediaFile = first.TryGetProperty("fileName", out var mf) ? mf.GetString() : null;
                        }
                    }
                }
                catch
                {
                    // no-op: contexto inválido no debe romper el mock
                }
            }

            if (templateKey.Contains("extractor", StringComparison.OrdinalIgnoreCase))
            {
                var tipo = InferTipo(input);
                var hora = RegexMatch(input, @"\b(\d{1,2}[:\.]\d{2}|\d{1,2}\s*y\s+cuarto)\b");
                var lugar = ExtractLugar(input);
                var json = JsonSerializer.Serialize(new
                {
                    tipoHecho = tipo,
                    lugarTexto = lugar,
                    horaAproximada = hora,
                    descripcion = input.Trim(),
                    media = mediaType != null ? new
                    {
                        type = mediaType,
                        mime = mediaMime,
                        fileName = mediaFile
                    } : null
                });
                return (json, json);
            }

            var mediaHint = mediaType switch
            {
                "audio" => " · audio recibido",
                "image" => " · imagen recibida",
                _ => ""
            };
            var answer = $"Respuesta ({templateKey}): {input.Trim()}{mediaHint}";
            return (answer, JsonSerializer.Serialize(new
            {
                answer,
                media = mediaType != null ? new
                {
                    type = mediaType,
                    mime = mediaMime,
                    fileName = mediaFile
                } : null,
                context = contextRoot
            }));
        }

        private static string InferTipo(string input)
        {
            var text = input.ToLowerInvariant();
            if (text.Contains("arrebato")) return "arrebato";
            if (text.Contains("hurto")) return "hurto";
            if (text.Contains("robo")) return "robo";
            return "hecho";
        }

        private static string? RegexMatch(string input, string pattern)
        {
            var match = System.Text.RegularExpressions.Regex.Match(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Value : null;
        }

        private static string? ExtractLugar(string input)
        {
            var lower = input.ToLowerInvariant();
            var marker = " en ";
            var idx = lower.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return null;

            var start = idx + marker.Length;
            var end = input.IndexOf(',', start);
            if (end < 0) end = input.Length;

            var place = input[start..end].Trim();
            return string.IsNullOrWhiteSpace(place) ? null : place;
        }
    }
}
