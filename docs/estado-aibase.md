# Estado actual - AIBase (03/03/2026)

Branch: `aibase`

## Corte actual implementado
AIBase ya está operativo como sistema generado dentro de SystemBase, con su runtime separado en `systems/aibase`.

## Arquitectura vigente
- SystemBase sigue siendo la fábrica (metadata + generadores).
- AIBase vive como producto dentro de `systems/aibase`, desacoplado de `systems/mapeo`.
- Generación y gestión se hacen desde la UI de SystemBase (Diseñador / Datos / Backend / Frontend / Herramientas).

## Estructura de AIBase
- `systems/aibase/backend`: API .NET 8 generada para runtime.
- `systems/aibase/frontend`: frontend Vue 3 + Vuetify generado.
- `systems/aibase/sql`: scripts iniciales de apoyo para esquema `sb_ai`.
- `systems/aibase/engine`: placeholder para motor IA externo.
- `systems/aibase/docker`: placeholder de infraestructura.

## Datos y modelo
En el Diseñador del sistema `aibase` quedaron definidas y visibles:
- Entidades:
  - `Templates` (`sb_ai.Templates`)
  - `Projects` (`sb_ai.Projects`)
  - `Runs` (`sb_ai.Runs`)
- Relaciones:
  - `Projects.TemplateId -> Templates.Id` (`ManyToOne`)
  - `Runs.ProjectId -> Projects.Id` (`ManyToOne`)

## Backend runtime AIBase
- API generada con auth local (`/api/v1/auth/*`) y JWT.
- Endpoints CRUD activos:
  - `/api/v1/templates`
  - `/api/v1/projects`
  - `/api/v1/runs`
- Gestión de arranque desde SystemBase usa `systems/ports.json` para resolver puertos por sistema (incluye `aibase` en `5036`).

## Frontend runtime AIBase
- Sidebar con entidades runtime (`Templates`, `Proyectos`, `Runs`).
- Home reemplazado por dashboard genérico por entidad:
  - métricas globales de registros,
  - registros por entidad,
  - actividad reciente.
- Se eliminó el acople visual de Home con widgets específicos de mapeo (`incidentes`, `jobs`, `audio`).

## Variables y entorno
- Archivo de referencia: `systems/aibase/backend/.env.example`
- Puertos actuales:
  - backend: `http://localhost:5036`
  - frontend: `http://localhost:5177`

## Estado funcional
- Base metadata lista.
- Backend y frontend generados y ejecutables.
- CRUD básico operativo para las 3 entidades.
- Menú de AIBase integrado en el ecosistema SystemBase.

## Pendientes inmediatos (siguiente sprint)
1. Crear módulo funcional AIBase (pipeline real) encima de estas entidades base.
2. Definir estados y transiciones de `Runs` (cola, ejecución, error, reintento).
3. Agregar vistas específicas por dominio (más allá del CRUD genérico).
4. Integrar `engine` real y registrar trazas por corrida.
5. Endurecer validaciones de negocio (`TemplateId`, `ProjectId`, integridad de payloads JSON).
