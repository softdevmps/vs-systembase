from __future__ import annotations

import json
import re
import unicodedata
from pathlib import Path
from typing import Any, Optional


WORD_RE = re.compile(r"[a-zA-Z0-9_áéíóúÁÉÍÓÚñÑ]+")
STOPWORDS = {
    "a", "al", "algo", "ante", "bajo", "cabe", "con", "contra", "de", "del", "desde",
    "donde", "el", "ella", "ellas", "ellos", "en", "entre", "era", "eramos", "eres",
    "es", "esa", "ese", "eso", "esta", "estas", "este", "esto", "estoy", "fue", "ha",
    "hay", "hola", "la", "las", "le", "les", "lo", "los", "me", "mi", "mis", "mucho",
    "muy", "o", "para", "pero", "por", "que", "se", "si", "sin", "sobre", "son", "su",
    "sus", "te", "tu", "tus", "un", "una", "uno", "y", "ya", "como", "qué", "que",
}


def parse_json_string(raw: Optional[str]) -> Any:
    text = (raw or "").strip()
    if not text:
        return {}
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        return {"_raw": text}


def to_json_text(payload: Any) -> str:
    return json.dumps(payload, ensure_ascii=False, indent=2)


def ensure_dir(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)


def slugify(value: str) -> str:
    text = (value or "").strip().lower()
    text = re.sub(r"[^a-z0-9]+", "-", text)
    text = re.sub(r"-{2,}", "-", text)
    return text.strip("-") or "service"


def tokenize(text: str) -> set[str]:
    tokens: set[str] = set()
    for match in WORD_RE.finditer(text or ""):
        raw = match.group(0).strip().lower()
        if not raw:
            continue
        normalized = unicodedata.normalize("NFD", raw)
        normalized = "".join(ch for ch in normalized if unicodedata.category(ch) != "Mn")
        normalized = normalized.strip("_")
        if not normalized:
            continue
        if normalized in STOPWORDS:
            continue
        if len(normalized) <= 1:
            continue
        tokens.add(normalized)
    return tokens


def overlap_score(query: str, text: str) -> float:
    q_tokens = tokenize(query)
    t_tokens = tokenize(text)
    if not q_tokens or not t_tokens:
        return 0.0
    common = q_tokens & t_tokens
    return len(common) / float(len(q_tokens))
