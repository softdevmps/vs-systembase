from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path


def _env(name: str, default: str) -> str:
    return os.getenv(name, default).strip()


def _env_bool(name: str, default: bool) -> bool:
    raw = os.getenv(name)
    if raw is None:
        return default
    return raw.strip().lower() in {"1", "true", "yes", "on"}


def _env_int(name: str, default: int, minimum: int, maximum: int) -> int:
    raw = os.getenv(name)
    if raw is None:
        return default
    try:
        value = int(raw.strip())
    except ValueError:
        return default
    return max(minimum, min(maximum, value))


def _env_float(name: str, default: float, minimum: float, maximum: float) -> float:
    raw = os.getenv(name)
    if raw is None:
        return default
    try:
        value = float(raw.strip())
    except ValueError:
        return default
    return max(minimum, min(maximum, value))


@dataclass
class EngineSettings:
    service_name: str
    storage_root: Path
    exports_root: Path

    model_mode: str
    hf_task: str
    hf_model_id: str
    hf_device: str
    hf_local_files_only: bool
    hf_max_new_tokens: int
    hf_temperature: float
    hf_top_p: float
    hf_repetition_penalty: float
    ollama_base_url: str
    ollama_model: str
    ollama_path: str
    ollama_timeout_sec: int

    default_system_prompt: str
    default_profile: str
    quality_gate_enabled: bool
    quality_gate_min_samples: int
    quality_gate_min_success_rate: float
    quality_gate_max_fallback_rate: float
    quality_gate_max_avg_latency_ms: int

    @classmethod
    def from_env(cls) -> "EngineSettings":
        storage_root = Path(_env("ENGINE_STORAGE_ROOT", "./data")).resolve()
        exports_root = Path(_env("ENGINE_EXPORTS_ROOT", "./exports")).resolve()

        return cls(
            service_name=_env("ENGINE_SERVICE_NAME", "aibase-local-engine"),
            storage_root=storage_root,
            exports_root=exports_root,
            model_mode=_env("ENGINE_MODEL_MODE", "hf").lower(),
            hf_task=_env("ENGINE_HF_TASK", "text2text-generation"),
            hf_model_id=_env("ENGINE_HF_MODEL_ID", "google/flan-t5-base"),
            hf_device=_env("ENGINE_HF_DEVICE", "cpu").lower(),
            hf_local_files_only=_env_bool("ENGINE_HF_LOCAL_FILES_ONLY", True),
            hf_max_new_tokens=_env_int("ENGINE_HF_MAX_NEW_TOKENS", 256, 64, 4096),
            hf_temperature=_env_float("ENGINE_HF_TEMPERATURE", 0.2, 0.0, 2.0),
            hf_top_p=_env_float("ENGINE_HF_TOP_P", 0.95, 0.1, 1.0),
            hf_repetition_penalty=_env_float("ENGINE_HF_REPETITION_PENALTY", 1.05, 0.8, 2.5),
            ollama_base_url=_env("ENGINE_OLLAMA_BASE_URL", "http://host.docker.internal:11434"),
            ollama_model=_env("ENGINE_OLLAMA_MODEL", "qwen2.5:7b"),
            ollama_path=_env("ENGINE_OLLAMA_PATH", "/api/generate"),
            ollama_timeout_sec=_env_int("ENGINE_OLLAMA_TIMEOUT_SEC", 180, 5, 1800),
            default_system_prompt=_env(
                "ENGINE_DEFAULT_SYSTEM_PROMPT",
                "Eres un asistente de IA local. Responde de forma clara, útil y en español.",
            ),
            default_profile=_env("ENGINE_DEFAULT_PROFILE", "assistant_general"),
            quality_gate_enabled=_env_bool("ENGINE_QUALITY_GATE_ENABLED", True),
            quality_gate_min_samples=_env_int("ENGINE_QUALITY_GATE_MIN_SAMPLES", 3, 1, 500),
            quality_gate_min_success_rate=_env_float("ENGINE_QUALITY_GATE_MIN_SUCCESS_RATE", 0.6, 0.0, 1.0),
            quality_gate_max_fallback_rate=_env_float("ENGINE_QUALITY_GATE_MAX_FALLBACK_RATE", 0.4, 0.0, 1.0),
            quality_gate_max_avg_latency_ms=_env_int("ENGINE_QUALITY_GATE_MAX_AVG_LATENCY_MS", 25000, 0, 1800000),
        )

    @property
    def use_hf(self) -> bool:
        return self.model_mode in {"hf", "huggingface", "transformers"}

    @property
    def hf_device_index(self) -> int:
        return -1 if self.hf_device in {"cpu", "", "-1"} else 0

    def ensure_dirs(self) -> None:
        self.storage_root.mkdir(parents=True, exist_ok=True)
        self.exports_root.mkdir(parents=True, exist_ok=True)
