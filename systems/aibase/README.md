# AIBase (independiente)

AIBase ahora vive como sistema separado dentro de `systems/aibase`, sin acoplar código al backend/frontend raíz de SystemBase.

## Estructura
- `systems/aibase/backend` -> API .NET 8 independiente (`AIBase.Backend.csproj`)
- `systems/aibase/frontend` -> UI Vue/Vuetify independiente
- `systems/aibase/sql` -> scripts de esquema `sb_ai`
- `systems/aibase/engine` -> placeholder para engine Python/FastAPI
- `systems/aibase/docker` -> placeholder para compose/infra propia

## Backend
### Endpoints
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/registrar`
- `GET /api/v1/aibase/templates`
- `GET /api/v1/aibase/projects`
- `GET /api/v1/aibase/projects/{id}`
- `POST /api/v1/aibase/projects`
- `GET /api/v1/aibase/projects/{projectId}/runs`
- `POST /api/v1/aibase/projects/{projectId}/runs`
- `GET /api/v1/aibase/runs/{id}`
- `POST /api/v1/aibase/runs/{id}/sync`

### Configuración
Copiar:
```bash
cd systems/aibase/backend
cp .env.example .env
```
Variables clave:
- `DB_SERVER`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`
- `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRE_MINUTES`
- `AIBASE_ENGINE_ENABLED`, `AIBASE_ENGINE_BASE_URL`, `AIBASE_ENGINE_TIMEOUT_SECONDS`

### Run local
```bash
cd systems/aibase/backend
dotnet restore
dotnet watch run
```
Backend default: `http://localhost:5036`

## Frontend
```bash
cd systems/aibase/frontend
npm install
npm run dev
```
Frontend default: `http://localhost:5177`

## SQL
- `systems/aibase/sql/001_sb_ai_init.sql`
- `systems/aibase/sql/002_sb_ai_runs.sql`

El backend también crea/valida estas tablas automáticamente al iniciar (`AibaseSchemaMigrator`).
