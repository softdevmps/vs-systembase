#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5035}"
DATASET_PATH="${1:-$(dirname "$0")/dataset.sample.json}"

if [[ ! -f "$DATASET_PATH" ]]; then
  echo "Dataset no encontrado: $DATASET_PATH" >&2
  exit 1
fi

echo "Evaluando dataset: $DATASET_PATH"
echo "Backend: $BASE_URL"

if ! curl -fsS "$BASE_URL/api/v1/dev/ping" >/dev/null; then
  echo "Backend no disponible en $BASE_URL (no responde /api/v1/dev/ping)." >&2
  echo "Levanta primero el backend de mapeo y reintenta." >&2
  exit 1
fi

response="$(curl -sS -X POST \
  "$BASE_URL/api/v1/dev/eval/batch" \
  -H "Content-Type: application/json" \
  --data-binary @"$DATASET_PATH")"

if command -v jq >/dev/null 2>&1; then
  echo "$response" | jq '.metrics'
else
  echo "$response"
fi
