# Engine (AIBase)

Motor local de IA (FastAPI) para ejecutar:
- inferencia (`POST /infer`)
- etapas de pipeline (`POST /runs/execute`)
- export de servicio docker (`POST /projects/{projectId}/export/docker`)

## Endpoints
- `GET /health`
- `POST /infer`
- `POST /runs/execute`
- `POST /projects/{projectId}/export/docker`

## Ejecutar local
```bash
cd systems/aibase/engine
cp .env.example .env
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
# opcional para modelo HF local:
pip install -r requirements-llm.txt
uvicorn app.main:app --host 0.0.0.0 --port 8010 --reload
```

## Modo de modelo
- `ENGINE_MODEL_MODE=hf`: usa modelo local de HuggingFace.
- `ENGINE_MODEL_MODE=rule`: modo local liviano (fallback sin dependencias pesadas).

Variables principales:
- `ENGINE_HF_MODEL_ID` (default `google/flan-t5-base`)
- `ENGINE_HF_TASK` (default `text2text-generation`)
- `ENGINE_HF_MAX_NEW_TOKENS`
- `ENGINE_HF_TEMPERATURE`

Override por template:
- En `PipelineJson.meta.modelService.model` puedes usar:
  - alias local (ej: `assistant-general-v1`)
  - o `hf:<org/model>` para forzar un modelo HF concreto.

## Docker
```bash
cd systems/aibase/docker
docker compose up -d --build
```

Salud:
- `http://localhost:8010/health`
