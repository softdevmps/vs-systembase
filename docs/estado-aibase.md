# Estado actual - AIBase (02/03/2026)

Branch: `aibase`

## Corte actual implementado
AIBase quedó separado del código raíz de SystemBase y vive como producto independiente dentro de `systems/aibase`.

### Estructura
- `systems/aibase/backend` (API .NET 8)
- `systems/aibase/frontend` (UI Vue 3 + Vuetify)
- `systems/aibase/sql` (schema `sb_ai`)
- `systems/aibase/engine` (placeholder FastAPI)
- `systems/aibase/docker` (placeholder infra)

### Backend AIBase
- Proyecto independiente: `systems/aibase/backend/AIBase.Backend.csproj`
- Auth propio (`/api/v1/auth/*`) reutilizando tabla `dbo.Usuarios`.
- Migrador idempotente al iniciar backend:
  - `sb_ai.Templates`
  - `sb_ai.Projects`
  - `sb_ai.Runs`
- Seed de templates iniciales:
  - `extractor-json`
  - `chat-rag`
- Endpoints implementados:
  - `GET /api/v1/aibase/templates`
  - `GET /api/v1/aibase/projects`
  - `GET /api/v1/aibase/projects/{id}`
  - `POST /api/v1/aibase/projects`
  - `GET /api/v1/aibase/projects/{projectId}/runs`
  - `POST /api/v1/aibase/projects/{projectId}/runs`
  - `GET /api/v1/aibase/runs/{id}`
  - `POST /api/v1/aibase/runs/{id}/sync`
- Orquestación inicial de runs:
  - `AibaseEngineClient` contra engine HTTP
  - modo `stub` si `AIBASE_ENGINE_ENABLED=false`

### Frontend AIBase
- App independiente con login + layout + módulo AIBase.
- Ruta principal funcional:
  - `/aibase`
- Funcionalidades UI:
  - listado de templates
  - alta de proyectos
  - ejecución de runs (`dataset_build`, `rag_index`, `train_lora`, `eval_run`)
  - listado y sincronización de runs

## Variables clave (backend)
- `DB_SERVER`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`
- `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRE_MINUTES`
- `AIBASE_ENGINE_ENABLED`
- `AIBASE_ENGINE_BASE_URL`
- `AIBASE_ENGINE_TIMEOUT_SECONDS`

Archivo de referencia: `systems/aibase/backend/.env.example`

## SQL
- `systems/aibase/sql/001_sb_ai_init.sql`
- `systems/aibase/sql/002_sb_ai_runs.sql`

## Puertos sugeridos
- AIBase backend: `http://localhost:5036`
- AIBase frontend: `http://localhost:5177`

## Estado arquitectónico
- SystemBase (raíz) queda como fábrica.
- AIBase corre separado, dentro del monorepo, al mismo nivel lógico que otros productos en `systems/`.

## Próximos pasos recomendados
1. Levantar `systems/aibase/engine` (FastAPI) con endpoints reales de ejecución.
2. Persistir historial de eventos de run (logs por step/progreso).
3. Agregar versiones de dataset/model/evaluación/despliegue (`sb_ai.DatasetVersions`, `sb_ai.ModelVersions`, etc.).
4. Crear wizard de proyecto AIBase (pasos 1-4) dentro del frontend independiente.
5. Definir docker-compose específico de AIBase (api + engine + redis opcional).
