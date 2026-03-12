# AIBase en SystemBase

AIBase es un sistema que habita dentro del ecosistema SystemBase y funciona como fabrica de asistentes/modelos de IA con flujo completo desde UI:

1. Template
2. Project
3. Dataset
4. RAG
5. Train
6. Eval
7. Deploy
8. Playground

## Estructura
- `systems/aibase/backend`: API .NET 8 (workflow, inferencia, Docker control).
- `systems/aibase/frontend`: UI Vue 3 + Vuetify.
- `systems/aibase/engine`: motor local FastAPI.
- `systems/aibase/docker`: stack local del engine.
- `systems/aibase/sql`: scripts auxiliares de base.

## Pre-requisitos
- .NET 8 SDK
- Node 20+
- Python 3.11+
- Docker Desktop
- Ollama (recomendado para chat y vision local)

## Levantar AIBase en local
### 1) Backend
```bash
cd systems/aibase/backend
cp .env.example .env
dotnet restore
dotnet run
```
Backend: `http://localhost:5036`

### 2) Frontend
```bash
cd systems/aibase/frontend
npm install
npm run dev
```
Frontend: `http://localhost:5177`

### 3) Engine local (opcional, provider `engine`)
```bash
cd systems/aibase/engine
cp .env.example .env

cd ../docker
docker compose up -d --build
```
Engine: `http://localhost:8010`

## Proveedores de inferencia soportados
En Template -> Servicio de inferencia:

- `engine` (motor propio AIBase via `AIBASE_ENGINE_URL`)
- `ollama` (local)
- `openai compatible`

## Configuracion recomendada hoy
### Chat 1 a 1 estable
- Provider: `Ollama (local)`
- Base URL: `http://localhost:11434`
- Path API: `/api/chat`
- Model: `llama3.2:3b`

### Vision / reconocimiento facial en local
- Provider: `Ollama (local)`
- Base URL: `http://localhost:11434`
- Path API: `/api/chat`
- Model sugerido: `moondream` (mas liviano que llava)

Notas:
- Si `llava` falla por memoria, AIBase intenta degradar automaticamente a modelos de vision mas livianos.
- Si la salida de vision viene no estructurada, AIBase la normaliza para devolver estructura util en Playground.

## Flujo operativo recomendado (end-to-end)
1. Crear o editar Template.
2. Crear Project asociado al Template.
3. Cargar/generar Dataset en Data Ops.
4. Construir RAG Index (si el template usa RAG).
5. Ejecutar Train/Eval si aplica al caso.
6. Ir a Deploy y desplegar servicio.
7. Probar en Playground.

## Playground
### Chat
- Enviar preguntas directamente.
- Revisar panel Resultado: provider, modelo, endpoint, fallback, latencia, quality score.

### Facial
- Cargar imagen.
- Enviar instruccion (por ejemplo: `detecta rostros`).
- El panel Resultado muestra JSON y resumen.
- La UI muestra contador visible: `ROSTROS DETECTADOS: N`.

## Comportamientos implementados relevantes
- Fallback de host Ollama (`localhost`, `127.0.0.1`, `host.docker.internal`).
- Auto pull de modelo Ollama cuando falta.
- Reintento de warm-up cuando Ollama responde con `done_reason=load`.
- Manejo de OOM/runner caido con degradacion de modelo en vision.
- Normalizacion de salida facial cuando el modelo devuelve texto no estructurado.

## Troubleshooting rapido
### Error: `host.docker.internal ... not known`
Si backend corre en host, usar `http://localhost:11434` en el template.

### Error: `model ... not found`
Descargar modelo:
```bash
ollama pull llama3.2:3b
ollama pull moondream
```

### Error: memoria insuficiente / runner killed
Reducir carga de Ollama:
```bash
ollama stop llama3.2:3b
ollama stop llava
ollama stop moondream
```
Luego levantar con menos concurrencia:
```bash
OLLAMA_MAX_LOADED_MODELS=1 OLLAMA_NUM_PARALLEL=1 OLLAMA_KEEP_ALIVE=0 ollama serve
```

### El Playground no refleja cambios del backend
Reiniciar backend .NET:
```bash
cd systems/aibase/backend
pkill -f Aibase.Backend || true
dotnet run
```

## Base de datos de AIBase
Modelo principal en schema `sb_ai`:
- `Templates`
- `Projects`
- `Runs`

## Documentacion complementaria
- Estado general del repo: `docs/estado-actual.md`
- Estado especifico AIBase: `docs/estado-aibase.md`
- Stack docker engine: `systems/aibase/docker/README.md`
