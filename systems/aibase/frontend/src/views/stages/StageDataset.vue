<template>
  <v-container fluid class="stage-page">
    <v-card class="card sb-page-header">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center">
          <div class="sb-page-icon"><v-icon color="primary" size="24">mdi-database-cog</v-icon></div>
          <div>
            <h2>Etapa 3 · Dataset</h2>
            <p class="sb-page-subtitle">Configura la construcción del dataset y ejecuta la corrida de preparación.</p>
          </div>
        </div>
        <div class="d-flex align-center ga-2 flex-wrap">
          <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-arrow-left" @click="router.push('/stage/project')">Volver</v-btn>
          <v-btn class="sb-btn primary" prepend-icon="mdi-arrow-right" :disabled="!selectedProjectId" @click="goNext">Ir a RAG</v-btn>
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
          <v-card-title>Configuración de dataset</v-card-title>
          <v-divider />
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="7">
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
              <v-col cols="12" md="5">
                <v-text-field
                  v-model="datasetForm.datasetVersion"
                  label="Versión dataset"
                  density="comfortable"
                  :rules="[rules.required, rules.max30]"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="4">
                <v-select
                  v-model="datasetForm.sourceType"
                  :items="sourceTypeOptions"
                  item-title="title"
                  item-value="value"
                  label="Fuente"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="8">
                <v-text-field
                  v-model="datasetForm.sourcePath"
                  label="Path/URI de fuente"
                  density="comfortable"
                  placeholder="/data/incidentes.csv o s3://bucket/file.jsonl"
                  :rules="[rules.required, rules.max300]"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="datasetForm.splitTrainPct"
                  type="number"
                  label="Train %"
                  density="comfortable"
                  min="0"
                  max="100"
                  step="1"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="datasetForm.splitValPct"
                  type="number"
                  label="Validation %"
                  density="comfortable"
                  min="0"
                  max="100"
                  step="1"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="datasetForm.splitTestPct"
                  type="number"
                  label="Test %"
                  density="comfortable"
                  min="0"
                  max="100"
                  step="1"
                />
              </v-col>
            </v-row>

            <v-alert
              v-if="splitTotal !== 100"
              class="mb-2"
              type="warning"
              variant="tonal"
              density="comfortable"
              :text="`El split debe sumar 100%. Actual: ${splitTotal}%`"
            />

            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="datasetForm.minRecords"
                  type="number"
                  label="Mínimo de registros"
                  density="comfortable"
                  min="1"
                  step="10"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="datasetForm.maxNullPct"
                  type="number"
                  label="% máximo nulos"
                  density="comfortable"
                  min="0"
                  max="100"
                  step="1"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model="datasetForm.tagsCsv"
                  label="Tags (csv)"
                  density="comfortable"
                  placeholder="baseline, v1, demo"
                  :rules="[rules.max200]"
                />
              </v-col>
            </v-row>

            <div class="toggle-grid mb-2">
              <v-checkbox v-model="datasetForm.deduplicate" hide-details density="compact" label="Deduplicar" />
              <v-checkbox v-model="datasetForm.normalizeText" hide-details density="compact" label="Normalizar texto" />
              <v-checkbox v-model="datasetForm.dropNullTarget" hide-details density="compact" label="Excluir target nulo" />
            </div>

            <v-textarea
              v-model="datasetForm.sampleRowsJson"
              label="Muestra opcional de registros (JSON array)"
              auto-grow
              rows="4"
              density="comfortable"
              :error-messages="sampleJsonError ? [sampleJsonError] : []"
              placeholder='[{"input":"texto 1","target":"json esperado"}]'
            />

            <div class="d-flex align-center ga-2 mt-3 flex-wrap">
              <v-btn
                class="sb-btn primary"
                prepend-icon="mdi-play"
                :loading="running"
                :disabled="!canRun"
                @click="triggerDatasetBuild"
              >
                Ejecutar Build Dataset
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
            Últimos runs de dataset
            <v-btn icon variant="text" :disabled="loadingRuns" @click="loadRuns">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loadingRuns" class="sb-skeleton" style="height: 120px;"></div>
            <div v-else-if="!datasetRuns.length" class="text-caption text-medium-emphasis">No hay corridas de dataset.</div>
            <div v-else class="run-list">
              <div v-for="run in datasetRuns" :key="run.Id || run.id" class="run-item">
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
          <v-card-title class="d-flex align-center justify-space-between">
            Contrato activo del template
            <v-chip v-if="selectedTemplateName" size="x-small" variant="tonal">{{ selectedTemplateName }}</v-chip>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <template v-if="templateContract">
              <div class="contract-item">
                <div class="contract-title">Objetivo</div>
                <div class="contract-body">{{ templateContract.objective || '—' }}</div>
              </div>
              <div class="contract-item">
                <div class="contract-title">Campos esperados</div>
                <div class="contract-body">
                  <v-chip
                    v-for="field in outputSchemaFields"
                    :key="field"
                    class="mr-1 mb-1"
                    size="x-small"
                    variant="tonal"
                    color="primary"
                  >
                    {{ field }}
                  </v-chip>
                  <span v-if="!outputSchemaFields.length" class="text-caption text-medium-emphasis">No definidos.</span>
                </div>
              </div>
              <div class="contract-item">
                <div class="contract-title">Reglas requeridas</div>
                <div class="contract-body">
                  <v-chip
                    v-for="field in requiredFields"
                    :key="field"
                    class="mr-1 mb-1"
                    size="x-small"
                    variant="tonal"
                    color="teal"
                  >
                    {{ field }}
                  </v-chip>
                  <span v-if="!requiredFields.length" class="text-caption text-medium-emphasis">Sin campos obligatorios definidos.</span>
                </div>
              </div>
            </template>
            <div v-else class="text-caption text-medium-emphasis">
              El template no tiene contrato explícito o el JSON de pipeline no es válido.
            </div>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title>Checklist de etapa</v-card-title>
          <v-divider />
          <v-card-text>
            <ul class="hint-list">
              <li :class="{ ok: !!selectedProjectId }">Proyecto seleccionado</li>
              <li :class="{ ok: !!datasetForm.sourcePath.trim() }">Fuente de datos definida</li>
              <li :class="{ ok: splitTotal === 100 }">Split train/val/test correcto</li>
              <li :class="{ ok: !sampleJsonError }">JSON de muestra válido</li>
              <li :class="{ ok: canRunStage }">Etapa habilitada por workflow</li>
            </ul>
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

const datasetForm = reactive({
  datasetVersion: defaultDatasetVersion(),
  sourceType: 'csv',
  sourcePath: '',
  splitTrainPct: 80,
  splitValPct: 10,
  splitTestPct: 10,
  minRecords: 200,
  maxNullPct: 5,
  deduplicate: true,
  normalizeText: true,
  dropNullTarget: true,
  tagsCsv: 'baseline,v1',
  sampleRowsJson: ''
})

const rules = {
  required: v => !!String(v ?? '').trim() || 'Campo requerido',
  max30: v => String(v ?? '').length <= 30 || 'Máximo 30 caracteres',
  max200: v => String(v ?? '').length <= 200 || 'Máximo 200 caracteres',
  max300: v => String(v ?? '').length <= 300 || 'Máximo 300 caracteres'
}

const optionGuideItems = [
  { label: 'Versión dataset', description: 'Etiqueta reproducible del dataset para auditoría y comparación de corridas.' },
  { label: 'Fuente / Path', description: 'Indica de dónde se importan los datos crudos para construir el dataset.' },
  { label: 'Split train/validation/test', description: 'Define la partición del dataset para entrenar y evaluar.' },
  { label: 'Deduplicar / Normalizar / Drop null', description: 'Aplica limpieza automática antes de generar el dataset final.' },
  { label: 'Mínimo registros / % nulos', description: 'Quality gates para evitar entrenar con datos insuficientes o sucios.' },
  { label: 'Sample rows (JSON)', description: 'Muestra ejemplos de registros para validar mapeo y contrato antes de correr.' }
]

const sourceTypeOptions = [
  { title: 'CSV tabular', value: 'csv' },
  { title: 'JSONL (registro por línea)', value: 'jsonl' },
  { title: 'Transcripciones', value: 'transcripts' },
  { title: 'Manifest audio', value: 'audio_manifest' }
]

function defaultDatasetVersion() {
  const now = new Date()
  const mm = String(now.getMonth() + 1).padStart(2, '0')
  const dd = String(now.getDate()).padStart(2, '0')
  return `v${now.getFullYear()}${mm}${dd}`
}

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
    'Id', 'Name', 'TemplateId', 'Templateid', 'Pipelinejson',
    'Status', 'RunType', 'ProgressPct', 'CreatedAt', 'UpdatedAt'
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

const selectedTemplateName = computed(() => selectedTemplate.value?.Name || selectedTemplate.value?.name || '')

const templateContract = computed(() => {
  const raw = selectedTemplate.value?.Pipelinejson || selectedTemplate.value?.pipelinejson
  if (!raw) return null
  const parsed = parseJson(raw, null)
  return parsed.ok ? (parsed.value?.contract || null) : null
})

const outputSchemaFields = computed(() => {
  const schema = templateContract.value?.outputSchema
  return schema && typeof schema === 'object' && !Array.isArray(schema)
    ? Object.keys(schema)
    : []
})

const requiredFields = computed(() => {
  const required = templateContract.value?.validationRules?.required
  return Array.isArray(required) ? required : []
})

const splitTotal = computed(() =>
  Number(datasetForm.splitTrainPct || 0) + Number(datasetForm.splitValPct || 0) + Number(datasetForm.splitTestPct || 0)
)

const sampleRowsParsed = computed(() => parseJson(datasetForm.sampleRowsJson, []))
const sampleJsonError = computed(() => (sampleRowsParsed.value.ok ? '' : 'La muestra JSON no es válida'))

const workflowSteps = computed(() => {
  const raw = projectWorkflow.value?.Steps || projectWorkflow.value?.steps
  return Array.isArray(raw) ? raw : []
})

const datasetStep = computed(() => workflowSteps.value.find(step =>
  String(step.RunType || step.runType || '').toLowerCase() === 'dataset_build'
))

const canRunStage = computed(() => {
  if (!datasetStep.value) return false
  const enabled = Boolean(datasetStep.value.Enabled ?? datasetStep.value.enabled)
  const available = Boolean(datasetStep.value.Available ?? datasetStep.value.available)
  const status = String(datasetStep.value.Status || datasetStep.value.status || '').toLowerCase()
  return enabled && (available || status === 'completed')
})

const canRun = computed(() =>
  Boolean(selectedProjectId.value) &&
  Boolean(String(datasetForm.datasetVersion || '').trim()) &&
  Boolean(String(datasetForm.sourcePath || '').trim()) &&
  splitTotal.value === 100 &&
  !sampleJsonError.value &&
  canRunStage.value
)

const payloadPreview = computed(() => JSON.stringify(buildRunInputPayload(), null, 2))

const datasetRuns = computed(() => runs.value.filter(run =>
  String(run.RunType || run.runType || '').toLowerCase() === 'dataset_build'
))

function buildRunInputPayload() {
  const tags = String(datasetForm.tagsCsv || '')
    .split(',')
    .map(item => item.trim())
    .filter(Boolean)

  return {
    datasetVersion: String(datasetForm.datasetVersion || '').trim(),
    source: {
      type: datasetForm.sourceType,
      path: String(datasetForm.sourcePath || '').trim()
    },
    split: {
      trainPct: Number(datasetForm.splitTrainPct || 0),
      valPct: Number(datasetForm.splitValPct || 0),
      testPct: Number(datasetForm.splitTestPct || 0)
    },
    transforms: {
      deduplicate: Boolean(datasetForm.deduplicate),
      normalizeText: Boolean(datasetForm.normalizeText),
      dropNullTarget: Boolean(datasetForm.dropNullTarget)
    },
    qualityGates: {
      minRecords: Number(datasetForm.minRecords || 1),
      maxNullPct: Number(datasetForm.maxNullPct || 0)
    },
    expectedOutputFields: outputSchemaFields.value,
    requiredFields: requiredFields.value,
    tags,
    sampleRows: sampleRowsParsed.value.ok ? sampleRowsParsed.value.value : []
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
  } catch (err) {
    error.value = errorText(err, 'No se pudo cargar la etapa de dataset.')
  } finally {
    loadingData.value = false
  }
}

async function triggerDatasetBuild() {
  if (!canRun.value) return

  running.value = true
  runError.value = ''
  runMessage.value = ''

  try {
    const payload = buildRunInputPayload()
    await runtimeApi.triggerProjectRun(
      selectedProjectId.value,
      'dataset_build',
      JSON.stringify(payload)
    )
    runMessage.value = 'Build Dataset ejecutado. Revisa la salida en runs.'
    await Promise.all([loadWorkflow(selectedProjectId.value), loadRuns()])
  } catch (err) {
    runError.value = errorText(err, 'No se pudo ejecutar el build de dataset.')
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
  router.push({ path: '/stage/rag', query: { projectId: selectedProjectId.value } })
}

watch(selectedProjectId, async id => {
  if (!id) return
  await Promise.all([loadWorkflow(id), loadRuns()])
})

watch(() => route.query.projectId, value => {
  if (!value) return
  selectedProjectId.value = Number(value)
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

.contract-item + .contract-item {
  margin-top: 10px;
}

.contract-title {
  font-size: 0.78rem;
  color: var(--sb-text-soft, var(--sb-muted));
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.contract-body {
  margin-top: 4px;
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

.preview-box {
  border: 1px solid rgba(120, 140, 170, 0.22);
  border-radius: 12px;
  padding: 10px;
  max-height: 260px;
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
