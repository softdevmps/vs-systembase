<template>
  <v-container fluid class="stage-page">
    <v-card class="card sb-page-header">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center">
          <div class="sb-page-icon"><v-icon color="primary" size="24">mdi-chart-line</v-icon></div>
          <div>
            <h2>Etapa 6 · Evaluación</h2>
            <p class="sb-page-subtitle">Mide calidad y define si el modelo está listo para pasar a deploy.</p>
          </div>
        </div>
        <div class="d-flex align-center ga-2 flex-wrap">
          <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-arrow-left" @click="router.push('/stage/train')">Volver</v-btn>
          <v-btn class="sb-btn primary" prepend-icon="mdi-arrow-right" :disabled="!selectedProjectId" @click="goNext">Ir a Deploy</v-btn>
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
          <v-card-title>Configuración de evaluación</v-card-title>
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
                  v-model="evalForm.suite"
                  :items="suiteOptions"
                  item-title="title"
                  item-value="value"
                  label="Suite"
                  density="comfortable"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="evalForm.datasetVersion"
                  label="Dataset version"
                  density="comfortable"
                  :rules="[rules.required, rules.max40]"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-select
                  v-model="evalForm.evalSplit"
                  :items="splitOptions"
                  item-title="title"
                  item-value="value"
                  label="Split"
                  density="comfortable"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="evalForm.thresholdGlobal"
                  type="number"
                  label="Threshold global"
                  density="comfortable"
                  min="0"
                  max="1"
                  step="0.01"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="evalForm.minPrecision"
                  type="number"
                  label="Min precision"
                  density="comfortable"
                  min="0"
                  max="1"
                  step="0.01"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="evalForm.minRecall"
                  type="number"
                  label="Min recall"
                  density="comfortable"
                  min="0"
                  max="1"
                  step="0.01"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="evalForm.sampleSize"
                  type="number"
                  label="Sample size"
                  density="comfortable"
                  min="1"
                  max="500000"
                  step="10"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="evalForm.maxLatencyMs"
                  type="number"
                  label="Max latency (ms)"
                  density="comfortable"
                  min="0"
                  max="20000"
                  step="10"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-select
                  v-model="evalForm.errorPolicy"
                  :items="errorPolicyOptions"
                  item-title="title"
                  item-value="value"
                  label="Error policy"
                  density="comfortable"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="evalForm.metricTag"
                  label="Metric tag"
                  density="comfortable"
                  placeholder="baseline-v1"
                  :rules="[rules.max80]"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="evalForm.outputReportName"
                  label="Nombre reporte"
                  density="comfortable"
                  placeholder="eval-report-v1"
                  :rules="[rules.max120]"
                />
              </v-col>
            </v-row>

            <div class="toggle-grid mb-2">
              <v-checkbox v-model="evalForm.compareWithBaseline" hide-details density="compact" label="Comparar baseline" />
              <v-checkbox v-model="evalForm.includeConfusionMatrix" hide-details density="compact" label="Confusion matrix" />
              <v-checkbox v-model="evalForm.runSmokeTestOnDeployed" hide-details density="compact" label="Smoke test deploy" />
            </div>

            <v-textarea
              v-model="evalForm.metricThresholdsJson"
              label="Thresholds por métrica (JSON)"
              auto-grow
              rows="3"
              density="comfortable"
              :error-messages="metricThresholdsError ? [metricThresholdsError] : []"
              placeholder='{"f1":0.85,"exactMatch":0.80}'
            />

            <v-textarea
              v-model="evalForm.extraJson"
              label="Overrides opcionales (JSON)"
              auto-grow
              rows="3"
              density="comfortable"
              :error-messages="extraJsonError ? [extraJsonError] : []"
              placeholder='{"slices":["barrio","tipoHecho"],"bootstrapSamples":1000}'
            />

            <v-alert
              v-if="numericError"
              class="mb-2"
              type="warning"
              variant="tonal"
              density="comfortable"
              :text="numericError"
            />

            <div class="d-flex align-center ga-2 mt-3 flex-wrap">
              <v-btn
                class="sb-btn primary"
                prepend-icon="mdi-play"
                :loading="running"
                :disabled="!canRun"
                @click="triggerEval"
              >
                Ejecutar evaluación
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
            Últimos runs de evaluación
            <v-btn icon variant="text" :disabled="loadingRuns" @click="loadRuns">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loadingRuns" class="sb-skeleton" style="height: 120px;"></div>
            <div v-else-if="!evalRuns.length" class="text-caption text-medium-emphasis">No hay corridas de evaluación.</div>
            <div v-else class="run-list">
              <div v-for="run in evalRuns" :key="run.Id || run.id" class="run-item">
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
          <v-card-title>Gate de release</v-card-title>
          <v-divider />
          <v-card-text>
            <ul class="hint-list">
              <li :class="{ ok: !!selectedProjectId }">Proyecto seleccionado</li>
              <li :class="{ ok: trainCompleted }">Train LoRA completado</li>
              <li :class="{ ok: canRunStage }">Etapa habilitada en workflow</li>
              <li :class="{ ok: !metricThresholdsError }">JSON de thresholds válido</li>
              <li :class="{ ok: !extraJsonError }">JSON de overrides válido</li>
              <li :class="{ ok: !numericError }">Parámetros numéricos válidos</li>
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
          <v-card-title>Último output Eval</v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="!latestEvalRun" class="text-caption text-medium-emphasis">
              Todavía no hay output de evaluación.
            </div>
            <div v-else class="preview-box">
              <pre>{{ latestEvalOutputPretty }}</pre>
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
const selectedProjectId = ref(null)
const projectWorkflow = ref(null)
const runs = ref([])

const evalForm = reactive({
  suite: 'default',
  datasetVersion: '',
  evalSplit: 'test',
  thresholdGlobal: 0.85,
  minPrecision: 0.85,
  minRecall: 0.8,
  sampleSize: 500,
  maxLatencyMs: 1200,
  errorPolicy: 'fail_on_threshold',
  metricTag: '',
  outputReportName: '',
  compareWithBaseline: true,
  includeConfusionMatrix: true,
  runSmokeTestOnDeployed: false,
  metricThresholdsJson: '{"f1":0.85,"exactMatch":0.80}',
  extraJson: '{}'
})

const suiteOptions = [
  { title: 'Default', value: 'default' },
  { title: 'Regresión', value: 'regression' },
  { title: 'Calidad estricta', value: 'strict' },
  { title: 'Aceptación negocio', value: 'business_acceptance' }
]

const splitOptions = [
  { title: 'test', value: 'test' },
  { title: 'validation', value: 'validation' },
  { title: 'holdout_custom', value: 'holdout_custom' }
]

const errorPolicyOptions = [
  { title: 'Fallar si no cumple threshold', value: 'fail_on_threshold' },
  { title: 'Warn y continuar', value: 'warn_only' }
]

const rules = {
  required: v => !!String(v ?? '').trim() || 'Campo requerido',
  max40: v => String(v ?? '').length <= 40 || 'Máximo 40 caracteres',
  max80: v => String(v ?? '').length <= 80 || 'Máximo 80 caracteres',
  max120: v => String(v ?? '').length <= 120 || 'Máximo 120 caracteres'
}

const optionGuideItems = [
  { label: 'Suite y split', description: 'Define qué pruebas correr y sobre qué subconjunto del dataset evaluar.' },
  { label: 'Threshold global', description: 'Umbral mínimo general para considerar el modelo apto.' },
  { label: 'Min precision / recall', description: 'Gate de calidad para evitar pasar modelos con bajo desempeño.' },
  { label: 'Sample size y latencia máxima', description: 'Controla tamaño de muestra y límite de performance esperado.' },
  { label: 'Comparar baseline', description: 'Compara resultados contra una versión de referencia previa.' },
  { label: 'Thresholds JSON', description: 'Permite reglas por métrica (f1, exactMatch, etc.) además del global.' }
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
  ;['Id', 'Name', 'RunType', 'Status', 'ProgressPct', 'CreatedAt', 'UpdatedAt', 'Inputjson', 'Outputjson'].forEach(ensure)
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

const workflowSteps = computed(() => {
  const raw = projectWorkflow.value?.Steps || projectWorkflow.value?.steps
  return Array.isArray(raw) ? raw : []
})

const projectStatus = computed(() => String(projectWorkflow.value?.ProjectStatus || projectWorkflow.value?.projectStatus || 'draft'))

const trainStep = computed(() => workflowSteps.value.find(step =>
  String(step.RunType || step.runType || '').toLowerCase() === 'train_lora'
))

const evalStep = computed(() => workflowSteps.value.find(step =>
  String(step.RunType || step.runType || '').toLowerCase() === 'eval_run'
))

const trainCompleted = computed(() => {
  const status = String(trainStep.value?.Status || trainStep.value?.status || '').toLowerCase()
  return status === 'completed'
})

const canRunStage = computed(() => {
  if (!evalStep.value) return false
  const enabled = Boolean(evalStep.value.Enabled ?? evalStep.value.enabled)
  const available = Boolean(evalStep.value.Available ?? evalStep.value.available)
  const status = String(evalStep.value.Status || evalStep.value.status || '').toLowerCase()
  return enabled && (available || status === 'completed')
})

const metricThresholdsParsed = computed(() => parseJson(evalForm.metricThresholdsJson, {}))
const metricThresholdsError = computed(() => (metricThresholdsParsed.value.ok ? '' : 'Thresholds JSON inválido'))

const extraParsed = computed(() => parseJson(evalForm.extraJson, {}))
const extraJsonError = computed(() => (extraParsed.value.ok ? '' : 'Overrides JSON inválido'))

const numericError = computed(() => {
  const isValidMetric = value => Number(value) >= 0 && Number(value) <= 1
  if (!isValidMetric(evalForm.thresholdGlobal)) return 'Threshold global debe estar entre 0 y 1.'
  if (!isValidMetric(evalForm.minPrecision)) return 'Min precision debe estar entre 0 y 1.'
  if (!isValidMetric(evalForm.minRecall)) return 'Min recall debe estar entre 0 y 1.'
  if (Number(evalForm.sampleSize || 0) < 1) return 'Sample size debe ser >= 1.'
  if (Number(evalForm.maxLatencyMs || 0) < 0) return 'Max latency debe ser >= 0.'
  return ''
})

const canRun = computed(() =>
  Boolean(selectedProjectId.value) &&
  Boolean(String(evalForm.datasetVersion || '').trim()) &&
  !metricThresholdsError.value &&
  !extraJsonError.value &&
  !numericError.value &&
  canRunStage.value &&
  trainCompleted.value
)

const evalRuns = computed(() => runs.value.filter(run =>
  String(run.RunType || run.runType || '').toLowerCase() === 'eval_run'
))

const latestEvalRun = computed(() => evalRuns.value[0] || null)

const latestEvalOutputPretty = computed(() => {
  if (!latestEvalRun.value) return ''
  const output = latestEvalRun.value.Outputjson || latestEvalRun.value.outputjson || latestEvalRun.value.LastError || latestEvalRun.value.lastError || ''
  const parsed = parseJson(output, null)
  if (parsed.ok && parsed.value && typeof parsed.value === 'object') {
    return JSON.stringify(parsed.value, null, 2)
  }
  return String(output || 'Sin output')
})

const payloadPreview = computed(() => JSON.stringify(buildRunInputPayload(), null, 2))

function buildRunInputPayload() {
  return {
    suite: evalForm.suite,
    datasetVersion: String(evalForm.datasetVersion || '').trim(),
    split: evalForm.evalSplit,
    thresholds: {
      global: Number(evalForm.thresholdGlobal || 0),
      precision: Number(evalForm.minPrecision || 0),
      recall: Number(evalForm.minRecall || 0),
      metrics: metricThresholdsParsed.value.ok ? (metricThresholdsParsed.value.value || {}) : {}
    },
    sampleSize: Number(evalForm.sampleSize || 0),
    maxLatencyMs: Number(evalForm.maxLatencyMs || 0),
    errorPolicy: evalForm.errorPolicy,
    compareWithBaseline: Boolean(evalForm.compareWithBaseline),
    includeConfusionMatrix: Boolean(evalForm.includeConfusionMatrix),
    runSmokeTestOnDeployed: Boolean(evalForm.runSmokeTestOnDeployed),
    metricTag: String(evalForm.metricTag || '').trim() || null,
    outputReportName: String(evalForm.outputReportName || '').trim() || null,
    overrides: extraParsed.value.ok ? (extraParsed.value.value || {}) : {}
  }
}

function suggestDefaultsFromProject() {
  if (!selectedProjectId.value) return

  if (!String(evalForm.datasetVersion || '').trim()) {
    const latestDatasetRun = runs.value.find(run =>
      String(run.RunType || run.runType || '').toLowerCase() === 'dataset_build'
      && String(run.Status || run.status || '').toLowerCase() === 'completed'
    )
    const parsedInput = parseJson(latestDatasetRun?.Inputjson || latestDatasetRun?.inputjson || '{}', {})
    const version = parsedInput.value?.datasetVersion
    evalForm.datasetVersion = version || `v${new Date().toISOString().slice(0, 10).replaceAll('-', '')}`
  }

  if (!String(evalForm.metricTag || '').trim()) {
    evalForm.metricTag = `eval-${new Date().toISOString().slice(0, 10)}`
  }

  if (!String(evalForm.outputReportName || '').trim()) {
    evalForm.outputReportName = `report-${new Date().toISOString().slice(0, 10)}`
  }
}

async function loadProjects() {
  const response = await runtimeApi.list('projects')
  projects.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
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
    await loadProjects()
    if (!selectedProjectId.value && projects.value.length) {
      selectedProjectId.value = projects.value[0].Id || projects.value[0].id
    }
    await Promise.all([loadWorkflow(selectedProjectId.value), loadRuns()])
    suggestDefaultsFromProject()
  } catch (err) {
    error.value = errorText(err, 'No se pudo cargar la etapa Eval.')
  } finally {
    loadingData.value = false
  }
}

async function triggerEval() {
  if (!canRun.value) return

  running.value = true
  runError.value = ''
  runMessage.value = ''

  try {
    const payload = buildRunInputPayload()
    await runtimeApi.triggerProjectRun(
      selectedProjectId.value,
      'eval_run',
      JSON.stringify(payload)
    )
    runMessage.value = 'Evaluación ejecutada. Revisa la salida en runs.'
    await Promise.all([loadWorkflow(selectedProjectId.value), loadRuns()])
  } catch (err) {
    runError.value = errorText(err, 'No se pudo ejecutar la evaluación.')
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
  router.push({ path: '/stage/deploy', query: { projectId: selectedProjectId.value } })
}

watch(selectedProjectId, async id => {
  if (!id) {
    runs.value = []
    projectWorkflow.value = null
    return
  }
  runMessage.value = ''
  runError.value = ''
  evalForm.datasetVersion = ''
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
