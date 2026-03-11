from __future__ import annotations

import csv
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Optional

from .settings import EngineSettings
from .utils import ensure_dir, parse_json_string, slugify, tokenize


class PipelineExecutor:
    def __init__(self, settings: EngineSettings) -> None:
        self._settings = settings

    def execute(self, project_id: int, run_type: str, input_json: Optional[str]) -> dict[str, Any]:
        payload = parse_json_string(input_json)
        run_key = (run_type or "").strip().lower()

        if run_key == "dataset_build":
            return self._run_dataset_build(project_id, payload)
        if run_key == "rag_index":
            return self._run_rag_index(project_id, payload)
        if run_key == "train_lora":
            return self._run_train_lora(project_id, payload)
        if run_key == "eval_run":
            return self._run_eval(project_id, payload)
        if run_key == "deploy_service":
            return self._run_deploy(project_id, payload)

        return {
            "status": "completed",
            "runType": run_key,
            "message": f"RunType {run_key} ejecutado en motor local.",
            "at": self._now_iso(),
        }

    def _run_dataset_build(self, project_id: int, payload: Any) -> dict[str, Any]:
        project_dir = self._project_dir(project_id) / "dataset"
        ensure_dir(project_dir)

        source = payload.get("source") if isinstance(payload, dict) and isinstance(payload.get("source"), dict) else {}
        source_type = str(
            source.get("type")
            or self._value(payload, "sourceType", "unknown")
            or "unknown"
        ).strip().lower() or "unknown"
        source_path = str(
            source.get("path")
            or self._value(payload, "sourcePath", "")
            or ""
        ).strip()

        sample_rows = self._extract_sample_rows(payload)
        resolved_source = self._resolve_source_path(project_id, source_path)
        file_records = self._estimate_records_from_source(source_type, resolved_source)
        records = file_records if file_records is not None else len(sample_rows)

        quality = payload.get("qualityGates") if isinstance(payload, dict) and isinstance(payload.get("qualityGates"), dict) else {}
        min_records = self._as_int(quality.get("minRecords"), 1)
        quality_ok = records >= min_records
        warnings: list[str] = []
        if source_path and resolved_source is None:
            warnings.append("sourcePath no resolvible dentro del storage del engine.")
        if records == 0:
            warnings.append("No se detectaron registros en el dataset.")

        manifest = {
            "projectId": project_id,
            "status": "ready" if quality_ok else "warning",
            "records": records,
            "datasetVersion": self._value(payload, "datasetVersion", "v1.0.0"),
            "sourceType": source_type,
            "sourcePath": source_path,
            "sourceResolvedPath": str(resolved_source) if resolved_source else None,
            "sampleRows": len(sample_rows),
            "qualityGates": {
                "minRecords": min_records,
                "ok": quality_ok,
            },
            "warnings": warnings,
            "builtAt": self._now_iso(),
        }
        (project_dir / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

        return {
            "status": "completed",
            "runType": "dataset_build",
            "message": "Dataset construido en almacenamiento local del motor.",
            "dataset": manifest,
            "artifactPath": str((project_dir / "manifest.json").resolve()),
            "at": self._now_iso(),
        }

    def _run_rag_index(self, project_id: int, payload: Any) -> dict[str, Any]:
        rag_dir = self._project_dir(project_id) / "rag"
        ensure_dir(rag_dir)

        docs = self._extract_docs(payload)
        loaded_from_dataset = False
        dataset_source_path = None
        if not docs:
            docs, dataset_source_path = self._extract_docs_from_dataset(project_id, payload)
            loaded_from_dataset = bool(docs)

        docs_path = rag_dir / "documents.json"
        docs_path.write_text(json.dumps(docs, ensure_ascii=False, indent=2), encoding="utf-8")

        index_rows = []
        for item in docs:
            terms = sorted(tokenize(item.get("text", "")))
            index_rows.append(
                {
                    "id": item.get("id"),
                    "title": item.get("title"),
                    "terms": terms[:200],
                }
            )
        index_path = rag_dir / "index.json"
        index_path.write_text(json.dumps(index_rows, ensure_ascii=False, indent=2), encoding="utf-8")

        return {
            "status": "completed",
            "runType": "rag_index",
            "message": "Índice RAG generado en motor local.",
            "rag": {
                "documents": len(docs),
                "indexEntries": len(index_rows),
                "documentsPath": str(docs_path.resolve()),
                "indexPath": str(index_path.resolve()),
                "loadedFromDataset": loaded_from_dataset,
                "datasetSourcePath": dataset_source_path,
            },
            "at": self._now_iso(),
        }

    def _run_train_lora(self, project_id: int, payload: Any) -> dict[str, Any]:
        train_dir = self._project_dir(project_id) / "train"
        ensure_dir(train_dir)

        output = payload.get("output") if isinstance(payload, dict) and isinstance(payload.get("output"), dict) else {}
        model = payload.get("model") if isinstance(payload, dict) and isinstance(payload.get("model"), dict) else {}
        optimization = payload.get("optimization") if isinstance(payload, dict) and isinstance(payload.get("optimization"), dict) else {}

        adapter_base = str(
            output.get("adapterName")
            or self._value(payload, "adapterName", f"project-{project_id}-adapter")
            or f"project-{project_id}-adapter"
        ).strip()
        adapter_name = slugify(adapter_base)
        if not adapter_name.endswith("-v1"):
            adapter_name = f"{adapter_name}-v1"

        artifact = {
            "adapterName": adapter_name,
            "registryTag": str(output.get("registryTag") or self._value(payload, "registryTag", "") or "").strip() or None,
            "datasetVersion": str(self._value(payload, "datasetVersion", "") or "").strip() or None,
            "profile": str(self._value(payload, "profile", "balanced") or "balanced"),
            "baseModel": str(model.get("baseModel") or self._value(payload, "baseModel", "local-factory-base")),
            "epochs": self._as_int(optimization.get("epochs"), self._as_int(self._value(payload, "epochs", 3), 3)),
            "learningRate": self._as_float(optimization.get("learningRate"), self._as_float(self._value(payload, "learningRate", 0.0001), 0.0001)),
            "batchSize": self._as_int(optimization.get("batchSize"), self._as_int(self._value(payload, "batchSize", 8), 8)),
            "gradAccum": self._as_int(optimization.get("gradAccum"), self._as_int(self._value(payload, "gradAccum", 1), 1)),
            "trainedAt": self._now_iso(),
        }
        artifact_path = train_dir / "adapter.json"
        artifact_path.write_text(json.dumps(artifact, ensure_ascii=False, indent=2), encoding="utf-8")

        return {
            "status": "completed",
            "runType": "train_lora",
            "message": "Entrenamiento local completado (artifact metadata).",
            "train": artifact,
            "artifactPath": str(artifact_path.resolve()),
            "at": self._now_iso(),
        }

    def _run_eval(self, project_id: int, payload: Any) -> dict[str, Any]:
        eval_dir = self._project_dir(project_id) / "eval"
        ensure_dir(eval_dir)

        tests = []
        if isinstance(payload, dict) and isinstance(payload.get("tests"), list):
            tests = payload.get("tests")

        total = len(tests)
        passed = 0
        details = []
        for idx, case in enumerate(tests, start=1):
            if not isinstance(case, dict):
                continue
            expected = str(case.get("expectedContains") or "").strip().lower()
            simulated = str(case.get("simulatedOutput") or case.get("output") or "").lower()
            ok = bool(expected) and expected in simulated if expected else True
            if ok:
                passed += 1
            details.append(
                {
                    "id": case.get("id") or f"case-{idx}",
                    "ok": ok,
                    "expectedContains": expected,
                }
            )

        accuracy = (passed / total) if total else 1.0
        report = {
            "projectId": project_id,
            "tests": total,
            "passed": passed,
            "accuracy": round(accuracy, 4),
            "evaluatedAt": self._now_iso(),
            "details": details,
        }
        report_path = eval_dir / "report.json"
        report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

        return {
            "status": "completed",
            "runType": "eval_run",
            "message": "Evaluación local completada.",
            "eval": report,
            "artifactPath": str(report_path.resolve()),
            "at": self._now_iso(),
        }

    def _run_deploy(self, project_id: int, payload: Any) -> dict[str, Any]:
        endpoint = self._value(payload, "endpoint", "http://localhost:8010")
        health = self._value(payload, "healthUrl", "http://localhost:8010/health")
        stack_name = self._value(payload, "stackName", f"aibase-project-{project_id}")
        target_env = self._value(payload, "targetEnv", "dev")

        command = (
            f"docker compose -f systems/aibase/docker/docker-compose.yml "
            f"-p {slugify(stack_name)} up -d"
        )
        logs = [
            f"[{self._now_iso()}] Preparando artefactos de deploy para project {project_id}.",
            f"[{self._now_iso()}] Stack: {stack_name} | env: {target_env}",
            f"[{self._now_iso()}] Endpoint esperado: {endpoint}",
        ]

        return {
            "status": "completed",
            "runType": "deploy_service",
            "message": "Servicio listo para levantar con Docker Compose.",
            "deploy": {
                "status": "deployed",
                "service": stack_name,
                "endpoint": endpoint,
                "health": health,
                "command": command,
                "logs": logs,
            },
            "at": self._now_iso(),
        }

    def _extract_sample_rows(self, payload: Any) -> list[Any]:
        if not isinstance(payload, dict):
            return []
        for key in ("sampleRows", "rows", "records"):
            value = payload.get(key)
            if isinstance(value, list):
                return value
        return []

    def _extract_source_from_payload(self, payload: Any) -> tuple[str, str]:
        source = payload.get("source") if isinstance(payload, dict) and isinstance(payload.get("source"), dict) else {}
        source_type = str(source.get("type") or self._value(payload, "sourceType", "unknown") or "unknown").strip().lower()
        source_path = str(source.get("path") or self._value(payload, "sourcePath", "") or "").strip()
        return source_type or "unknown", source_path

    def _extract_docs_from_dataset(self, project_id: int, payload: Any) -> tuple[list[dict[str, str]], Optional[str]]:
        manifest_path = self._project_dir(project_id) / "dataset" / "manifest.json"
        if not manifest_path.exists():
            return [], None

        try:
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        except Exception:
            return [], None

        if not isinstance(manifest, dict):
            return [], None

        requested_version = str(self._value(payload, "datasetVersion", "") or "").strip()
        manifest_version = str(manifest.get("datasetVersion") or "").strip()
        if requested_version and manifest_version and requested_version != manifest_version:
            return [], manifest.get("sourcePath")

        source_type = str(manifest.get("sourceType") or "unknown").strip().lower() or "unknown"
        source_path = str(manifest.get("sourcePath") or "").strip()
        resolved_path = self._resolve_source_path(project_id, source_path)
        if not resolved_path:
            return [], source_path or None

        docs = self._extract_docs_from_file(resolved_path, source_type)
        return docs, source_path or str(resolved_path)

    def _extract_docs_from_file(self, source_path: Path, source_type: str) -> list[dict[str, str]]:
        max_docs = 20000
        docs: list[dict[str, str]] = []
        suffix = source_path.suffix.lower()
        kind = (source_type or "").strip().lower()

        try:
            if kind == "csv" or suffix == ".csv":
                with source_path.open("r", encoding="utf-8", errors="ignore", newline="") as f:
                    reader = csv.DictReader(f)
                    for idx, row in enumerate(reader, start=1):
                        if not isinstance(row, dict):
                            continue
                        doc = self._row_to_doc(row, idx)
                        if doc:
                            docs.append(doc)
                        if len(docs) >= max_docs:
                            break
                return docs

            if kind in {"jsonl", "transcripts", "audio_manifest"} or suffix in {".jsonl", ".txt"}:
                with source_path.open("r", encoding="utf-8", errors="ignore") as f:
                    for idx, line in enumerate(f, start=1):
                        raw = line.strip()
                        if not raw:
                            continue
                        row: Any = raw
                        if raw.startswith("{") and raw.endswith("}"):
                            try:
                                parsed = json.loads(raw)
                                if isinstance(parsed, dict):
                                    row = parsed
                            except Exception:
                                row = raw
                        doc = self._row_to_doc(row, idx)
                        if doc:
                            docs.append(doc)
                        if len(docs) >= max_docs:
                            break
                return docs

            if suffix == ".json":
                payload = json.loads(source_path.read_text(encoding="utf-8", errors="ignore"))
                if isinstance(payload, list):
                    for idx, item in enumerate(payload, start=1):
                        doc = self._row_to_doc(item, idx)
                        if doc:
                            docs.append(doc)
                        if len(docs) >= max_docs:
                            break
                elif isinstance(payload, dict):
                    doc = self._row_to_doc(payload, 1)
                    if doc:
                        docs.append(doc)
                return docs
        except Exception:
            return []

        return docs

    def _row_to_doc(self, row: Any, idx: int) -> Optional[dict[str, str]]:
        if isinstance(row, str):
            text = row.strip()
            if not text:
                return None
            return {"id": f"doc-{idx}", "title": f"Documento {idx}", "text": text}

        if not isinstance(row, dict):
            return None

        question = self._pick_first_non_empty(
            row,
            ("question", "pregunta", "instruction", "instruccion", "prompt", "input", "query"),
        )
        answer = self._pick_first_non_empty(
            row,
            ("answer", "respuesta", "output", "target", "completion", "response", "text", "content", "body"),
        )

        text = ""
        if answer and question:
            text = f"Pregunta: {question}\nRespuesta: {answer}"
        elif answer:
            text = answer
        elif question:
            text = question
        else:
            parts = []
            ignored = {
                "id", "split", "lang", "language", "source", "sources",
                "tag", "tags", "intent", "label", "category", "type",
                "topic", "domain", "dataset", "subset",
            }
            for key, value in row.items():
                key_norm = str(key or "").strip().lower()
                if key_norm in ignored:
                    continue
                item = str(value or "").strip()
                if item:
                    parts.append(item)
            text = " | ".join(parts)

        text = str(text or "").strip()
        if not text:
            return None

        title = (
            self._pick_first_non_empty(row, ("title", "name"))
            or question
            or f"Documento {idx}"
        )

        return {
            "id": str(row.get("id") or f"doc-{idx}"),
            "title": str(title),
            "text": text,
        }

    @staticmethod
    def _pick_first_non_empty(row: dict[str, Any], keys: tuple[str, ...]) -> str:
        for key in keys:
            if key not in row:
                continue
            value = str(row.get(key) or "").strip()
            if value:
                return value
        return ""

    def _resolve_source_path(self, project_id: int, source_path: str) -> Optional[Path]:
        raw = str(source_path or "").strip()
        if not raw:
            return None

        if raw.startswith("local://"):
            return None

        if raw.startswith("engine://"):
            relative = raw[len("engine://"):].lstrip("/")
            if not relative.startswith("projects/"):
                relative = f"projects/{project_id}/{relative}"
            expected_prefix = f"projects/{project_id}/"
            if not relative.startswith(expected_prefix):
                return None
            candidate = (self._settings.storage_root / relative).resolve()
            if self._is_inside_storage(candidate):
                return candidate
            return None

        candidate = Path(raw)
        try:
            resolved = candidate.resolve()
        except Exception:
            return None
        if self._is_inside_storage(resolved):
            return resolved
        return None

    def _is_inside_storage(self, path: Path) -> bool:
        root = self._settings.storage_root.resolve()
        try:
            path.resolve().relative_to(root)
            return True
        except Exception:
            return False

    def _estimate_records_from_source(self, source_type: str, source_path: Optional[Path]) -> Optional[int]:
        if not source_path or not source_path.exists():
            return None

        suffix = source_path.suffix.lower()
        kind = (source_type or "").strip().lower()

        try:
            if kind == "csv" or suffix == ".csv":
                count = 0
                with source_path.open("r", encoding="utf-8", errors="ignore", newline="") as f:
                    reader = csv.reader(f)
                    header_seen = False
                    for row in reader:
                        if not header_seen:
                            header_seen = True
                            continue
                        if any(str(item or "").strip() for item in row):
                            count += 1
                return count

            if kind in {"jsonl", "transcripts", "audio_manifest"} or suffix in {".jsonl", ".txt"}:
                count = 0
                with source_path.open("r", encoding="utf-8", errors="ignore") as f:
                    for line in f:
                        if line.strip():
                            count += 1
                return count

            if suffix == ".json":
                payload = json.loads(source_path.read_text(encoding="utf-8", errors="ignore"))
                if isinstance(payload, list):
                    return len(payload)
                if isinstance(payload, dict):
                    return 1
        except Exception:
            return None

        return None

    def _extract_docs(self, payload: Any) -> list[dict[str, str]]:
        docs: list[dict[str, str]] = []
        if not isinstance(payload, dict):
            return docs

        raw_docs = payload.get("documents") or payload.get("rows") or payload.get("sampleRows")
        if not isinstance(raw_docs, list):
            return docs

        for idx, row in enumerate(raw_docs, start=1):
            if isinstance(row, str):
                text = row.strip()
                if not text:
                    continue
                docs.append({"id": f"doc-{idx}", "title": f"Documento {idx}", "text": text})
                continue

            if not isinstance(row, dict):
                continue

            text = str(row.get("text") or row.get("content") or row.get("body") or "").strip()
            if not text:
                text = str(row)
            docs.append(
                {
                    "id": str(row.get("id") or f"doc-{idx}"),
                    "title": str(row.get("title") or row.get("name") or f"Documento {idx}"),
                    "text": text,
                }
            )
        return docs

    def _project_dir(self, project_id: int) -> Path:
        root = self._settings.storage_root / "projects" / str(project_id)
        ensure_dir(root)
        return root

    @staticmethod
    def _value(payload: Any, key: str, default: Any) -> Any:
        if isinstance(payload, dict) and key in payload and payload[key] not in (None, ""):
            return payload[key]
        return default

    @staticmethod
    def _as_int(value: Any, default: int) -> int:
        try:
            return int(value)
        except Exception:
            return int(default)

    @staticmethod
    def _as_float(value: Any, default: float) -> float:
        try:
            return float(value)
        except Exception:
            return float(default)

    @staticmethod
    def _now_iso() -> str:
        return datetime.now(timezone.utc).isoformat()
