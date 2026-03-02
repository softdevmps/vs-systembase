# AIBase (bootstrap)

Base de trabajo inicial para el nuevo producto AIBase, construido desde la fabrica SystemBase.

## Estado actual
- Branch de trabajo: `aibase`
- Documentación técnica inicial:
  - `docs/estado-aibase.md`
- Scaffold creado:
  - `systems/aibase/backend/`
  - `systems/aibase/frontend/`
  - `systems/aibase/engine/`
  - `systems/aibase/sql/`
  - `systems/aibase/docker/`
- SQL inicial:
  - `systems/aibase/sql/001_sb_ai_init.sql`
  - `systems/aibase/sql/002_sb_ai_runs.sql`
- Bootstrap funcional en SystemBase:
  - Menú `/aibase`
  - Endpoints de proyectos/templates/runs en `/api/v1/aibase/*`
  - Orquestación inicial de runs (HTTP hacia engine + modo stub)

## Variables backend recomendadas
- `AIBASE_ENGINE_ENABLED=false`
- `AIBASE_ENGINE_BASE_URL=http://localhost:8090/engine/v1`
- `AIBASE_ENGINE_TIMEOUT_SECONDS=30`

## Objetivo del scaffold
Preparar la estructura para implementar V1:
- Template `extractor-json`
- Template `chat-rag`
- Orquestación por jobs
- Versionado dataset/model/eval/deployment

## Próximos pasos inmediatos
1. Extender esquema `sb_ai` (runs, versiones, deployments, audit).
2. Crear orquestador de runs contra `aibase-engine`.
3. Crear `aibase-engine` (FastAPI) con endpoints stub.
4. Implementar wizard paso 1-3 en frontend.
