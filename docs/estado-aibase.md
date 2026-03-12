# Estado actual - AIBase (12/03/2026)

Branch de trabajo: `aibase`

## Resumen ejecutivo
AIBase esta operativo como fabrica end-to-end dentro de SystemBase:
- definicion de template/proyecto,
- dataset (carga, generacion por topicos y merge),
- RAG index,
- train/eval/deploy,
- playground para chat y vision facial.

Configuracion mas estable para chat general hoy:
- `provider=ollama`
- `model=llama3.2:3b`

## Arquitectura vigente
- `systems/aibase/backend`: API .NET 8 (workflow, inferencia, docker control).
- `systems/aibase/frontend`: Vue 3 + Vuetify (flujo por etapas).
- `systems/aibase/engine`: FastAPI (motor local y pipelines).
- `systems/aibase/docker`: stack local para engine.

## Estado funcional por etapa
1. Template: ok (crear/editar/eliminar, contrato, modelService).
2. Project: ok (crear/editar/eliminar, seleccion de template).
3. Dataset: ok (upload + data ops + generador + merge).
4. RAG: ok (index y estado por proyecto).
5. Train: ok (train LoRA con artifact metadata local).
6. Eval: ok (ejecucion y metricas).
7. Deploy: ok (gate + control Docker desde UI).
8. Playground: ok (chat, OCR/vision y facial).

## Cambios consolidados en este corte
### Inferencia y robustez (backend)
- Fallback de endpoints Ollama (`localhost`, `127.0.0.1`, `host.docker.internal`).
- Auto-pull de modelo cuando Ollama responde "model not found".
- Reintento de warm-up cuando `done_reason=load`.
- Degradacion automatica en vision cuando hay OOM/runner kill.
- Fallback adicional `/api/generate` para modelos que no devuelven contenido en `/api/chat`.

### Modo facial
- Deteccion de templates faciales corregida en backend.
- Normalizacion de salida facial cuando el modelo devuelve texto libre (ej: `ids = [...]`).
- Salida estructurada estable:
  - `faces[]`
  - `summary`
  - `model`
  - `source`
  - `raw`

### UI / Playground
- Etiquetas y hints especificos para modo facial.
- Casos demo de vision/facial.
- Indicador visible en resultado:
  - `ROSTROS DETECTADOS: N`

## Configuracion recomendada de uso
### Chat 1 a 1
- Provider: `Ollama (local)`
- Base URL: `http://localhost:11434`
- Path: `/api/chat`
- Modelo: `llama3.2:3b`

### Facial/vision en hardware limitado
- Provider: `Ollama (local)`
- Base URL: `http://localhost:11434`
- Path: `/api/chat`
- Modelo: `moondream`

## Limitaciones conocidas
- En maquinas con poca RAM/VRAM, `llava` puede fallar por OOM.
- Algunos modelos vision pueden devolver salida no estructurada o vacia; AIBase la normaliza/fallback, pero no siempre ofrece `bbox` real.
- Para deteccion facial estricta (bbox confiable) se recomienda integrar detector dedicado (OpenCV/MediaPipe) como backend especializado.

## Verificaciones realizadas
- `dotnet build systems/aibase/backend/Aibase.Backend.csproj` OK.
- `npm run build` en `systems/aibase/frontend` OK.
- Pruebas reales en Playground:
  - chat con ollama,
  - facial con salida normalizada y contador de rostros.

## Pendiente inmediato sugerido
1. Agregar detector facial dedicado (no LLM) para `bbox` real y confianza consistente.
2. Exponer tabla visual de `faces` (id, score, bbox) en Playground.
3. Afinar contratos de salida por tipo de template (chat/extractor/facial/ocr).
4. Agregar smoke tests automaticos por etapa para no depender de prueba manual.
