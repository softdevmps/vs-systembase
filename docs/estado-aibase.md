# Estado actual - AIBase (11/03/2026)

Branch de trabajo: `aibase`

## Resumen ejecutivo
AIBase está operativo como fábrica end-to-end desde UI:
- definición de template/proyecto,
- dataset (carga/generación/merge),
- RAG index,
- train/eval/deploy,
- playground conversacional.

Hoy la configuración más estable para chat general es `provider=ollama` con `llama3.2:3b`.

## Arquitectura vigente
- `systems/aibase/backend`: API .NET 8 (orquestación, workflow, inferencia, Docker control).
- `systems/aibase/frontend`: Vue 3 + Vuetify (flujo por etapas).
- `systems/aibase/engine`: FastAPI para motor local (HF/rules/ollama bridge).
- `systems/aibase/docker`: compose para stack local.

## Capacidades implementadas
### Flujo por etapas
1. Template
2. Project
3. Dataset
4. RAG
5. Train
6. Eval
7. Deploy
8. Playground

Cada etapa usa gate de prerequisitos y estado de workflow.

### Template / Model Service
- Crear, editar y eliminar templates desde UI.
- Contrato del modelo (schema/taxonomía/reglas).
- Perfil de playground por template.
- `modelService` configurable por UI:
  - `engine`,
  - `ollama`,
  - `openai compatible`.
- Parámetros de inferencia:
  - task, temperature, topP, repetition penalty, maxTokens.
- Opciones avanzadas:
  - quality gate,
  - auto-learning,
  - modo razonamiento (passes/verifier/score mínimo/plan steps).

### Dataset y Data Ops
- Upload CSV/JSON/JSONL.
- Mapeo al contrato y preview.
- Generador de dataset por tópicos (Wikipedia) integrado en UI.
- Progreso visual durante generación.
- Listado de datasets generados.
- Selección de fuente activa de dataset.
- Merge de datasets (unificación).

### RAG / Train / Eval / Deploy
- Build dataset y guardado de versión.
- Construcción de índice RAG por proyecto.
- Train LoRA (artifact local metadata).
- Eval y métricas por corrida.
- Deploy con configuración de endpoint/health.
- Control Docker desde UI:
  - stack up/down/restart,
  - acciones por servicio,
  - lectura de logs.

### Playground
- Chat 1 a 1 por template/proyecto.
- Input texto y contexto.
- Panel de resultado con provider/model/endpoint/latencia/fallback/score.
- Métricas de inferencia y estado de deploy.

## Integración de inferencia (estado de hoy)
### Backend (.NET) `ModelServiceClient`
- Soporte operativo para `provider=ollama`.
- Soporte de `path`:
  - `/api/chat`,
  - `/api/generate`.
- Fallback automático de host para Ollama:
  - `host.docker.internal`,
  - `localhost`,
  - `127.0.0.1`.
- Manejo de warm-up de Ollama (`done_reason=load`): reintento automático.

### Engine (FastAPI)
- Motor HF/rules activo.
- Mejoras de razonamiento y auto-learning incorporadas.
- Soporte `provider=ollama` en runtime del engine.

## Configuración recomendada para uso diario
En Template -> Servicio de inferencia:
- Provider: `Ollama (local)`
- Base URL: `http://localhost:11434`
- Path: `/api/chat`
- Model: `llama3.2:3b`

Notas:
- Si el consumidor corre en contenedor, puede requerir `host.docker.internal`.
- Para modelos HF grandes (ej. 7B) en CPU/memoria limitada hay latencia alta o fallback.

## Verificaciones realizadas
- `dotnet build` OK en `systems/aibase/backend/Aibase.Backend.csproj`.
- `python -m py_compile` OK en engine app.
- Inferencia validada con salida real por Ollama (`fallback=false` en pruebas exitosas).

## Pendiente inmediato
1. Afinar calidad conversacional (prompting + evaluación continua por dominio).
2. Política de selección RAG vs memoria conversacional por intent/score.
3. Perfilar latencia y throughput por modelo Ollama (3B/7B) en hardware objetivo.
4. Consolidar documentación de operación para demo y producción.
