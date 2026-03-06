using System.Text.Json;
using System.Globalization;
using System.Text;
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
            var match = System.Text.RegularExpressions.Regex.Match(
                prompt,
                "(?:modelo|proyecto)\\s+(?:de|para)\\s+([a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\\-\\s]{4,60})",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
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
