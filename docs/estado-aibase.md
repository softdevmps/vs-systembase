# Estado actual - AIBase (06/03/2026)

Branch: `aibase`

## Corte actual implementado
AIBase ya quedó funcionando como sistema independiente dentro del ecosistema SystemBase, con backend/frontend propios en `systems/aibase` y flujo guiado por etapas desde UI.

## Arquitectura vigente
- SystemBase se mantiene como fábrica metadata-driven.
- AIBase vive desacoplado en `systems/aibase` (igual patrón que `mapeo`).
- Alta de entidades, relaciones, generación y gestión de runtime se hace desde la UI de SystemBase.
- Arranque/parada de servicios usa `systems/ports.json` para resolver puertos del sistema (`aibase`: backend `5036`, frontend `5177`).

## Estructura de AIBase
- `systems/aibase/backend`: API .NET 8 runtime AIBase.
- `systems/aibase/frontend`: frontend Vue 3 + Vuetify runtime AIBase.
- `systems/aibase/sql`: scripts de esquema y soporte.
- `systems/aibase/engine`: integración prevista de motor IA.
- `systems/aibase/docker`: compose/infra de despliegue.

## Modelo de datos (System Designer)
Definido en SystemBase y visible en pestaña Datos:
- `Templates` (`sb_ai.Templates`)
- `Projects` (`sb_ai.Projects`)
- `Runs` (`sb_ai.Runs`)

Relaciones:
- `Projects.TemplateId -> Templates.Id` (`ManyToOne`)
- `Runs.ProjectId -> Projects.Id` (`ManyToOne`)

## Backend AIBase (runtime)
Además del CRUD base, se agregó capa de orquestación de runs, inferencia, asistente y operación Docker:
- `GET /api/v1/aibase/overview`
- `GET /api/v1/aibase/projects/{projectId}/runs`
- `GET /api/v1/aibase/projects/{projectId}/workflow`
- `POST /api/v1/aibase/projects/{projectId}/run`
- `POST /api/v1/aibase/projects/{projectId}/infer`
- `POST /api/v1/aibase/assistant/suggest`
- `GET /api/v1/aibase/docker/status`
- `GET /api/v1/aibase/docker/services`
- `POST /api/v1/aibase/docker/up`
- `POST /api/v1/aibase/docker/down`
- `POST /api/v1/aibase/docker/restart`
- `GET /api/v1/aibase/docker/logs`
- `POST /api/v1/aibase/docker/services/{service}/action`

Tipos de run soportados en esta fase:
- `dataset_build`
- `rag_index`
- `train_lora`
- `eval_run`
- `deploy_service`

Comportamiento:
- Crea run en estado `running`.
- Ejecuta en modo mock o contra engine externo.
- Cierra run en `completed` o `failed` con trazas de error.
- Actualiza `Projects.Status` automáticamente según etapa completada:
  - `dataset_build` -> `data_ready`
  - `rag_index` -> `index_ready`
  - `train_lora` -> `trained`
  - `eval_run` -> `evaluated`
  - `deploy_service` -> `deployed`
- Si una etapa falla, marca proyecto en `error`.
- Valida secuencia de ejecución por proyecto/template (bloquea etapas fuera de orden).
- Asistente IA por etapa:
  - interpreta prompt libre del usuario,
  - detecta tipo de modelo (chatbot RAG, OCR, transcripción, geolocalización, facial, etc.),
  - devuelve sugerencias aplicables para `Template`, `Project` y `Dataset`.
- Control Docker seguro en backend:
  - validación de `stack/service`,
  - ejecución de `docker compose` con timeout,
  - salida normalizada de estado, servicios, `ps` y logs.

## Frontend AIBase (runtime)
Se consolidó un flujo de 8 etapas con vistas separadas:
- `Home.vue` como tablero de workflow (cards clickeables por etapa, estado de proyecto y runs recientes).
- Etapas dedicadas:
  1. `StageTemplate` (contrato del modelo)
  2. `StageProject` (alta/config de proyecto)
  3. `StageDataset` (build + data quality/split)
  4. `StageRag` (indexación y parámetros)
  5. `StageTrain` (entrenamiento LoRA)
  6. `StageEval` (suite/métricas/thresholds)
  7. `StageDeploy` (publicación del servicio)
  8. `StagePlayground` (prueba interactiva del modelo)
- La navegación `pipeline`/`playground` legacy redirige a las etapas nuevas.
- El estado de cada etapa se gobierna desde backend (`workflow`) con bloqueos por dependencias.
- Se agregó ayuda contextual por etapa (`Qué significa cada opción`) para explicar campos y decisiones.
- Se agregó asistente integrado de etapa (`StageAssistant`) en:
  - `StageTemplate`,
  - `StageProject`,
  - `StageDataset`.
- `StageTemplate` ahora define explícitamente `Perfil Playground` y lo guarda en `PipelineJson.meta.playgroundProfile`.
- Playground multimodal:
  - modo `texto`, `audio` e `imagen`,
  - carga de archivo con preview,
  - inyección opcional en `contextJson`,
  - validación rápida (`debe contener`),
  - historial de sesión de pruebas,
  - modo chat “input → respuesta” cuando el perfil del template es conversacional.
- `StageDeploy` incorpora panel `Control Docker` desde UI:
  - acciones de stack (`up/down/restart`),
  - acciones por servicio (`start/stop/restart`),
  - lectura de logs por tail y servicio,
  - estado en vivo de contenedores (`running/status`).

## Configuración y entorno
Archivo de referencia:
- `systems/aibase/backend/.env.example`

Variables relevantes agregadas:
- `AIBASE_ENGINE_URL`
- `AIBASE_ENGINE_MOCK`
- `AIBASE_ENGINE_TIMEOUT_SECONDS`
- `AIBASE_DEPLOY_ENDPOINT`
- `AIBASE_DEPLOY_HEALTH`
- `AIBASE_DOCKER_COMPOSE_FILE`
- `AIBASE_DOCKER_PROJECT`
- `AIBASE_DOCKER_COMMAND_TIMEOUT_SECONDS`

## Estado funcional verificado
- `dotnet build` OK en `systems/aibase/backend`.
- `npm run build` OK en `systems/aibase/frontend`.
- CRUD + orquestación de runs + inferencia mock disponibles.
- Workflow guiado por etapas activo en backend + frontend.
- Deploy stage muestra endpoint/health y salida de ejecución.
- Playground permite smoke test funcional antes de cerrar demo.
- Deploy stage permite además administrar stack Docker y logs desde UI.
- Asistente por etapa disponible para completar formularios con sugerencias.
- Login AIBase con branding propio y soporte de tema oscuro basado en variables de tema.
- Sistema integrado en menú y operación desde SystemBase.

## Pendiente inmediato (siguiente bloque)
1. Integrar engine real (reemplazar mock por ejecución efectiva end-to-end).
2. Playground 100% orientado por capacidades declaradas del template (UX dinámica por tipo sin configuración manual).
3. Agregar capa de "problem definition":
   - `SchemaVersion`,
   - `Taxonomy/Ontology`,
   - reglas de validación/anotación.
4. Completar DataOps gobernado:
   - import/mapping,
   - limpieza y dedupe,
   - split reproducible,
   - quality report y lineage.
5. Incorporar Model Registry + métricas de despliegue + feedback loop.
6. Mejorar UX guiada por etapas para demo completa (definir -> dataset -> train -> evaluar -> deploy -> probar).
