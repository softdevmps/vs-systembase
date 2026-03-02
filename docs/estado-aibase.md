# AIBase V1 - Arquitectura y Plan de Implementacion

Fecha: 2026-03-02
Branch: `aibase`

## 0) Avance implementado en esta rama
- Backend (SystemBase):
  - Migrador idempotente `sb_ai` al iniciar API: `backend/Utils/AibaseSchemaMigrator.cs`.
  - Tablas iniciales creadas/gestionadas:
    - `sb_ai.Templates`
    - `sb_ai.Projects`
  - Seed de templates V1:
    - `extractor-json`
    - `chat-rag`
  - Nuevos endpoints:
    - `GET /api/v1/aibase/templates`
    - `GET /api/v1/aibase/projects`
    - `GET /api/v1/aibase/projects/{id}`
    - `POST /api/v1/aibase/projects`
    - `GET /api/v1/aibase/projects/{projectId}/runs`
    - `POST /api/v1/aibase/projects/{projectId}/runs`
    - `GET /api/v1/aibase/runs/{id}`
    - `POST /api/v1/aibase/runs/{id}/sync`
  - Orquestación inicial de runs:
    - tabla `sb_ai.Runs`
    - despacho al engine por HTTP (`AibaseEngineClient`)
    - fallback `stub` cuando `AIBASE_ENGINE_ENABLED=false`
- Menú base agregado en seed: `AIBase` (`/aibase`).
- Frontend (SystemBase):
  - Vista inicial AIBase: `frontend/src/views/Sistema/Aibase.vue`.
  - Servicio API: `frontend/src/api/aibase.service.js`.
  - Ruta habilitada: `/aibase`.
  - UI de runs por proyecto:
    - disparo de `dataset_build`, `rag_index`, `train_lora`, `eval_run`
    - listado de runs y sync manual de estado.

> Este corte deja listo el bootstrap funcional para crear y listar proyectos AIBase desde la fábrica.

## 1) Supuestos de V1
- AIBase se construye sobre la fabrica actual de SystemBase, pero como modulo/producto nuevo.
- Stack principal:
  - Plataforma: `.NET 8 + SQL Server + Vue 3 + Vuetify`.
  - Motor IA: `Python + FastAPI` en contenedor separado.
- Templates V1 incluidos:
  - `extractor-json`
  - `chat-rag`
- Multi-tenant:
  - V1 con estructura preparada (`TenantId`) pero operacion inicial `single-tenant`.

## 2) Vision de arquitectura (C4 textual)

### Contexto (Nivel 1)
- Usuario de negocio/analista usa AIBase para crear y educar asistentes/modelos por dominio.
- AIBase consume servicios IA (entrenamiento, indexado, inferencia) y storage de artefactos.

### Contenedores (Nivel 2)
- `aibase-ui` (Vue/Vuetify)
  - Wizard, proyectos, datasets, evaluacion, deployments, monitoreo.
- `aibase-api` (.NET)
  - Auth, permisos, metadata-driven, versionado, orquestacion de jobs.
- `aibase-engine` (FastAPI)
  - Build dataset, RAG index, train LoRA, infer, eval.
- `sqlserver`
  - Metadata, versiones, auditoria, estados de runs.
- `minio` (o filesystem local en dev)
  - Datasets, adapters, reportes, artefactos exportables.
- `qdrant`
  - Índices vectoriales para RAG.
- `redis`
  - Cola de trabajos y pub/sub de progreso.

### Componentes internos clave (Nivel 3)
- En `aibase-api`:
  - `ProjectService`, `TemplateService`, `RunOrchestratorService`, `DeploymentService`, `AuditService`.
  - `EngineClient` (HTTP) para delegar tareas IA.
  - `RunWorker` para trabajos async (redis queue).
- En `aibase-engine`:
  - `DatasetBuilder`, `RagIndexer`, `LoraTrainer`, `Evaluator`, `InferenceGateway`.
  - `ArtifactStoreAdapter` (MinIO/fs).

## 3) Flujo end-to-end
1. Usuario crea proyecto y selecciona template en UI.
2. UI llama API (`POST /api/v1/aibase/projects`).
3. API registra metadata + configuracion versionable en SQL.
4. Usuario sube documentos/ejemplos.
5. API guarda metadata y archivo en storage.
6. Usuario ejecuta “Educar” (indexar o entrenar).
7. API crea `Run` y encola trabajo en Redis.
8. Worker llama `aibase-engine` (`/rag/index` o `/train/lora`).
9. Engine procesa, guarda artefactos, actualiza progreso.
10. API persiste resultados (`DatasetVersion`, `ModelVersion`, `EvalRun`).
11. Usuario evalua y publica.
12. API activa `DeploymentVersion` + API key.

## 4) Decisiones tecnicas clave

### Comunicacion .NET <-> Python
- Decision V1: `HTTP REST + JSON`.
- Motivo:
  - Menor friccion cross-language.
  - Debug simple por curl/swagger.
  - Contratos versionables sin complejidad de gRPC al inicio.
- Futuro:
  - gRPC para streaming/progreso fino si la carga lo justifica.

### Cola de jobs
- Decision V1: `Redis` (listas/streams) + worker en `aibase-api`.
- Motivo:
  - Separacion de UI/API y procesos largos.
  - Retry/backoff y estado no bloqueante.
- Fallback local:
  - SQL polling queue (compatibilidad con entornos sin Redis).

## 5) Modelo de datos (SQL Server)

## 5.1 Tablas principales
- `sb_ai.Tenants`
  - `Id`, `Name`, `Slug`, `IsActive`, `CreatedAt`.
- `sb_ai.Projects`
  - `Id`, `TenantId`, `Name`, `Description`, `TemplateKey`, `Status`, `CreatedBy`, `CreatedAt`.
- `sb_ai.Templates`
  - `Id`, `Key`, `Name`, `PipelineJson`, `IsActive`, `Version`.
- `sb_ai.PromptProfiles`
  - `Id`, `ProjectId`, `Name`, `SystemPrompt`, `Tone`, `PoliciesJson`, `SchemaJson`.
- `sb_ai.DataSources`
  - `Id`, `ProjectId`, `Type` (`document`, `csv`, `audio`, `url`, `text`), `ConfigJson`, `Status`.
- `sb_ai.Documents`
  - `Id`, `DataSourceId`, `StoragePath`, `MimeType`, `Checksum`, `Tokens`, `MetadataJson`.
- `sb_ai.DatasetVersions`
  - `Id`, `ProjectId`, `Version`, `SourceSnapshotJson`, `ArtifactPath`, `RecordCount`, `Status`.
- `sb_ai.TrainingRuns`
  - `Id`, `ProjectId`, `DatasetVersionId`, `RunType`, `EngineRunId`, `Status`, `ProgressPct`, `ConfigJson`, `StartedAt`, `FinishedAt`.
- `sb_ai.ModelVersions`
  - `Id`, `ProjectId`, `BaseModel`, `AdapterPath`, `TrainRunId`, `Version`, `MetricsJson`, `Status`.
- `sb_ai.EvalSuites`
  - `Id`, `ProjectId`, `Name`, `CasesPath`, `SchemaJson`.
- `sb_ai.EvalRuns`
  - `Id`, `ProjectId`, `ModelVersionId`, `EvalSuiteId`, `EngineRunId`, `SummaryJson`, `Status`.
- `sb_ai.Deployments`
  - `Id`, `ProjectId`, `ModelVersionId`, `EndpointUrl`, `Status`, `IsActive`, `PublishedAt`.
- `sb_ai.APIKeys`
  - `Id`, `ProjectId`, `KeyHash`, `ScopesJson`, `ExpiresAt`, `RevokedAt`.
- `sb_ai.AuditLogs`
  - `Id`, `TenantId`, `UserId`, `Action`, `Entity`, `EntityId`, `PayloadJson`, `CreatedAt`.

## 5.2 Relaciones (clave)
- `Tenant -> Projects (1:N)`
- `Project -> DataSources/Documents/DatasetVersions/TrainingRuns/ModelVersions/EvalRuns/Deployments/APIKeys`
- `DatasetVersion -> TrainingRuns`
- `TrainingRun -> ModelVersion`
- `EvalSuite -> EvalRuns`
- `ModelVersion -> EvalRuns/Deployments`

## 5.3 Versionado y estados
- Versionado semantico interno por entidad:
  - `DatasetVersion`: `v1`, `v2`...
  - `ModelVersion`: `m1`, `m2`...
  - `DeploymentVersion`: `d1`, `d2`...
- Estado estandar de run:
  - `queued`, `running`, `completed`, `failed`, `canceled`.

## 6) Templates/plugins

## 6.1 Definicion de template
Cada template se define como registro en `sb_ai.Templates.PipelineJson`.

Ejemplo:
```json
{
  "key": "extractor-json",
  "steps": [
    {"name": "dataset_build", "engine": "/dataset/build"},
    {"name": "train_lora", "engine": "/train/lora"},
    {"name": "eval", "engine": "/eval/run"},
    {"name": "publish", "engine": "/infer"}
  ],
  "requires": ["schema_json", "examples"],
  "supports": ["versioning", "rollback"]
}
```

## 6.2 Agregar un template nuevo
1. Alta en tabla `Templates` con `PipelineJson`.
2. Mapeo UI de formulario dinamico por `requires`.
3. Validaciones backend por `TemplateKey`.
4. Si hay step nuevo, implementar endpoint en engine.

No rompe plataforma porque:
- El wizard consume metadata.
- El orquestador ejecuta por step declarativo.

## 7) Diseño de pantallas (Vue/Vuetify)
- `AibaseHome.vue` (resumen global de proyectos/runs/deployments).
- `AibaseProjects.vue` (lista + filtros + acciones).
- `AibaseProjectWizard.vue` (7 pasos).
- `AibaseProjectDetail.vue` (versiones, jobs, logs).
- `AibaseDataManager.vue` (documentos/ejemplos/data sources).
- `AibaseEvalCenter.vue` (suites, runs, regresiones).
- `AibaseDeployments.vue` (publicar/rollback/api keys).
- `AibaseObservability.vue` (latencia, errores, uso por version).

### Wizard (7 pasos)
1. Identidad proyecto.
2. Template.
3. Datos.
4. Educacion (ejemplos/reglas/schema).
5. Ejecutar pipeline.
6. Evaluar.
7. Publicar.

## 8) Pipeline “educacion” para usuarios
- RAG (explicacion simple):
  - “No reentrena el modelo; consulta documentos indexados para responder mejor”.
- LoRA (explicacion simple):
  - “Ajusta el modelo para tu formato/estilo usando ejemplos”.

Recomendaciones V1:
- Extractor JSON:
  - Minimo util: `50-100` ejemplos.
  - Bueno: `300+` ejemplos balanceados.
- RAG:
  - Documentos limpios, segmentados y con metadata.
  - Evitar PDFs escaneados sin OCR.

## 9) Roadmap

### V1 (MVP)
- Templates `extractor-json` y `chat-rag`.
- Wizard completo.
- Versionado dataset/model/eval/deploy.
- Publicacion simple por API key.

### V1.1
- Hibrido `rag+lora`.
- Auto-eval nocturna y alertas de regresion.
- Mejoras de trazabilidad por tenant.

### V2
- Multi-tenant avanzado (cuotas/aislamiento estricto).
- Marketplace de templates.
- Registry interno de modelos.

## 10) Plan de implementacion (orden recomendado)
1. SQL base (`sb_ai.*`) + migrador en backend.
2. API core (Projects/Templates/Versions/Runs/Deployments).
3. Engine FastAPI con endpoints mock funcionales.
4. Worker de runs (redis queue).
5. Wizard frontend (pasos 1-4).
6. Ejecucion/evaluacion/publicacion (pasos 5-7).
7. Observabilidad + auditoria.

## 11) Contratos API .NET -> Engine (ejemplos)

### `POST /engine/v1/dataset/build`
Request:
```json
{
  "projectId": "PRJ-001",
  "templateKey": "extractor-json",
  "sourcePaths": ["s3://aibase/docs/a.pdf"],
  "schema": {"type": "object"},
  "outputVersion": "v3"
}
```
Response:
```json
{
  "runId": "eng-run-123",
  "status": "queued"
}
```

### `POST /engine/v1/train/lora`
Request:
```json
{
  "projectId": "PRJ-001",
  "datasetVersion": "v3",
  "baseModel": "qwen2.5:7b",
  "params": {"epochs": 3, "lr": 0.0002}
}
```
Response:
```json
{
  "runId": "eng-run-124",
  "status": "running"
}
```

### `GET /engine/v1/runs/{id}`
Response:
```json
{
  "runId": "eng-run-124",
  "status": "running",
  "progressPct": 42,
  "step": "training",
  "metrics": {"loss": 1.42},
  "artifacts": []
}
```

Estados posibles:
```json
["queued", "running", "completed", "failed", "canceled"]
```

## 12) Docker compose (alto nivel)
Servicios:
- `aibase-api`
- `aibase-ui`
- `aibase-engine`
- `sqlserver`
- `redis`
- `qdrant`
- `minio`

Variables clave:
- API:
  - `ConnectionStrings__Default`
  - `ENGINE_BASE_URL`
  - `REDIS_URL`
  - `S3_ENDPOINT` / `S3_ACCESS_KEY` / `S3_SECRET_KEY`
- Engine:
  - `MODEL_PROVIDER` (`ollama`|`vllm`)
  - `OLLAMA_URL`
  - `QDRANT_URL`
  - `ARTIFACT_ROOT`

## 13) Estrategia de testing
- Backend:
  - Unit tests (services/validators).
  - Integration tests (SQL + controllers).
- Engine:
  - Contract tests por endpoint.
  - Smoke tests de pipeline.
- E2E:
  - Flujo wizard completo hasta deployment.
- Eval tests:
  - Conjunto fijo de regresion por template.

## 14) Recomendaciones de modelos
- Dev local (CPU/GPU chica):
  - `qwen2.5:3b` o `llama3.2:3b`.
- Entorno robusto:
  - `qwen2.5:7b` o `mistral:7b`.
- Embeddings:
  - `bge-small` (dev) / `bge-base` (prod).

GPU opcional:
- Sin GPU: solo RAG + inferencia liviana.
- Con GPU: habilitar entrenamiento LoRA real.

Cache de modelos/adapters:
- Warmup al iniciar engine.
- Cache LRU de adapters por proyecto.
- Evitar reload completo por request.

## 15) Riesgos y mitigaciones
- Costo/latencia de entrenamientos:
  - jobs async + cuotas por proyecto.
- Seguridad de datos:
  - cifrado en storage + auditoria + RBAC.
- Deriva de calidad:
  - eval suite fija + comparacion por version.
- Sesgo/datos sensibles:
  - filtros y redacción en ingestion.

## 16) Entregable inmediato sugerido (siguiente commit)
- SQL inicial `sb_ai` con tablas core.
- Endpoints API base:
  - `POST /api/v1/aibase/projects`
  - `GET /api/v1/aibase/projects`
  - `POST /api/v1/aibase/runs`
  - `GET /api/v1/aibase/runs/{id}`
- Engine stub:
  - `/dataset/build`, `/rag/index`, `/train/lora`, `/eval/run`, `/runs/{id}`
- Menú `AIBase` en UI con pantalla `Proyectos` + `Wizard` placeholder.
