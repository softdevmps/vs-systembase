from __future__ import annotations

import json
import os
import re
import time
import unicodedata
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from threading import Lock
from typing import Any, Optional

from .settings import EngineSettings
from .utils import ensure_dir, overlap_score, parse_json_string, tokenize


@dataclass
class InferResult:
    answer: str
    sources: list[dict[str, Any]]
    provider: str
    model: str
    mode: str
    used_fallback: bool
    latency_ms: int
    quality_score: float
    trace_id: str
    diagnostics: dict[str, Any]


@dataclass
class GenerationConfig:
    task: str
    max_new_tokens: int
    temperature: float
    top_p: float
    repetition_penalty: float


@dataclass
class QualityGateConfig:
    enabled: bool
    min_samples: int
    min_success_rate: float
    max_fallback_rate: float
    max_avg_latency_ms: int


@dataclass
class AutoLearningConfig:
    enabled: bool
    capture_fallback: bool
    min_quality_score: float
    max_records: int
    max_docs_for_retrieval: int


@dataclass
class ReasoningConfig:
    enabled: bool
    passes: int
    use_verifier: bool
    min_self_score: float
    max_plan_steps: int


class RuntimeModel:
    def __init__(self, settings: EngineSettings) -> None:
        self._settings = settings
        self._pipeline_lock = Lock()
        self._metrics_lock = Lock()
        self._memory_lock = Lock()
        self._pipelines: dict[str, Any] = {}
        self._pipeline_errors: dict[str, str] = {}
        self._pipeline_error_at: dict[str, float] = {}
        self._provider_errors: dict[str, str] = {}

    def infer(self, project_id: int, user_input: str, context_json: Optional[str]) -> InferResult:
        started_at = time.perf_counter()
        context = parse_json_string(context_json)

        chat_history = self._extract_chat_history(context)
        auto_learning = self._resolve_auto_learning_config(context)
        reasoning = self._resolve_reasoning_config(context)
        documents = self._extract_documents(project_id, context, auto_learning=auto_learning)
        retrieval_query = self._build_retrieval_query(user_input=user_input, chat_history=chat_history)
        top_docs = self._select_top_documents(retrieval_query, documents)
        system_prompt = self._extract_system_prompt(context)
        model_provider = self._resolve_model_provider(context)
        model_id = (
            self._resolve_ollama_model_id(context)
            if model_provider == "ollama"
            else self._resolve_hf_model_id(context)
        )
        generation = self._resolve_generation_config(context, model_id)
        local_files_only = self._resolve_local_files_only(context)
        route_to_rules = self._should_route_to_rules(user_input=user_input)

        prompt = self._build_prompt(
            user_input=user_input,
            system_prompt=system_prompt,
            chat_history=chat_history,
            top_docs=top_docs,
            model_task=generation.task,
        )
        reasoning_diag: dict[str, Any] = {
            "enabled": reasoning.enabled,
            "applied": False,
            "passes": reasoning.passes,
            "useVerifier": reasoning.use_verifier,
            "minSelfScore": reasoning.min_self_score,
            "maxPlanSteps": reasoning.max_plan_steps,
            "reason": "disabled",
        }

        pipeline_error = ""
        if model_provider == "ollama" and not route_to_rules:
            text, used_fallback, reasoning_diag = self._generate_with_ollama(
                prompt=prompt,
                model_id=model_id,
                generation=generation,
                system_prompt=system_prompt,
                user_input=user_input,
                top_docs=top_docs,
                chat_history=chat_history,
                reasoning=reasoning,
                context=context,
            )
            provider = "factory_ollama"
            model = model_id
            mode = "ollama"
            pipeline_error = self._provider_errors.get(f"ollama::{model_id}") or ""
        elif self._settings.use_hf and not route_to_rules:
            text, used_fallback, reasoning_diag = self._generate_with_hf(
                prompt=prompt,
                model_id=model_id,
                generation=generation,
                local_files_only=local_files_only,
                system_prompt=system_prompt,
                fallback_input=user_input,
                fallback_docs=top_docs,
                fallback_chat_history=chat_history,
                reasoning=reasoning,
            )
            provider = "factory_hf"
            model = model_id
            mode = "hf"
            pipeline_error = self._pipeline_errors.get(
                f"{self._pipeline_key(model_id, generation.task)}::local={1 if local_files_only else 0}"
            ) or ""
        else:
            text, used_fallback = self._generate_with_rules(
                user_input,
                top_docs,
                chat_history=chat_history,
            )
            provider = "factory_rule"
            model = "rule-based-local"
            mode = "rule"
            reasoning_diag["reason"] = "provider_rule"

        if not text.strip():
            text = "No pude generar una respuesta útil con el contexto actual."
            used_fallback = True

        if text.startswith("No encontré contexto confiable para responder"):
            top_docs = []

        latency_ms = int((time.perf_counter() - started_at) * 1000)
        quality_score = self._estimate_quality_score(
            answer=text,
            user_input=user_input,
            used_fallback=used_fallback,
        )
        learned_this_turn, learned_records = self._maybe_capture_auto_learning(
            project_id=project_id,
            config=auto_learning,
            user_input=user_input,
            chat_history=chat_history,
            answer=text,
            used_fallback=used_fallback,
            quality_score=quality_score,
            sources=top_docs,
        )
        trace_id = f"tr-{int(time.time() * 1000)}-{abs(hash((project_id, user_input, latency_ms))) % 1000000:06d}"

        trace = {
            "traceId": trace_id,
            "projectId": project_id,
            "input": user_input[:2000],
            "answer": text.strip()[:4000],
            "provider": provider,
            "model": model,
            "mode": mode,
            "usedFallback": used_fallback,
            "latencyMs": latency_ms,
            "qualityScore": quality_score,
            "documentsCount": len(documents),
            "historyCount": len(chat_history),
            "hfTask": generation.task,
            "localFilesOnly": local_files_only,
            "autoLearningEnabled": auto_learning.enabled,
            "autoLearningLearned": learned_this_turn,
            "autoLearningRecords": learned_records,
            "reasoningEnabled": reasoning.enabled,
            "reasoningApplied": bool(reasoning_diag.get("applied")),
            "createdAt": self._now_iso(),
        }
        self._append_infer_trace(project_id, trace)

        return InferResult(
            answer=text.strip(),
            sources=[{"id": d.get("id"), "title": d.get("title"), "score": d.get("score")} for d in top_docs],
            provider=provider,
            model=model,
            mode=mode,
            used_fallback=used_fallback,
            latency_ms=latency_ms,
            quality_score=quality_score,
            trace_id=trace_id,
            diagnostics={
                "documentsCount": len(documents),
                "historyCount": len(chat_history),
                "hfTask": generation.task,
                "localFilesOnly": local_files_only,
                "generation": {
                    "maxNewTokens": generation.max_new_tokens,
                    "temperature": generation.temperature,
                    "topP": generation.top_p,
                    "repetitionPenalty": generation.repetition_penalty,
                },
                "autoLearning": {
                    "enabled": auto_learning.enabled,
                    "captureFallback": auto_learning.capture_fallback,
                    "minQualityScore": auto_learning.min_quality_score,
                    "maxRecords": auto_learning.max_records,
                    "maxDocsForRetrieval": auto_learning.max_docs_for_retrieval,
                    "learnedThisTurn": learned_this_turn,
                    "records": learned_records,
                },
                "reasoning": reasoning_diag,
                "pipelineError": pipeline_error or None,
            },
        )

    def get_project_infer_metrics(
        self,
        project_id: int,
        *,
        take: int = 20,
        gate: Optional[QualityGateConfig] = None,
    ) -> dict[str, Any]:
        effective_take = max(1, min(200, int(take or 20)))

        if gate is None:
            gate = QualityGateConfig(
                enabled=bool(getattr(self._settings, "quality_gate_enabled", True)),
                min_samples=max(1, int(getattr(self._settings, "quality_gate_min_samples", 3))),
                min_success_rate=max(0.0, min(1.0, float(getattr(self._settings, "quality_gate_min_success_rate", 0.6)))),
                max_fallback_rate=max(0.0, min(1.0, float(getattr(self._settings, "quality_gate_max_fallback_rate", 0.4)))),
                max_avg_latency_ms=max(0, int(getattr(self._settings, "quality_gate_max_avg_latency_ms", 25000))),
            )

        traces = self._read_infer_traces(project_id, effective_take)
        total = len(traces)
        success_count = sum(1 for row in traces if not bool(row.get("usedFallback")) and str(row.get("answer") or "").strip())
        fallback_count = sum(1 for row in traces if bool(row.get("usedFallback")))

        latency_values = [int(row.get("latencyMs") or 0) for row in traces if int(row.get("latencyMs") or 0) > 0]
        quality_values = [float(row.get("qualityScore") or 0.0) for row in traces]

        success_rate = round((success_count / total), 4) if total else 0.0
        fallback_rate = round((fallback_count / total), 4) if total else 0.0
        avg_latency = int(sum(latency_values) / len(latency_values)) if latency_values else 0
        avg_quality = round(sum(quality_values) / len(quality_values), 4) if quality_values else 0.0

        summary = {
            "total": total,
            "successCount": success_count,
            "fallbackCount": fallback_count,
            "successRate": success_rate,
            "fallbackRate": fallback_rate,
            "avgLatencyMs": avg_latency,
            "avgQualityScore": avg_quality,
            "lastAt": traces[0].get("createdAt") if traces else None,
        }

        gate_info = self._evaluate_quality_gate(summary, gate)

        return {
            "projectId": project_id,
            "summary": summary,
            "gate": gate_info,
            "runs": traces,
            "createdAt": self._now_iso(),
        }

    def _resolve_model_provider(self, context: Any) -> str:
        mode = str(getattr(self._settings, "model_mode", "hf") or "").strip().lower()
        default_provider = "engine"
        if mode in {"ollama"}:
            default_provider = "ollama"

        if not isinstance(context, dict):
            return default_provider

        model_service = self._extract_model_service_config(context)
        candidate = str(model_service.get("provider") or model_service.get("type") or "").strip().lower()
        if candidate in {"engine", "hf", "huggingface", "transformers"}:
            return "engine"
        if candidate in {"ollama"}:
            return "ollama"
        if candidate in {"openai", "openai-compatible", "openai_compatible"}:
            # OpenAI-compatible provider is currently proxied through Ollama-compatible calls
            # when local deployment is requested.
            return "ollama"
        return default_provider

    def _resolve_hf_model_id(self, context: Any) -> str:
        aliases = {
            "assistant-general-v1": "google/flan-t5-base",
            "extractor-json-v1": "google/flan-t5-base",
            "transcriptor-audio-v1": "google/flan-t5-base",
            "vision-ocr-v1": "google/flan-t5-base",
            "facial-recognition-v1": "google/flan-t5-base",
            "reconocimiento-facial-v1": "google/flan-t5-base",
            "custom-local-v1": "google/flan-t5-base",
        }
        if not isinstance(context, dict):
            return self._settings.hf_model_id

        model_service = self._extract_model_service_config(context)
        candidate = str(model_service.get("model") or "").strip()

        if not candidate:
            return self._settings.hf_model_id

        key = candidate.lower()
        if key.startswith("hf:"):
            model_id = candidate[3:].strip()
            return model_id or self._settings.hf_model_id

        if "/" in candidate:
            return candidate

        return aliases.get(key, self._settings.hf_model_id)

    def _resolve_ollama_model_id(self, context: Any) -> str:
        default_model = str(getattr(self._settings, "ollama_model", "qwen2.5:7b") or "qwen2.5:7b").strip()
        if not isinstance(context, dict):
            return default_model

        model_service = self._extract_model_service_config(context)
        candidate = str(model_service.get("model") or "").strip()
        if not candidate:
            return default_model

        if candidate.lower().startswith("ollama:"):
            candidate = candidate.split(":", 1)[1].strip()
        if candidate.lower().startswith("hf:"):
            candidate = candidate.split(":", 1)[1].strip()

        hf_to_ollama = {
            "qwen/qwen2.5-7b-instruct": "qwen2.5:7b",
            "qwen/qwen2.5-3b-instruct": "qwen2.5:3b",
            "qwen/qwen2.5-1.5b-instruct": "qwen2.5:1.5b",
            "qwen/qwen2.5-0.5b-instruct": "qwen2.5:0.5b",
            "meta-llama/llama-3.2-3b-instruct": "llama3.2:3b",
        }
        mapped = hf_to_ollama.get(candidate.lower())
        if mapped:
            return mapped
        return candidate

    def _extract_system_prompt(self, context: Any) -> str:
        if isinstance(context, dict):
            if isinstance(context.get("systemPrompt"), str) and context["systemPrompt"].strip():
                return context["systemPrompt"].strip()

            model_service = self._extract_model_service_config(context)
            sp = model_service.get("systemPrompt") or model_service.get("prompt")
            if isinstance(sp, str) and sp.strip():
                return sp.strip()

        return self._settings.default_system_prompt

    def _extract_model_service_config(self, context: Any) -> dict[str, Any]:
        if not isinstance(context, dict):
            return {}

        def as_dict(value: Any) -> dict[str, Any]:
            return value if isinstance(value, dict) else {}

        aibase = as_dict(context.get("_aibase"))
        template = as_dict(aibase.get("template"))
        pipeline = as_dict(template.get("pipeline"))
        meta = as_dict(pipeline.get("meta"))
        runtime = as_dict(aibase.get("runtime"))

        from_meta = as_dict(meta.get("modelService") or meta.get("modelservice"))
        from_pipeline = as_dict(pipeline.get("modelService") or pipeline.get("modelservice"))
        from_runtime = as_dict(runtime.get("modelServiceResolved") or runtime.get("modelservice"))

        merged: dict[str, Any] = {}
        for source in (from_pipeline, from_meta, from_runtime):
            for key, value in source.items():
                if value is not None:
                    merged[key] = value
        return merged

    def _resolve_generation_config(self, context: Any, model_id: str) -> GenerationConfig:
        defaults = {
            "maxTokens": self._settings.hf_max_new_tokens,
            "temperature": self._settings.hf_temperature,
            "topP": self._settings.hf_top_p,
            "repetitionPenalty": max(0.9, min(2.0, float(getattr(self._settings, "hf_repetition_penalty", 1.05)))),
            "task": str(self._settings.hf_task or "").strip().lower(),
        }

        context_obj = context if isinstance(context, dict) else {}
        generation = context_obj.get("generation") if isinstance(context_obj.get("generation"), dict) else {}
        model_service = self._extract_model_service_config(context)

        merged = dict(defaults)

        for source in (model_service, generation, context_obj):
            if not isinstance(source, dict):
                continue
            for key in ("maxTokens", "maxNewTokens", "temperature", "topP", "top_p", "repetitionPenalty", "repetition_penalty", "task", "hfTask", "inferenceTask"):
                if key in source and source[key] is not None:
                    merged[key] = source[key]

        max_tokens = self._to_int(
            merged.get("maxNewTokens") if merged.get("maxNewTokens") is not None else merged.get("maxTokens"),
            self._settings.hf_max_new_tokens,
            32,
            4096,
        )
        temperature = self._to_float(merged.get("temperature"), self._settings.hf_temperature, 0.0, 2.0)
        top_p = self._to_float(
            merged.get("top_p") if merged.get("top_p") is not None else merged.get("topP"),
            self._settings.hf_top_p,
            0.05,
            1.0,
        )
        repetition_penalty = self._to_float(
            merged.get("repetition_penalty") if merged.get("repetition_penalty") is not None else merged.get("repetitionPenalty"),
            max(0.9, min(2.0, float(getattr(self._settings, "hf_repetition_penalty", 1.05)))),
            0.8,
            2.5,
        )

        task_candidate = str(
            merged.get("inferenceTask")
            or merged.get("hfTask")
            or merged.get("task")
            or ""
        ).strip().lower()

        allowed_tasks = {"text2text-generation", "text-generation"}
        if task_candidate not in allowed_tasks:
            model_key = str(model_id or "").lower()
            settings_model_key = str(self._settings.hf_model_id or "").strip().lower()
            is_default_model = not model_key or model_key == settings_model_key
            if is_default_model and str(self._settings.hf_task or "").strip().lower() in allowed_tasks:
                task_candidate = str(self._settings.hf_task or "").strip().lower()
            elif any(token in model_key for token in ("t5", "mt5", "bart")):
                task_candidate = "text2text-generation"
            else:
                task_candidate = "text-generation"

        return GenerationConfig(
            task=task_candidate,
            max_new_tokens=max_tokens,
            temperature=temperature,
            top_p=top_p,
            repetition_penalty=repetition_penalty,
        )

    def _resolve_local_files_only(self, context: Any) -> bool:
        fallback = bool(getattr(self._settings, "hf_local_files_only", True))
        if not isinstance(context, dict):
            return fallback

        model_service = self._extract_model_service_config(context)
        for source in (model_service, context):
            if not isinstance(source, dict):
                continue
            for key in ("localFilesOnly", "hfLocalFilesOnly", "local_files_only"):
                if key not in source:
                    continue
                raw = source.get(key)
                parsed = self._to_bool(raw, fallback=None)
                if parsed is not None:
                    return parsed
        return fallback

    def _resolve_auto_learning_config(self, context: Any) -> AutoLearningConfig:
        defaults = AutoLearningConfig(
            enabled=False,
            capture_fallback=True,
            min_quality_score=0.35,
            max_records=3000,
            max_docs_for_retrieval=300,
        )
        if not isinstance(context, dict):
            return defaults

        model_service = self._extract_model_service_config(context)
        if not isinstance(model_service, dict):
            return defaults

        raw_cfg = model_service.get("autoLearning") or model_service.get("autolearning")
        cfg = raw_cfg if isinstance(raw_cfg, dict) else {}

        enabled = self._to_bool(
            cfg.get("enabled", model_service.get("autoLearningEnabled")),
            fallback=defaults.enabled,
        )
        capture_fallback = self._to_bool(
            cfg.get(
                "captureFallback",
                cfg.get("allowFallback", model_service.get("autoLearningCaptureFallback")),
            ),
            fallback=defaults.capture_fallback,
        )
        min_quality = self._to_float(
            cfg.get("minQualityScore", model_service.get("autoLearningMinQualityScore")),
            defaults.min_quality_score,
            0.0,
            1.0,
        )
        max_records = self._to_int(
            cfg.get("maxRecords", model_service.get("autoLearningMaxRecords")),
            defaults.max_records,
            50,
            100000,
        )
        max_docs = self._to_int(
            cfg.get("maxDocsForRetrieval", model_service.get("autoLearningMaxDocsForRetrieval")),
            min(defaults.max_docs_for_retrieval, max_records),
            20,
            max_records,
        )

        return AutoLearningConfig(
            enabled=bool(enabled),
            capture_fallback=bool(capture_fallback),
            min_quality_score=float(min_quality),
            max_records=int(max_records),
            max_docs_for_retrieval=int(max_docs),
        )

    def _resolve_reasoning_config(self, context: Any) -> ReasoningConfig:
        defaults = ReasoningConfig(
            enabled=False,
            passes=1,
            use_verifier=True,
            min_self_score=0.55,
            max_plan_steps=4,
        )
        if not isinstance(context, dict):
            return defaults

        model_service = self._extract_model_service_config(context)
        if not isinstance(model_service, dict):
            return defaults

        raw_cfg = model_service.get("reasoning") or model_service.get("reasoningMode")
        cfg = raw_cfg if isinstance(raw_cfg, dict) else {}

        enabled = self._to_bool(
            cfg.get("enabled", model_service.get("reasoningEnabled")),
            fallback=defaults.enabled,
        )
        passes = self._to_int(
            cfg.get("passes", model_service.get("reasoningPasses")),
            defaults.passes,
            1,
            3,
        )
        use_verifier = self._to_bool(
            cfg.get("useVerifier", model_service.get("reasoningUseVerifier")),
            fallback=defaults.use_verifier,
        )
        min_self_score = self._to_float(
            cfg.get("minSelfScore", model_service.get("reasoningMinSelfScore")),
            defaults.min_self_score,
            0.0,
            1.0,
        )
        max_plan_steps = self._to_int(
            cfg.get("maxPlanSteps", model_service.get("reasoningMaxPlanSteps")),
            defaults.max_plan_steps,
            2,
            8,
        )

        return ReasoningConfig(
            enabled=bool(enabled),
            passes=int(passes),
            use_verifier=bool(use_verifier),
            min_self_score=float(min_self_score),
            max_plan_steps=int(max_plan_steps),
        )

    def _should_route_to_rules(self, *, user_input: str) -> bool:
        text = self._normalize_for_echo(user_input).replace("?", " ").strip()
        if not text:
            return True
        tokens = [tok for tok in text.split() if tok]
        if not tokens:
            return True

        phrase = " ".join(tokens)
        if phrase in {"hola", "buenas", "hello", "hi", "como estas", "que tal"}:
            return True
        if len(tokens) <= 2 and any(tok in {"hola", "buenas", "hello", "hi"} for tok in tokens):
            return True
        return False

    def _build_retrieval_query(self, *, user_input: str, chat_history: list[dict[str, str]]) -> str:
        current = str(user_input or "").strip()
        if not current:
            return ""
        normalized = self._normalize_for_echo(current)
        if not normalized:
            return current
        short_query = len([tok for tok in normalized.split() if tok]) <= 4

        # Follow-up turns often omit the topic ("y cómo lo aprendo?").
        # Re-anchor retrieval with the previous user utterance.
        is_followup = short_query or normalized.startswith("y ") or bool(
            re.search(r"\b(lo|eso|esto|esa|ese|aquello|anterior|mismo tema)\b", normalized)
        )
        if not is_followup:
            return current

        current_semantic = self._semantic_tokens(tokenize(normalized))
        pronoun_followup = bool(re.search(r"\b(lo|eso|esto|esa|ese|aquello|anterior|mismo tema)\b", normalized))

        previous_users: list[str] = []
        for message in reversed(chat_history or []):
            if str(message.get("role") or "").lower() != "user":
                continue
            candidate = str(message.get("content") or "").strip()
            if not candidate:
                continue
            if self._normalize_for_echo(candidate) == normalized:
                continue
            # Prevent context pollution: only anchor with prior turns that share topical tokens,
            # unless the current turn is explicitly pronominal ("y eso?", "y lo anterior?").
            if current_semantic and not pronoun_followup:
                cand_norm = self._normalize_for_echo(candidate)
                cand_semantic = self._semantic_tokens(tokenize(cand_norm))
                if cand_semantic and not (cand_semantic & current_semantic):
                    continue
            previous_users.append(candidate)
            if len(previous_users) >= 3:
                break

        if not previous_users:
            return current

        anchors: list[str] = []
        for candidate in previous_users:
            norm = self._normalize_for_echo(candidate)
            # Skip ultra-short follow-ups with no topical signal.
            if (
                len(tokenize(norm)) < 2
                and re.search(r"\b(lo|eso|esto|asi)\b", norm)
                and not re.search(r"\baprend\w*\b", norm)
            ):
                continue
            anchors.append(candidate)
            if len(anchors) >= 2:
                break
        if not anchors:
            anchors = previous_users[:1]

        anchor_text = " ".join(reversed(anchors)).strip()
        if not anchor_text:
            return current
        return f"{current} {anchor_text}".strip()

    def _extract_chat_history(self, context: Any) -> list[dict[str, str]]:
        items: list[dict[str, str]] = []
        candidates: Any = None
        if isinstance(context, dict):
            candidates = context.get("chatHistory") or context.get("messages")
            if not candidates and isinstance(context.get("userContext"), dict):
                nested = context["userContext"]
                candidates = nested.get("chatHistory") or nested.get("messages")

        if isinstance(candidates, list):
            for row in candidates[-20:]:
                if not isinstance(row, dict):
                    continue
                role = str(row.get("role", "user")).strip().lower()
                if role not in {"assistant", "user", "system"}:
                    role = "user"
                content = row.get("text") if "text" in row else row.get("content")
                text = str(content or "").strip()
                if text:
                    items.append({"role": role, "content": text})

        return items

    def _extract_documents(
        self,
        project_id: int,
        context: Any,
        *,
        auto_learning: Optional[AutoLearningConfig] = None,
    ) -> list[dict[str, str]]:
        docs: list[dict[str, str]] = []

        def push_doc(doc_id: str, title: str, text: str) -> None:
            if not text.strip():
                return
            docs.append({"id": doc_id, "title": title, "text": text.strip()})

        if isinstance(context, dict):
            collections = []
            collections.append(context.get("documents"))
            collections.append(context.get("knowledge"))
            rag = context.get("rag")
            if isinstance(rag, dict):
                collections.append(rag.get("documents"))

            user_ctx = context.get("userContext")
            if isinstance(user_ctx, dict):
                collections.append(user_ctx.get("documents"))
                nested_rag = user_ctx.get("rag")
                if isinstance(nested_rag, dict):
                    collections.append(nested_rag.get("documents"))

            for block in collections:
                if not isinstance(block, list):
                    continue
                for idx, item in enumerate(block, start=1):
                    if isinstance(item, str):
                        push_doc(f"ctx-{idx}", f"Documento {idx}", item)
                        continue
                    if isinstance(item, dict):
                        text = str(item.get("text") or item.get("content") or item.get("body") or "").strip()
                        if not text:
                            continue
                        doc_id = str(item.get("id") or item.get("docId") or f"ctx-{idx}")
                        title = str(item.get("title") or item.get("name") or f"Documento {idx}")
                        push_doc(doc_id, title, text)

        project_docs = self._load_project_docs(project_id)
        docs.extend(project_docs)
        if auto_learning and auto_learning.enabled:
            docs.extend(
                self._load_auto_learning_docs(
                    project_id=project_id,
                    limit=auto_learning.max_docs_for_retrieval,
                )
            )
        return docs

    def _load_project_docs(self, project_id: int) -> list[dict[str, str]]:
        docs_path = self._settings.storage_root / "projects" / str(project_id) / "rag" / "documents.json"
        if not docs_path.exists():
            return []

        try:
            data = json.loads(docs_path.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            return []

        if not isinstance(data, list):
            return []

        docs: list[dict[str, str]] = []
        for idx, item in enumerate(data, start=1):
            if not isinstance(item, dict):
                continue
            text = str(item.get("text") or "").strip()
            if not text:
                continue
            docs.append(
                {
                    "id": str(item.get("id") or f"file-{idx}"),
                    "title": str(item.get("title") or f"Documento {idx}"),
                    "text": text,
                }
            )
        return docs

    def _select_top_documents(self, query: str, documents: list[dict[str, str]]) -> list[dict[str, Any]]:
        query_tokens_raw = tokenize(query)
        query_tokens = self._semantic_tokens(query_tokens_raw)
        if not query_tokens:
            query_tokens = query_tokens_raw
        if not query_tokens:
            return []

        if len(query_tokens) <= 2:
            min_score = 0.5
        elif len(query_tokens) <= 4:
            min_score = 0.34
        else:
            min_score = 0.25

        query_norm = self._normalize_for_echo(query)
        intent = self._detect_query_intent(query_norm)
        intent_hints = self._intent_hint_tokens(intent)
        query_focus = query_tokens | intent_hints

        def collect(min_required: float) -> list[dict[str, Any]]:
            matches: list[dict[str, Any]] = []
            for doc in documents:
                text = doc.get("text", "")
                text_tokens_raw = tokenize(text)
                text_tokens = self._semantic_tokens(text_tokens_raw)
                if not text_tokens:
                    text_tokens = text_tokens_raw
                if not text_tokens:
                    continue
                score = overlap_score(query, text)
                if score < min_required:
                    continue
                common = query_tokens & text_tokens
                hint_hits = len(intent_hints & text_tokens)
                focus_hits = len(query_focus & text_tokens)
                # For longer questions, one-token overlap is too weak and creates noisy matches.
                if len(query_tokens) >= 5 and len(common) < 2 and focus_hits < 2:
                    continue
                title_tokens = self._semantic_tokens(tokenize(str(doc.get("title") or "")))
                title_hits = len(query_tokens & title_tokens)

                rank_score = score
                if hint_hits:
                    rank_score += min(0.18, hint_hits * 0.05)
                if title_hits:
                    rank_score += min(0.06, title_hits * 0.03)
                if intent == "use_case" and any(tok in text_tokens for tok in {"usar", "usado", "uso", "aplicaciones", "aplicacion", "automatizacion", "datos", "web", "ia"}):
                    rank_score += 0.08
                if intent == "definition" and any(chunk in self._normalize_for_echo(text[:240]) for chunk in {" es un ", " es una ", " se define ", " consiste en "}):
                    rank_score += 0.06
                if intent == "learn" and any(tok in text_tokens for tok in {"aprender", "practica", "ejercicio", "curso", "tutorial", "proyecto"}):
                    rank_score += 0.08
                if str(doc.get("id") or "").startswith("memory-"):
                    rank_score -= 0.08

                matches.append(
                    {
                        "id": doc.get("id", ""),
                        "title": doc.get("title", "Documento"),
                        "text": text,
                        "score": round(score, 4),
                        "rankScore": round(rank_score, 4),
                        "commonCount": len(common),
                        "focusHits": focus_hits,
                        "hintHits": hint_hits,
                    }
                )
            return matches

        scored = collect(min_score)
        # Second-turn and natural-language follow-up queries often add extra tokens
        # ("que puedo hacer con python"), which can hide otherwise relevant docs.
        # If strict retrieval yields no docs, relax threshold to "at least one matched token".
        if not scored and len(query_tokens) >= 2:
            relaxed_min = max(0.1, 1.0 / float(len(query_tokens)))
            relaxed = collect(relaxed_min)
            if len(query_tokens) >= 4:
                relaxed = [
                    row for row in relaxed
                    if int(row.get("commonCount") or 0) >= 2 or int(row.get("focusHits") or 0) >= 2
                ]
            scored = relaxed

        scored.sort(
            key=lambda item: (
                float(item.get("rankScore") or 0.0),
                int(item.get("focusHits") or 0),
                int(item.get("commonCount") or 0),
                float(item.get("score") or 0.0),
            ),
            reverse=True,
        )
        if not scored:
            return []

        # Avoid "copy/paste" behavior where conversational memory dominates retrieval.
        # Prefer project/RAG docs first; memory complements as backup context.
        non_memory = [row for row in scored if not str(row.get("id") or "").startswith("memory-")]
        memory = [row for row in scored if str(row.get("id") or "").startswith("memory-")]

        selected: list[dict[str, Any]] = []
        if non_memory:
            selected.extend(non_memory[:4])
            if len(selected) < 4 and memory:
                selected.append(memory[0])
                selected = selected[:4]
            return selected
        return memory[:4]

    def _semantic_tokens(self, tokens: set[str]) -> set[str]:
        if not tokens:
            return set()
        stopwords = {
            "de", "del", "la", "las", "el", "los", "un", "una", "unos", "unas",
            "y", "o", "u", "en", "con", "sin", "por", "para", "a", "al",
            "que", "como", "cual", "cuales", "quien", "quienes", "donde",
            "cuando", "porque", "sobre", "entre", "se", "es", "son", "ser",
            "lo", "le", "les", "su", "sus", "mi", "mis", "tu", "tus", "ya",
            "me", "te", "nos", "vos", "ustedes", "esto", "eso", "esta", "este",
            "si", "no",
        }
        keep_short = {"ia", "ml", "nlp", "api", "sql", "ui", "ux"}
        result: set[str] = set()
        for token in tokens:
            t = str(token or "").strip().lower()
            if not t:
                continue
            if t in keep_short:
                result.add(t)
                continue
            if len(t) <= 2:
                continue
            if t in stopwords:
                continue
            result.add(t)
        return result

    def _build_prompt(
        self,
        *,
        user_input: str,
        system_prompt: str,
        chat_history: list[dict[str, str]],
        top_docs: list[dict[str, Any]],
        model_task: str,
    ) -> str:
        if model_task == "text2text-generation":
            return self._build_text2text_prompt(
                user_input=user_input,
                system_prompt=system_prompt,
                chat_history=chat_history,
                top_docs=top_docs,
            )
        return self._build_chat_prompt(
            user_input=user_input,
            system_prompt=system_prompt,
            chat_history=chat_history,
            top_docs=top_docs,
        )

    def _build_chat_prompt(
        self,
        *,
        user_input: str,
        system_prompt: str,
        chat_history: list[dict[str, str]],
        top_docs: list[dict[str, Any]],
    ) -> str:
        parts = [f"SYSTEM:\n{system_prompt.strip()}"]

        if top_docs:
            context_lines = []
            for doc in top_docs:
                doc_text = self._compact_doc_text_for_prompt(str(doc.get("text") or ""))
                context_lines.append(
                    f"- [{doc.get('id')}] {doc.get('title')} :: {doc_text}"
                )
            parts.append("CONTEXTO DISPONIBLE:\n" + "\n".join(context_lines))

        if chat_history:
            history_lines = []
            for message in chat_history[-10:]:
                history_lines.append(f"{message['role'].upper()}: {message['content']}")
            parts.append("HISTORIAL:\n" + "\n".join(history_lines))

        parts.append("USUARIO:\n" + user_input.strip())
        parts.append("ASISTENTE:")
        return "\n\n".join(parts)

    def _build_text2text_prompt(
        self,
        *,
        user_input: str,
        system_prompt: str,
        chat_history: list[dict[str, str]],
        top_docs: list[dict[str, Any]],
    ) -> str:
        parts = [f"Instrucción: {system_prompt.strip().replace(chr(10), ' ')}"]

        if top_docs:
            context_lines = []
            for doc in top_docs:
                doc_text = self._compact_doc_text_for_prompt(str(doc.get("text") or ""))
                context_lines.append(f"- {doc.get('title')} :: {doc_text}")
            parts.append("Contexto:\n" + "\n".join(context_lines))

        if chat_history:
            history_lines = []
            for message in chat_history[-6:]:
                role = "Usuario" if message["role"] == "user" else "Asistente"
                history_lines.append(f"{role}: {message['content']}")
            parts.append("Historial reciente:\n" + "\n".join(history_lines))

        parts.append("Pregunta: " + user_input.strip())
        parts.append("Respuesta:")
        return "\n\n".join(parts)

    def _build_retry_prompt(
        self,
        *,
        model_task: str,
        user_input: str,
        top_docs: list[dict[str, Any]],
    ) -> str:
        if model_task == "text2text-generation":
            if top_docs:
                context_line = f"Contexto: {top_docs[0].get('text', '')}"
                return (
                    "Responde en español, de forma breve y directa.\n"
                    f"{context_line}\n"
                    f"Pregunta: {user_input.strip()}\n"
                    "Respuesta:"
                )
            return (
                "Responde en español, de forma breve y directa.\n"
                f"Pregunta: {user_input.strip()}\n"
                "Respuesta:"
            )

        parts = [
            "SYSTEM:\nResponde en español, de forma breve y directa.",
            "USUARIO:\n" + user_input.strip(),
            "ASISTENTE:",
        ]
        return "\n\n".join(parts)

    def _generate_with_hf(
        self,
        *,
        prompt: str,
        model_id: str,
        generation: GenerationConfig,
        local_files_only: bool,
        system_prompt: str,
        fallback_input: str,
        fallback_docs: list[dict[str, Any]],
        fallback_chat_history: list[dict[str, str]],
        reasoning: ReasoningConfig,
    ) -> tuple[str, bool, dict[str, Any]]:
        reasoning_diag: dict[str, Any] = {
            "enabled": reasoning.enabled,
            "applied": False,
            "passes": reasoning.passes,
            "passesApplied": 0,
            "useVerifier": reasoning.use_verifier,
            "verifierApplied": False,
            "verifierScore": None,
            "minSelfScore": reasoning.min_self_score,
            "maxPlanSteps": reasoning.max_plan_steps,
            "planUsed": False,
            "reason": "disabled",
        }
        pipe = self._get_pipeline(model_id, generation.task, local_files_only)
        if pipe is None:
            text, _ = self._generate_with_rules(
                fallback_input,
                fallback_docs,
                chat_history=fallback_chat_history,
            )
            reasoning_diag["reason"] = "pipeline_unavailable"
            return text, True, reasoning_diag

        try:
            cleaned = self._run_hf_prompt(
                pipe=pipe,
                prompt=prompt,
                generation=generation,
                system_prompt=system_prompt,
                user_input=fallback_input,
            )
            if not cleaned:
                retry_prompt = self._build_retry_prompt(
                    model_task=generation.task,
                    user_input=fallback_input,
                    top_docs=fallback_docs,
                )
                cleaned = self._run_hf_prompt(
                    pipe=pipe,
                    prompt=retry_prompt,
                    generation=generation,
                    system_prompt=system_prompt,
                    user_input=fallback_input,
                )

            if cleaned:
                if reasoning.enabled:
                    refined, diag = self._run_reasoning_pipeline(
                        pipe=pipe,
                        generation=generation,
                        system_prompt=system_prompt,
                        user_input=fallback_input,
                        top_docs=fallback_docs,
                        chat_history=fallback_chat_history,
                        draft_answer=cleaned,
                        config=reasoning,
                    )
                    return refined, False, diag
                reasoning_diag["reason"] = "disabled"
                return cleaned, False, reasoning_diag

            text, _ = self._generate_with_rules(
                fallback_input,
                fallback_docs,
                chat_history=fallback_chat_history,
            )
            reasoning_diag["reason"] = "empty_hf_output"
            return text, True, reasoning_diag
        except Exception as ex:  # pragma: no cover - defensive runtime path
            key = f"{self._pipeline_key(model_id, generation.task)}::local={1 if local_files_only else 0}"
            self._pipeline_errors[key] = str(ex)
            text, _ = self._generate_with_rules(
                fallback_input,
                fallback_docs,
                chat_history=fallback_chat_history,
            )
            reasoning_diag["reason"] = "hf_exception"
            return text, True, reasoning_diag

    def _resolve_ollama_target(self, context: Any) -> tuple[list[str], str]:
        model_service = self._extract_model_service_config(context) if isinstance(context, dict) else {}
        base_url = str(
            model_service.get("baseUrl")
            or model_service.get("url")
            or model_service.get("endpoint")
            or getattr(self._settings, "ollama_base_url", "http://host.docker.internal:11434")
            or ""
        ).strip()
        path = str(
            model_service.get("path")
            or model_service.get("apiPath")
            or getattr(self._settings, "ollama_path", "/api/generate")
            or "/api/generate"
        ).strip()
        if not path.startswith("/"):
            path = f"/{path}"

        candidates: list[str] = []

        def push(url: str) -> None:
            raw = str(url or "").strip()
            if not raw:
                return
            if not raw.startswith("http://") and not raw.startswith("https://"):
                raw = f"http://{raw}"
            normalized = raw.rstrip("/")
            if normalized and normalized not in candidates:
                candidates.append(normalized)

        push(base_url)
        if "localhost" in base_url or "127.0.0.1" in base_url:
            push(base_url.replace("localhost", "host.docker.internal").replace("127.0.0.1", "host.docker.internal"))
        push("http://host.docker.internal:11434")
        push("http://localhost:11434")

        return candidates, path

    def _ollama_options(self, generation: GenerationConfig) -> dict[str, Any]:
        return {
            "temperature": float(generation.temperature),
            "top_p": float(generation.top_p),
            "repeat_penalty": float(generation.repetition_penalty),
            "num_predict": int(generation.max_new_tokens),
        }

    def _ollama_request(
        self,
        *,
        url: str,
        payload: dict[str, Any],
        timeout_sec: int,
    ) -> dict[str, Any]:
        body = json.dumps(payload).encode("utf-8")
        req = urllib.request.Request(
            url=url,
            data=body,
            headers={"Content-Type": "application/json"},
            method="POST",
        )
        with urllib.request.urlopen(req, timeout=timeout_sec) as response:
            raw = response.read().decode("utf-8", errors="ignore")
        parsed = json.loads(raw or "{}")
        return parsed if isinstance(parsed, dict) else {}

    def _generate_with_ollama(
        self,
        *,
        prompt: str,
        model_id: str,
        generation: GenerationConfig,
        system_prompt: str,
        user_input: str,
        top_docs: list[dict[str, Any]],
        chat_history: list[dict[str, str]],
        reasoning: ReasoningConfig,
        context: Any,
    ) -> tuple[str, bool, dict[str, Any]]:
        diag: dict[str, Any] = {
            "enabled": reasoning.enabled,
            "applied": False,
            "passes": reasoning.passes,
            "passesApplied": 0,
            "useVerifier": reasoning.use_verifier,
            "verifierApplied": False,
            "verifierScore": None,
            "minSelfScore": reasoning.min_self_score,
            "maxPlanSteps": reasoning.max_plan_steps,
            "planUsed": False,
            "reason": "provider_ollama",
        }

        candidates, path = self._resolve_ollama_target(context)
        timeout_sec = int(getattr(self._settings, "ollama_timeout_sec", 180))
        errors: list[str] = []

        for base in candidates:
            target = f"{base}{path}"
            try:
                if path.endswith("/api/chat"):
                    messages: list[dict[str, str]] = []
                    if system_prompt.strip():
                        messages.append({"role": "system", "content": system_prompt.strip()})
                    for item in chat_history[-8:]:
                        role = str(item.get("role") or "").strip().lower()
                        if role not in {"user", "assistant"}:
                            continue
                        content = str(item.get("content") or "").strip()
                        if content:
                            messages.append({"role": role, "content": content})
                    messages.append({"role": "user", "content": user_input.strip()})
                    payload = {
                        "model": model_id,
                        "messages": messages,
                        "stream": False,
                        "options": self._ollama_options(generation),
                    }
                else:
                    payload = {
                        "model": model_id,
                        "prompt": prompt,
                        "system": system_prompt.strip(),
                        "stream": False,
                        "options": self._ollama_options(generation),
                    }

                data = self._ollama_request(url=target, payload=payload, timeout_sec=timeout_sec)
                if path.endswith("/api/chat"):
                    message = data.get("message") if isinstance(data.get("message"), dict) else {}
                    raw_text = str(message.get("content") or "").strip()
                else:
                    raw_text = str(data.get("response") or "").strip()

                cleaned = self._sanitize_generated_text(
                    text=raw_text,
                    system_prompt=system_prompt,
                    user_input=user_input,
                )
                if not cleaned:
                    cleaned = self._best_effort_generated_text(
                        text=raw_text,
                        user_input=user_input,
                        system_prompt=system_prompt,
                    )
                if cleaned:
                    self._provider_errors.pop(f"ollama::{model_id}", None)
                    return cleaned, False, diag
                errors.append(f"{target}: respuesta vacía")
            except urllib.error.HTTPError as ex:
                detail = ex.read().decode("utf-8", errors="ignore")[:500]
                errors.append(f"{target}: HTTP {ex.code} {detail}")
            except Exception as ex:  # pragma: no cover
                errors.append(f"{target}: {ex}")

        fallback_text, _ = self._generate_with_rules(
            user_input,
            top_docs,
            chat_history=chat_history,
        )
        err_text = " | ".join(errors[:3]) if errors else "No se pudo conectar a Ollama."
        self._provider_errors[f"ollama::{model_id}"] = err_text
        diag["reason"] = "ollama_unavailable"
        return fallback_text, True, diag

    def _hf_kwargs(self, generation: GenerationConfig, *, max_new_tokens: Optional[int] = None) -> dict[str, Any]:
        return {
            "max_new_tokens": int(max_new_tokens or generation.max_new_tokens),
            "do_sample": generation.temperature > 0,
            "temperature": max(0.01, generation.temperature),
            "top_p": generation.top_p,
            "repetition_penalty": generation.repetition_penalty,
        }

    def _run_hf_prompt(
        self,
        *,
        pipe: Any,
        prompt: str,
        generation: GenerationConfig,
        system_prompt: str,
        user_input: str,
    ) -> str:
        raw = self._invoke_hf(
            pipe=pipe,
            prompt=prompt,
            generation=generation,
        )
        cleaned = self._sanitize_generated_text(
            text=raw,
            system_prompt=system_prompt,
            user_input=user_input,
        )
        if cleaned:
            return cleaned
        return self._best_effort_generated_text(
            text=raw,
            user_input=user_input,
            system_prompt=system_prompt,
        )

    def _invoke_hf(
        self,
        *,
        pipe: Any,
        prompt: str,
        generation: GenerationConfig,
        max_new_tokens: Optional[int] = None,
    ) -> str:
        result = pipe(prompt, **self._hf_kwargs(generation, max_new_tokens=max_new_tokens))
        return self._extract_hf_text(result, prompt)

    def _run_reasoning_pipeline(
        self,
        *,
        pipe: Any,
        generation: GenerationConfig,
        system_prompt: str,
        user_input: str,
        top_docs: list[dict[str, Any]],
        chat_history: list[dict[str, str]],
        draft_answer: str,
        config: ReasoningConfig,
    ) -> tuple[str, dict[str, Any]]:
        diag: dict[str, Any] = {
            "enabled": True,
            "applied": False,
            "passes": config.passes,
            "passesApplied": 0,
            "useVerifier": config.use_verifier,
            "verifierApplied": False,
            "verifierScore": None,
            "minSelfScore": config.min_self_score,
            "maxPlanSteps": config.max_plan_steps,
            "planUsed": False,
            "reason": "no_improvement",
        }
        current = str(draft_answer or "").strip()
        if not current:
            diag["reason"] = "empty_draft"
            return draft_answer, diag

        plan_prompt = self._build_reasoning_plan_prompt(
            model_task=generation.task,
            user_input=user_input,
            top_docs=top_docs,
            chat_history=chat_history,
            max_steps=config.max_plan_steps,
        )
        plan_raw = self._invoke_hf(
            pipe=pipe,
            prompt=plan_prompt,
            generation=generation,
            max_new_tokens=min(max(64, generation.max_new_tokens // 2), 240),
        )
        plan = self._sanitize_reasoning_plan(plan_raw, max_steps=config.max_plan_steps)
        if plan:
            diag["planUsed"] = True
        else:
            diag["reason"] = "plan_empty"

        for _ in range(max(1, config.passes)):
            refine_prompt = self._build_reasoning_refine_prompt(
                model_task=generation.task,
                system_prompt=system_prompt,
                user_input=user_input,
                top_docs=top_docs,
                chat_history=chat_history,
                draft_answer=current,
                plan=plan,
            )
            candidate = self._run_hf_prompt(
                pipe=pipe,
                prompt=refine_prompt,
                generation=generation,
                system_prompt=system_prompt,
                user_input=user_input,
            )
            if not candidate:
                continue
            if self._is_reasoning_candidate_better(candidate, current, user_input=user_input):
                current = candidate
                diag["passesApplied"] = int(diag["passesApplied"] or 0) + 1

        if config.use_verifier:
            verify_prompt = self._build_reasoning_verify_prompt(
                model_task=generation.task,
                user_input=user_input,
                answer=current,
                top_docs=top_docs,
            )
            verify_raw = self._invoke_hf(
                pipe=pipe,
                prompt=verify_prompt,
                generation=generation,
                max_new_tokens=min(max(72, generation.max_new_tokens // 2), 280),
            )
            verifier_score, verifier_answer = self._parse_reasoning_verifier(
                verify_raw,
                system_prompt=system_prompt,
                user_input=user_input,
            )
            diag["verifierScore"] = verifier_score
            if verifier_answer and verifier_score is not None and verifier_score >= config.min_self_score:
                if self._is_reasoning_candidate_better(verifier_answer, current, user_input=user_input, allow_equal=True):
                    current = verifier_answer
                    diag["verifierApplied"] = True

        diag["applied"] = bool(diag["passesApplied"] or diag["verifierApplied"])
        if diag["applied"]:
            diag["reason"] = "refined"
        return current, diag

    def _build_reasoning_plan_prompt(
        self,
        *,
        model_task: str,
        user_input: str,
        top_docs: list[dict[str, Any]],
        chat_history: list[dict[str, str]],
        max_steps: int,
    ) -> str:
        context = self._reasoning_context_block(top_docs=top_docs, chat_history=chat_history)
        if model_task == "text2text-generation":
            return (
                f"Instrucción: Armá un plan breve (máximo {max_steps} pasos) para responder la pregunta. "
                "No des la respuesta final, solo el plan.\n"
                f"{context}\n"
                f"Pregunta: {user_input.strip()}\n"
                "Plan:"
            )
        return (
            "SYSTEM:\nCreá un plan breve para responder. No des respuesta final.\n\n"
            f"CONTEXTO:\n{context}\n\n"
            f"USUARIO:\n{user_input.strip()}\n\n"
            "ASISTENTE:\nPlan:"
        )

    def _build_reasoning_refine_prompt(
        self,
        *,
        model_task: str,
        system_prompt: str,
        user_input: str,
        top_docs: list[dict[str, Any]],
        chat_history: list[dict[str, str]],
        draft_answer: str,
        plan: str,
    ) -> str:
        context = self._reasoning_context_block(top_docs=top_docs, chat_history=chat_history)
        plan_block = f"Plan interno:\n{plan}\n" if plan else ""
        if model_task == "text2text-generation":
            return (
                f"Instrucción: {system_prompt.strip().replace(chr(10), ' ')}\n"
                "Reescribí y mejorá la respuesta usando razonamiento interno. "
                "Devolvé solo la respuesta final en español, clara y útil.\n"
                f"{plan_block}"
                f"{context}\n"
                f"Pregunta: {user_input.strip()}\n"
                f"Borrador actual: {draft_answer.strip()}\n"
                "Respuesta final:"
            )
        return (
            f"SYSTEM:\n{system_prompt.strip()}\n"
            "Mejorá el borrador con razonamiento interno. Devolvé solo respuesta final.\n\n"
            f"{plan_block}"
            f"CONTEXTO:\n{context}\n\n"
            f"USUARIO:\n{user_input.strip()}\n\n"
            f"BORRADOR:\n{draft_answer.strip()}\n\n"
            "ASISTENTE:"
        )

    def _build_reasoning_verify_prompt(
        self,
        *,
        model_task: str,
        user_input: str,
        answer: str,
        top_docs: list[dict[str, Any]],
    ) -> str:
        context = self._reasoning_context_block(top_docs=top_docs, chat_history=[])
        base = (
            "Evaluá si la respuesta contesta la pregunta sin contradicciones.\n"
            "Formato obligatorio:\n"
            "SCORE: <0-1>\n"
            "FINAL: <respuesta final corregida o igual>\n"
        )
        if model_task == "text2text-generation":
            return (
                f"Instrucción: {base}\n"
                f"{context}\n"
                f"Pregunta: {user_input.strip()}\n"
                f"Respuesta propuesta: {answer.strip()}\n"
                "Resultado:"
            )
        return (
            "SYSTEM:\n" + base + "\n"
            f"CONTEXTO:\n{context}\n\n"
            f"USUARIO:\nPregunta: {user_input.strip()}\nRespuesta propuesta: {answer.strip()}\n\n"
            "ASISTENTE:"
        )

    def _reasoning_context_block(
        self,
        *,
        top_docs: list[dict[str, Any]],
        chat_history: list[dict[str, str]],
    ) -> str:
        lines: list[str] = []
        if top_docs:
            for doc in top_docs[:3]:
                text = self._compact_doc_text_for_prompt(str(doc.get("text") or ""))
                if text:
                    lines.append(f"- {doc.get('title')}: {text}")
        if chat_history:
            for item in chat_history[-4:]:
                role = "Usuario" if str(item.get("role") or "").lower() == "user" else "Asistente"
                content = str(item.get("content") or "").strip()
                if content:
                    lines.append(f"{role}: {content}")
        return "\n".join(lines) if lines else "Sin contexto adicional."

    def _sanitize_reasoning_plan(self, text: str, *, max_steps: int) -> str:
        raw = str(text or "").strip()
        if not raw:
            return ""
        cleaned = raw.replace("\r\n", "\n").replace("\r", "\n")
        lines: list[str] = []
        for line in cleaned.splitlines():
            item = line.strip(" -*\t")
            if not item:
                continue
            if item.lower().startswith(("system:", "usuario:", "asistente:", "respuesta:")):
                continue
            lines.append(item)
            if len(lines) >= max_steps:
                break
        return "\n".join(lines).strip()[:800]

    def _parse_reasoning_verifier(
        self,
        raw: str,
        *,
        system_prompt: str,
        user_input: str,
    ) -> tuple[Optional[float], str]:
        text = str(raw or "").strip()
        if not text:
            return None, ""
        score: Optional[float] = None
        score_match = re.search(r"score\s*:\s*([01](?:\.\d+)?)", text, flags=re.IGNORECASE)
        if score_match:
            try:
                score = max(0.0, min(1.0, float(score_match.group(1))))
            except ValueError:
                score = None
        final_match = re.search(r"final\s*:\s*(.+)$", text, flags=re.IGNORECASE | re.DOTALL)
        final_raw = final_match.group(1).strip() if final_match else text
        final_clean = self._sanitize_generated_text(
            text=final_raw,
            system_prompt=system_prompt,
            user_input=user_input,
        )
        if not final_clean:
            final_clean = self._best_effort_generated_text(
                text=final_raw,
                user_input=user_input,
                system_prompt=system_prompt,
            )
        return score, final_clean

    def _is_reasoning_candidate_better(
        self,
        candidate: str,
        current: str,
        *,
        user_input: str,
        allow_equal: bool = False,
    ) -> bool:
        cand = str(candidate or "").strip()
        cur = str(current or "").strip()
        if not cand:
            return False
        if self._is_generic_fallback_text(cand):
            return False
        if self._is_question_echo(cand, user_input):
            return False
        cand_score = self._estimate_quality_score(answer=cand, user_input=user_input, used_fallback=False)
        cur_score = self._estimate_quality_score(answer=cur, user_input=user_input, used_fallback=False)
        if cand_score > cur_score + 0.03:
            return True
        if allow_equal and cand_score >= cur_score and len(cand.split()) >= max(6, int(len(cur.split()) * 0.65)):
            return True
        return False

    def _extract_hf_text(self, result: Any, prompt: str) -> str:
        if isinstance(result, list) and result:
            row = result[0]
            if isinstance(row, dict):
                text = str(row.get("generated_text") or row.get("summary_text") or "").strip()
                if text.startswith(prompt):
                    text = text[len(prompt):].strip()
                return text
        return str(result).strip()

    def _sanitize_generated_text(self, *, text: str, system_prompt: str, user_input: str) -> str:
        cleaned = str(text or "").strip()
        if not cleaned:
            return ""

        lines: list[str] = []
        for raw in cleaned.splitlines():
            line = raw.strip()
            if not line:
                continue
            upper = line.upper()
            if upper.startswith(
                (
                    "SYSTEM:",
                    "USUARIO:",
                    "USER:",
                    "HISTORIAL:",
                    "CONTEXTO:",
                    "CONTEXTO DISPONIBLE:",
                    "INSTRUCCIÓN:",
                    "INSTRUCCION:",
                    "INSTRUCTION:",
                    "PREGUNTA:",
                )
            ):
                continue
            if upper.startswith(("ASISTENTE:", "ASSISTANT:", "RESPUESTA:", "ANSWER:")):
                line = line.split(":", 1)[1].strip()
            if line:
                lines.append(line)

        if lines:
            cleaned = "\n".join(lines).strip()

        normalized = " ".join(cleaned.lower().split())
        normalized_system = " ".join(str(system_prompt or "").lower().split())
        normalized_user = " ".join(str(user_input or "").lower().split())

        if not normalized:
            return ""
        if normalized_system and (normalized == normalized_system or normalized in normalized_system):
            return ""
        if normalized_system and normalized_system in normalized:
            return ""
        if normalized_user and self._is_question_echo(cleaned, user_input):
            return ""
        if self._looks_like_prompt_leak(cleaned, normalized):
            return ""
        if self._looks_like_low_value_answer(cleaned, user_input):
            return ""
        return cleaned

    def _looks_like_prompt_leak(self, text: str, normalized: str) -> bool:
        if "historial reciente:" in normalized or "contexto disponible:" in normalized:
            return True
        if "usuario:" in normalized and "asistente:" in normalized:
            return True
        if "instruccion:" in normalized or "instruction:" in normalized:
            return True
        prompt_signals = (
            "es un asistente",
            "eres un asistente",
            "sos un asistente",
        )
        if any(signal in normalized for signal in prompt_signals):
            # Allow genuine assistant intros only when they are short and complete.
            words = [w for w in normalized.split() if w]
            if len(words) > 8:
                return True
        # Multi-turn prompt dumps usually contain this separator pattern.
        if " | " in text and text.count("|") >= 2:
            return True
        return False

    def _looks_like_low_value_answer(self, text: str, user_input: str) -> bool:
        candidate = str(text or "").strip()
        if not candidate:
            return True
        # Reject tiny question-like echoes such as "Hay hola?".
        cand_norm = self._normalize_for_echo(candidate).replace("?", " ").strip()
        user_norm = self._normalize_for_echo(user_input).replace("?", " ").strip()
        cand_tokens = [tok for tok in cand_norm.split() if tok]
        user_tokens = [tok for tok in user_norm.split() if tok]
        if candidate.endswith("?") and len(cand_tokens) <= 4 and user_tokens:
            user_set = set(user_tokens)
            overlap = sum(1 for tok in cand_tokens if tok in user_set) / float(len(user_set))
            if overlap >= 0.75:
                return True
        if candidate.endswith("?") and len(cand_tokens) <= 3:
            return True
        if "hola" in user_tokens and cand_tokens[:1] == ["hello"]:
            return True
        return False

    def _is_question_echo(self, candidate: str, user_input: str) -> bool:
        c = self._normalize_for_echo(candidate)
        u = self._normalize_for_echo(user_input)
        if not c or not u:
            return False
        if c == u:
            return True
        if c.endswith("?"):
            c = c[:-1].strip()
        if u.endswith("?"):
            u = u[:-1].strip()
        if c == u:
            return True
        return overlap_score(c, u) >= 0.8

    @staticmethod
    def _normalize_for_echo(text: str) -> str:
        raw = str(text or "").strip().lower()
        if not raw:
            return ""
        norm = unicodedata.normalize("NFD", raw)
        norm = "".join(ch for ch in norm if unicodedata.category(ch) != "Mn")
        norm = re.sub(r"\s+", " ", norm)
        norm = re.sub(r"[^\w\s\?]", "", norm)
        return norm.strip()

    def _best_effort_generated_text(self, *, text: str, user_input: str, system_prompt: str) -> str:
        cleaned = str(text or "").strip()
        if not cleaned:
            return ""

        cleaned = cleaned.replace("ASSISTANT:", "").replace("ASISTENTE:", "").strip()
        # Best-effort output cannot bypass sanitization rules.
        return self._sanitize_generated_text(
            text=cleaned,
            system_prompt=system_prompt,
            user_input=user_input,
        )

    def _pipeline_key(self, model_id: str, model_task: str) -> str:
        return f"{model_task}::{model_id}"

    def _set_hf_offline_mode(self, enabled: bool) -> None:
        if enabled:
            os.environ["HF_HUB_OFFLINE"] = "1"
            os.environ["TRANSFORMERS_OFFLINE"] = "1"
        else:
            os.environ.pop("HF_HUB_OFFLINE", None)
            os.environ.pop("TRANSFORMERS_OFFLINE", None)

        # HF and transformers cache offline flags at import-time. Keep both in sync
        # so runtime toggles from UI (`localFilesOnly`) are honored per request.
        try:
            import huggingface_hub.constants as hub_constants  # type: ignore

            hub_constants.HF_HUB_OFFLINE = bool(enabled)
        except Exception:
            pass

        try:
            import transformers.utils.hub as tf_hub  # type: ignore

            if hasattr(tf_hub, "_is_offline_mode"):
                tf_hub._is_offline_mode = bool(enabled)
        except Exception:
            pass

    def _get_pipeline(self, model_id: str, model_task: str, local_files_only: bool) -> Any:
        key = f"{self._pipeline_key(model_id, model_task)}::local={1 if local_files_only else 0}"
        if key in self._pipelines:
            return self._pipelines[key]
        if key in self._pipeline_errors:
            # Online mode can recover from transient network/cache failures.
            # Do not keep a hard failure forever.
            if not local_files_only:
                last = float(self._pipeline_error_at.get(key) or 0.0)
                if (time.time() - last) >= 15.0:
                    self._pipeline_errors.pop(key, None)
                    self._pipeline_error_at.pop(key, None)
                else:
                    return None
            else:
                return None

        with self._pipeline_lock:
            if key in self._pipelines:
                return self._pipelines[key]
            if key in self._pipeline_errors:
                if not local_files_only:
                    last = float(self._pipeline_error_at.get(key) or 0.0)
                    if (time.time() - last) >= 15.0:
                        self._pipeline_errors.pop(key, None)
                        self._pipeline_error_at.pop(key, None)
                    else:
                        return None
                else:
                    return None

            try:
                from transformers import pipeline  # type: ignore

                self._set_hf_offline_mode(local_files_only)
                pipe = pipeline(
                    task=model_task,
                    model=model_id,
                    device=self._settings.hf_device_index,
                )

                self._pipelines[key] = pipe
                self._pipeline_errors.pop(key, None)
                self._pipeline_error_at.pop(key, None)
                return pipe
            except Exception as ex:  # pragma: no cover - defensive runtime path
                message = str(ex).strip() or ex.__class__.__name__
                lowered = message.lower()
                if local_files_only:
                    if "nonetype" in lowered or "local" in lowered or "offline" in lowered or "not found" in lowered:
                        message = (
                            f"modelo no disponible en cache local: '{model_id}'. "
                            "Carga pesos locales en el contenedor o desactiva localFilesOnly."
                        )
                elif "outgoing traffic has been disabled" in lowered or "offline mode" in lowered:
                    message = (
                        f"modo online bloqueado para '{model_id}'. "
                        "Reinicia el servicio con localFilesOnly=false o vuelve a desplegar el stack."
                    )
                self._pipeline_errors[key] = message
                self._pipeline_error_at[key] = time.time()
                return None

    def _generate_with_rules(
        self,
        user_input: str,
        top_docs: list[dict[str, Any]],
        chat_history: Optional[list[dict[str, str]]] = None,
    ) -> tuple[str, bool]:
        question = user_input.strip()
        if not question:
            return "No recibí una pregunta para responder.", True

        normalized = self._normalize_for_echo(question)
        previous_user = ""
        if chat_history:
            for message in reversed(chat_history):
                if message.get("role") == "user":
                    previous_user = self._normalize_for_echo(message.get("content", ""))
                    if previous_user:
                        break
        use_previous_context = self._is_followup_query(normalized)
        combined = f"{previous_user} {normalized}".strip() if use_previous_context else normalized

        requested_days = self._extract_requested_days(combined)
        if requested_days and ("python" in combined or "programacion" in combined or "programar" in combined):
            return self._build_python_study_plan(requested_days), False

        python_scope = normalized if ("python" in normalized or not use_previous_context) else combined
        if "python" in python_scope:
            if any(chunk in python_scope for chunk in {"que es python", "python que es", "definime python", "definir python"}):
                return (
                    "Python es un lenguaje de programación de alto nivel, simple de leer y muy usado para web, automatización, datos e IA.",
                    False,
                )
            if any(chunk in python_scope for chunk in {"para que sirve python", "que puedo hacer con python", "usos de python"}):
                return (
                    "Con Python podés automatizar tareas, crear APIs y apps web, analizar datos, trabajar con IA y construir scripts de productividad.",
                    False,
                )
            if any(chunk in python_scope for chunk in {"python es facil", "es facil python", "python es dificil", "es dificil python"}):
                return (
                    "Sí, Python suele ser de los lenguajes más fáciles para empezar. "
                    "Su sintaxis es clara y podés avanzar rápido si practicás todos los días con ejercicios cortos y un proyecto simple.",
                    False,
                )

        if top_docs:
            synthetic_answer, source = self._synthesize_answer_from_docs(
                user_input=user_input,
                top_docs=top_docs,
                chat_history=chat_history,
            )
            if synthetic_answer:
                best = source or top_docs[0]
                answer = (
                    f"{synthetic_answer}\n\n"
                    f"Fuente principal: {best.get('title', 'Documento')} ({best.get('id', 'doc')})."
                )
                return answer.strip(), False

        if normalized in {"hola", "buenas", "hello", "hi"}:
            return "Hola, ¿cómo estás? Puedo ayudarte con lo que necesites.", False
        if "como estas" in combined or "que tal" in combined:
            return "¡Estoy bien! Gracias por preguntar. ¿En qué te ayudo hoy?", False
        if "docker" in combined:
            return (
                "Docker empaqueta aplicaciones y sus dependencias en contenedores aislados para ejecutarlas de forma consistente en cualquier entorno."
            ), False
        if "rojo" in combined and "verde" in combined and "azul" in combined:
            return (
                "Si agregas azul a la mezcla de rojo y verde, el color suele ir hacia un tono más oscuro y apagado, "
                "entre marrón grisáceo o neutro."
            ), False
        if "rojo" in combined and "verde" in combined:
            return "Al mezclar rojo y verde suele obtenerse un tono marrón.", False

        topic = question.strip()
        if len(topic) > 120:
            topic = topic[:120].rsplit(" ", 1)[0].strip()
        answer = (
            f"No encontré contexto confiable para responder '{topic}' sin inventar datos. "
            "Si querés, te doy una explicación general o armamos un dataset específico de ese tema."
        )
        return answer, True

    def _is_followup_query(self, normalized: str) -> bool:
        text = str(normalized or "").strip()
        if not text:
            return False
        tokens = [tok for tok in text.split() if tok]
        if len(tokens) <= 4:
            return True
        if text.startswith("y "):
            return True
        if re.search(r"\b(lo|eso|esto|esa|ese|aquello|anterior|mismo tema|asi)\b", text):
            return True
        return False

    def _extract_requested_days(self, text: str) -> int:
        normalized = self._normalize_for_echo(text)
        if not normalized:
            return 0
        match = re.search(r"\bplan\s+de\s+(\d{1,2})\s+dias?\b", normalized)
        if match:
            try:
                days = int(match.group(1))
                return max(1, min(30, days))
            except ValueError:
                return 0
        if "plan semanal" in normalized:
            return 7
        return 0

    def _build_python_study_plan(self, days: int) -> str:
        if days <= 1:
            return (
                "Plan de 1 día (Python):\n"
                "1) Instalá Python + VS Code.\n"
                "2) Aprendé variables, tipos y condicionales.\n"
                "3) Resolvé 3 ejercicios cortos.\n"
                "4) Cerrá con un script útil (por ejemplo, calculadora)."
            )

        base = [
            "Fundamentos: instalación, variables, tipos, input/output.",
            "Control de flujo: if/elif/else y bucles for/while.",
            "Funciones y módulos: crear funciones reutilizables.",
            "Estructuras: listas, diccionarios, sets y tuplas.",
            "Archivos + errores: leer/escribir archivos y try/except.",
            "Proyecto mini: script real (CLI, scraper simple o automatización).",
            "Repaso final: refactor, tests básicos y próximos pasos.",
        ]
        lines = [f"Plan de {days} días para aprender Python (1 hora/día):"]
        for idx in range(days):
            topic = base[idx] if idx < len(base) else "Práctica guiada: ejercicios y mejora de un mini proyecto."
            lines.append(f"Día {idx + 1}: {topic}")
        lines.append("Si querés, te lo adapto a backend, data science o automatización.")
        return "\n".join(lines)

    def _detect_query_intent(self, normalized_query: str) -> str:
        text = str(normalized_query or "").strip()
        if not text:
            return "generic"
        if any(chunk in text for chunk in {"para que sirve", "para que se usa", "que puedo hacer con", "usos de"}):
            return "use_case"
        if re.search(r"\baprend\w*\b", text):
            return "learn"
        if any(chunk in text for chunk in {"como aprendo", "como empiezo", "como hago", "como usar"}):
            return "learn"
        if text.startswith("que es ") or text.startswith("quien es ") or text.startswith("defin"):
            return "definition"
        return "generic"

    def _intent_hint_tokens(self, intent: str) -> set[str]:
        if intent == "use_case":
            return {"usar", "usado", "uso", "aplicacion", "aplicaciones", "automatizacion", "datos", "web", "api", "ia"}
        if intent == "learn":
            return {"aprender", "practica", "ejercicio", "curso", "tutorial", "proyecto", "paso", "pasos"}
        if intent == "definition":
            return {"es", "define", "concepto", "lenguaje", "descripcion"}
        return set()

    def _split_sentences(self, text: str) -> list[str]:
        raw = self._clean_doc_noise(text)
        if not raw:
            return []
        parts = re.split(r"(?<=[\.\!\?])\s+", raw)
        return [p.strip() for p in parts if p and len(p.strip()) >= 20]

    def _synthesize_answer_from_docs(
        self,
        *,
        user_input: str,
        top_docs: list[dict[str, Any]],
        chat_history: Optional[list[dict[str, str]]] = None,
    ) -> tuple[str, Optional[dict[str, Any]]]:
        effective_query = self._build_retrieval_query(user_input=user_input, chat_history=chat_history or [])
        query_norm = self._normalize_for_echo(effective_query)
        query_tokens = tokenize(effective_query)
        intent = self._detect_query_intent(query_norm)
        hint_tokens = self._intent_hint_tokens(intent)
        focus_tokens = query_tokens | hint_tokens

        if intent == "learn" and "python" in query_tokens:
            return (
                "Para aprender Python rápido: 1) instalá Python, 2) practicá sintaxis base, 3) resolvé ejercicios diarios, "
                "4) hacé un proyecto pequeño (API, scraper o bot), 5) revisá errores y mejorá el código en iteraciones.",
                top_docs[0] if top_docs else None,
            )

        last_assistant = ""
        if chat_history:
            for message in reversed(chat_history):
                if str(message.get("role") or "").lower() == "assistant":
                    last_assistant = str(message.get("content") or "").strip()
                    if last_assistant:
                        break

        # Structured QA rows keep a strong signal when they are relevant to the
        # current question and not a near-duplicate of the previous answer.
        for doc in top_docs:
            doc_score = float(doc.get("score") or 0.0)
            common_count = int(doc.get("commonCount") or 0)
            focus_hits = int(doc.get("focusHits") or 0)
            if doc_score < 0.28 and common_count < 2 and focus_hits < 2:
                continue
            extracted = self._extract_answer_from_doc_text(str(doc.get("text") or ""))
            if extracted:
                if last_assistant and self._is_question_echo(extracted, last_assistant):
                    continue
                return extracted, doc

        scored_sentences: list[tuple[float, str, dict[str, Any]]] = []
        for doc in top_docs:
            text = str(doc.get("text") or "")
            for sentence in self._split_sentences(text):
                sentence_tokens = tokenize(sentence)
                if not sentence_tokens:
                    continue
                overlap = len(query_tokens & sentence_tokens)
                focus = len(focus_tokens & sentence_tokens)
                if overlap == 0 and focus == 0:
                    continue
                score = 0.0
                if query_tokens:
                    score += (overlap / float(len(query_tokens))) * 0.75
                if focus_tokens:
                    score += (focus / float(len(focus_tokens))) * 0.25
                scored_sentences.append((score, sentence.strip(), doc))

        scored_sentences.sort(key=lambda item: item[0], reverse=True)
        if scored_sentences:
            picked: Optional[tuple[float, str, dict[str, Any]]] = None
            for candidate in scored_sentences:
                _, sentence, _ = candidate
                if last_assistant and self._is_question_echo(sentence, last_assistant):
                    continue
                picked = candidate
                break
            if picked is None:
                picked = scored_sentences[0]

            best_score, best_sentence, best_doc = picked
            best_tokens = tokenize(best_sentence)
            if intent == "learn" and "python" in query_tokens and not (hint_tokens & best_tokens):
                return (
                    "Para aprender Python rápido: 1) instalá Python, 2) practicá sintaxis base, 3) resolvé ejercicios diarios, "
                    "4) hacé un proyecto pequeño (API, scraper o bot), 5) revisá errores y mejorá el código en iteraciones.",
                    best_doc,
                )
            if intent == "use_case" and "python" in query_tokens and not (hint_tokens & best_tokens):
                return (
                    "Python se usa para automatizar tareas, crear APIs y apps web, análisis de datos, IA y scripting en general.",
                    best_doc,
                )
            if best_score >= 0.28:
                return best_sentence.strip(), best_doc

        # Domain-specific guardrails for Python when retrieval is too weak/noisy.
        if "python" in query_tokens and intent == "use_case":
            return (
                "Python se usa para automatizar tareas, crear APIs y apps web, análisis de datos, IA y scripting en general.",
                top_docs[0] if top_docs else None,
            )
        if "python" in query_tokens and intent == "learn":
            return (
                "Para aprender Python rápido: 1) instalá Python, 2) practicá sintaxis base, 3) resolvé ejercicios diarios, "
                "4) hacé un proyecto pequeño (API, scraper o bot), 5) revisá errores y mejorá el código en iteraciones.",
                top_docs[0] if top_docs else None,
            )

        if top_docs:
            compact = self._compact_doc_text_for_prompt(str(top_docs[0].get("text") or ""))
            if compact:
                return compact, top_docs[0]
        return "", None

    def _extract_answer_from_doc_text(self, text: str) -> str:
        raw = str(text or "").strip()
        if not raw:
            return ""

        # Prefer explicit answer blocks from dataset rows transformed as:
        # "Pregunta: ...\nRespuesta: ..."
        match = re.search(r"respuesta\s*:\s*(.+)$", raw, flags=re.IGNORECASE | re.DOTALL)
        if match:
            candidate = match.group(1).strip()
            candidate = self._clean_doc_noise(candidate)
            candidate = self._strip_source_footer(candidate)
            if candidate:
                return candidate

        return ""

    def _compact_doc_text_for_prompt(self, text: str) -> str:
        raw = str(text or "").strip()
        if not raw:
            return ""

        raw = self._clean_doc_noise(raw)
        if not raw:
            return ""

        # If it still has pipe-separated metadata, keep only informative fragments.
        if raw.count("|") >= 2:
            chunks = [part.strip() for part in raw.split("|") if part.strip()]
            useful: list[str] = []
            ignored_tokens = {
                "train", "test", "val", "es", "en", "faq", "intro", "synthetic",
                "general", "resumen", "summary", "qa", "chat",
            }
            for chunk in chunks:
                norm = self._normalize_for_echo(chunk)
                if not norm:
                    continue
                if norm in ignored_tokens:
                    continue
                if norm.isdigit():
                    continue
                if len(chunk.split()) < 3:
                    continue
                useful.append(chunk)
            if useful:
                raw = ". ".join(useful[:2]).strip()
            else:
                raw = chunks[-1] if chunks else raw

        if len(raw) > 900:
            raw = raw[:900].rsplit(" ", 1)[0].strip() + "..."
        return raw

    def _clean_doc_noise(self, text: str) -> str:
        cleaned = str(text or "").strip()
        if not cleaned:
            return ""

        cleaned = cleaned.replace("\r\n", "\n").replace("\r", "\n")
        cleaned = re.sub(r"\n{2,}", "\n", cleaned)
        cleaned = re.sub(r"\s+\|\s+", " | ", cleaned)
        cleaned = re.sub(r"\s{2,}", " ", cleaned)
        return cleaned.strip()

    def _estimate_quality_score(self, *, answer: str, user_input: str, used_fallback: bool) -> float:
        score = 1.0
        text = str(answer or "").strip()
        if used_fallback:
            score -= 0.55

        if len(text) < 15:
            score -= 0.15
        if len(text.split()) < 4:
            score -= 0.15
        if text.endswith("?"):
            score -= 0.1

        overlap = overlap_score(text, user_input)
        if overlap >= 0.7:
            score -= 0.25
        elif overlap >= 0.5:
            score -= 0.12

        return round(max(0.0, min(1.0, score)), 4)

    def _auto_learning_file(self, project_id: int) -> Path:
        return self._settings.storage_root / "projects" / str(project_id) / "memory" / "learned_qa.jsonl"

    def _read_auto_learning_records(self, project_id: int, limit: int) -> list[dict[str, Any]]:
        if limit <= 0:
            return []
        path = self._auto_learning_file(project_id)
        if not path.exists():
            return []
        try:
            lines = path.read_text(encoding="utf-8").splitlines()
        except OSError:
            return []

        rows: list[dict[str, Any]] = []
        for raw in reversed(lines):
            if not raw.strip():
                continue
            try:
                item = json.loads(raw)
            except json.JSONDecodeError:
                continue
            if isinstance(item, dict):
                rows.append(item)
            if len(rows) >= limit:
                break
        return rows

    def _load_auto_learning_docs(self, *, project_id: int, limit: int) -> list[dict[str, str]]:
        records = self._read_auto_learning_records(project_id, max(20, limit))
        if not records:
            return []
        docs: list[dict[str, str]] = []
        for row in records:
            question = str(row.get("question") or "").strip()
            answer = str(row.get("answer") or "").strip()
            if not question or not answer:
                continue
            rec_id = str(row.get("id") or "")
            if not rec_id:
                rec_id = f"memory-{abs(hash((question, answer))) % 10000000}"
            docs.append(
                {
                    "id": rec_id if rec_id.startswith("memory-") else f"memory-{rec_id}",
                    "title": "Memoria conversacional",
                    "text": f"Pregunta: {question}\nRespuesta: {answer}",
                }
            )
        return docs

    def _maybe_capture_auto_learning(
        self,
        *,
        project_id: int,
        config: AutoLearningConfig,
        user_input: str,
        chat_history: Optional[list[dict[str, str]]] = None,
        answer: str,
        used_fallback: bool,
        quality_score: float,
        sources: list[dict[str, Any]],
    ) -> tuple[bool, int]:
        if not config.enabled:
            return False, 0

        question = str(user_input or "").strip()
        output = self._strip_source_footer(str(answer or "").strip())
        snapshot = self._read_auto_learning_records(project_id, min(config.max_records, 5000))
        snapshot_count = len(snapshot)
        if len(question) < 3 or len(output) < 12:
            return False, snapshot_count
        if quality_score < config.min_quality_score:
            return False, snapshot_count
        if used_fallback and not config.capture_fallback:
            return False, snapshot_count
        if self._is_generic_fallback_text(output):
            return False, snapshot_count
        if sources:
            first_source_id = str(sources[0].get("id") or "").strip().lower()
            # Avoid recursive drift: do not learn from answers that came from learned memory itself.
            if first_source_id.startswith("memory-"):
                return False, snapshot_count

        norm_q = self._normalize_for_echo(question)
        norm_a = self._normalize_for_echo(output)
        if not norm_q or not norm_a:
            return False, snapshot_count
        dedupe_key = f"{norm_q}::{norm_a}"

        with self._memory_lock:
            existing = self._read_auto_learning_records(project_id, max(200, config.max_records))
            if any(str(row.get("key") or "") == dedupe_key for row in existing):
                return False, len(existing)

            path = self._auto_learning_file(project_id)
            ensure_dir(path.parent)
            now = self._now_iso()
            source_ids = [str(item.get("id") or "").strip() for item in sources[:3] if str(item.get("id") or "").strip()]
            payload = {
                "id": f"memory-{int(time.time() * 1000)}",
                "projectId": project_id,
                "question": question[:1200],
                "answer": output[:4000],
                "usedFallback": bool(used_fallback),
                "qualityScore": float(quality_score),
                "sourceIds": source_ids,
                "key": dedupe_key,
                "createdAt": now,
            }
            path.open("a", encoding="utf-8").write(json.dumps(payload, ensure_ascii=False) + "\n")

            all_rows = self._read_auto_learning_records(project_id, max(1, config.max_records * 2))
            if len(all_rows) > config.max_records:
                trimmed = list(reversed(all_rows[: config.max_records]))
                path.write_text(
                    "\n".join(json.dumps(row, ensure_ascii=False) for row in trimmed) + "\n",
                    encoding="utf-8",
                )
                return True, len(trimmed)
            return True, len(all_rows)

    def _is_generic_fallback_text(self, answer: str) -> bool:
        norm = self._normalize_for_echo(answer)
        if not norm:
            return True
        low_value_signals = (
            "estoy corriendo en modo local sin contexto documental adicional",
            "agrega documentos al proyecto o mejora la configuracion del modelo hf local",
            "no pude generar una respuesta util con el contexto actual",
        )
        return any(signal in norm for signal in low_value_signals)

    def _strip_source_footer(self, text: str) -> str:
        raw = str(text or "").strip()
        if not raw:
            return ""
        cleaned = re.sub(r"\n*\s*Fuente principal\s*:[^\n]*(?:\n|$)", "\n", raw, flags=re.IGNORECASE)
        cleaned = re.sub(r"\n{2,}", "\n\n", cleaned).strip()
        return cleaned

    def _metrics_file(self, project_id: int) -> Path:
        return self._settings.storage_root / "projects" / str(project_id) / "metrics" / "infer-traces.jsonl"

    def _append_infer_trace(self, project_id: int, trace: dict[str, Any]) -> None:
        path = self._metrics_file(project_id)
        ensure_dir(path.parent)

        encoded = json.dumps(trace, ensure_ascii=False)
        with self._metrics_lock:
            path.open("a", encoding="utf-8").write(encoded + "\n")

    def _read_infer_traces(self, project_id: int, limit: int) -> list[dict[str, Any]]:
        path = self._metrics_file(project_id)
        if not path.exists():
            return []

        try:
            lines = path.read_text(encoding="utf-8").splitlines()
        except OSError:
            return []

        rows: list[dict[str, Any]] = []
        for raw in reversed(lines):
            if not raw.strip():
                continue
            try:
                item = json.loads(raw)
            except json.JSONDecodeError:
                continue
            if not isinstance(item, dict):
                continue
            rows.append(item)
            if len(rows) >= limit:
                break
        return rows

    def _evaluate_quality_gate(self, summary: dict[str, Any], gate: QualityGateConfig) -> dict[str, Any]:
        if not gate.enabled:
            return {
                "ok": True,
                "enabled": False,
                "reason": "quality gate deshabilitado",
                "minSamples": gate.min_samples,
                "minSuccessRate": gate.min_success_rate,
                "maxFallbackRate": gate.max_fallback_rate,
                "maxAvgLatencyMs": gate.max_avg_latency_ms,
            }

        total = int(summary.get("total") or 0)
        success_rate = float(summary.get("successRate") or 0.0)
        fallback_rate = float(summary.get("fallbackRate") or 0.0)
        avg_latency = int(summary.get("avgLatencyMs") or 0)

        checks = {
            "enoughSamples": total >= gate.min_samples,
            "successRate": success_rate >= gate.min_success_rate,
            "fallbackRate": fallback_rate <= gate.max_fallback_rate,
            "latency": gate.max_avg_latency_ms <= 0 or avg_latency <= gate.max_avg_latency_ms,
        }

        ok = all(checks.values())
        failing = [name for name, passed in checks.items() if not passed]

        return {
            "ok": ok,
            "enabled": True,
            "reason": "ok" if ok else f"fallo en: {', '.join(failing)}",
            "checks": checks,
            "minSamples": gate.min_samples,
            "minSuccessRate": gate.min_success_rate,
            "maxFallbackRate": gate.max_fallback_rate,
            "maxAvgLatencyMs": gate.max_avg_latency_ms,
        }

    @staticmethod
    def _to_int(value: Any, fallback: int, minimum: int, maximum: int) -> int:
        try:
            parsed = int(str(value).strip())
        except Exception:
            return fallback
        return max(minimum, min(maximum, parsed))

    @staticmethod
    def _to_float(value: Any, fallback: float, minimum: float, maximum: float) -> float:
        try:
            parsed = float(str(value).strip())
        except Exception:
            return fallback
        return max(minimum, min(maximum, parsed))

    @staticmethod
    def _to_bool(value: Any, fallback: Optional[bool] = False) -> Optional[bool]:
        if isinstance(value, bool):
            return value
        if value is None:
            return fallback
        text = str(value).strip().lower()
        if text in {"1", "true", "yes", "on"}:
            return True
        if text in {"0", "false", "no", "off"}:
            return False
        return fallback

    @staticmethod
    def _now_iso() -> str:
        return datetime.now(timezone.utc).isoformat()
