# SystemBase Monorepo

Monorepo con dos líneas principales:

- **SystemBase (fábrica)**: plataforma para diseñar, publicar y operar sistemas CRUD.
- **Mapeo (producto)**: runtime de incidentes con audio, transcripción, extracción y geocodificación.

## Componentes

- `backend/`: API principal de SystemBase (.NET 8).
- `frontend/`: UI principal de SystemBase (Vue 3 + Vuetify).
- `frontend-runtime/`: plantilla runtime base que usa el generador.
- `systems/mapeo/backend/`: backend del sistema de mapeo.
- `systems/mapeo/frontend/`: frontend del sistema de mapeo.
- `whisper-service/`: docker compose de Whisper ASR.
- `llm-service/`: docker compose de Ollama.
- `geocoder-service/`: docker compose de Nominatim (OSM local).
- `systems/ports.json`: asignación de puertos por sistema generado.
- `docs/`: documentación funcional.

## Requisitos

- .NET SDK 8
- Node.js 18+
- SQL Server
- Docker + Docker Compose
- `ffmpeg` (recomendado para pipeline de audio en mapeo)

## Variables de entorno

Crear/copiar:

- `backend/.env` desde `backend/.env.example`
- `systems/mapeo/backend/.env` desde `systems/mapeo/backend/.env.example`
- `geocoder-service/.env` desde `geocoder-service/.env.example`

## Puertos por defecto

- SystemBase backend: `http://localhost:5032`
- SystemBase frontend: `http://localhost:5173`
- Mapeo backend: `http://localhost:5035`
- Mapeo frontend: `http://localhost:5176` (levantar con `--port 5176`)
- Whisper: `http://localhost:9000`
- Ollama: `http://localhost:11434`
- Nominatim: `http://localhost:8080`

## Levantar SystemBase (fábrica)

### Backend

```bash
cd backend
dotnet restore
dotnet watch run
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## Levantar Mapeo (producto)

### 1) Servicios Docker

#### Whisper (ASR)

```bash
cd whisper-service
docker compose up -d
```

Endpoint esperado por mapeo: `http://localhost:9000/asr`

#### Ollama (LLM local)

```bash
cd llm-service
cp .env.example .env
docker compose up -d
```

Endpoint: `http://localhost:11434`

Por defecto se recomienda `qwen2.5:7b` (configurable con `OLLAMA_MODEL`).

Primer setup del modelo:

```bash
cd llm-service
docker compose exec ollama ollama pull qwen2.5:7b
docker compose exec ollama ollama list
```

#### Nominatim (geocoder)

```bash
cd geocoder-service
cp .env.example .env
```

En `geocoder-service/.env`, configurar **una** opción:

- `PBF_URL=...` (descarga automática)
- `PBF_PATH=/nominatim-import/cordoba.pbf` (archivo local montado en `./import`)

Luego:

```bash
docker compose up -d
docker logs -f geocoder-nominatim
```

El primer import puede demorar varios minutos.

### 2) Backend mapeo

```bash
cd systems/mapeo/backend
dotnet restore
dotnet watch run
```

### 3) Frontend mapeo

```bash
cd systems/mapeo/frontend
npm install
npm run dev -- --port 5176
```

## Ejecución simultánea recomendada (4 terminales + docker)

1. `backend` -> `dotnet watch run`
2. `frontend` -> `npm run dev`
3. `systems/mapeo/backend` -> `dotnet watch run`
4. `systems/mapeo/frontend` -> `npm run dev -- --port 5176`
5. Docker: `whisper-service`, `llm-service`, `geocoder-service`

## Flujos funcionales

### SystemBase

- Crear sistema
- Diseñar entidades/campos/relaciones
- Publicar base de datos
- Generar backend/frontend runtime
- Iniciar/Detener procesos desde herramientas del runtime

### Mapeo

- Grabar o subir audio
- Pipeline async: transcripción -> extracción -> geocodificación
- Visualización de incidentes en tabla + mapa
- Retry manual de jobs fallidos

## Troubleshooting

### Error: no se encontró proyecto MSBuild (`.csproj`)

Si falla al iniciar un backend generado, falta el proyecto runtime:

1. En SystemBase, ejecutar **Generar Backend** del sistema.
2. Reintentar iniciar desde herramientas.

### Geocodificación falla con `Connection refused (localhost:8080)`

- Nominatim no está levantado o no terminó el import inicial.
- Revisar: `docker logs -f geocoder-nominatim`

### Pipeline de audio falla por `ffmpeg`

Instalar `ffmpeg` en el host y verificar que esté disponible en `PATH`.

### Mapeo sin coordenadas

- Verificar servicios externos (Whisper/Ollama/Nominatim).
- Revisar `LugarTexto` y `LugarNormalizado`.
- Reintentar job manualmente.

### Evaluar calidad (dataset)

Con backend de mapeo levantado:

```bash
cd systems/mapeo/backend/eval
bash run-eval.sh
```

Esto ejecuta `POST /api/v1/dev/eval/batch` con `dataset.sample.json` y devuelve métricas de extracción/geocoding.

### Evaluar calidad (dataset automático)

Con backend de mapeo levantado y datos procesados en BD:

```bash
cd systems/mapeo/backend/eval
bash run-eval-auto.sh
```

El script:
- arma el dataset desde incidentes reales (`GET /api/v1/dev/eval/dataset/auto`)
- evalúa en batch contra el pipeline actual (`POST /api/v1/dev/eval/batch`)

Parámetros opcionales:
- `TAKE=300` cantidad de casos a tomar
- `DISTANCE_TOLERANCE=250` tolerancia de coordenadas en metros
- `INCLUDE_WITHOUT_COORDS=true` incluye casos sin lat/lng esperados

### Base SQL para aprendizaje de ubicaciones

Se incorporó una base SQL para desacoplar correcciones de ubicación del código:

- `sys_mapeo.LocationNormalizationRules`
- `sys_mapeo.LocationNormalizationFeedback`

El normalizador de ubicación consume reglas activas de `LocationNormalizationRules`
(`Scope='location'`) sin reiniciar el backend (cache corta).

Estas tablas se aseguran automáticamente al iniciar `systems/mapeo/backend` (vía `DbSchemaMigrator`).

Script manual (idempotente):

```bash
systems/mapeo/backend/sql/001_location_learning.sql
```

## Estrategia de ramas

- `main`: fábrica SystemBase (base del generador).
- `mapeo`: sistema mapeo + ajustes específicos.

Recomendado:

1. Cambios de fábrica en `main`.
2. Propagar a `mapeo` con `merge` o `cherry-pick`.

## Documentación adicional

- `docs/estado-actual.md`
- `docs/estado-mapeo.md`
- `systems/ports.json`
