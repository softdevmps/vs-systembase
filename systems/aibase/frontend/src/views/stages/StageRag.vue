<template>
  <v-container fluid class="stage-page">
    <v-card class="card sb-page-header">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center">
          <div class="sb-page-icon"><v-icon color="primary" size="24">mdi-vector-link</v-icon></div>
          <div>
            <h2>Etapa 4 · RAG Index</h2>
            <p class="sb-page-subtitle">Construye el índice de conocimiento para consultas contextuales del modelo.</p>
          </div>
        </div>
        <div class="d-flex align-center ga-2 flex-wrap">
          <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-arrow-left" @click="router.push('/stage/dataset')">Volver</v-btn>
          <v-btn class="sb-btn primary" prepend-icon="mdi-arrow-right" :disabled="!selectedProjectId" @click="goNext">Ir a Train</v-btn>
        </div>
      </div>
    </v-card>

    <v-alert
      v-if="error"
      class="mt-4"
      type="warning"
      variant="tonal"
      border="start"
      density="comfortable"
      :text="error"
    />

    <OptionGuide class="mt-4" :items="optionGuideItems" />

    <v-row class="mt-4" dense>
      <v-col cols="12" md="7">
        <v-card class="card">
          <v-card-title>Configuración de indexación</v-card-title>
          <v-divider />
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="8">
                <v-select
                  v-model="selectedProjectId"
                  :items="projectItems"
                  item-title="title"
                  item-value="value"
                  label="Proyecto"
                  density="comfortable"
                  :loading="loadingData"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-select
                  v-model="ragForm.reindexMode"
                  :items="reindexModes"
                  item-title="title"
                  item-value="value"
                  label="Modo"
                  density="comfortable"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="8">
                <v-text-field
                  v-model="ragForm.indexName"
                  label="Nombre de índice"
                  density="comfortable"
                  :rules="[rules.required, rules.max120]"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-select
                  v-model="ragForm.vectorStore"
                  :items="vectorStoreOptions"
                  item-title="title"
                  item-value="value"
                  label="Vector store"
                  density="comfortable"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="ragForm.datasetVersion"
                  label="Dataset version"
                  density="comfortable"
                  :rules="[rules.required, rules.max40]"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="ragForm.embeddingModel"
                  label="Embedding model"
                  density="comfortable"
                  :rules="[rules.required, rules.max120]"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="ragForm.chunkSize"
                  type="number"
                  label="Chunk size"
                  density="comfortable"
                  min="100"
                  max="4000"
                  step="50"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="ragForm.chunkOverlap"
                  type="number"
                  label="Chunk overlap"
                  density="comfortable"
                  min="0"
                  max="1000"
                  step="10"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="ragForm.topK"
                  type="number"
                  label="Top-K retrieval"
                  density="comfortable"
                  min="1"
                  max="50"
                  step="1"
                />
              </v-col>
            </v-row>

            <v-alert
              v-if="chunkingError"
              class="mb-2"
              type="warning"
              variant="tonal"
              density="comfortable"
              :text="chunkingError"
            />

            <v-row dense>
              <v-col cols="12" md="7">
                <v-text-field
                  v-model="ragForm.metadataFieldsCsv"
                  label="Metadata fields (csv)"
                  density="comfortable"
                  placeholder="source,type,date"
                  :rules="[rules.max200]"
                />
              </v-col>
              <v-col cols="12" md="5">
                <v-text-field
                  v-model="ragForm.persistPath"
                  label="Ruta persistencia (opcional)"
                  density="comfortable"
                  placeholder="/var/lib/aibase/indexes"
                  :rules="[rules.max300]"
                />
              </v-col>
            </v-row>

            <div class="toggle-grid mb-2">
              <v-checkbox v-model="ragForm.enableRerank" hide-details density="compact" label="Rerank" />
              <v-checkbox v-model="ragForm.enableMetadataFilter" hide-details density="compact" label="Filtro metadata" />
              <v-checkbox v-model="ragForm.failOnEmptyDataset" hide-details density="compact" label="Fallar si dataset vacío" />
            </div>

            <v-textarea
              v-model="ragForm.filtersJson"
              label="Filtros opcionales (JSON)"
              auto-grow
              rows="3"
              density="comfortable"
              :error-messages="filtersJsonError ? [filtersJsonError] : []"
              placeholder='{"language":"es","domain":"seguridad"}'
            />

            <div class="d-flex align-center ga-2 mt-3 flex-wrap">
              <v-btn
                class="sb-btn primary"
                prepend-icon="mdi-play"
                :loading="running"
                :disabled="!canRun"
                @click="triggerRagIndex"
              >
                Ejecutar RAG Index
              </v-btn>
              <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-content-copy" @click="copyPayloadPreview">
                Copiar inputJson
              </v-btn>
            </div>

            <v-alert
              v-if="runError"
              class="mt-3"
              type="error"
              variant="tonal"
              density="comfortable"
              :text="runError"
            />
            <v-alert
              v-if="runMessage"
              class="mt-3"
              type="success"
              variant="tonal"
              density="comfortable"
              :text="runMessage"
            />
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title class="d-flex align-center justify-space-between">
            Últimos runs de RAG
            <v-btn icon variant="text" :disabled="loadingRuns" @click="loadRuns">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loadingRuns" class="sb-skeleton" style="height: 120px;"></div>
            <div v-else-if="!ragRuns.length" class="text-caption text-medium-emphasis">No hay corridas de RAG.</div>
            <div v-else class="run-list">
              <div v-for="run in ragRuns" :key="run.Id || run.id" class="run-item">
                <div class="run-head">
                  <strong>#{{ run.Id || run.id }}</strong>
                  <v-chip size="small" variant="tonal" :color="statusColor(run.Status || run.status)">
                    {{ run.Status || run.status || 'unknown' }}
                  </v-chip>
                </div>
                <div class="run-meta">
                  {{ formatDate(run.CreatedAt || run.createdAt) }} · progreso {{ run.ProgressPct ?? run.progressPct ?? 0 }}%
                </div>
                <div v-if="run.LastError || run.lastError" class="run-error">
                  {{ run.LastError || run.lastError }}
                </div>
              </div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="5">
        <v-card class="card">
          <v-card-title>Prerequisitos</v-card-title>
          <v-divider />
          <v-card-text>
            <ul class="hint-list">
              <li :class="{ ok: !!selectedProjectId }">Proyecto seleccionado</li>
              <li :class="{ ok: datasetCompleted }">Dataset build completado</li>
              <li :class="{ ok: canRunStage }">Etapa habilitada en workflow</li>
              <li :class="{ ok: !filtersJsonError }">JSON de filtros válido</li>
              <li :class="{ ok: !chunkingError }">Chunk overlap menor que chunk size</li>
              <li :class="{ ok: !!ragForm.datasetVersion.trim() }">Versión de dataset definida</li>
            </ul>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title class="d-flex align-center justify-space-between">
            Estado de workflow
            <v-chip v-if="projectStatus" size="x-small" variant="tonal">{{ projectStatus }}</v-chip>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loadingData" class="sb-skeleton" style="height: 100px;"></div>
            <div v-else-if="!workflowSteps.length" class="text-caption text-medium-emphasis">Sin workflow disponible.</div>
            <div v-else class="workflow-list">
              <div v-for="step in workflowSteps" :key="step.RunType || step.runType" class="workflow-item">
                <span>{{ step.Order || step.order }} · {{ step.Label || step.label }}</span>
                <v-chip size="x-small" variant="tonal" :color="statusColor(step.Status || step.status)">
                  {{ statusLabel(step.Status || step.status) }}
                </v-chip>
              </div>
            </div>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title class="d-flex align-center justify-space-between">
            Preview inputJson
            <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-content-copy" @click="copyPayloadPreview">Copiar</v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div class="preview-box">
              <pre>{{ payloadPreview }}</pre>
            </div>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title>Último output RAG</v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="!latestRagRun" class="text-caption text-medium-emphasis">
              Todavía no hay output de RAG.
            </div>
            <div v-else class="preview-box">
              <pre>{{ latestRagOutputPretty }}</pre>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import runtimeApi from '../../api/runtime.service'
import OptionGuide from '../../components/Workflow/OptionGuide.vue'

const router = useRouter()
const route = useRoute()

const loadingData = ref(false)
const loadingRuns = ref(false)
const running = ref(false)

const error = ref('')
const runMessage = ref('')
const runError = ref('')

const projects = ref([])
const templates = ref([])
const selectedProjectId = ref(null)
const projectWorkflow = ref(null)
const runs = ref([])

const ragForm = reactive({
  reindexMode: 'incremental',
  indexName: '',
  vectorStore: 'pgvector',
  datasetVersion: '',
  embeddingModel: 'text-embedding-3-small',
  chunkSize: 800,
  chunkOverlap: 120,
  topK: 8,
  metadataFieldsCsv: 'source,type,date',
  persistPath: '',
  enableRerank: false,
  enableMetadataFilter: true,
  failOnEmptyDataset: true,
  filtersJson: '{}'
})

const reindexModes = [
  { title: 'Incremental', value: 'incremental' },
  { title: 'Rebuild completo', value: 'rebuild' },
  { title: 'Solo refresh metadata', value: 'metadata_refresh' }
]

const vectorStoreOptions = [
  { title: 'Postgres + pgvector', value: 'pgvector' },
  { title: 'Qdrant', value: 'qdrant' },
  { title: 'FAISS local', value: 'faiss' },
  { title: 'Chroma', value: 'chroma' }
]

const rules = {
  required: v => !!String(v ?? '').trim() || 'Campo requerido',
  max40: v => String(v ?? '').length <= 40 || 'Máximo 40 caracteres',
  max120: v => String(v ?? '').length <= 120 || 'Máximo 120 caracteres',
  max200: v => String(v ?? '').length <= 200 || 'Máximo 200 caracteres',
  max300: v => String(v ?? '').length <= 300 || 'Máximo 300 caracteres'
}

const optionGuideItems = [
  { label: 'Modo de indexación', description: 'Incremental añade cambios; rebuild recrea todo el índice.' },
  { label: 'Índice y vector store', description: 'Define dónde y cómo se almacenan los embeddings.' },
  { label: 'Embedding model', description: 'Modelo que convierte texto en vectores para búsqueda semántica.' },
  { label: 'Chunk size / overlap', description: 'Ajusta granularidad del contexto y continuidad entre fragmentos.' },
  { label: 'Top K', description: 'Cantidad de fragmentos recuperados por consulta.' },
  { label: 'Filtros JSON', description: 'Permite restringir documentos por metadata (idioma, dominio, etc.).' }
]

function normalizeRecord(record) {
  if (!record || typeof record !== 'object') return record
  const copy = { ...record }
  const keyMap = new Map(Object.keys(copy).map(k => [k.toLowerCase(), k]))
  const ensure = name => {
    if (copy[name] !== undefined) return
    const match = keyMap.get(String(name).toLowerCase())
    if (match) copy[name] = copy[match]
  }
  ;[
    'Id', 'Name', 'Slug', 'TemplateId', 'Templateid', 'Pipelinejson',
    'Status', 'RunType', 'ProgressPct', 'CreatedAt', 'UpdatedAt', 'Inputjson'
  ].forEach(ensure)
  return copy
}

function errorText(err, fallback = 'Error de comunicación.') {
  const msg = err?.response?.data?.error
    || err?.response?.data?.message
    || (typeof err?.response?.data === 'string' ? err.response.data : null)
    || err?.message
  return String(msg || fallback)
}

function statusColor(status) {
  const value = String(status || '').toLowerCase()
  if (value === 'completed' || value === 'deployed') return 'success'
  if (value === 'running') return 'warning'
  if (value === 'error' || value === 'failed') return 'error'
  if (value === 'blocked') return 'grey'
  if (value === 'na') return 'blue-grey'
  return 'primary'
}

function statusLabel(status) {
  const value = String(status || '').toLowerCase()
  if (value === 'completed' || value === 'deployed') return 'Completado'
  if (value === 'running') return 'En curso'
  if (value === 'error' || value === 'failed') return 'Error'
  if (value === 'blocked') return 'Bloqueado'
  if (value === 'na') return 'No aplica'
  return 'Pendiente'
}

function formatDate(value) {
  if (!value) return 'Sin fecha'
  return new Date(value).toLocaleString('es-AR')
}

function parseJson(text, fallback = null) {
  try {
    const raw = String(text || '').trim()
    if (!raw) return { ok: true, value: fallback }
    return { ok: true, value: JSON.parse(raw) }
  } catch (err) {
    return { ok: false, value: fallback, message: err?.message || 'JSON inválido' }
  }
}

const projectItems = computed(() => projects.value.map(project => ({
  title: project.Name || project.name || `Proyecto #${project.Id || project.id}`,
  value: project.Id || project.id
})))

const selectedProject = computed(() =>
  projects.value.find(project => String(project.Id || project.id) === String(selectedProjectId.value)) || null
)

const selectedTemplate = computed(() => {
  const templateId = selectedProject.value?.Templateid || selectedProject.value?.TemplateId
  return templates.value.find(item => String(item.Id || item.id) === String(templateId)) || null
})

const workflowSteps = computed(() => {
  const raw = projectWorkflow.value?.Steps || projectWorkflow.value?.steps
  return Array.isArray(raw) ? raw : []
})

const projectStatus = computed(() => String(projectWorkflow.value?.ProjectStatus || projectWorkflow.value?.projectStatus || 'draft'))

const datasetStep = computed(() => workflowSteps.value.find(step =>
  String(step.RunType || step.runType || '').toLowerCase() === 'dataset_build'
))

const ragStep = computed(() => workflowSteps.value.find(step =>
  String(step.RunType || step.runType || '').toLowerCase() === 'rag_index'
))

const chunkingError = computed(() => {
  const size = Number(ragForm.chunkSize || 0)
  const overlap = Number(ragForm.chunkOverlap || 0)
  if (size <= 0 || overlap < 0) return 'Valores de chunk inválidos.'
  if (overlap >= size) return 'Chunk overlap debe ser menor que chunk size.'
  return ''
})

const datasetCompleted = computed(() => {
  const status = String(datasetStep.value?.Status || datasetStep.value?.status || '').toLowerCase()
  return status === 'completed'
})

const canRunStage = computed(() => {
  if (!ragStep.value) return false
  const enabled = Boolean(ragStep.value.Enabled ?? ragStep.value.enabled)
  const available = Boolean(ragStep.value.Available ?? ragStep.value.available)
  const status = String(ragStep.value.Status || ragStep.value.status || '').toLowerCase()
  return enabled && (available || status === 'completed')
})

const filtersParsed = computed(() => parseJson(ragForm.filtersJson, {}))
const filtersJsonError = computed(() => (filtersParsed.value.ok ? '' : 'Filters JSON inválido'))

const canRun = computed(() =>
  Boolean(selectedProjectId.value) &&
  Boolean(String(ragForm.indexName || '').trim()) &&
  Boolean(String(ragForm.datasetVersion || '').trim()) &&
  Boolean(String(ragForm.embeddingModel || '').trim()) &&
  Number(ragForm.chunkSize || 0) >= 100 &&
  Number(ragForm.chunkOverlap || 0) >= 0 &&
  Number(ragForm.topK || 0) >= 1 &&
  !chunkingError.value &&
  !filtersJsonError.value &&
  canRunStage.value &&
  datasetCompleted.value
)

const ragRuns = computed(() => runs.value.filter(run =>
  String(run.RunType || run.runType || '').toLowerCase() === 'rag_index'
))

const latestRagRun = computed(() => ragRuns.value[0] || null)

const latestRagOutputPretty = computed(() => {
  if (!latestRagRun.value) return ''
  const output = latestRagRun.value.Outputjson || latestRagRun.value.outputjson || latestRagRun.value.LastError || latestRagRun.value.lastError || ''
  const parsed = parseJson(output, null)
  if (parsed.ok && parsed.value && typeof parsed.value === 'object') {
    return JSON.stringify(parsed.value, null, 2)
  }
  return String(output || 'Sin output')
})

const payloadPreview = computed(() => JSON.stringify(buildRunInputPayload(), null, 2))

function buildRunInputPayload() {
  const metadataFields = String(ragForm.metadataFieldsCsv || '')
    .split(',')
    .map(item => item.trim())
    .filter(Boolean)

  return {
    indexName: String(ragForm.indexName || '').trim(),
    mode: ragForm.reindexMode,
    vectorStore: ragForm.vectorStore,
    datasetVersion: String(ragForm.datasetVersion || '').trim(),
    embedding: {
      model: String(ragForm.embeddingModel || '').trim()
    },
    chunking: {
      size: Number(ragForm.chunkSize || 0),
      overlap: Number(ragForm.chunkOverlap || 0)
    },
    retrieval: {
      topK: Number(ragForm.topK || 0),
      rerank: Boolean(ragForm.enableRerank)
    },
    metadata: {
      fields: metadataFields,
      filterEnabled: Boolean(ragForm.enableMetadataFilter),
      filters: filtersParsed.value.ok ? (filtersParsed.value.value || {}) : {}
    },
    runtime: {
      failOnEmptyDataset: Boolean(ragForm.failOnEmptyDataset),
      persistPath: String(ragForm.persistPath || '').trim() || null
    }
  }
}

function suggestDefaultsFromProject() {
  if (!selectedProject.value) return

  if (!String(ragForm.indexName || '').trim()) {
    const slug = selectedProject.value.Slug || selectedProject.value.slug || `project-${selectedProjectId.value}`
    ragForm.indexName = `${slug}-kb-v1`
  }

  if (!String(ragForm.datasetVersion || '').trim()) {
    const latestDatasetRun = runs.value.find(run =>
      String(run.RunType || run.runType || '').toLowerCase() === 'dataset_build'
      && String(run.Status || run.status || '').toLowerCase() === 'completed'
    )
    const parsedInput = parseJson(latestDatasetRun?.Inputjson || latestDatasetRun?.inputjson || '{}', {})
    const version = parsedInput.value?.datasetVersion
    ragForm.datasetVersion = version || `v${new Date().toISOString().slice(0, 10).replaceAll('-', '')}`
  }
}

async function loadProjects() {
  const response = await runtimeApi.list('projects')
  projects.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
}

async function loadTemplates() {
  const response = await runtimeApi.list('templates')
  templates.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
}

async function loadWorkflow(projectId) {
  if (!projectId) {
    projectWorkflow.value = null
    return
  }
  const response = await runtimeApi.getProjectWorkflow(projectId)
  projectWorkflow.value = response?.data || null
}

async function loadRuns() {
  if (!selectedProjectId.value) {
    runs.value = []
    return
  }
  loadingRuns.value = true
  try {
    const response = await runtimeApi.listProjectRuns(selectedProjectId.value, 50)
    runs.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
  } finally {
    loadingRuns.value = false
  }
}

async function loadData() {
  loadingData.value = true
  error.value = ''
  try {
    await Promise.all([loadProjects(), loadTemplates()])
    if (!selectedProjectId.value && projects.value.length) {
      selectedProjectId.value = projects.value[0].Id || projects.value[0].id
    }
    await Promise.all([loadWorkflow(selectedProjectId.value), loadRuns()])
    suggestDefaultsFromProject()
  } catch (err) {
    error.value = errorText(err, 'No se pudo cargar la etapa RAG.')
  } finally {
    loadingData.value = false
  }
}

async function triggerRagIndex() {
  if (!canRun.value) return

  running.value = true
  runError.value = ''
  runMessage.value = ''

  try {
    const payload = buildRunInputPayload()
    await runtimeApi.triggerProjectRun(
      selectedProjectId.value,
      'rag_index',
      JSON.stringify(payload)
    )
    runMessage.value = 'RAG Index ejecutado. Revisa la salida en runs.'
    await Promise.all([loadWorkflow(selectedProjectId.value), loadRuns()])
  } catch (err) {
    runError.value = errorText(err, 'No se pudo ejecutar la indexación RAG.')
  } finally {
    running.value = false
  }
}

async function copyPayloadPreview() {
  try {
    await navigator.clipboard.writeText(payloadPreview.value)
  } catch {
    // no-op
  }
}

function goNext() {
  if (!selectedProjectId.value) return
  router.push({ path: '/stage/train', query: { projectId: selectedProjectId.value } })
}

watch(selectedProjectId, async id => {
  if (!id) {
    runs.value = []
    projectWorkflow.value = null
    return
  }
  runMessage.value = ''
  runError.value = ''
  ragForm.indexName = ''
  ragForm.datasetVersion = ''
  await Promise.all([loadWorkflow(id), loadRuns()])
  suggestDefaultsFromProject()
})

watch(() => route.query.projectId, value => {
  if (!value) return
  const asNumber = Number(value)
  selectedProjectId.value = Number.isFinite(asNumber) && asNumber > 0 ? asNumber : value
}, { immediate: true })

onMounted(loadData)
</script>

<style scoped>
.stage-page {
  padding-bottom: 30px;
}

.toggle-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0 10px;
}

.hint-list {
  margin: 0;
  padding-left: 18px;
  display: grid;
  gap: 8px;
}

.hint-list li {
  color: var(--sb-text-soft, var(--sb-muted));
}

.hint-list li.ok {
  color: var(--sb-primary, #2563eb);
  font-weight: 600;
}

.workflow-list {
  display: grid;
  gap: 8px;
}

.workflow-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.run-list {
  display: grid;
  gap: 10px;
}

.run-item {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  padding: 10px;
  background: rgba(37, 99, 235, 0.02);
}

.run-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.run-meta {
  color: var(--sb-text-soft, var(--sb-muted));
  font-size: 0.84rem;
  margin-top: 4px;
}

.run-error {
  color: var(--sb-danger, #dc2626);
  font-size: 0.84rem;
  margin-top: 6px;
}

.preview-box {
  border: 1px solid rgba(120, 140, 170, 0.22);
  border-radius: 12px;
  padding: 10px;
  max-height: 240px;
  overflow: auto;
}

.preview-box pre {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
  font-size: 0.8rem;
}

@media (max-width: 960px) {
  .toggle-grid {
    grid-template-columns: 1fr;
  }
}
</style>
