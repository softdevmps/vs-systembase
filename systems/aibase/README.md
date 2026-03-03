# AIBase (runtime generado)

AIBase corre como sistema dentro del ecosistema SystemBase, desacoplado de `mapeo`, en `systems/aibase`.

## Estructura
- `systems/aibase/backend` -> API .NET 8 runtime.
- `systems/aibase/frontend` -> frontend runtime Vue 3 + Vuetify.
- `systems/aibase/sql` -> scripts auxiliares (`sb_ai`).
- `systems/aibase/engine` -> placeholder para motor IA.
- `systems/aibase/docker` -> placeholder de infraestructura.

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
