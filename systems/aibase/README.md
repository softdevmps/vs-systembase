# AIBase (runtime generado)

AIBase corre como sistema dentro del ecosistema SystemBase, desacoplado de `mapeo`, en `systems/aibase`.

## Estructura
- `systems/aibase/backend` -> API .NET 8 runtime.
- `systems/aibase/frontend` -> frontend runtime Vue 3 + Vuetify.
- `systems/aibase/sql` -> scripts auxiliares (`sb_ai`).
- `systems/aibase/engine` -> motor IA local (FastAPI) con inferencia/pipeline/export.
- `systems/aibase/docker` -> stack docker de AIBase.

## Modelo actual
- `sb_ai.Templates`
- `sb_ai.Projects` (FK: `TemplateId -> Templates.Id`)
- `sb_ai.Runs` (FK: `ProjectId -> Projects.Id`)

## Backend
### Endpoints
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/registrar`
- `GET|POST /api/v1/templates`
- `GET|PUT|DELETE /api/v1/templates/{id}`
- `GET|POST /api/v1/projects`
- `GET|PUT|DELETE /api/v1/projects/{id}`
- `GET|POST /api/v1/runs`
- `GET|PUT|DELETE /api/v1/runs/{id}`

### Configuración
```bash
cd systems/aibase/backend
cp .env.example .env
```

Variables mínimas:
- `DB_SERVER`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`
- `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRE_MINUTES`

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

## Notas
- El Home de runtime es genérico por entidad (sin widgets específicos de mapeo).
- El sistema se administra desde SystemBase (Diseñador y herramientas), pero se ejecuta de forma separada en `systems/aibase`.

## Playground con modelo real (chat tipo ChatGPT)
### Opción recomendada hoy: Ollama local
1. Crear/editar un `Template` en **Etapa 1 · Definir Template**.
2. En **Servicio de inferencia** elegir proveedor:
   - `Engine local (propio)`: usa `AIBASE_ENGINE_URL` (motor AIBase en `systems/aibase/engine`).
   - `Ollama (local)`: `Base URL` `http://localhost:11434`, `Path` `/api/chat`, `Modelo` `llama3.2:3b`.
   - `OpenAI compatible`: `Base URL` (por ejemplo API compatible), `Path` `/v1/chat/completions`, `Modelo` (por ejemplo `gpt-4o-mini`), `API Key` o `API Key Env`.
3. Guardar el template y asociarlo a un proyecto (Etapa 2).
4. Ir a **Etapa 8 · Playground** y enviar preguntas (chat 1 a 1).

El backend ahora intenta inferir en este orden:
1. `modelService` configurado en `Template.PipelineJson.meta`.
2. Mock (`AIBASE_ENGINE_MOCK=true`).
3. Engine externo (`AIBASE_ENGINE_URL`).

## Nota importante de red (Ollama)
- Si AIBase backend corre en host (`http://localhost:5036`), usar `http://localhost:11434`.
- Si el consumidor corre dentro de contenedor Docker, puede usar `http://host.docker.internal:11434`.
- El backend ahora incluye fallback automático de host para Ollama (`host.docker.internal` <-> `localhost`/`127.0.0.1`).

## Nota importante de warm-up (Ollama)
- En la primera llamada puede devolverse `done_reason: "load"` con respuesta vacía.
- El backend ahora reintenta automáticamente una vez para obtener respuesta útil en ese primer turno.

## Motor propio en Docker (recomendado)
```bash
cd systems/aibase/engine
cp .env.example .env

cd ../docker
docker compose up -d --build
```

Luego, en `systems/aibase/backend/.env`:
- `AIBASE_ENGINE_URL=http://localhost:8010`
- `AIBASE_ENGINE_MOCK=false`
