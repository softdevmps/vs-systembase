from __future__ import annotations

import csv
import json
import os
import re
import shutil
import subprocess
import sys
import unicodedata
from datetime import datetime, timezone
from typing import Any

from dotenv import load_dotenv
from pathlib import Path

from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from fastapi.middleware.cors import CORSMiddleware

from .pipeline import PipelineExecutor
from .runtime_model import RuntimeModel
from .schemas import DatasetGenerateRequest, DatasetMergeRequest, ExportDockerRequest, InferRequest, RunExecuteRequest
from .settings import EngineSettings
from .utils import ensure_dir, slugify


load_dotenv()

settings = EngineSettings.from_env()
settings.ensure_dirs()
runtime_model = RuntimeModel(settings)
pipeline_executor = PipelineExecutor(settings)

app = FastAPI(
    title="AIBase Local Engine",
    version="1.0.0",
    description="Motor local de IA para AIBase (inferencia + pipeline + export Docker).",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/health")
def health() -> dict[str, Any]:
    return {
        "ok": True,
        "service": settings.service_name,
        "modelMode": settings.model_mode,
        "hfModelId": settings.hf_model_id if settings.use_hf else None,
        "hfTask": settings.hf_task,
        "hfLocalFilesOnly": settings.hf_local_files_only,
        "hfTopP": settings.hf_top_p,
        "hfRepetitionPenalty": settings.hf_repetition_penalty,
        "ollama": {
            "baseUrl": settings.ollama_base_url,
            "model": settings.ollama_model,
            "path": settings.ollama_path,
            "timeoutSec": settings.ollama_timeout_sec,
        },
        "qualityGate": {
            "enabled": settings.quality_gate_enabled,
            "minSamples": settings.quality_gate_min_samples,
            "minSuccessRate": settings.quality_gate_min_success_rate,
            "maxFallbackRate": settings.quality_gate_max_fallback_rate,
            "maxAvgLatencyMs": settings.quality_gate_max_avg_latency_ms,
        },
        "storageRoot": str(settings.storage_root),
        "exportsRoot": str(settings.exports_root),
        "at": _now_iso(),
    }


@app.post("/infer")
def infer(request: InferRequest) -> dict[str, Any]:
    if not request.input.strip():
        raise HTTPException(status_code=400, detail="input requerido")

    result = runtime_model.infer(
        project_id=request.projectId,
        user_input=request.input,
        context_json=request.contextJson,
    )

    return {
        "projectId": request.projectId,
        "answer": result.answer,
        "provider": result.provider,
        "model": result.model,
        "mode": result.mode,
        "usedFallback": result.used_fallback,
        "qualityScore": result.quality_score,
        "traceId": result.trace_id,
        "sources": result.sources,
        "latencyMs": result.latency_ms,
        "diagnostics": result.diagnostics,
        "createdAt": _now_iso(),
    }


@app.get("/projects/{project_id}/metrics/infer")
def infer_metrics(
    project_id: int,
    take: int = 20,
    gateEnabled: bool | None = None,
    minSamples: int | None = None,
    minSuccessRate: float | None = None,
    maxFallbackRate: float | None = None,
    maxAvgLatencyMs: int | None = None,
) -> dict[str, Any]:
    if project_id <= 0:
        raise HTTPException(status_code=400, detail="project_id inválido")

    gate = None
    if any(value is not None for value in (gateEnabled, minSamples, minSuccessRate, maxFallbackRate, maxAvgLatencyMs)):
        from .runtime_model import QualityGateConfig

        gate = QualityGateConfig(
            enabled=settings.quality_gate_enabled if gateEnabled is None else bool(gateEnabled),
            min_samples=settings.quality_gate_min_samples if minSamples is None else max(1, int(minSamples)),
            min_success_rate=settings.quality_gate_min_success_rate if minSuccessRate is None else max(0.0, min(1.0, float(minSuccessRate))),
            max_fallback_rate=settings.quality_gate_max_fallback_rate if maxFallbackRate is None else max(0.0, min(1.0, float(maxFallbackRate))),
            max_avg_latency_ms=settings.quality_gate_max_avg_latency_ms if maxAvgLatencyMs is None else max(0, int(maxAvgLatencyMs)),
        )

    return runtime_model.get_project_infer_metrics(project_id, take=take, gate=gate)


@app.post("/runs/execute")
def runs_execute(request: RunExecuteRequest) -> dict[str, Any]:
    run_type = request.runType.strip().lower()
    if not run_type:
        raise HTTPException(status_code=400, detail="runType requerido")

    payload = pipeline_executor.execute(
        project_id=request.projectId,
        run_type=run_type,
        input_json=request.inputJson,
    )

    return {
        "projectId": request.projectId,
        "runType": run_type,
        "engine": settings.service_name,
        "output": payload,
        **payload,
    }


@app.post("/projects/{project_id}/dataset/upload")
async def upload_dataset_file(
    project_id: int,
    file: UploadFile = File(...),
    sourceType: str | None = Form(default=None),
) -> dict[str, Any]:
    if project_id <= 0:
        raise HTTPException(status_code=400, detail="project_id inválido")

    original_name = (file.filename or "").strip()
    if not original_name:
        raise HTTPException(status_code=400, detail="file requerido")

    suffix = Path(original_name).suffix.lower()
    allowed = {".csv", ".json", ".jsonl", ".txt"}
    if suffix not in allowed:
        raise HTTPException(status_code=400, detail="Extensión no soportada. Usa .csv, .json, .jsonl o .txt")

    uploads_dir = settings.storage_root / "projects" / str(project_id) / "dataset" / "uploads"
    ensure_dir(uploads_dir)

    timestamp = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S")
    stem = slugify(Path(original_name).stem)
    stored_name = f"{timestamp}-{stem}{suffix}"
    stored_path = uploads_dir / stored_name

    size_bytes = 0
    with stored_path.open("wb") as out:
        while True:
            chunk = await file.read(1024 * 1024)
            if not chunk:
                break
            out.write(chunk)
            size_bytes += len(chunk)
    await file.close()

    source_type = (sourceType or "").strip().lower() or _source_type_from_suffix(suffix)
    source_path = f"engine://projects/{project_id}/dataset/uploads/{stored_name}"

    return {
        "ok": True,
        "projectId": project_id,
        "fileName": original_name,
        "storedFileName": stored_name,
        "sourceType": source_type,
        "sourcePath": source_path,
        "storedPath": str(stored_path.resolve()),
        "sizeBytes": size_bytes,
        "uploadedAt": _now_iso(),
    }


@app.get("/projects/{project_id}/dataset/sources")
def list_project_dataset_sources(project_id: int) -> dict[str, Any]:
    if project_id <= 0:
        raise HTTPException(status_code=400, detail="project_id inválido")

    uploads_dir = _project_uploads_dir(project_id)
    ensure_dir(uploads_dir)

    active_source_path = _active_project_source_path(project_id)
    sources = []

    for file_path in sorted(uploads_dir.glob("*"), key=lambda p: p.stat().st_mtime, reverse=True):
        if not file_path.is_file():
            continue
        suffix = file_path.suffix.lower()
        if suffix not in {".csv", ".json", ".jsonl", ".txt"}:
            continue

        source_path = _source_path_for_file(project_id, file_path.name)
        source_type = _source_type_from_suffix(suffix)
        updated_at = datetime.fromtimestamp(file_path.stat().st_mtime, tz=timezone.utc).isoformat()

        sources.append(
            {
                "fileName": file_path.name,
                "sourcePath": source_path,
                "sourceType": source_type,
                "sizeBytes": int(file_path.stat().st_size),
                "updatedAt": updated_at,
                "origin": "generated" if "-generated-" in file_path.name else "upload",
                "isActive": bool(active_source_path and source_path == active_source_path),
            }
        )

    if active_source_path and not any(item["sourcePath"] == active_source_path for item in sources):
        sources.insert(
            0,
            {
                "fileName": Path(active_source_path).name if "/" in active_source_path else active_source_path,
                "sourcePath": active_source_path,
                "sourceType": _source_type_from_suffix(Path(active_source_path).suffix),
                "sizeBytes": 0,
                "updatedAt": None,
                "origin": "external",
                "isActive": True,
            },
        )

    return {
        "ok": True,
        "projectId": project_id,
        "activeSourcePath": active_source_path,
        "count": len(sources),
        "sources": sources,
        "at": _now_iso(),
    }


@app.post("/projects/{project_id}/dataset/generate")
def generate_dataset_from_topics(project_id: int, request: DatasetGenerateRequest) -> dict[str, Any]:
    if project_id <= 0:
        raise HTTPException(status_code=400, detail="project_id inválido")

    topics = _normalize_topics(request.topics)
    if not topics:
        raise HTTPException(status_code=400, detail="topics requerido (lista no vacía).")

    generator_root = settings.storage_root / "generator"
    run_script = generator_root / "run.py"
    if not run_script.exists():
        raise HTTPException(status_code=404, detail="No se encontró módulo generador en storage: /data/generator/run.py")

    inputs_dir = generator_root / "inputs"
    dataset_root = generator_root / "datasetMasivo"
    ensure_dir(inputs_dir)
    ensure_dir(dataset_root)

    if request.resetTopicFolders:
        for topic in topics:
            folder = dataset_root / _generator_topic_slug(topic)
            if folder.exists() and folder.is_dir():
                shutil.rmtree(folder, ignore_errors=True)

    topics_file = inputs_dir / "topics.txt"
    topics_file.write_text("\n".join(topics) + "\n", encoding="utf-8")

    env = os.environ.copy()
    if request.maxWikipediaResults is not None:
        env["MAX_WIKIPEDIA_RESULTS"] = str(request.maxWikipediaResults)
    if request.maxExpandedQueries is not None:
        env["MAX_EXPANDED_QUERIES"] = str(request.maxExpandedQueries)
    if request.chunkSize is not None:
        env["CHUNK_SIZE"] = str(request.chunkSize)
    if request.chunkOverlap is not None:
        env["CHUNK_OVERLAP"] = str(request.chunkOverlap)
    if request.sleepSeconds is not None:
        env["SLEEP_SECONDS"] = str(request.sleepSeconds)

    try:
        completed = subprocess.run(
            [sys.executable, "run.py"],
            cwd=generator_root,
            env=env,
            capture_output=True,
            text=True,
            check=False,
            timeout=1800,
        )
    except subprocess.TimeoutExpired:
        raise HTTPException(status_code=504, detail="Generación de dataset excedió el tiempo límite (1800s).")

    stdout_tail = _tail_text(completed.stdout, max_chars=3000)
    stderr_tail = _tail_text(completed.stderr, max_chars=3000)
    if completed.returncode != 0:
        raise HTTPException(
            status_code=500,
            detail={
                "message": "Falló la generación de dataset por tópicos.",
                "exitCode": completed.returncode,
                "stdoutTail": stdout_tail,
                "stderrTail": stderr_tail,
            },
        )

    topic_files: list[Path] = []
    for topic in topics:
        topic_dir = dataset_root / _generator_topic_slug(topic)
        if not topic_dir.exists() or not topic_dir.is_dir():
            continue
        for path in sorted(topic_dir.glob("*.csv")):
            if path.is_file():
                topic_files.append(path)

    if not topic_files:
        raise HTTPException(status_code=422, detail="El generador finalizó pero no produjo archivos CSV.")

    uploads_dir = _project_uploads_dir(project_id)
    ensure_dir(uploads_dir)
    timestamp = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S")
    dataset_slug = slugify(request.datasetName or "-".join(topics[:3]) or "dataset")
    stored_name = f"{timestamp}-generated-{dataset_slug}.csv"
    stored_path = uploads_dir / stored_name

    records, fieldnames = _merge_csv_files(topic_files, stored_path)
    source_path = _source_path_for_file(project_id, stored_name)

    return {
        "ok": True,
        "projectId": project_id,
        "topics": topics,
        "topicCount": len(topics),
        "sourceType": "csv",
        "sourcePath": source_path,
        "fileName": stored_name,
        "storedFileName": stored_name,
        "storedPath": str(stored_path.resolve()),
        "sizeBytes": int(stored_path.stat().st_size) if stored_path.exists() else 0,
        "records": records,
        "columns": fieldnames,
        "topicFiles": [str(path.resolve()) for path in topic_files],
        "stdoutTail": stdout_tail,
        "stderrTail": stderr_tail,
        "generatedAt": _now_iso(),
    }


@app.post("/projects/{project_id}/dataset/merge")
def merge_project_dataset_sources(project_id: int, request: DatasetMergeRequest) -> dict[str, Any]:
    if project_id <= 0:
        raise HTTPException(status_code=400, detail="project_id inválido")

    source_paths = _normalize_source_paths(request.sourcePaths)
    if len(source_paths) < 2:
        raise HTTPException(status_code=400, detail="Se requieren al menos 2 sourcePaths para unificar.")

    resolved_paths: list[Path] = []
    missing_sources: list[str] = []
    for source_path in source_paths:
        resolved = _resolve_project_source_path(project_id, source_path)
        if not resolved or not resolved.exists() or not resolved.is_file():
            missing_sources.append(source_path)
            continue
        resolved_paths.append(resolved)

    if missing_sources:
        raise HTTPException(
            status_code=422,
            detail={
                "message": "Algunas fuentes no se pudieron resolver dentro del storage del engine.",
                "missingSources": missing_sources,
            },
        )

    uploads_dir = _project_uploads_dir(project_id)
    ensure_dir(uploads_dir)
    timestamp = datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S")
    default_name = "-".join(path.stem for path in resolved_paths[:2]) or "dataset"
    dataset_slug = slugify(request.datasetName or default_name)
    stored_name = f"{timestamp}-merged-{dataset_slug}.csv"
    stored_path = uploads_dir / stored_name

    records, fieldnames = _merge_dataset_sources(
        files=resolved_paths,
        output_path=stored_path,
        deduplicate=bool(request.deduplicate),
    )
    source_path = _source_path_for_file(project_id, stored_name)

    return {
        "ok": True,
        "projectId": project_id,
        "sourceType": "csv",
        "sourcePath": source_path,
        "fileName": stored_name,
        "storedFileName": stored_name,
        "storedPath": str(stored_path.resolve()),
        "sizeBytes": int(stored_path.stat().st_size) if stored_path.exists() else 0,
        "records": records,
        "columns": fieldnames,
        "sourcesMerged": source_paths,
        "deduplicate": bool(request.deduplicate),
        "mergedAt": _now_iso(),
    }


@app.post("/projects/{project_id}/export/docker")
def export_docker_bundle(project_id: int, request: ExportDockerRequest) -> dict[str, Any]:
    if project_id <= 0:
        raise HTTPException(status_code=400, detail="project_id inválido")

    service_name = slugify(request.serviceName or f"aibase-model-{project_id}")
    image_tag = (request.imageTag or "aibase-local-engine:latest").strip()
    host_port = request.hostPort
    container_port = request.containerPort

    export_dir = settings.exports_root / f"project-{project_id}" / datetime.now(timezone.utc).strftime("%Y%m%d-%H%M%S")
    ensure_dir(export_dir)

    compose_content = _compose_template(
        service_name=service_name,
        image_tag=image_tag,
        host_port=host_port,
        container_port=container_port,
    )
    env_rows = {
        "ENGINE_SERVICE_NAME": service_name,
        "ENGINE_MODEL_MODE": settings.model_mode,
        "ENGINE_HF_TASK": settings.hf_task,
        "ENGINE_HF_MODEL_ID": settings.hf_model_id,
        "ENGINE_HF_LOCAL_FILES_ONLY": str(settings.hf_local_files_only).lower(),
        "ENGINE_HF_MAX_NEW_TOKENS": str(settings.hf_max_new_tokens),
        "ENGINE_HF_TEMPERATURE": str(settings.hf_temperature),
        "ENGINE_HF_TOP_P": str(settings.hf_top_p),
        "ENGINE_HF_REPETITION_PENALTY": str(settings.hf_repetition_penalty),
        "ENGINE_OLLAMA_BASE_URL": settings.ollama_base_url,
        "ENGINE_OLLAMA_MODEL": settings.ollama_model,
        "ENGINE_OLLAMA_PATH": settings.ollama_path,
        "ENGINE_OLLAMA_TIMEOUT_SEC": str(settings.ollama_timeout_sec),
        "ENGINE_DEFAULT_PROFILE": settings.default_profile,
        "ENGINE_DEFAULT_SYSTEM_PROMPT": settings.default_system_prompt,
        "ENGINE_QUALITY_GATE_ENABLED": str(settings.quality_gate_enabled).lower(),
        "ENGINE_QUALITY_GATE_MIN_SAMPLES": str(settings.quality_gate_min_samples),
        "ENGINE_QUALITY_GATE_MIN_SUCCESS_RATE": str(settings.quality_gate_min_success_rate),
        "ENGINE_QUALITY_GATE_MAX_FALLBACK_RATE": str(settings.quality_gate_max_fallback_rate),
        "ENGINE_QUALITY_GATE_MAX_AVG_LATENCY_MS": str(settings.quality_gate_max_avg_latency_ms),
        "AIBASE_DEFAULT_PROJECT_ID": str(project_id),
        **{k: str(v) for k, v in request.extraEnv.items()},
    }

    compose_path = export_dir / "docker-compose.yml"
    env_path = export_dir / ".env"
    compose_path.write_text(compose_content, encoding="utf-8")
    env_path.write_text(_env_template(env_rows), encoding="utf-8")

    return {
        "projectId": project_id,
        "serviceName": service_name,
        "imageTag": image_tag,
        "bundleDir": str(export_dir.resolve()),
        "composeFile": str(compose_path.resolve()),
        "envFile": str(env_path.resolve()),
        "dockerCommand": f"docker compose -f {compose_path.resolve()} --env-file {env_path.resolve()} up -d",
        "createdAt": _now_iso(),
    }


def _compose_template(*, service_name: str, image_tag: str, host_port: int, container_port: int) -> str:
    return f"""services:
  {service_name}:
    image: {image_tag}
    restart: unless-stopped
    env_file:
      - ./.env
    ports:
      - "{host_port}:{container_port}"
    healthcheck:
      test: ["CMD", "python", "-c", "import urllib.request;urllib.request.urlopen('http://localhost:{container_port}/health', timeout=2)"]
      interval: 30s
      timeout: 5s
      retries: 5
"""


def _env_template(values: dict[str, str]) -> str:
    lines = [f"{key}={value}" for key, value in values.items()]
    return "\n".join(lines) + "\n"


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _source_type_from_suffix(suffix: str) -> str:
    value = (suffix or "").strip().lower()
    if value == ".csv":
        return "csv"
    if value == ".jsonl":
        return "jsonl"
    if value == ".json":
        return "jsonl"
    return "csv"


def _project_uploads_dir(project_id: int) -> Path:
    return settings.storage_root / "projects" / str(project_id) / "dataset" / "uploads"


def _source_path_for_file(project_id: int, file_name: str) -> str:
    return f"engine://projects/{project_id}/dataset/uploads/{file_name}"


def _active_project_source_path(project_id: int) -> str | None:
    manifest_path = settings.storage_root / "projects" / str(project_id) / "dataset" / "manifest.json"
    if not manifest_path.exists():
        return None
    try:
        payload = json.loads(manifest_path.read_text(encoding="utf-8"))
    except Exception:
        return None
    if not isinstance(payload, dict):
        return None
    source_path = str(payload.get("sourcePath") or "").strip()
    return source_path or None


def _normalize_topics(items: list[str]) -> list[str]:
    normalized: list[str] = []
    seen: set[str] = set()
    for raw in items:
        topic = str(raw or "").strip()
        if not topic:
            continue
        key = topic.lower()
        if key in seen:
            continue
        seen.add(key)
        normalized.append(topic)
    return normalized


def _normalize_source_paths(items: list[str]) -> list[str]:
    normalized: list[str] = []
    seen: set[str] = set()
    for raw in items:
        source_path = str(raw or "").strip()
        if not source_path:
            continue
        key = source_path.lower()
        if key in seen:
            continue
        seen.add(key)
        normalized.append(source_path)
    return normalized


def _resolve_project_source_path(project_id: int, source_path: str) -> Path | None:
    raw = str(source_path or "").strip()
    if not raw or raw.startswith("local://"):
        return None

    if raw.startswith("engine://"):
        relative = raw[len("engine://"):].lstrip("/")
        if not relative.startswith("projects/"):
            relative = f"projects/{project_id}/{relative}"
        expected_prefix = f"projects/{project_id}/"
        if not relative.startswith(expected_prefix):
            return None
        candidate = (settings.storage_root / relative).resolve()
        return candidate if _is_inside_storage(candidate) else None

    candidate = Path(raw)
    try:
        resolved = candidate.resolve()
    except Exception:
        return None
    return resolved if _is_inside_storage(resolved) else None


def _is_inside_storage(path: Path) -> bool:
    root = settings.storage_root.resolve()
    try:
        path.resolve().relative_to(root)
        return True
    except Exception:
        return False


def _row_from_value(value: Any) -> dict[str, str]:
    if isinstance(value, dict):
        return {str(k): ("" if v is None else str(v)) for k, v in value.items() if k is not None}
    if isinstance(value, list):
        return {"items": json.dumps(value, ensure_ascii=False)}
    text = str(value or "").strip()
    if not text:
        return {}
    return {"text": text}


def _iter_rows_from_source_file(path: Path) -> list[dict[str, str]]:
    suffix = path.suffix.lower()
    rows: list[dict[str, str]] = []

    if suffix == ".csv":
        with path.open("r", encoding="utf-8", errors="ignore", newline="") as f:
            reader = csv.DictReader(f)
            for row in reader:
                payload = _row_from_value(row)
                if payload:
                    rows.append(payload)
        return rows

    if suffix in {".jsonl", ".txt"}:
        with path.open("r", encoding="utf-8", errors="ignore") as f:
            for line in f:
                text = line.strip()
                if not text:
                    continue
                item: Any = text
                if text.startswith("{") or text.startswith("["):
                    try:
                        item = json.loads(text)
                    except Exception:
                        item = text
                payload = _row_from_value(item)
                if payload:
                    rows.append(payload)
        return rows

    if suffix == ".json":
        item: Any
        try:
            item = json.loads(path.read_text(encoding="utf-8", errors="ignore"))
        except Exception:
            item = None
        if isinstance(item, list):
            for entry in item:
                payload = _row_from_value(entry)
                if payload:
                    rows.append(payload)
        elif item is not None:
            payload = _row_from_value(item)
            if payload:
                rows.append(payload)
        return rows

    return rows


def _merge_dataset_sources(files: list[Path], output_path: Path, deduplicate: bool) -> tuple[int, list[str]]:
    rows: list[dict[str, str]] = []
    fieldnames: list[str] = []
    seen: set[str] = set()

    for path in files:
        source_rows = _iter_rows_from_source_file(path)
        for row in source_rows:
            payload = {str(k): str(v) for k, v in row.items()}
            for key in payload.keys():
                if key not in fieldnames:
                    fieldnames.append(key)

            if deduplicate:
                dedupe_key = json.dumps(payload, ensure_ascii=False, sort_keys=True)
                if dedupe_key in seen:
                    continue
                seen.add(dedupe_key)

            rows.append(payload)

    if not fieldnames:
        raise HTTPException(status_code=422, detail="No se detectaron columnas válidas en las fuentes seleccionadas.")

    with output_path.open("w", encoding="utf-8", newline="") as out:
        writer = csv.DictWriter(out, fieldnames=fieldnames, quoting=csv.QUOTE_ALL)
        writer.writeheader()
        for row in rows:
            writer.writerow({name: row.get(name, "") for name in fieldnames})

    return len(rows), fieldnames


def _merge_csv_files(files: list[Path], output_path: Path) -> tuple[int, list[str]]:
    fieldnames: list[str] = []
    for path in files:
        with path.open("r", encoding="utf-8", errors="ignore", newline="") as f:
            reader = csv.DictReader(f)
            for name in reader.fieldnames or []:
                if name and name not in fieldnames:
                    fieldnames.append(name)

    if not fieldnames:
        raise HTTPException(status_code=422, detail="No se detectaron columnas válidas en los CSV generados.")

    records = 0
    with output_path.open("w", encoding="utf-8", newline="") as out:
        writer = csv.DictWriter(out, fieldnames=fieldnames, quoting=csv.QUOTE_ALL)
        writer.writeheader()

        for path in files:
            with path.open("r", encoding="utf-8", errors="ignore", newline="") as f:
                reader = csv.DictReader(f)
                for row in reader:
                    if not isinstance(row, dict):
                        continue
                    payload = {name: row.get(name, "") for name in fieldnames}
                    writer.writerow(payload)
                    records += 1

    return records, fieldnames


def _tail_text(text: str | None, *, max_chars: int = 3000) -> str:
    raw = str(text or "").strip()
    if len(raw) <= max_chars:
        return raw
    return raw[-max_chars:]


def _generator_topic_slug(value: str) -> str:
    raw = str(value or "").strip().lower()
    norm = unicodedata.normalize("NFD", raw)
    norm = "".join(ch for ch in norm if unicodedata.category(ch) != "Mn")
    norm = re.sub(r"\s+", " ", norm)
    return norm.replace(" ", "_").replace("/", "_").strip("_")
