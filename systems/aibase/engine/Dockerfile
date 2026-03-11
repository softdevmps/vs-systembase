FROM python:3.11-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1

WORKDIR /app

COPY requirements.txt ./requirements.txt
COPY requirements-llm.txt ./requirements-llm.txt

ARG INSTALL_LLM_EXTRAS=1
RUN pip install --upgrade pip \
    && pip install -r requirements.txt \
    && if [ "$INSTALL_LLM_EXTRAS" = "1" ]; then pip install -r requirements-llm.txt; fi

ARG PRELOAD_HF_MODEL_ID=
ARG PRELOAD_HF_TASK=text2text-generation

COPY app ./app
COPY data/generator ./data/generator
COPY .env.example ./.env.example

RUN if [ "$INSTALL_LLM_EXTRAS" = "1" ] && [ -n "$PRELOAD_HF_MODEL_ID" ]; then \
      PRELOAD_HF_MODEL_ID="$PRELOAD_HF_MODEL_ID" PRELOAD_HF_TASK="$PRELOAD_HF_TASK" python /app/app/preload_model.py || true; \
    fi

RUN mkdir -p /app/data /app/exports

EXPOSE 8010

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8010"]
