# Docker (AIBase)

Stack Docker para levantar el motor local de AIBase y exportarlo como servicio.

## Levantar engine
```bash
cd systems/aibase/engine
cp .env.example .env

cd ../docker
docker compose up -d --build
```

## Verificar
- Health: `http://localhost:8010/health`
- Infer: `POST http://localhost:8010/infer`

## Notas
- El backend AIBase consume este engine vía `AIBASE_ENGINE_URL` (default `http://localhost:8010`).
- Si quieres evitar mock, usar en `systems/aibase/backend/.env`:
  - `AIBASE_ENGINE_MOCK=false`
  - `AIBASE_MODEL_PROVIDER=engine`
