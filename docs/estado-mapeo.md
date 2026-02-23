# Estado actual - Sistema Mapeo (22/02/2026)

## Alcance actual
- Backend y frontend completos para el sistema `mapeo` (runtime).
- Pipeline de audio asíncrono con cola (`IncidenteJobs`) y extracción automática.
- Seed automático del catálogo de hechos.
- Storage local listo para migrar a nube.
- Retención automática de audios por políticas.
- Transcodificación antes de transcribir (FFmpeg).
- Reintento manual de jobs fallidos.
- Swagger habilita upload multipart (filtro de file upload).
- UI runtime incluye grabación y envío de audio desde navegador (vista Incidentes).
- Extracción LLM on‑prem (Ollama) + normalización de ubicación.

## Pipeline de audio
1. `POST /api/v1/incidentes/audio` (multipart `audio`) crea `Incidente`, `IncidenteAudio` y `IncidenteJobs`.
2. Worker (`IncidenteJobWorker`) procesa la cola:
   - Transcribe (Whisper local si existe, stub si no).
   - Extrae hechos/fecha/lugar/categoría (reglas + LLM on‑prem).
   - Limpia ruido de ubicación (fecha/hora/lugar).
   - Geocodifica con Nominatim local (prioriza número de puerta).
   - Guarda extracción + ubicación.
3. Reintento manual:
   - `POST /api/v1/incidente-jobs/{id}/retry`
4. Timeouts:
   - `AUDIO_JOB_TIMEOUT_SECONDS` corta transcripcion/geocoding si se cuelga.
   - Jobs en `processing` con `UpdateAt` viejo se pasan a `retry`.
   - Fallas de geocodificacion no bloquean el job (se continua sin coordenadas).

## Ubicación y geocodificación
- **Normalización fonética** (ej: `velez arfiel` → `velez sarsfield`).
- **Detección de intersecciones y POIs** (plaza, parque, banco, etc.).
- **Barrio**: no se usa como calle para evitar falsos cruces.
- **LLM Location Parts** (calle/numero/interseccion/barrio/ciudad/poi) como fallback.
- **Nominatim mejorado**:
  - `addressdetails=1` y ranking por `house_number` cuando existe.
  - Búsqueda estructurada `street=numero + calle` + `city=Cordoba` si hay número.
  - Fallback por calle sin número cuando no hay match exacto de altura.
  - Fallback por intersección aproximada (punto medio entre trazas de ambas calles).
  - `GEOCODER_DEFAULT_SUFFIX` agregado automáticamente.

## Estado de calidad (último corte)
- Corrida de control reciente (`TAKE=7`, `includeWithoutCoords=true`):
  - `type`: 100%
  - `location`: 85.71%
  - `date`: 100%
  - `hour`: 85.71%
  - `coords`: 100% en casos con coordenadas esperadas
  - `avgDurationMs`: ~7.1s
- Mejora aplicada para casos recientes:
  - normalización adicional de variantes (`alverdi`, `fuerza area`, `rondeo`)
  - parser de dirección tolerante a comas antes de `al <número>`
  - búsqueda de respaldo por calle+barrio+ciudad

## Storage y borrado
- Archivos se guardan en `AUDIO_STORAGE_ROOT` (default `storage/audio`) con subcarpetas por fecha.
- Se guarda solo `FilePath` relativo + `Format` + `Hash`.
- Provider actual: `local` (adapter listo para nube).
- Borrado lógico y permanente:
  - `DELETE /api/v1/incidenteaudio/{id}?mode=soft|hard&deleteFile=true|false`
- Retención automática con worker:
  - `AUDIO_RETENTION_SOFT_DAYS`: marca `IsDeleted=1`.
  - `AUDIO_RETENTION_PURGE_DAYS`: borra físicamente y elimina el registro.

## Transcodificación
- Controlada por variables:
  - `AUDIO_TRANSCODE_ENABLED=true`
  - `AUDIO_TRANSCODE_FORMAT=mp3`
  - `AUDIO_TRANSCODE_BITRATE=64k`
  - `AUDIO_TRANSCODE_SAMPLE_RATE=44100`
  - `AUDIO_TRANSCODE_DELETE_ORIGINAL=true`
  - `FFMPEG_PATH=/opt/homebrew/bin/ffmpeg`
- Se ejecuta antes de la transcripción si está habilitada.

## Reproducción de audio
- Download:
  - `GET /api/v1/incidente-audio/{id}/file`
- Stream para navegador:
  - `GET /api/v1/incidente-audio/{id}/stream?token=JWT`
- Soporta `Range` para reproducción en browser.

## Configuracion en SystemBase
- Backend > Audio y pipeline:
  - Storage provider
  - Transcodificacion (formato/bitrate/borrar original)
  - Retencion (soft/purge/frecuencia)

## Variables clave (.env.example)
- `AUDIO_STORAGE_PROVIDER=local`
- `AUDIO_STORAGE_ROOT=storage/audio`
- `AUDIO_ALLOWED_EXT=mp3,wav,m4a,ogg,opus,webm,aac`
- `AUDIO_MAX_MB=50`
- `WHISPER_FIX_MAP=cast=calle;urto=hurto;...`
- `WHISPER_LOCATION_LABELS=lugar,ubicacion,direccion,...`
- `WHISPER_LOCATION_PREPOSITIONS=en,sobre,frente a,...`
- `WHISPER_LOCATION_STOP_WORDS=hubo,ocurrio,se produjo,...`
- `LLM_ENABLED=true|false`
- `LLM_URL=http://localhost:11434/api/chat`
- `LLM_MODEL=qwen2.5:7b`
- `LLM_MODE=chat`
- `LLM_JSON_SCHEMA_ENABLED=true`
- `LLM_TEMPERATURE=0`
- `GEOCODER_DEFAULT_SUFFIX=Cordoba, Argentina`
- `GEOCODER_COUNTRY_CODES=ar`
- `GEOCODER_LANGUAGE=es`

## Evaluacion automatica (calidad)
- Endpoint de evaluacion por caso:
  - `POST /api/v1/dev/eval/incident`
- Endpoint de evaluacion batch:
  - `POST /api/v1/dev/eval/batch`
- Endpoint para generar dataset automatico desde incidentes procesados:
  - `GET /api/v1/dev/eval/dataset/auto`
- Solo disponible en `Development` y desde loopback (localhost).
- Dataset de ejemplo:
  - `systems/mapeo/backend/eval/dataset.sample.json`
- Script de corrida:
  - `systems/mapeo/backend/eval/run-eval.sh`
- Script de corrida con dataset automatico:
  - `systems/mapeo/backend/eval/run-eval-auto.sh`

## Base SQL para aprendizaje de ubicaciones
- Se agregaron tablas idempotentes para soportar reglas y feedback sin tocar código:
  - `sys_mapeo.LocationNormalizationRules`
  - `sys_mapeo.LocationNormalizationFeedback`
- Creación automática al iniciar backend:
  - `systems/mapeo/backend/Utils/DbSchemaMigrator.cs`
- Script SQL manual:
  - `systems/mapeo/backend/sql/001_location_learning.sql`
- Objetivo:
  - almacenar reemplazos/correcciones frecuentes
  - guardar evidencia de casos fallidos/corregidos para entrenamiento incremental

## Config aplicado en mapeo
- `systems/mapeo/backend/.env` actualizado con transcodificacion, retencion y endpoints locales (Whisper/Nominatim).
- `WHISPER_STUB_TEXT` vacio para usar transcripcion real via `WHISPER_URL`.
- Auto-start opcional de Whisper via `WHISPER_AUTOSTART` + `WHISPER_START_COMMAND`.
- `WHISPER_URL` apunta a `/asr` (imagen `onerahmet/openai-whisper-asr-webservice`), `WHISPER_STUB_TEXT`
- `GEOCODER_URL` apuntando a Nominatim local.
- `AUDIO_RETENTION_SOFT_DAYS`, `AUDIO_RETENTION_PURGE_DAYS`, `AUDIO_RETENTION_RUN_MINUTES`
- `LLM_ENABLED` y configuracion LLM disponibles (ollama on-prem).

## Geocoder local (Nominatim)
- Servicio local requerido para obtener coordenadas.
- Config en backend: `GEOCODER_URL=http://localhost:8080/search`.
- Compose listo en `geocoder-service/docker-compose.yml`.
- Definir `PBF_URL` (o `PBF_PATH`) en `geocoder-service/.env` y levantar.
- Dataset recomendado: **Córdoba provincia** (extract local).

```bash
cd geocoder-service
cp .env.example .env
# editar PBF_URL (o PBF_PATH) con el extract que quieras indexar
docker compose up -d
```

- Verificar progreso y esperar import inicial:

```bash
docker logs -f geocoder-nominatim
```

- Probar búsqueda cuando termine:

```bash
curl "http://localhost:8080/search?q=avenida+colon+1200+cordoba&format=json"
```

## Servicios Docker
- `whisper-service/` (Whisper local)
- `llm-service/` (Ollama)
- `geocoder-service/` (Nominatim)

## Archivos principales
- Backend:
  - `systems/mapeo/backend/Program.cs`
  - `systems/mapeo/backend/Controllers/IncidentesController.cs`
  - `systems/mapeo/backend/Controllers/IncidentejobsController.cs`
  - `systems/mapeo/backend/Services/IncidenteJobWorker.cs`
  - `systems/mapeo/backend/Services/AudioRetentionWorker.cs`
  - `systems/mapeo/backend/Utils/AudioStorage.cs`
  - `systems/mapeo/backend/Utils/AudioStorageProviders.cs`
  - `systems/mapeo/backend/Utils/AudioTranscoder.cs`
  - `systems/mapeo/backend/Negocio/Pipeline/IncidentePipelineRepository.cs`
  - `systems/mapeo/backend/Negocio/Pipeline/AudioRetentionRepository.cs`
  - `systems/mapeo/backend/Utils/DbSchemaMigrator.cs`
  - `systems/mapeo/backend/Utils/CatalogoHechosSeeder.cs`

## Pendiente
- Adapter cloud (S3/MinIO) real.
- Mejoras de extracción con modelo dedicado.
- Dashboard del sistema y mapa de calor.
