from __future__ import annotations

import os


def main() -> None:
    model_id = os.environ.get("PRELOAD_HF_MODEL_ID", "").strip()
    task = os.environ.get("PRELOAD_HF_TASK", "text2text-generation").strip() or "text2text-generation"
    if not model_id:
        print("[engine] preload skipped: empty model id")
        return

    try:
        from transformers import pipeline  # type: ignore

        pipeline(task=task, model=model_id, device=-1)
        print(f"[engine] preloaded model: {model_id} ({task})")
    except Exception as ex:  # pragma: no cover - defensive on build environments
        print(f"[engine] preload skipped: {ex}")


if __name__ == "__main__":
    main()
