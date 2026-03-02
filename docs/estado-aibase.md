Quiero que actúes como Arquitecto Principal y Líder Técnico. Necesito que diseñes de punta a punta un sistema open-source llamado “AIBase” (nombre tentativo), inspirado en mi proyecto SystemBase (arquitectura metadata-driven), pero orientado a que usuarios creen y “eduquen” su propia inteligencia artificial desde una interfaz web.

Contexto real:
- Mi stack principal es .NET 8 (C#) + SQL Server en backend y Vue.js + Vuetify en frontend.
- Ya trabajé un sistema llamado “Mapeo” usando servicios locales dockerizados y utilicé Python para construir datasets.
- En AIBase quiero usar .NET/Vue para la plataforma (UI, permisos, metadata, orquestación) e integrar módulos de IA en Python (microservicios dockerizados) para todo lo relacionado a IA: dataset building, embeddings/RAG, entrenamiento LoRA/QLoRA, inferencia y evaluación.

Objetivo del producto:
- Una plataforma web (wizard) donde un usuario pueda crear una “IA por dominio/caso de uso”.
- Que la IA pueda “educarse” mediante: documentos (RAG) + ejemplos (dataset para fine-tuning) + reglas/schemas.
- Que el resultado sea un artefacto versionado y reproducible: base model + adapter (LoRA) + config + dataset version + reporte de evaluación, publicable como API y exportable como paquete Docker.
- Debe ser modular y metadata-driven (como SystemBase): templates/plugins de “tipos de IA” que cambian el pipeline.

Requerimientos funcionales:
1) Gestión de usuarios, roles, permisos y multi-tenant (opcional pero planeado).
2) Gestión de proyectos IA: crear, editar, versionar, clonar, borrar lógicamente.
3) Wizard UI:
   - Paso 1: crear proyecto IA (nombre, descripción, idioma, tono/políticas)
   - Paso 2: elegir template (Extractor JSON / Chat RAG / Clasificador / etc.)
   - Paso 3: cargar datos (docs, links, audios, textos, CSV, etc.)
   - Paso 4: “educar” con ejemplos: input→output, reglas, schema, prompts
   - Paso 5: entrenar o indexar (según template), con progreso de job
   - Paso 6: evaluar (tests + métricas + regresiones)
   - Paso 7: publicar (endpoint API, API key, versión activa, rollback)
4) Templates mínimos para V1:
   - Template A: Extractor estructurado (salida JSON validada por JSON Schema)
   - Template B: Chat RAG sobre documentos
   - (Opcional V1.1): Híbrido RAG + LoRA para formato/estilo
5) Versionado:
   - DatasetVersion (v1, v2…)
   - ModelVersion (adapter LoRA v1, v2…)
   - EvalRun (métricas por versión)
   - DeploymentVersion (versión publicada)
6) Logging/auditoría:
   - registrar consultas, versión usada, latencia, errores, usuario/tenant.
7) Seguridad:
   - JWT, permisos por proyecto y acciones.
   - aislamiento por tenant.
   - sanitización de datos y control de acceso a artifacts.

Requerimientos no funcionales:
- Debe correr local con Docker Compose (como Mapeo).
- Debe ser escalable a producción (separación de servicios).
- Debe ser reproducible (misma config -> mismo pipeline).
- Debe ser mantenible: clean architecture, servicios separados, contratos claros.

Servicios y componentes esperados:
- aibase-api (.NET): plataforma principal (auth, metadata, orquestación, control de versiones, endpoints).
- aibase-ui (Vue/Vuetify): wizard, dashboards, gestión de proyectos, evaluación, publicación.
- aibase-engine (Python/FastAPI): motor IA con endpoints:
  - /dataset/build
  - /rag/index
  - /train/lora
  - /infer
  - /eval/run
  - /runs/{id} (estado/progreso)
- vector db (Qdrant recomendado) para RAG.
- storage (MinIO o filesystem) para artifacts: datasets, adapters, logs.
- opcional: redis/rabbitmq para colas de jobs.
- opcional: servicio de “model registry” interno.

Lo que necesito que entregues:
A) Visión y arquitectura completa:
   - diagrama textual de componentes (estilo C4)
   - flujo end-to-end: UI -> API -> Engine -> storage -> DB -> publicación
   - decisión sobre comunicación (HTTP vs gRPC) y por qué
   - decisión sobre cola de jobs (si aplica)
B) Modelo de datos (SQL Server) basado en metadata-driven:
   - tablas mínimas y relaciones: Tenants, Users, Roles, Projects, Templates, PromptProfiles, DataSources, Documents, DatasetVersions, TrainingRuns, ModelVersions, EvalSuites, EvalRuns, Deployments, APIKeys, AuditLogs.
   - campos importantes de cada tabla y claves foráneas
   - estrategia de versionado y estados
C) Diseño de módulos “Template” (plugins):
   - cómo se define un template y su pipeline (dataset/index/train/eval/serve)
   - cómo agregar un nuevo template sin romper la plataforma
D) Diseño de pantallas (Vue/Vuetify):
   - listado de pantallas y componentes principales
   - wizard paso a paso con validaciones y UX
   - dashboard de métricas por versión (simple al inicio)
E) Pipeline de “educación” explicado para usuarios:
   - cómo explicar “RAG” y “Fine-tuning LoRA” en términos simples
   - recomendaciones de “cuántos ejemplos” para extractor
   - mejores prácticas de documentos para RAG
F) Roadmap por fases (V1, V1.1, V2):
   - qué construir primero para tener valor rápido
   - qué dejar para después (multi-tenant avanzado, marketplace de templates, etc.)
G) Plan de implementación paso a paso:
   - orden de construcción backend, engine, frontend
   - endpoints exactos y contratos (request/response) entre .NET y Python
   - docker-compose de alto nivel (servicios y variables)
   - estrategia de testing (unit + integration + eval tests)
H) Recomendaciones técnicas:
   - modelos base sugeridos para dev y prod (ligeros para local)
   - cómo manejar GPU opcional (si hay vs si no hay)
   - cómo cachear modelos/adapters y evitar recargas
   - límites y riesgos (costos, seguridad, sesgos, privacidad)

Formato de respuesta:
- En español.
- Muy estructurado con títulos claros.
- Listas accionables y decisiones justificadas.
- No me pidas aclaraciones; asumí una V1 razonable con Extractor JSON + Chat RAG.
- Incluí ejemplos de contratos JSON y ejemplos de estados de runs.
- Si necesitás asumir algo, decláralo explícitamente.