#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5035}"
TAKE="${TAKE:-200}"
DISTANCE_TOLERANCE="${DISTANCE_TOLERANCE:-250}"
INCLUDE_WITHOUT_COORDS="${INCLUDE_WITHOUT_COORDS:-false}"

echo "Generando dataset automatico desde incidentes procesados..."
echo "Backend: $BASE_URL | take=$TAKE | tolerance=${DISTANCE_TOLERANCE}m | includeWithoutCoords=$INCLUDE_WITHOUT_COORDS"

if ! curl -fsS "$BASE_URL/api/v1/dev/ping" >/dev/null; then
  echo "Backend no disponible en $BASE_URL (no responde /api/v1/dev/ping)." >&2
  echo "Levanta primero el backend de mapeo y reintenta." >&2
  exit 1
fi

dataset_json="$(curl -sS \
  "$BASE_URL/api/v1/dev/eval/dataset/auto?take=$TAKE&distanceToleranceMeters=$DISTANCE_TOLERANCE&includeWithoutCoords=$INCLUDE_WITHOUT_COORDS")"

if command -v jq >/dev/null 2>&1; then
  echo "Casos generados: $(printf '%s' "$dataset_json" | jq '.Cases | length')"
fi

response="$(curl -sS -X POST \
  "$BASE_URL/api/v1/dev/eval/batch" \
  -H "Content-Type: application/json" \
  --data-binary "$dataset_json")"

if command -v jq >/dev/null 2>&1; then
  echo "$response" | jq '.metrics'
else
  echo "$response"
fi
