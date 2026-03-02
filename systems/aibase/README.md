# AIBase (bootstrap)

Base de trabajo inicial para el nuevo producto AIBase, construido desde la fabrica SystemBase.

## Estado actual
- Branch de trabajo: `aibase`
- Documentación técnica inicial:
  - `docs/arquitectura-aibase-v1.md`
  - `docs/estado-aibase.md` (brief funcional)
- Scaffold creado:
  - `systems/aibase/backend/`
  - `systems/aibase/frontend/`
  - `systems/aibase/engine/`
  - `systems/aibase/sql/`
  - `systems/aibase/docker/`

## Objetivo del scaffold
Preparar la estructura para implementar V1:
- Template `extractor-json`
- Template `chat-rag`
- Orquestación por jobs
- Versionado dataset/model/eval/deployment

## Próximos pasos inmediatos
1. Definir tablas SQL `sb_ai.*`.
2. Crear backend API base de AIBase en `.NET 8`.
3. Crear `aibase-engine` (FastAPI) con endpoints stub.
4. Crear vistas iniciales de AIBase en frontend (lista proyectos + wizard base).
