using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Backend.Models.Aibase;
using Backend.Models.Projects;
using Backend.Models.Templates;
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
        private static readonly Dictionary<string, string> RunTypeLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            ["dataset_build"] = "Build Dataset",
            ["rag_index"] = "Construir Índice RAG",
            ["train_lora"] = "Entrenar LoRA",
            ["eval_run"] = "Evaluar",
            ["deploy_service"] = "Deploy"
        };

        private readonly EngineClient _engineClient;
        private readonly ModelServiceClient _modelServiceClient;

        public AibaseOpsController(IHttpClientFactory httpClientFactory)
        {
            _engineClient = new EngineClient(httpClientFactory);
            _modelServiceClient = new ModelServiceClient(httpClientFactory);
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
        [RequestSizeLimit(1_073_741_824)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1_073_741_824)]
        [HttpPost(Routes.v1.Aibase.DatasetUpload)]
        public async Task<IActionResult> UploadDatasetFile(int projectId, [FromForm] IFormFile? file, [FromForm] string? sourceType = null)
        {
            var project = AibaseOpsGestor.GetProject(projectId);
            if (project == null) return NotFound("Proyecto inexistente.");

            if (file == null || file.Length <= 0)
                return BadRequest("Archivo requerido.");

            var fileName = Path.GetFileName(file.FileName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("Nombre de archivo inválido.");

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".csv", ".json", ".jsonl", ".txt" };
            if (!allowed.Contains(ext))
                return BadRequest("Extensión no soportada. Usa .csv, .json, .jsonl o .txt.");

            var engine = await _engineClient.UploadDatasetFileAsync(
                projectId,
                () => file.OpenReadStream(),
                fileName,
                file.ContentType,
                sourceType);

            if (!engine.Ok)
            {
                return StatusCode(502, new
                {
                    error = engine.Error ?? "No se pudo cargar dataset en engine.",
                    response = engine.ResponseJson,
                    engineEndpoint = engine.Endpoint,
                    engineNotice = engine.Notice,
                    triedEndpoints = engine.TriedEndpoints
                });
            }

            return Content(engine.ResponseJson ?? "{}", "application/json", Encoding.UTF8);
        }

        [Authorize]
        [HttpGet(Routes.v1.Aibase.DatasetSources)]
        public async Task<IActionResult> DatasetSources(int projectId)
        {
            var project = AibaseOpsGestor.GetProject(projectId);
            if (project == null) return NotFound("Proyecto inexistente.");

            var engine = await _engineClient.ListProjectDatasetSourcesAsync(projectId);
            if (!engine.Ok)
            {
                return StatusCode(502, new
                {
                    error = engine.Error ?? "No se pudo listar fuentes de dataset desde engine.",
                    response = engine.ResponseJson,
                    engineEndpoint = engine.Endpoint,
                    engineNotice = engine.Notice,
                    triedEndpoints = engine.TriedEndpoints
                });
            }

            return Content(engine.ResponseJson ?? "{}", "application/json", Encoding.UTF8);
        }

        [Authorize]
        [HttpPost(Routes.v1.Aibase.DatasetGenerate)]
        public async Task<IActionResult> DatasetGenerate(int projectId, [FromBody] AibaseDatasetGenerateRequest? request)
        {
            var project = AibaseOpsGestor.GetProject(projectId);
            if (project == null) return NotFound("Proyecto inexistente.");

            var topics = (request?.Topics ?? new List<string>())
                .Select(x => (x ?? "").Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (topics.Count == 0)
                return BadRequest("Debe enviar al menos un tópico en 'topics'.");

            var payload = new
            {
                topics,
                datasetName = request?.DatasetName,
                maxWikipediaResults = request?.MaxWikipediaResults,
                maxExpandedQueries = request?.MaxExpandedQueries,
                chunkSize = request?.ChunkSize,
                chunkOverlap = request?.ChunkOverlap,
                sleepSeconds = request?.SleepSeconds,
                resetTopicFolders = request?.ResetTopicFolders ?? true,
            };
            var body = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var engine = await _engineClient.GenerateProjectDatasetAsync(projectId, body);
            if (!engine.Ok)
            {
                return StatusCode(502, new
                {
                    error = engine.Error ?? "No se pudo generar dataset por tópicos en engine.",
                    response = engine.ResponseJson,
                    engineEndpoint = engine.Endpoint,
                    engineNotice = engine.Notice,
                    triedEndpoints = engine.TriedEndpoints
                });
            }

            return Content(engine.ResponseJson ?? "{}", "application/json", Encoding.UTF8);
        }

        [Authorize]
        [HttpPost(Routes.v1.Aibase.DatasetMerge)]
        public async Task<IActionResult> DatasetMerge(int projectId, [FromBody] AibaseDatasetMergeRequest? request)
        {
            var project = AibaseOpsGestor.GetProject(projectId);
            if (project == null) return NotFound("Proyecto inexistente.");

            var sourcePaths = (request?.SourcePaths ?? new List<string>())
                .Select(x => (x ?? "").Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (sourcePaths.Count < 2)
                return BadRequest("Debes seleccionar al menos 2 datasets para unificar.");

            var payload = new
            {
                sourcePaths,
                datasetName = request?.DatasetName,
                deduplicate = request?.Deduplicate ?? true,
            };
            var body = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var engine = await _engineClient.MergeProjectDatasetsAsync(projectId, body);
            if (!engine.Ok)
            {
                return StatusCode(502, new
                {
                    error = engine.Error ?? "No se pudo unificar datasets en engine.",
                    response = engine.ResponseJson,
                    engineEndpoint = engine.Endpoint,
                    engineNotice = engine.Notice,
                    triedEndpoints = engine.TriedEndpoints
                });
            }

            return Content(engine.ResponseJson ?? "{}", "application/json", Encoding.UTF8);
        }

        [Authorize]
        [HttpPost(Routes.v1.Aibase.Bootstrap)]
        public IActionResult Bootstrap([FromBody] AibaseBootstrapRequest? request)
        {
            var prompt = request?.Prompt?.Trim();
            if (string.IsNullOrWhiteSpace(prompt) && !string.IsNullOrWhiteSpace(request?.ModelType))
            {
                prompt = $"Crear modelo de tipo {request!.ModelType}";
            }
            if (string.IsNullOrWhiteSpace(prompt))
                return BadRequest("Prompt o ModelType requerido.");

            var blueprint = DetectBlueprint(prompt!);
            var now = DateTime.UtcNow;
            var userId = Math.Max(1, UsuarioToken().UsuarioId);

            var response = new AibaseBootstrapResponse
            {
                Message = $"Bootstrap generado para '{blueprint.ModelType}'.",
                DetectedModelType = blueprint.ModelType
            };

            TemplatesResponse? template = null;
            var templateKey = blueprint.TemplateKey;
            var existingTemplate = TemplatesGestor
                .ObtenerTodos(null, null, null)
                .FirstOrDefault(x => string.Equals(x.Key, templateKey, StringComparison.OrdinalIgnoreCase));
            if (existingTemplate != null)
            {
                response.TemplateAlreadyExists = true;
                template = existingTemplate;
            }
            else if (request?.CreateTemplate != false)
            {
                var pipelineJson = JsonSerializer.Serialize(new
                {
                    version = "1.0.0",
                    preset = blueprint.TemplatePreset,
                    steps = blueprint.Steps.Select(s => new { name = s }).ToArray(),
                    contract = new
                    {
                        objective = blueprint.Objective,
                        outputSchema = blueprint.OutputSchema,
                        taxonomy = blueprint.Taxonomy,
                        validationRules = blueprint.ValidationRules,
                        annotationGuide = blueprint.AnnotationGuide
                    },
                    meta = new
                    {
                        playgroundProfile = BlueprintToPlaygroundProfile(blueprint.ModelType),
                        bootstrap = true
                    }
                });

                var createTemplateResult = TemplatesGestor.Crear(new TemplatesCreateRequest
                {
                    Key = templateKey,
                    Name = blueprint.TemplateName,
                    Description = blueprint.TemplateDescription,
                    Pipelinejson = pipelineJson,
                    Isactive = true,
                    Version = "1.0.0",
                    Createdat = now
                });

                if (!createTemplateResult.Ok)
                    return BadRequest($"No se pudo crear template: {createTemplateResult.Error}");

                template = TemplatesGestor
                    .ObtenerTodos(null, null, null)
                    .FirstOrDefault(x => string.Equals(x.Key, templateKey, StringComparison.OrdinalIgnoreCase));
                response.TemplateCreated = template != null;
            }

            if (template == null && request?.CreateProject == true)
            {
                return BadRequest("No hay template disponible para crear el proyecto.");
            }

            ProjectsResponse? project = null;
            var projectName = string.IsNullOrWhiteSpace(request?.ProjectName) ? blueprint.ProjectName : request!.ProjectName!.Trim();
            var projectSlug = string.IsNullOrWhiteSpace(request?.ProjectSlug) ? blueprint.ProjectSlug : Slugify(request!.ProjectSlug!);
            if (string.IsNullOrWhiteSpace(projectSlug))
                projectSlug = Slugify(projectName);

            var existingProject = ProjectsGestor
                .ObtenerTodos(null, null, null)
                .FirstOrDefault(x => string.Equals(x.Slug, projectSlug, StringComparison.OrdinalIgnoreCase));
            if (existingProject != null)
            {
                response.ProjectAlreadyExists = true;
                project = existingProject;
            }
            else if (request?.CreateProject != false)
            {
                var createProjectResult = ProjectsGestor.Crear(new ProjectsCreateRequest
                {
                    Slug = projectSlug,
                    Name = projectName,
                    Description = blueprint.ProjectDescription,
                    Language = string.IsNullOrWhiteSpace(request?.Language) ? "es" : request!.Language!.Trim(),
                    Tone = string.IsNullOrWhiteSpace(request?.Tone) ? blueprint.Tone : request!.Tone!.Trim(),
                    Status = "draft",
                    Isactive = true,
                    Templateid = template!.Id,
                    Tenantid = null,
                    Createdbyuserid = userId,
                    Createdat = now
                });
                if (!createProjectResult.Ok)
                    return BadRequest($"No se pudo crear proyecto: {createProjectResult.Error}");

                project = ProjectsGestor
                    .ObtenerTodos(null, null, null)
                    .FirstOrDefault(x => string.Equals(x.Slug, projectSlug, StringComparison.OrdinalIgnoreCase));
                response.ProjectCreated = project != null;
            }

            response.TemplateId = template?.Id;
            response.TemplateKey = template?.Key;
            response.ProjectId = project?.Id;
            response.ProjectSlug = project?.Slug;
            response.ProjectName = project?.Name;
            return Ok(response);
        }

        [Authorize]
        [HttpPost(Routes.v1.Aibase.RunAll)]
        public async Task<IActionResult> RunAll(int projectId, [FromBody] AibaseRunAllRequest? request)
        {
            var project = AibaseOpsGestor.GetProject(projectId);
            if (project == null) return NotFound("Proyecto inexistente.");
            var template = AibaseOpsGestor.GetTemplate(project.Templateid);
            if (template == null) return BadRequest("Template asociado inexistente.");

            var workflow = AibaseOpsGestor.GetProjectWorkflow(projectId);
            if (workflow == null) return NotFound("No se pudo recuperar workflow.");
            var plannedRunTypes = workflow.Steps
                .OrderBy(x => x.Order)
                .Select(x => x.RunType)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var userId = Math.Max(1, UsuarioToken().UsuarioId);
            var stopOnError = request?.StopOnError != false;
            var result = new AibaseRunAllResponse
            {
                ProjectId = project.Id,
                ProjectName = project.Name ?? $"Proyecto #{project.Id}",
                Completed = true
            };

            foreach (var runType in plannedRunTypes)
            {
                var liveWorkflow = AibaseOpsGestor.GetProjectWorkflow(projectId);
                if (liveWorkflow == null)
                {
                    result.Completed = false;
                    result.LastError = "No se pudo refrescar workflow durante la ejecución.";
                    break;
                }

                var step = liveWorkflow.Steps
                    .OrderBy(x => x.Order)
                    .FirstOrDefault(x => string.Equals(x.RunType, runType, StringComparison.OrdinalIgnoreCase));

                if (step == null)
                {
                    result.Steps.Add(new AibaseRunAllStepResult
                    {
                        RunType = runType,
                        Label = runType,
                        Status = "blocked",
                        Executed = false,
                        Success = false,
                        Error = "No se encontró la etapa en el workflow actual.",
                        At = DateTime.UtcNow
                    });
                    if (stopOnError)
                    {
                        result.Completed = false;
                        result.LastError = "Workflow inconsistente.";
                        break;
                    }
                    continue;
                }

                if (!step.Enabled)
                {
                    result.Steps.Add(new AibaseRunAllStepResult
                    {
                        RunType = step.RunType,
                        Label = step.Label,
                        Status = "na",
                        Executed = false,
                        Success = true,
                        At = DateTime.UtcNow
                    });
                    continue;
                }

                var status = (step.Status ?? "").Trim().ToLowerInvariant();
                if (status == "completed")
                {
                    result.Steps.Add(new AibaseRunAllStepResult
                    {
                        RunType = step.RunType,
                        Label = step.Label,
                        Status = "completed",
                        Executed = false,
                        Success = true,
                        RunId = step.LastRunId,
                        ProgressPct = 100,
                        At = DateTime.UtcNow
                    });
                    continue;
                }

                if (status == "running")
                {
                    result.Steps.Add(new AibaseRunAllStepResult
                    {
                        RunType = step.RunType,
                        Label = step.Label,
                        Status = "running",
                        Executed = false,
                        Success = true,
                        RunId = step.LastRunId,
                        ProgressPct = 0,
                        At = DateTime.UtcNow
                    });
                    continue;
                }

                if (!step.Available)
                {
                    result.Steps.Add(new AibaseRunAllStepResult
                    {
                        RunType = step.RunType,
                        Label = step.Label,
                        Status = "blocked",
                        Executed = false,
                        Success = false,
                        Error = "Etapa bloqueada por prerequisitos.",
                        At = DateTime.UtcNow
                    });
                    if (stopOnError)
                    {
                        result.Completed = false;
                        result.LastError = "Workflow bloqueado por prerequisitos.";
                        break;
                    }
                    continue;
                }

                if (string.Equals(step.RunType, "deploy_service", StringComparison.OrdinalIgnoreCase))
                {
                    var qualityGate = await ValidateDeployQualityGateAsync(project, template);
                    if (!qualityGate.Ok)
                    {
                        result.Steps.Add(new AibaseRunAllStepResult
                        {
                            RunType = step.RunType,
                            Label = step.Label,
                            Status = "blocked",
                            Executed = false,
                            Success = false,
                            Error = qualityGate.Error,
                            At = DateTime.UtcNow
                        });
                        result.Completed = false;
                        result.LastError = qualityGate.Error;
                        if (stopOnError) break;
                        continue;
                    }
                }

                var execution = await ExecuteRunAsync(
                    project,
                    step.RunType,
                    null,
                    userId,
                    "workflow_auto");

                var run = execution.Run;
                var ok = run != null && string.Equals(run.Status, "completed", StringComparison.OrdinalIgnoreCase);
                result.Steps.Add(new AibaseRunAllStepResult
                {
                    RunType = step.RunType,
                    Label = step.Label,
                    Status = run?.Status ?? "error",
                    Executed = true,
                    Success = ok,
                    RunId = run?.RunId,
                    ProgressPct = run?.ProgressPct ?? 0,
                    Error = execution.Error ?? run?.LastError,
                    OutputJson = run?.OutputJson,
                    At = DateTime.UtcNow
                });

                if (!ok)
                {
                    result.Completed = false;
                    result.LastError = execution.Error ?? run?.LastError ?? "Error en ejecución de etapa.";
                    if (stopOnError) break;
                }
            }

            return Ok(result);
        }

        [Authorize]
        [HttpGet(Routes.v1.Aibase.DeployAssets)]
        public IActionResult DeployAssets(int projectId)
        {
            var project = AibaseOpsGestor.GetProject(projectId);
            if (project == null) return NotFound("Proyecto inexistente.");

            var runs = AibaseOpsGestor
                .GetRunsByProject(projectId, 50)
                .Where(x => string.Equals(x.Runtype, "deploy_service", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Id)
                .ToList();

            var lastDeploy = runs.FirstOrDefault();
            var endpoint = AppConfig.AIBASE_DEPLOY_ENDPOINT?.Trim();
            var health = AppConfig.AIBASE_DEPLOY_HEALTH?.Trim();
            string? service = null;

            if (!string.IsNullOrWhiteSpace(lastDeploy?.Outputjson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(lastDeploy.Outputjson);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("deploy", out var deploy) && deploy.ValueKind == JsonValueKind.Object)
                    {
                        if (deploy.TryGetProperty("endpoint", out var endpointNode))
                            endpoint = endpointNode.GetString() ?? endpoint;
                        if (deploy.TryGetProperty("health", out var healthNode))
                            health = healthNode.GetString() ?? health;
                        if (deploy.TryGetProperty("service", out var serviceNode))
                            service = serviceNode.GetString();
                    }
                    if (root.TryGetProperty("endpoint", out var endpointFlat))
                        endpoint = endpointFlat.GetString() ?? endpoint;
                    if (root.TryGetProperty("health", out var healthFlat))
                        health = healthFlat.GetString() ?? health;
                }
                catch
                {
                    // ignore malformed output json
                }
            }

            endpoint = NormalizeBaseUrl(endpoint);
            health = NormalizeBaseUrl(health);
            var inferUrl = string.IsNullOrWhiteSpace(endpoint) ? "" : $"{endpoint.TrimEnd('/')}/infer";

            var composeFile = AppConfig.AIBASE_DOCKER_COMPOSE_FILE;
            if (string.IsNullOrWhiteSpace(composeFile))
            {
                composeFile = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docker", "docker-compose.yml"));
            }
            var stackName = AppConfig.AIBASE_DOCKER_PROJECT;
            var dockerCommand = $"docker compose -f \"{composeFile}\" -p \"{stackName}\" up -d";

            var payload = new AibaseDeployAssetsResponse
            {
                ProjectId = project.Id,
                ProjectName = project.Name ?? $"Proyecto #{project.Id}",
                Endpoint = endpoint,
                Health = health,
                StackName = stackName,
                ComposeFile = composeFile,
                DockerCommand = dockerCommand,
                CurlSnippet = BuildCurlSnippet(inferUrl, project.Id),
                JavaScriptSnippet = BuildJavaScriptSnippet(inferUrl),
                PythonSnippet = BuildPythonSnippet(inferUrl)
            };

            if (!string.IsNullOrWhiteSpace(service))
            {
                payload.DockerCommand = $"docker compose -f \"{composeFile}\" -p \"{stackName}\" up -d \"{service}\"";
            }

            return Ok(payload);
        }

        [Authorize]
        [HttpPost(Routes.v1.Aibase.DeployExport)]
        public async Task<IActionResult> DeployExport(int projectId, [FromBody] AibaseDeployExportRequest? request)
        {
            var project = AibaseOpsGestor.GetProject(projectId);
            if (project == null) return NotFound("Proyecto inexistente.");

            var serviceName = NormalizeServiceName(request?.ServiceName, projectId);
            var imageTagRaw = request?.ImageTag;
            var imageTag = string.IsNullOrWhiteSpace(imageTagRaw)
                ? "docker-aibase-engine:latest"
                : imageTagRaw.Trim();

            var endpoint = NormalizeBaseUrl(request?.Endpoint)
                ?? NormalizeBaseUrl(AppConfig.AIBASE_DEPLOY_ENDPOINT)
                ?? "http://localhost:8010";
            var health = NormalizeBaseUrl(request?.HealthUrl)
                ?? $"{endpoint.TrimEnd('/')}/health";

            var hostPort = Math.Clamp(request?.HostPort ?? InferPortFromUrl(endpoint, 8010), 1, 65535);
            var containerPort = Math.Clamp(request?.ContainerPort ?? 8010, 1, 65535);

            var extraEnv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (request?.ExtraEnv != null)
            {
                foreach (var row in request.ExtraEnv)
                {
                    var key = (row.Key ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    extraEnv[key] = row.Value ?? "";
                }
            }

            if (!extraEnv.ContainsKey("AIBASE_DEFAULT_PROJECT_ID"))
            {
                extraEnv["AIBASE_DEFAULT_PROJECT_ID"] = projectId.ToString(CultureInfo.InvariantCulture);
            }

            var export = await _engineClient.ExportDockerBundleAsync(
                projectId,
                serviceName,
                imageTag,
                hostPort,
                containerPort,
                extraEnv);

            if (!export.Ok)
            {
                return StatusCode(502, new
                {
                    error = export.Error ?? "No se pudo exportar bundle Docker en engine.",
                    response = export.ResponseJson,
                    engineEndpoint = export.Endpoint,
                    engineNotice = export.Notice,
                    triedEndpoints = export.TriedEndpoints
                });
            }

            var responseService = serviceName;
            var responseImage = imageTag;
            string? bundleDir = null;
            string? composeFile = null;
            string? envFile = null;
            string? dockerCommand = null;
            DateTime? createdAt = null;

            if (!string.IsNullOrWhiteSpace(export.ResponseJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(export.ResponseJson);
                    var root = doc.RootElement;
                    responseService = ReadString(root, "serviceName") ?? responseService;
                    responseImage = ReadString(root, "imageTag") ?? responseImage;
                    bundleDir = NormalizeEngineArtifactPath(ReadString(root, "bundleDir"));
                    composeFile = NormalizeEngineArtifactPath(ReadString(root, "composeFile"));
                    envFile = NormalizeEngineArtifactPath(ReadString(root, "envFile"));
                    dockerCommand = ReadString(root, "dockerCommand");
                    createdAt = ReadDateTime(root, "createdAt");
                }
                catch
                {
                    // Si engine devuelve JSON inesperado, mantenemos defaults.
                }
            }

            var resolvedArtifacts = ResolveExportArtifacts(projectId, bundleDir, composeFile, envFile);
            bundleDir = resolvedArtifacts.BundleDir;
            composeFile = resolvedArtifacts.ComposeFile;
            envFile = resolvedArtifacts.EnvFile;

            var inferUrl = $"{endpoint.TrimEnd('/')}/infer";
            var resolvedDockerCommand = dockerCommand;
            if (string.IsNullOrWhiteSpace(resolvedDockerCommand))
            {
                if (!string.IsNullOrWhiteSpace(composeFile))
                {
                    resolvedDockerCommand = string.IsNullOrWhiteSpace(envFile)
                        ? $"docker compose -f \"{composeFile}\" up -d"
                        : $"docker compose -f \"{composeFile}\" --env-file \"{envFile}\" up -d";
                }
                else
                {
                    resolvedDockerCommand = $"docker compose -p \"{responseService}\" up -d";
                }
            }
            else
            {
                // Ajusta comandos devueltos por engine cuando corren dentro de contenedor.
                if (!string.IsNullOrWhiteSpace(composeFile))
                {
                    resolvedDockerCommand = resolvedDockerCommand.Replace("/app/exports", NormalizeEngineArtifactPath("/app/exports") ?? "/app/exports");
                    resolvedDockerCommand = resolvedDockerCommand.Replace("/app/data", NormalizeEngineArtifactPath("/app/data") ?? "/app/data");
                }
            }

            return Ok(new AibaseDeployAssetsResponse
            {
                ProjectId = project.Id,
                ProjectName = project.Name ?? $"Proyecto #{project.Id}",
                Endpoint = endpoint,
                Health = health,
                StackName = responseService,
                ServiceName = responseService,
                ImageTag = responseImage,
                BundleDir = bundleDir,
                ComposeFile = composeFile,
                EnvFile = envFile,
                EngineEndpoint = export.Endpoint,
                EngineNotice = export.Notice,
                DockerCommand = resolvedDockerCommand,
                CurlSnippet = BuildCurlSnippet(inferUrl, project.Id),
                JavaScriptSnippet = BuildJavaScriptSnippet(inferUrl),
                PythonSnippet = BuildPythonSnippet(inferUrl),
                CreatedAt = createdAt
            });
        }

        [Authorize]
        [HttpPost(Routes.v1.Aibase.TriggerRun)]
        public async Task<IActionResult> TriggerRun(int projectId, [FromBody] AibaseRunRequest? request)
        {
            var project = AibaseOpsGestor.GetProject(projectId);
            if (project == null) return NotFound("Proyecto inexistente.");
            var template = AibaseOpsGestor.GetTemplate(project.Templateid);
            if (template == null) return BadRequest("Template asociado inexistente.");

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

            if (string.Equals(runType, "deploy_service", StringComparison.OrdinalIgnoreCase))
            {
                var qualityGate = await ValidateDeployQualityGateAsync(project, template);
                if (!qualityGate.Ok)
                {
                    return Conflict(qualityGate.Error);
                }
            }

            var execution = await ExecuteRunAsync(
                project,
                runType,
                request?.InputJson,
                Math.Max(1, UsuarioToken().UsuarioId),
                "manual");

            if (execution.Run == null)
                return StatusCode(500, execution.Error ?? "No se pudo recuperar la ejecución.");

            return Ok(execution.Run);
        }

        private async Task<RunExecutionResult> ExecuteRunAsync(
            ProjectsResponse project,
            string runType,
            string? inputJson,
            int requestedByUserId,
            string triggerSource)
        {
            var runId = AibaseOpsGestor.InsertRunningRun(
                project.Id,
                runType,
                inputJson,
                requestedByUserId,
                triggerSource);

            var isMock = AppConfig.AIBASE_ENGINE_MOCK;
            if (isMock)
            {
                var output = BuildMockRunOutput(project, runType, inputJson);
                AibaseOpsGestor.CompleteRun(runId, output);
                AibaseOpsGestor.UpdateProjectStatus(project.Id, AibaseOpsGestor.StatusForRunType(runType));
                return new RunExecutionResult
                {
                    Run = MapRun(AibaseOpsGestor.GetRun(runId)!, true)
                };
            }

            var engine = await _engineClient.ExecuteRunAsync(project.Id, runType, inputJson);
            if (!engine.Ok)
            {
                var details = string.IsNullOrWhiteSpace(engine.ResponseJson)
                    ? null
                    : $" | engine: {engine.ResponseJson}";
                var endpoint = string.IsNullOrWhiteSpace(engine.Endpoint) ? "" : $" [endpoint: {engine.Endpoint}]";
                var notice = string.IsNullOrWhiteSpace(engine.Notice) ? "" : $" [notice: {engine.Notice}]";
                var error = $"{engine.Error ?? "No se pudo ejecutar en engine."}{endpoint}{notice}{details}";
                AibaseOpsGestor.FailRun(runId, error);
                return new RunExecutionResult
                {
                    Run = MapRun(AibaseOpsGestor.GetRun(runId)!, false),
                    Error = error
                };
            }

            AibaseOpsGestor.CompleteRun(runId, engine.ResponseJson);
            AibaseOpsGestor.UpdateProjectStatus(project.Id, AibaseOpsGestor.StatusForRunType(runType));
            return new RunExecutionResult
            {
                Run = MapRun(AibaseOpsGestor.GetRun(runId)!, false)
            };
        }

        private async Task<(bool Ok, string? Error)> ValidateDeployQualityGateAsync(ProjectsResponse project, TemplatesResponse template)
        {
            var modelSettings = ResolveModelServiceSettings(template);
            if (!modelSettings.QualityGateEnabled) return (true, null);
            if (AppConfig.AIBASE_ENGINE_MOCK) return (true, null);

            var metrics = await _engineClient.GetInferMetricsAsync(
                project.Id,
                take: 50,
                gateEnabled: modelSettings.QualityGateEnabled,
                minSamples: modelSettings.QualityGateMinSamples,
                minSuccessRate: modelSettings.QualityGateMinSuccessRate,
                maxFallbackRate: modelSettings.QualityGateMaxFallbackRate,
                maxAvgLatencyMs: modelSettings.QualityGateMaxAvgLatencyMs);

            if (!metrics.Ok)
            {
                return (
                    false,
                    $"No se pudo validar quality gate previo a deploy. {metrics.Error ?? "Sin detalle del engine."}"
                );
            }

            if (string.IsNullOrWhiteSpace(metrics.ResponseJson))
            {
                return (false, "No se recibieron métricas de inferencia para validar quality gate.");
            }

            try
            {
                using var doc = JsonDocument.Parse(metrics.ResponseJson);
                var root = doc.RootElement;
                if (!TryGetPropertyIgnoreCase(root, "gate", out var gateElement) || gateElement.ValueKind != JsonValueKind.Object)
                {
                    return (false, "Respuesta de quality gate inválida desde engine.");
                }

                var gateOk = ReadBoolean(gateElement, "ok", false);
                if (gateOk) return (true, null);

                var reason = ReadString(gateElement, "reason") ?? "No cumple quality gate.";
                var summaryText = "";
                if (TryGetPropertyIgnoreCase(root, "summary", out var summaryElement) && summaryElement.ValueKind == JsonValueKind.Object)
                {
                    var total = ReadInt32(summaryElement, "total", 0);
                    var successRate = ReadDouble(summaryElement, "successRate", 0);
                    var fallbackRate = ReadDouble(summaryElement, "fallbackRate", 1);
                    var avgLatency = ReadInt32(summaryElement, "avgLatencyMs", 0);
                    summaryText = $" muestras={total}, successRate={successRate:0.###}, fallbackRate={fallbackRate:0.###}, avgLatencyMs={avgLatency}.";
                }

                return (
                    false,
                    $"Deploy bloqueado por quality gate: {reason}.{summaryText} Ejecuta más pruebas en Playground y vuelve a intentar."
                );
            }
            catch
            {
                return (false, "No se pudo interpretar quality gate desde engine.");
            }
        }

        private static string BlueprintToPlaygroundProfile(string modelType)
        {
            var key = (modelType ?? "").Trim().ToLowerInvariant();
            return key switch
            {
                "chatbot_rag" => "chat",
                "transcription" => "transcription",
                "vision_ocr" => "vision",
                "geo_extractor" => "extraction",
                "facial_recognition" => "vision",
                _ => "generic"
            };
        }

        private static string? NormalizeBaseUrl(string? raw)
        {
            var value = (raw ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                value = $"http://{value}";
            }
            return value.TrimEnd('/');
        }

        private static string NormalizeServiceName(string? raw, int projectId)
        {
            var source = string.IsNullOrWhiteSpace(raw) ? $"aibase-model-{projectId}" : raw;
            var slug = Slugify(source!);
            if (string.IsNullOrWhiteSpace(slug) || string.Equals(slug, "proyecto-ia", StringComparison.OrdinalIgnoreCase))
            {
                return $"aibase-model-{projectId}";
            }
            return slug;
        }

        private static string? NormalizeEngineArtifactPath(string? rawPath)
        {
            var path = (rawPath ?? "").Trim();
            if (string.IsNullOrWhiteSpace(path)) return null;

            var engineRoot = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "engine")
            );
            var hostExports = Path.Combine(engineRoot, "exports");
            var hostData = Path.Combine(engineRoot, "data");

            if (path.StartsWith("/app/exports/", StringComparison.OrdinalIgnoreCase))
            {
                var suffix = path["/app/exports/".Length..].Replace('/', Path.DirectorySeparatorChar);
                return Path.GetFullPath(Path.Combine(hostExports, suffix));
            }
            if (string.Equals(path, "/app/exports", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(hostExports);
            }
            if (path.StartsWith("/app/data/", StringComparison.OrdinalIgnoreCase))
            {
                var suffix = path["/app/data/".Length..].Replace('/', Path.DirectorySeparatorChar);
                return Path.GetFullPath(Path.Combine(hostData, suffix));
            }
            if (string.Equals(path, "/app/data", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(hostData);
            }

            return path;
        }

        private static (string? BundleDir, string? ComposeFile, string? EnvFile) ResolveExportArtifacts(
            int projectId,
            string? bundleDir,
            string? composeFile,
            string? envFile)
        {
            var resolvedBundleDir = ResolveExistingDirectory(bundleDir);
            var resolvedComposeFile = ResolveExistingFile(composeFile);
            var resolvedEnvFile = ResolveExistingFile(envFile);

            if (string.IsNullOrWhiteSpace(resolvedComposeFile))
            {
                var latestCompose = FindLatestExportCompose(projectId);
                if (!string.IsNullOrWhiteSpace(latestCompose))
                {
                    resolvedComposeFile = latestCompose;
                }
            }

            if (!string.IsNullOrWhiteSpace(resolvedComposeFile))
            {
                var composeDir = Path.GetDirectoryName(resolvedComposeFile);
                if (!string.IsNullOrWhiteSpace(composeDir) && Directory.Exists(composeDir))
                {
                    resolvedBundleDir = Path.GetFullPath(composeDir);
                    if (string.IsNullOrWhiteSpace(resolvedEnvFile))
                    {
                        var siblingEnv = Path.Combine(composeDir, ".env");
                        if (System.IO.File.Exists(siblingEnv))
                        {
                            resolvedEnvFile = Path.GetFullPath(siblingEnv);
                        }
                    }
                }
            }

            return (resolvedBundleDir, resolvedComposeFile, resolvedEnvFile);
        }

        private static string? FindLatestExportCompose(int projectId)
        {
            try
            {
                var engineRoot = Path.GetFullPath(
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "engine")
                );
                var projectExports = Path.Combine(engineRoot, "exports", $"project-{projectId}");
                if (!Directory.Exists(projectExports)) return null;

                return Directory
                    .EnumerateDirectories(projectExports)
                    .Select(dir => Path.Combine(dir, "docker-compose.yml"))
                    .Where(System.IO.File.Exists)
                    .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                    .Select(Path.GetFullPath)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static string? ResolveExistingFile(string? path)
        {
            var value = (path ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (!System.IO.File.Exists(value)) return null;
            return Path.GetFullPath(value);
        }

        private static string? ResolveExistingDirectory(string? path)
        {
            var value = (path ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (!Directory.Exists(value)) return null;
            return Path.GetFullPath(value);
        }

        private static int InferPortFromUrl(string? url, int fallback)
        {
            var value = (url ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            if (!value.Contains("://", StringComparison.Ordinal))
            {
                value = $"http://{value}";
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return fallback;
            if (!uri.IsDefaultPort) return uri.Port;
            return string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80;
        }

        private static ModelServiceSettings ResolveModelServiceSettings(TemplatesResponse template)
        {
            var settings = new ModelServiceSettings
            {
                Enabled = true,
                Provider = NormalizeModelProvider(AppConfig.AIBASE_MODEL_PROVIDER),
                BaseUrl = AppConfig.AIBASE_MODEL_BASE_URL,
                Path = AppConfig.AIBASE_MODEL_PATH,
                Model = AppConfig.AIBASE_MODEL_NAME,
                ApiKey = AppConfig.AIBASE_MODEL_API_KEY,
                ApiKeyEnv = AppConfig.AIBASE_MODEL_API_KEY_ENV,
                SystemPrompt = AppConfig.AIBASE_MODEL_SYSTEM_PROMPT,
                Task = AppConfig.AIBASE_MODEL_TASK,
                LocalFilesOnly = AppConfig.AIBASE_MODEL_LOCAL_FILES_ONLY,
                Temperature = AppConfig.AIBASE_MODEL_TEMPERATURE,
                MaxTokens = AppConfig.AIBASE_MODEL_MAX_TOKENS,
                TopP = AppConfig.AIBASE_MODEL_TOP_P,
                RepetitionPenalty = AppConfig.AIBASE_MODEL_REPETITION_PENALTY,
                QualityGateEnabled = AppConfig.AIBASE_QUALITY_GATE_ENABLED,
                QualityGateMinSamples = AppConfig.AIBASE_QUALITY_GATE_MIN_SAMPLES,
                QualityGateMinSuccessRate = AppConfig.AIBASE_QUALITY_GATE_MIN_SUCCESS_RATE,
                QualityGateMaxFallbackRate = AppConfig.AIBASE_QUALITY_GATE_MAX_FALLBACK_RATE,
                QualityGateMaxAvgLatencyMs = AppConfig.AIBASE_QUALITY_GATE_MAX_AVG_LATENCY_MS
            };

            if (!string.IsNullOrWhiteSpace(template.Pipelinejson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(template.Pipelinejson);
                    var root = doc.RootElement;

                    if (TryGetPropertyIgnoreCase(root, "meta", out var meta)
                        && meta.ValueKind == JsonValueKind.Object
                        && TryGetPropertyIgnoreCase(meta, "modelService", out var modelService)
                        && modelService.ValueKind == JsonValueKind.Object)
                    {
                        ApplyModelServiceSettings(modelService, settings);
                    }
                    else if (TryGetPropertyIgnoreCase(root, "modelService", out var modelServiceRoot)
                             && modelServiceRoot.ValueKind == JsonValueKind.Object)
                    {
                        ApplyModelServiceSettings(modelServiceRoot, settings);
                    }
                }
                catch
                {
                    // Si el pipeline json está malformado, usamos defaults de env.
                }
            }

            settings.Provider = NormalizeModelProvider(settings.Provider);
            settings.BaseUrl = NormalizeBaseUrl(settings.BaseUrl) ?? "";

            if (string.IsNullOrWhiteSpace(settings.Path))
            {
                settings.Path = string.Equals(settings.Provider, "openai", StringComparison.OrdinalIgnoreCase)
                    ? "/v1/chat/completions"
                    : "/api/chat";
            }
            if (!settings.Path.StartsWith("/")) settings.Path = "/" + settings.Path;

            settings.Temperature = Math.Clamp(settings.Temperature, 0, 2);
            settings.MaxTokens = Math.Clamp(settings.MaxTokens, 64, 4096);
            settings.TopP = Math.Clamp(settings.TopP, 0.05, 1);
            settings.RepetitionPenalty = Math.Clamp(settings.RepetitionPenalty, 0.8, 2.5);
            settings.QualityGateMinSamples = Math.Clamp(settings.QualityGateMinSamples, 1, 500);
            settings.QualityGateMinSuccessRate = Math.Clamp(settings.QualityGateMinSuccessRate, 0, 1);
            settings.QualityGateMaxFallbackRate = Math.Clamp(settings.QualityGateMaxFallbackRate, 0, 1);
            settings.QualityGateMaxAvgLatencyMs = Math.Clamp(settings.QualityGateMaxAvgLatencyMs, 0, 1800000);
            return settings;
        }

        private static void ApplyModelServiceSettings(JsonElement config, ModelServiceSettings settings)
        {
            var enabled = ReadBoolean(config, "enabled", settings.Enabled);
            var provider = ReadString(config, "provider")
                           ?? ReadString(config, "type")
                           ?? settings.Provider;
            var baseUrl = ReadString(config, "baseUrl")
                          ?? ReadString(config, "url")
                          ?? ReadString(config, "endpoint")
                          ?? settings.BaseUrl;
            var path = ReadString(config, "path")
                       ?? ReadString(config, "apiPath")
                       ?? settings.Path;
            var model = ReadString(config, "model")
                        ?? ReadString(config, "modelName")
                        ?? settings.Model;
            var apiKey = ReadString(config, "apiKey") ?? settings.ApiKey;
            var apiKeyEnv = ReadString(config, "apiKeyEnv") ?? settings.ApiKeyEnv;
            var systemPrompt = ReadString(config, "systemPrompt")
                               ?? ReadString(config, "prompt")
                               ?? settings.SystemPrompt;
            var task = ReadString(config, "task")
                       ?? ReadString(config, "hfTask")
                       ?? ReadString(config, "inferenceTask")
                       ?? settings.Task;
            var localFilesOnly = ReadBoolean(config, "localFilesOnly",
                ReadBoolean(config, "hfLocalFilesOnly",
                    ReadBoolean(config, "local_files_only", settings.LocalFilesOnly)));
            var temperature = ReadDouble(config, "temperature", settings.Temperature);
            var maxTokens = ReadInt32(config, "maxTokens", settings.MaxTokens);
            var topP = ReadDouble(config, "topP", ReadDouble(config, "top_p", settings.TopP));
            var repetitionPenalty = ReadDouble(config, "repetitionPenalty", ReadDouble(config, "repetition_penalty", settings.RepetitionPenalty));
            var qualityGateEnabled = ReadBoolean(config, "qualityGateEnabled", settings.QualityGateEnabled);
            var qualityGateMinSamples = ReadInt32(config, "qualityGateMinSamples", settings.QualityGateMinSamples);
            var qualityGateMinSuccessRate = ReadDouble(config, "qualityGateMinSuccessRate", settings.QualityGateMinSuccessRate);
            var qualityGateMaxFallbackRate = ReadDouble(config, "qualityGateMaxFallbackRate", settings.QualityGateMaxFallbackRate);
            var qualityGateMaxAvgLatencyMs = ReadInt32(config, "qualityGateMaxAvgLatencyMs", settings.QualityGateMaxAvgLatencyMs);

            if (TryGetPropertyIgnoreCase(config, "qualityGate", out var qualityGateObj)
                && qualityGateObj.ValueKind == JsonValueKind.Object)
            {
                qualityGateEnabled = ReadBoolean(qualityGateObj, "enabled", qualityGateEnabled);
                qualityGateMinSamples = ReadInt32(qualityGateObj, "minSamples", qualityGateMinSamples);
                qualityGateMinSuccessRate = ReadDouble(qualityGateObj, "minSuccessRate", qualityGateMinSuccessRate);
                qualityGateMaxFallbackRate = ReadDouble(qualityGateObj, "maxFallbackRate", qualityGateMaxFallbackRate);
                qualityGateMaxAvgLatencyMs = ReadInt32(qualityGateObj, "maxAvgLatencyMs", qualityGateMaxAvgLatencyMs);
            }

            settings.Enabled = enabled;
            settings.Provider = provider;
            settings.BaseUrl = baseUrl;
            settings.Path = path;
            settings.Model = model;
            settings.ApiKey = apiKey;
            settings.ApiKeyEnv = apiKeyEnv;
            settings.SystemPrompt = systemPrompt;
            settings.Task = task;
            settings.LocalFilesOnly = localFilesOnly;
            settings.Temperature = temperature;
            settings.MaxTokens = maxTokens;
            settings.TopP = topP;
            settings.RepetitionPenalty = repetitionPenalty;
            settings.QualityGateEnabled = qualityGateEnabled;
            settings.QualityGateMinSamples = qualityGateMinSamples;
            settings.QualityGateMinSuccessRate = qualityGateMinSuccessRate;
            settings.QualityGateMaxFallbackRate = qualityGateMaxFallbackRate;
            settings.QualityGateMaxAvgLatencyMs = qualityGateMaxAvgLatencyMs;
        }

        private static string NormalizeModelProvider(string? provider)
        {
            var key = (provider ?? "").Trim().ToLowerInvariant();
            return key switch
            {
                "openai-compatible" => "openai",
                "openai_compatible" => "openai",
                "chatgpt" => "openai",
                "ollama" => "ollama",
                "openai" => "openai",
                _ => key
            };
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string key, out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
                    {
                        value = prop.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static string? ReadString(JsonElement element, string key)
        {
            if (!TryGetPropertyIgnoreCase(element, key, out var value)) return null;
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined) return null;
            if (value.ValueKind == JsonValueKind.String) return value.GetString();
            return value.ToString();
        }

        private static bool ReadBoolean(JsonElement element, string key, bool fallback)
        {
            if (!TryGetPropertyIgnoreCase(element, key, out var value)) return fallback;
            if (value.ValueKind == JsonValueKind.True) return true;
            if (value.ValueKind == JsonValueKind.False) return false;
            if (value.ValueKind == JsonValueKind.String
                && bool.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
            return fallback;
        }

        private static double ReadDouble(JsonElement element, string key, double fallback)
        {
            if (!TryGetPropertyIgnoreCase(element, key, out var value)) return fallback;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var numeric))
                return numeric;
            if (value.ValueKind == JsonValueKind.String
                && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
            return fallback;
        }

        private static int ReadInt32(JsonElement element, string key, int fallback)
        {
            if (!TryGetPropertyIgnoreCase(element, key, out var value)) return fallback;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numeric))
                return numeric;
            if (value.ValueKind == JsonValueKind.String
                && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
            return fallback;
        }

        private static DateTime? ReadDateTime(JsonElement element, string key)
        {
            var raw = ReadString(element, key);
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (DateTime.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            {
                return parsed;
            }
            return null;
        }

        private static string BuildEngineContextJson(
            ProjectsResponse project,
            TemplatesResponse template,
            string? rawContextJson,
            ModelServiceSettings? modelSettings)
        {
            JsonObject root;
            try
            {
                var parsed = string.IsNullOrWhiteSpace(rawContextJson)
                    ? null
                    : JsonNode.Parse(rawContextJson!);

                if (parsed is JsonObject parsedObject)
                {
                    root = parsedObject;
                }
                else
                {
                    root = new JsonObject();
                    if (!string.IsNullOrWhiteSpace(rawContextJson))
                    {
                        root["inputContextRaw"] = rawContextJson;
                    }
                }
            }
            catch
            {
                root = new JsonObject();
                if (!string.IsNullOrWhiteSpace(rawContextJson))
                {
                    root["inputContextRaw"] = rawContextJson;
                }
            }

            JsonNode? pipelineNode = null;
            if (!string.IsNullOrWhiteSpace(template.Pipelinejson))
            {
                try
                {
                    pipelineNode = JsonNode.Parse(template.Pipelinejson!);
                }
                catch
                {
                    pipelineNode = null;
                }
            }

            var resolvedModelService = new JsonObject
            {
                ["enabled"] = modelSettings?.Enabled ?? true,
                ["provider"] = NormalizeModelProvider(modelSettings?.Provider ?? AppConfig.AIBASE_MODEL_PROVIDER),
                ["model"] = modelSettings?.Model ?? "",
                ["task"] = modelSettings?.Task ?? "",
                ["localFilesOnly"] = modelSettings?.LocalFilesOnly ?? AppConfig.AIBASE_MODEL_LOCAL_FILES_ONLY,
                ["temperature"] = modelSettings?.Temperature ?? AppConfig.AIBASE_MODEL_TEMPERATURE,
                ["maxTokens"] = modelSettings?.MaxTokens ?? AppConfig.AIBASE_MODEL_MAX_TOKENS,
                ["topP"] = modelSettings?.TopP ?? AppConfig.AIBASE_MODEL_TOP_P,
                ["repetitionPenalty"] = modelSettings?.RepetitionPenalty ?? AppConfig.AIBASE_MODEL_REPETITION_PENALTY,
                ["systemPrompt"] = modelSettings?.SystemPrompt ?? AppConfig.AIBASE_MODEL_SYSTEM_PROMPT,
                ["qualityGateEnabled"] = modelSettings?.QualityGateEnabled ?? AppConfig.AIBASE_QUALITY_GATE_ENABLED,
                ["qualityGateMinSamples"] = modelSettings?.QualityGateMinSamples ?? AppConfig.AIBASE_QUALITY_GATE_MIN_SAMPLES,
                ["qualityGateMinSuccessRate"] = modelSettings?.QualityGateMinSuccessRate ?? AppConfig.AIBASE_QUALITY_GATE_MIN_SUCCESS_RATE,
                ["qualityGateMaxFallbackRate"] = modelSettings?.QualityGateMaxFallbackRate ?? AppConfig.AIBASE_QUALITY_GATE_MAX_FALLBACK_RATE,
                ["qualityGateMaxAvgLatencyMs"] = modelSettings?.QualityGateMaxAvgLatencyMs ?? AppConfig.AIBASE_QUALITY_GATE_MAX_AVG_LATENCY_MS,
            };

            root["_aibase"] = new JsonObject
            {
                ["project"] = new JsonObject
                {
                    ["id"] = project.Id,
                    ["slug"] = project.Slug,
                    ["name"] = project.Name,
                    ["language"] = project.Language,
                    ["tone"] = project.Tone,
                    ["status"] = project.Status
                },
                ["template"] = new JsonObject
                {
                    ["id"] = template.Id,
                    ["key"] = template.Key,
                    ["name"] = template.Name,
                    ["version"] = template.Version,
                    ["pipeline"] = pipelineNode
                },
                ["runtime"] = new JsonObject
                {
                    ["modelServiceResolved"] = resolvedModelService
                },
                ["generatedAt"] = DateTime.UtcNow
            };

            return root.ToJsonString();
        }

        private static string BuildCurlSnippet(string inferUrl, int projectId)
        {
            if (string.IsNullOrWhiteSpace(inferUrl))
            {
                return "# Completa endpoint de inferencia para generar snippet";
            }

            return $"curl -X POST \"{inferUrl}\" \\\n" +
                   "  -H \"Content-Type: application/json\" \\\n" +
                   "  -d '{\n" +
                   $"    \"projectId\": {projectId},\n" +
                   "    \"input\": \"Ejemplo de prueba desde cURL\"\n" +
                   "  }'";
        }

        private static string BuildJavaScriptSnippet(string inferUrl)
        {
            if (string.IsNullOrWhiteSpace(inferUrl))
            {
                return "// Completa endpoint de inferencia para generar snippet";
            }

            return "const payload = {\n" +
                   "  input: 'Ejemplo de prueba desde JavaScript',\n" +
                   "  contextJson: null\n" +
                   "};\n\n" +
                   $"const response = await fetch('{inferUrl}', {{\n" +
                   "  method: 'POST',\n" +
                   "  headers: { 'Content-Type': 'application/json' },\n" +
                   "  body: JSON.stringify(payload)\n" +
                   "});\n\n" +
                   "const data = await response.json();\n" +
                   "console.log(data);";
        }

        private static string BuildPythonSnippet(string inferUrl)
        {
            if (string.IsNullOrWhiteSpace(inferUrl))
            {
                return "# Completa endpoint de inferencia para generar snippet";
            }

            return "import requests\n\n" +
                   "payload = {\n" +
                   "    \"input\": \"Ejemplo de prueba desde Python\",\n" +
                   "    \"contextJson\": None,\n" +
                   "}\n\n" +
                   $"response = requests.post(\"{inferUrl}\", json=payload, timeout=60)\n" +
                   "response.raise_for_status()\n" +
                   "print(response.json())";
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
            var provider = "mock";
            string? model = null;
            string? endpoint = null;
            var usedFallback = false;
            double? qualityScore = null;
            string? traceId = null;
            string? diagnosticsJson = null;
            string? engineNotice = null;

            var modelSettings = ResolveModelServiceSettings(template);
            var useModelService = modelSettings.Enabled
                                  && (string.Equals(modelSettings.Provider, "ollama", StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(modelSettings.Provider, "openai", StringComparison.OrdinalIgnoreCase));

            if (useModelService)
            {
                var modelResult = await _modelServiceClient.InferAsync(modelSettings, request.Input, request.ContextJson);
                if (!modelResult.Ok)
                {
                    return StatusCode(502, new
                    {
                        error = modelResult.Error ?? "No se pudo inferir con model service.",
                        provider = modelSettings.Provider,
                        model = modelSettings.Model,
                        endpoint = modelSettings.BaseUrl
                    });
                }

                output = modelResult.Output ?? "";
                outputJson = modelResult.OutputJson;
                provider = modelResult.Provider;
                model = modelResult.Model;
                endpoint = modelResult.Endpoint;
                useMock = false;
            }
            else if (useMock)
            {
                var mock = BuildMockInferOutput(template.Key, request.Input, request.ContextJson);
                output = mock.Output;
                outputJson = mock.OutputJson;
            }
            else
            {
                var engineContextJson = BuildEngineContextJson(project, template, request.ContextJson, modelSettings);
                var result = await _engineClient.InferAsync(projectId, request.Input, engineContextJson);
                if (!result.Ok)
                {
                    return StatusCode(502, new
                    {
                        error = result.Error ?? "No se pudo inferir con engine.",
                        response = result.ResponseJson,
                        engineEndpoint = result.Endpoint,
                        engineNotice = result.Notice,
                        triedEndpoints = result.TriedEndpoints
                    });
                }

                output = result.ResponseJson ?? "";
                outputJson = result.ResponseJson;
                provider = "engine";
                endpoint = result.Endpoint ?? AppConfig.AIBASE_ENGINE_URL;
                engineNotice = result.Notice;

                if (!string.IsNullOrWhiteSpace(result.ResponseJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(result.ResponseJson);
                        var root = doc.RootElement;
                        var parsedAnswer = ReadString(root, "answer");
                        if (!string.IsNullOrWhiteSpace(parsedAnswer))
                        {
                            output = parsedAnswer!;
                        }

                        var parsedProvider = ReadString(root, "provider");
                        if (!string.IsNullOrWhiteSpace(parsedProvider))
                        {
                            provider = parsedProvider!;
                        }

                        model = ReadString(root, "model") ?? model;
                        usedFallback = ReadBoolean(root, "usedFallback", false);
                        var parsedScore = ReadDouble(root, "qualityScore", -1);
                        if (parsedScore >= 0) qualityScore = parsedScore;
                        traceId = ReadString(root, "traceId") ?? traceId;
                        if (TryGetPropertyIgnoreCase(root, "diagnostics", out var diagnosticsNode)
                            && diagnosticsNode.ValueKind == JsonValueKind.Object)
                        {
                            diagnosticsJson = diagnosticsNode.GetRawText();
                        }
                    }
                    catch
                    {
                        // Si la respuesta no es JSON válido dejamos salida cruda.
                    }
                }
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
                Provider = provider,
                Model = model,
                Endpoint = endpoint,
                EngineNotice = engineNotice,
                UsedFallback = usedFallback,
                QualityScore = qualityScore,
                TraceId = traceId,
                DiagnosticsJson = diagnosticsJson,
                CreatedAt = DateTime.UtcNow
            });
        }

        [Authorize]
        [HttpGet(Routes.v1.Aibase.InferMetrics)]
        public async Task<IActionResult> InferMetrics(int projectId, [FromQuery] int take = 20)
        {
            var project = AibaseOpsGestor.GetProject(projectId);
            if (project == null) return NotFound("Proyecto inexistente.");
            var template = AibaseOpsGestor.GetTemplate(project.Templateid);
            if (template == null) return BadRequest("Template asociado inexistente.");

            if (AppConfig.AIBASE_ENGINE_MOCK)
            {
                return Ok(new
                {
                    projectId,
                    summary = new
                    {
                        total = 0,
                        successCount = 0,
                        fallbackCount = 0,
                        successRate = 0.0,
                        fallbackRate = 0.0,
                        avgLatencyMs = 0,
                        avgQualityScore = 0.0,
                        lastAt = (string?)null
                    },
                    gate = new
                    {
                        ok = true,
                        enabled = false,
                        reason = "engine mock habilitado"
                    },
                    runs = Array.Empty<object>(),
                    createdAt = DateTime.UtcNow
                });
            }

            var modelSettings = ResolveModelServiceSettings(template);
            var metrics = await _engineClient.GetInferMetricsAsync(
                projectId,
                take: Math.Clamp(take, 1, 200),
                gateEnabled: modelSettings.QualityGateEnabled,
                minSamples: modelSettings.QualityGateMinSamples,
                minSuccessRate: modelSettings.QualityGateMinSuccessRate,
                maxFallbackRate: modelSettings.QualityGateMaxFallbackRate,
                maxAvgLatencyMs: modelSettings.QualityGateMaxAvgLatencyMs);

            if (!metrics.Ok)
            {
                return StatusCode(502, new
                {
                    error = metrics.Error ?? "No se pudieron obtener métricas de inferencia.",
                    response = metrics.ResponseJson,
                    engineEndpoint = metrics.Endpoint,
                    engineNotice = metrics.Notice,
                    triedEndpoints = metrics.TriedEndpoints
                });
            }

            return Content(metrics.ResponseJson ?? "{}", "application/json", Encoding.UTF8);
        }

        [Authorize]
        [HttpPost(Routes.v1.Aibase.AssistantSuggest)]
        public IActionResult AssistantSuggest([FromBody] AibaseAssistantSuggestRequest? request)
        {
            var prompt = request?.Prompt?.Trim();
            if (string.IsNullOrWhiteSpace(prompt))
                return BadRequest("Prompt requerido.");

            var stage = NormalizeStage(request?.Stage);
            var blueprint = DetectBlueprint(prompt);

            var suggestions = BuildSuggestions(blueprint);
            if (!string.IsNullOrWhiteSpace(stage))
            {
                suggestions = suggestions
                    .Where(s => string.Equals(s.Stage, stage, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var response = new AibaseAssistantSuggestResponse
            {
                Message = BuildAssistantMessage(blueprint, stage),
                DetectedModelType = blueprint.ModelType,
                RecommendedStage = stage ?? "template",
                Suggestions = suggestions
            };

            return Ok(response);
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
            var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
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

        private static string? NormalizeStage(string? stage)
        {
            if (string.IsNullOrWhiteSpace(stage)) return null;
            var key = stage.Trim().ToLowerInvariant();
            return key switch
            {
                "template" or "project" or "dataset" or "rag" or "train" or "eval" or "deploy" or "playground" => key,
                _ => null
            };
        }

        private static List<AibaseAssistantStageSuggestion> BuildSuggestions(AssistantBlueprint blueprint)
        {
            var steps = blueprint.Steps.ToArray();
            var outputSchemaJson = PrettyJson(blueprint.OutputSchema);
            var taxonomyJson = PrettyJson(blueprint.Taxonomy);
            var validationRulesJson = PrettyJson(blueprint.ValidationRules);
            var sampleRowsJson = PrettyJson(blueprint.SampleRows);

            return new List<AibaseAssistantStageSuggestion>
            {
                new()
                {
                    Stage = "template",
                    Title = "Template sugerido",
                    Description = "Contrato inicial del modelo con pipeline recomendado.",
                    Fields = new Dictionary<string, object?>
                    {
                        ["presetKey"] = blueprint.TemplatePreset,
                        ["key"] = blueprint.TemplateKey,
                        ["name"] = blueprint.TemplateName,
                        ["version"] = "1.0.0",
                        ["description"] = blueprint.TemplateDescription,
                        ["objective"] = blueprint.Objective,
                        ["outputSchemaJson"] = outputSchemaJson,
                        ["taxonomyJson"] = taxonomyJson,
                        ["validationRulesJson"] = validationRulesJson,
                        ["annotationGuide"] = blueprint.AnnotationGuide,
                        ["steps"] = steps
                    }
                },
                new()
                {
                    Stage = "project",
                    Title = "Proyecto sugerido",
                    Description = "Configuración operativa inicial del proyecto.",
                    Fields = new Dictionary<string, object?>
                    {
                        ["name"] = blueprint.ProjectName,
                        ["slug"] = blueprint.ProjectSlug,
                        ["description"] = blueprint.ProjectDescription,
                        ["language"] = "es",
                        ["tone"] = blueprint.Tone,
                        ["status"] = "draft"
                    }
                },
                new()
                {
                    Stage = "dataset",
                    Title = "Dataset sugerido",
                    Description = "Parámetros para la primera corrida de build dataset.",
                    Fields = new Dictionary<string, object?>
                    {
                        ["datasetVersion"] = "v1.0.0",
                        ["sourceType"] = blueprint.DatasetSourceType,
                        ["sourcePath"] = blueprint.DatasetSourcePath,
                        ["splitTrainPct"] = blueprint.SplitTrainPct,
                        ["splitValPct"] = blueprint.SplitValPct,
                        ["splitTestPct"] = blueprint.SplitTestPct,
                        ["minRecords"] = blueprint.MinRecords,
                        ["maxNullPct"] = blueprint.MaxNullPct,
                        ["tagsCsv"] = blueprint.TagsCsv,
                        ["deduplicate"] = true,
                        ["normalizeText"] = true,
                        ["dropNullTarget"] = false,
                        ["sampleRowsJson"] = sampleRowsJson
                    }
                }
            };
        }

        private static string BuildAssistantMessage(AssistantBlueprint blueprint, string? stage)
        {
            if (!string.IsNullOrWhiteSpace(stage))
            {
                return $"Interpreté un caso de tipo '{blueprint.ModelType}'. Te devuelvo sugerencias para la etapa '{stage}'.";
            }

            return $"Interpreté un caso de tipo '{blueprint.ModelType}'. Ya dejé sugerencias para Template, Proyecto y Dataset.";
        }

        private static AssistantBlueprint DetectBlueprint(string prompt)
        {
            var normalized = NormalizeText(prompt);
            var modelType = "general";
            var preset = "custom";
            var templateName = "Template IA Custom";
            var templateKey = "custom-template-v1";
            var objective = "Definir un modelo genérico configurable por workflow.";
            var description = "Template base para crear un modelo IA personalizado.";
            var steps = new List<string> { "dataset_build", "train_lora", "eval_run", "deploy_service" };
            object outputSchema = new { output = "", confidence = 0.0 };
            object taxonomy = new { classes = new[] { "class_a", "class_b", "other" } };
            object validation = new { required = new[] { "output" }, confidenceMin = 0.7 };
            var annotationGuide = "Etiquetar con consistencia y sin inventar datos.";
            var projectName = "Proyecto IA";
            var projectSlug = "proyecto-ia";
            var projectDescription = "Proyecto creado con sugerencias del asistente.";
            var tone = "neutral";
            var sourceType = "jsonl";
            var sourcePath = "/datasets/train.jsonl";
            var tagsCsv = "baseline,v1";
            object sampleRows = new object[]
            {
                new { input = "Ejemplo de entrada", target = new { output = "resultado esperado" } }
            };
            var minRecords = 500;
            var maxNullPct = 15;
            var splitTrain = 80;
            var splitVal = 10;
            var splitTest = 10;

            if (ContainsAny(normalized, "chatbot", "asistente", "faq", "preguntas y respuestas", "rag"))
            {
                modelType = "chatbot_rag";
                preset = "chatbot_rag";
                templateName = "Chatbot RAG";
                templateKey = "chatbot-rag-v1";
                objective = "Responder preguntas del usuario usando contexto documental indexado.";
                description = "Asistente conversacional con recuperación de contexto (RAG).";
                steps = new List<string> { "dataset_build", "rag_index", "eval_run", "deploy_service" };
                outputSchema = new { answer = "", sources = new[] { "doc_id" }, confidence = 0.0 };
                taxonomy = new { intents = new[] { "consulta", "seguimiento", "otro" }, tone = new[] { "formal", "neutral" } };
                validation = new { required = new[] { "answer", "sources" }, minSources = 1 };
                annotationGuide = "La respuesta debe citar fuentes; si no hay evidencia devolver 'sin evidencia suficiente'.";
                projectName = "Asistente RAG";
                projectSlug = "asistente-rag";
                projectDescription = "Proyecto de chatbot con base documental.";
                sourceType = "csv";
                sourcePath = "/datasets/faq_corpus.csv";
                tagsCsv = "rag,faq,v1";
                sampleRows = new object[]
                {
                    new { input = "¿Cuál es el horario de atención?", target = new { answer = "Lunes a viernes de 8 a 18.", sources = new[] { "faq-12" } } }
                };
            }
            else if (ContainsAny(normalized, "transcripcion", "transcribir", "audio", "voz", "whisper"))
            {
                modelType = "transcription";
                preset = "transcriptor_audio";
                templateName = "Transcriptor Audio";
                templateKey = "transcriptor-audio-v1";
                objective = "Transcribir audio a texto con limpieza y extracción básica de entidades.";
                description = "Pipeline de transcripción de voz con salida estructurada.";
                outputSchema = new { text = "", entities = new[] { "persona", "lugar", "hora" }, confidence = 0.0 };
                taxonomy = new { entities = new[] { "persona", "lugar", "hora", "evento" } };
                validation = new { required = new[] { "text" }, minTextLength = 30 };
                annotationGuide = "Corregir ortografía mínima sin alterar sentido del audio.";
                projectName = "Transcriptor Inteligente";
                projectSlug = "transcriptor-inteligente";
                projectDescription = "Proyecto para transcripción automática de audios.";
                sourceType = "jsonl";
                sourcePath = "/datasets/audio_transcripts.jsonl";
                tagsCsv = "audio,asr,v1";
                sampleRows = new object[]
                {
                    new { input = "audio://sample-001.wav", target = new { text = "Hoy 5 de marzo ocurrió...", entities = new[] { "hoy", "cordoba" } } }
                };
                minRecords = 1000;
                maxNullPct = 10;
            }
            else if (ContainsAny(normalized, "ocr", "imagen", "foto", "documento", "escaneo"))
            {
                modelType = "vision_ocr";
                preset = "vision_ocr";
                templateName = "OCR Vision";
                templateKey = "ocr-vision-v1";
                objective = "Extraer texto y bloques desde imágenes o documentos escaneados.";
                description = "Modelo OCR para reconocimiento de texto en imagen.";
                outputSchema = new { text = "", blocks = new[] { new { x = 0, y = 0, w = 0, h = 0, text = "" } }, confidence = 0.0 };
                taxonomy = new { blockTypes = new[] { "line", "paragraph", "table" } };
                validation = new { required = new[] { "text" } };
                annotationGuide = "Mantener orden de lectura y coordenadas de bloques detectados.";
                projectName = "OCR Documentos";
                projectSlug = "ocr-documentos";
                projectDescription = "Proyecto para digitalizar texto de imágenes.";
                sourceType = "jsonl";
                sourcePath = "/datasets/ocr_pairs.jsonl";
                tagsCsv = "ocr,vision,v1";
                sampleRows = new object[]
                {
                    new { input = "image://ticket-001.jpg", target = new { text = "TOTAL: 4500", blocks = new[] { new { x = 10, y = 20, w = 120, h = 30, text = "TOTAL: 4500" } } } }
                };
            }
            else if (ContainsAny(normalized, "geolocalizacion", "direccion", "mapa", "ubicacion", "incidente"))
            {
                modelType = "geo_extractor";
                preset = "extractor_json";
                templateName = "Extractor Geolocalizado";
                templateKey = "extractor-geo-v1";
                objective = "Extraer ubicación y metadatos desde texto libre para georreferenciar eventos.";
                description = "Modelo extractor de campos con foco en ubicación.";
                outputSchema = new { tipoHecho = "", lugarTexto = "", fechaHora = "", lat = (double?)null, lng = (double?)null, confidence = 0.0 };
                taxonomy = new { tipoHecho = new[] { "robo", "arrebato", "hurto", "otro" } };
                validation = new { required = new[] { "tipoHecho", "lugarTexto" }, confidenceMin = 0.75 };
                annotationGuide = "No inventar coordenadas. Si no hay certeza, devolver null en lat/lng.";
                projectName = "Extractor Geolocalización";
                projectSlug = "extractor-geolocalizacion";
                projectDescription = "Proyecto para extracción estructurada de hechos con ubicación.";
                sourceType = "csv";
                sourcePath = "/datasets/incidentes_cordoba.csv";
                tagsCsv = "geo,extractor,v1";
                sampleRows = new object[]
                {
                    new { input = "Arrebato en Av. Colón 1200, Córdoba", target = new { tipoHecho = "arrebato", lugarTexto = "Av. Colón 1200, Córdoba", fechaHora = "2026-03-05T21:30:00" } }
                };
            }
            else if (ContainsAny(normalized, "facial", "rostro", "cara", "identidad"))
            {
                modelType = "facial_recognition";
                preset = "custom";
                templateName = "Reconocimiento Facial";
                templateKey = "reconocimiento-facial-v1";
                objective = "Detectar y comparar rostros para identificación asistida.";
                description = "Pipeline de reconocimiento facial con evaluación de umbral.";
                outputSchema = new { faces = new[] { new { id = "", score = 0.0, bbox = new[] { 0, 0, 0, 0 } } } };
                taxonomy = new { classes = new[] { "match", "unknown" } };
                validation = new { required = new[] { "faces" }, scoreThreshold = 0.8 };
                annotationGuide = "Respetar normativa de privacidad. No confirmar identidad bajo score umbral.";
                projectName = "Reconocimiento Facial";
                projectSlug = "reconocimiento-facial";
                projectDescription = "Proyecto para detección y comparación de rostros.";
                steps = new List<string> { "dataset_build", "train_lora", "eval_run", "deploy_service" };
                sourceType = "jsonl";
                sourcePath = "/datasets/faces_annotations.jsonl";
                tagsCsv = "vision,facial,v1";
                sampleRows = new object[]
                {
                    new { input = "image://cam-2026-03-05.jpg", target = new { faces = new[] { new { id = "persona_001", score = 0.93, bbox = new[] { 200, 120, 80, 80 } } } } }
                };
                minRecords = 3000;
                maxNullPct = 5;
            }

            var suggestedName = ExtractNameHint(prompt);
            if (!string.IsNullOrWhiteSpace(suggestedName))
            {
                projectName = suggestedName;
                projectSlug = Slugify(suggestedName);
            }

            return new AssistantBlueprint
            {
                ModelType = modelType,
                TemplatePreset = preset,
                TemplateName = templateName,
                TemplateKey = templateKey,
                TemplateDescription = description,
                Objective = objective,
                OutputSchema = outputSchema,
                Taxonomy = taxonomy,
                ValidationRules = validation,
                AnnotationGuide = annotationGuide,
                Steps = steps,
                ProjectName = projectName,
                ProjectSlug = projectSlug,
                ProjectDescription = projectDescription,
                Tone = tone,
                DatasetSourceType = sourceType,
                DatasetSourcePath = sourcePath,
                SplitTrainPct = splitTrain,
                SplitValPct = splitVal,
                SplitTestPct = splitTest,
                MinRecords = minRecords,
                MaxNullPct = maxNullPct,
                TagsCsv = tagsCsv,
                SampleRows = sampleRows
            };
        }

        private static string PrettyJson(object value)
        {
            return JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
        }

        private static string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new System.Text.StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark) builder.Append(char.ToLowerInvariant(c));
            }
            return builder.ToString();
        }

        private static bool ContainsAny(string text, params string[] words)
        {
            return words.Any(w => text.Contains(NormalizeText(w), StringComparison.Ordinal));
        }

        private static string? ExtractNameHint(string prompt)
        {
            var match = Regex.Match(
                prompt,
                "(?:modelo|proyecto)\\s+(?:de|para)\\s+([a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\\-\\s]{4,60})",
                RegexOptions.IgnoreCase
            );
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private static string Slugify(string value)
        {
            var normalized = NormalizeText(value);
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^a-z0-9]+", "-");
            normalized = normalized.Trim('-');
            return string.IsNullOrWhiteSpace(normalized) ? "proyecto-ia" : normalized;
        }

        private sealed class RunExecutionResult
        {
            public AibaseRunResponse? Run { get; set; }
            public string? Error { get; set; }
        }

        private sealed class AssistantBlueprint
        {
            public string ModelType { get; set; } = "general";
            public string TemplatePreset { get; set; } = "custom";
            public string TemplateName { get; set; } = "Template IA";
            public string TemplateKey { get; set; } = "template-ia-v1";
            public string TemplateDescription { get; set; } = "";
            public string Objective { get; set; } = "";
            public object OutputSchema { get; set; } = new { };
            public object Taxonomy { get; set; } = new { };
            public object ValidationRules { get; set; } = new { };
            public string AnnotationGuide { get; set; } = "";
            public List<string> Steps { get; set; } = new();
            public string ProjectName { get; set; } = "Proyecto IA";
            public string ProjectSlug { get; set; } = "proyecto-ia";
            public string ProjectDescription { get; set; } = "";
            public string Tone { get; set; } = "neutral";
            public string DatasetSourceType { get; set; } = "jsonl";
            public string DatasetSourcePath { get; set; } = "/datasets/train.jsonl";
            public int SplitTrainPct { get; set; } = 80;
            public int SplitValPct { get; set; } = 10;
            public int SplitTestPct { get; set; } = 10;
            public int MinRecords { get; set; } = 500;
            public int MaxNullPct { get; set; } = 15;
            public string TagsCsv { get; set; } = "baseline,v1";
            public object SampleRows { get; set; } = Array.Empty<object>();
        }
    }
}
