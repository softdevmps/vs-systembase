<template>
  <v-container fluid class="stage-page">
    <v-card class="card sb-page-header">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center">
          <div class="sb-page-icon"><v-icon color="primary" size="24">mdi-brain</v-icon></div>
          <div>
            <h2>Etapa 5 · Train LoRA</h2>
            <p class="sb-page-subtitle">Entrena el adapter del modelo con el dataset y configuración del proyecto.</p>
          </div>
        </div>
        <div class="d-flex align-center ga-2 flex-wrap">
          <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-arrow-left" @click="router.push('/stage/rag')">Volver</v-btn>
          <v-btn class="sb-btn primary" prepend-icon="mdi-arrow-right" :disabled="!selectedProjectId" @click="goNext">Ir a Eval</v-btn>
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
          <v-card-title>Configuración de entrenamiento</v-card-title>
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
                  v-model="trainForm.profile"
                  :items="profileOptions"
                  item-title="title"
                  item-value="value"
                  label="Perfil"
                  density="comfortable"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="trainForm.datasetVersion"
                  label="Dataset version"
                  density="comfortable"
                  :rules="[rules.required, rules.max40]"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="trainForm.baseModel"
                  label="Base model"
                  density="comfortable"
                  :rules="[rules.required, rules.max120]"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="trainForm.adapterName"
                  label="Adapter name"
                  density="comfortable"
                  :rules="[rules.required, rules.max120]"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="trainForm.registryTag"
                  label="Registry tag"
                  density="comfortable"
                  :rules="[rules.max80]"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="trainForm.epochs"
                  type="number"
                  label="Epochs"
                  density="comfortable"
                  min="1"
                  max="20"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="trainForm.batchSize"
                  type="number"
                  label="Batch size"
                  density="comfortable"
                  min="1"
                  max="256"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="trainForm.gradAccum"
                  type="number"
                  label="Grad accum"
                  density="comfortable"
                  min="1"
                  max="64"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="trainForm.learningRate"
                  type="number"
                  label="Learning rate"
                  density="comfortable"
                  step="0.00001"
                  min="0.000001"
                  max="0.01"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="trainForm.loraRank"
                  type="number"
                  label="LoRA rank (r)"
                  density="comfortable"
                  min="2"
                  max="256"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="trainForm.loraAlpha"
                  type="number"
                  label="LoRA alpha"
                  density="comfortable"
                  min="1"
                  max="512"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="trainForm.loraDropout"
                  type="number"
                  label="LoRA dropout"
                  density="comfortable"
                  min="0"
                  max="0.9"
                  step="0.01"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="trainForm.maxSeqLen"
                  type="number"
                  label="Max seq len"
                  density="comfortable"
                  min="128"
                  max="8192"
                  step="64"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-select
                  v-model="trainForm.precision"
                  :items="precisionOptions"
                  item-title="title"
                  item-value="value"
                  label="Precision"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-select
                  v-model="trainForm.quantization"
                  :items="quantizationOptions"
                  item-title="title"
                  item-value="value"
                  label="Quantization"
                  density="comfortable"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model.number="trainForm.warmupRatio"
                  type="number"
                  label="Warmup ratio"
                  density="comfortable"
                  min="0"
                  max="0.5"
                  step="0.01"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model.number="trainForm.evalEverySteps"
                  type="number"
                  label="Eval cada N steps"
                  density="comfortable"
                  min="0"
                  max="5000"
                  step="10"
                />
              </v-col>
            </v-row>

            <v-alert
              v-if="numericError"
              class="mb-2"
              type="warning"
              variant="tonal"
              density="comfortable"
              :text="numericError"
            />

            <div class="toggle-grid mb-2">
              <v-checkbox v-model="trainForm.gradientCheckpointing" hide-details density="compact" label="Gradient checkpointing" />
              <v-checkbox v-model="trainForm.earlyStopping" hide-details density="compact" label="Early stopping" />
              <v-checkbox v-model="trainForm.exportCompact" hide-details density="compact" label="Export compact model" />
            </div>

            <v-textarea
              v-model="trainForm.targetModulesCsv"
              label="Target modules (csv)"
              auto-grow
              rows="2"
              density="comfortable"
              placeholder="q_proj,k_proj,v_proj,o_proj"
              :rules="[rules.max200]"
            />

            <v-textarea
              v-model="trainForm.extraJson"
              label="Overrides opcionales (JSON)"
              auto-grow
              rows="3"
              density="comfortable"
              :error-messages="extraJsonError ? [extraJsonError] : []"
              placeholder='{"optimizer":"adamw_8bit","weightDecay":0.01}'
            />

            <div class="d-flex align-center ga-2 mt-3 flex-wrap">
              <v-btn
                class="sb-btn primary"
                prepend-icon="mdi-play"
                :loading="running"
                :disabled="!canRun"
                @click="triggerTrain"
              >
                Ejecutar Train LoRA
              </v-btn>
              <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-refresh" @click="applyProfileDefaults">
                Reaplicar perfil
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
            Últimos runs de Train
            <v-btn icon variant="text" :disabled="loadingRuns" @click="loadRuns">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loadingRuns" class="sb-skeleton" style="height: 120px;"></div>
            <div v-else-if="!trainRuns.length" class="text-caption text-medium-emphasis">No hay corridas de entrenamiento.</div>
            <div v-else class="run-list">
              <div v-for="run in trainRuns" :key="run.Id || run.id" class="run-item">
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
              <li :class="{ ok: ragSatisfied }">RAG index listo (si aplica)</li>
              <li :class="{ ok: canRunStage }">Etapa habilitada en workflow</li>
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
          <v-card-title>Último output Train</v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="!latestTrainRun" class="text-caption text-medium-emphasis">
              Todavía no hay output de entrenamiento.
            </div>
            <div v-else class="preview-box">
              <pre>{{ latestTrainOutputPretty }}</pre>
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

const trainForm = reactive({
  profile: 'balanced',
  datasetVersion: '',
  baseModel: 'qwen2.5:7b',
  adapterName: '',
  registryTag: '',
  epochs: 3,
  batchSize: 8,
  gradAccum: 2,
  learningRate: 0.0001,
  loraRank: 16,
  loraAlpha: 32,
  loraDropout: 0.05,
  maxSeqLen: 2048,
  precision: 'bf16',
  quantization: '4bit',
  warmupRatio: 0.03,
  evalEverySteps: 100,
  gradientCheckpointing: true,
  earlyStopping: true,
  exportCompact: true,
  targetModulesCsv: 'q_proj,k_proj,v_proj,o_proj',
  extraJson: '{}'
})

const profileOptions = [
  { title: 'Rápido (POC)', value: 'quick' },
  { title: 'Balanceado', value: 'balanced' },
  { title: 'Calidad máxima', value: 'quality' }
]

const precisionOptions = [
  { title: 'bf16', value: 'bf16' },
  { title: 'fp16', value: 'fp16' },
  { title: 'fp32', value: 'fp32' }
]

const quantizationOptions = [
  { title: '4bit', value: '4bit' },
  { title: '8bit', value: '8bit' },
  { title: 'none', value: 'none' }
]

const rules = {
  required: v => !!String(v ?? '').trim() || 'Campo requerido',
  max40: v => String(v ?? '').length <= 40 || 'Máximo 40 caracteres',
  max80: v => String(v ?? '').length <= 80 || 'Máximo 80 caracteres',
  max120: v => String(v ?? '').length <= 120 || 'Máximo 120 caracteres',
  max200: v => String(v ?? '').length <= 200 || 'Máximo 200 caracteres'
}

const optionGuideItems = [
  { label: 'Perfil de entrenamiento', description: 'Carga un set base de hiperparámetros (rápido, balanceado o calidad).' },
  { label: 'Base model + Adapter name', description: 'Modelo base a ajustar y nombre del adapter LoRA resultante.' },
  { label: 'Epochs / Batch / Learning rate', description: 'Controlan intensidad, velocidad y estabilidad del entrenamiento.' },
  { label: 'LoRA rank/alpha/dropout', description: 'Ajustan capacidad del adapter y riesgo de sobreajuste.' },
  { label: 'Precision / Quantization', description: 'Balance entre consumo de recursos, velocidad y calidad.' },
  { label: 'Export compact model', description: 'Genera un artefacto ligero listo para despliegue portable.' }
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
    'Status', 'RunType', 'ProgressPct', 'CreatedAt', 'UpdatedAt', 'Inputjson', 'Outputjson'
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

const trainStep = computed(() => workflowSteps.value.find(step =>
  String(step.RunType || step.runType || '').toLowerCase() === 'train_lora'
))

const datasetCompleted = computed(() => {
  const status = String(datasetStep.value?.Status || datasetStep.value?.status || '').toLowerCase()
  return status === 'completed'
})

const ragSatisfied = computed(() => {
  if (!ragStep.value) return true
  const enabled = Boolean(ragStep.value.Enabled ?? ragStep.value.enabled)
  if (!enabled) return true
  const status = String(ragStep.value.Status || ragStep.value.status || '').toLowerCase()
  return status === 'completed'
})

const canRunStage = computed(() => {
  if (!trainStep.value) return false
  const enabled = Boolean(trainStep.value.Enabled ?? trainStep.value.enabled)
  const available = Boolean(trainStep.value.Available ?? trainStep.value.available)
  const status = String(trainStep.value.Status || trainStep.value.status || '').toLowerCase()
  return enabled && (available || status === 'completed')
})

const extraParsed = computed(() => parseJson(trainForm.extraJson, {}))
const extraJsonError = computed(() => (extraParsed.value.ok ? '' : 'Overrides JSON inválido'))

const numericError = computed(() => {
  if (Number(trainForm.epochs || 0) < 1) return 'Epochs debe ser >= 1.'
  if (Number(trainForm.batchSize || 0) < 1) return 'Batch size debe ser >= 1.'
  if (Number(trainForm.gradAccum || 0) < 1) return 'Grad accum debe ser >= 1.'
  if (Number(trainForm.learningRate || 0) <= 0) return 'Learning rate debe ser > 0.'
  if (Number(trainForm.loraRank || 0) < 2) return 'LoRA rank debe ser >= 2.'
  if (Number(trainForm.loraAlpha || 0) < 1) return 'LoRA alpha debe ser >= 1.'
  if (Number(trainForm.loraDropout || 0) < 0 || Number(trainForm.loraDropout || 0) >= 1) return 'LoRA dropout debe estar entre 0 y 0.99.'
  if (Number(trainForm.warmupRatio || 0) < 0 || Number(trainForm.warmupRatio || 0) > 0.5) return 'Warmup ratio debe estar entre 0 y 0.5.'
  if (Number(trainForm.maxSeqLen || 0) < 128) return 'Max seq len debe ser >= 128.'
  return ''
})

const canRun = computed(() =>
  Boolean(selectedProjectId.value) &&
  Boolean(String(trainForm.datasetVersion || '').trim()) &&
  Boolean(String(trainForm.baseModel || '').trim()) &&
  Boolean(String(trainForm.adapterName || '').trim()) &&
  !extraJsonError.value &&
  !numericError.value &&
  canRunStage.value &&
  datasetCompleted.value &&
  ragSatisfied.value
)

const trainRuns = computed(() => runs.value.filter(run =>
  String(run.RunType || run.runType || '').toLowerCase() === 'train_lora'
))

const latestTrainRun = computed(() => trainRuns.value[0] || null)

const latestTrainOutputPretty = computed(() => {
  if (!latestTrainRun.value) return ''
  const output = latestTrainRun.value.Outputjson || latestTrainRun.value.outputjson || latestTrainRun.value.LastError || latestTrainRun.value.lastError || ''
  const parsed = parseJson(output, null)
  if (parsed.ok && parsed.value && typeof parsed.value === 'object') {
    return JSON.stringify(parsed.value, null, 2)
  }
  return String(output || 'Sin output')
})

const payloadPreview = computed(() => JSON.stringify(buildRunInputPayload(), null, 2))

function applyProfileDefaults() {
  if (trainForm.profile === 'quick') {
    trainForm.epochs = 1
    trainForm.batchSize = 4
    trainForm.gradAccum = 1
    trainForm.learningRate = 0.0002
    trainForm.loraRank = 8
    trainForm.loraAlpha = 16
    trainForm.loraDropout = 0.05
    trainForm.evalEverySteps = 0
    trainForm.earlyStopping = false
  } else if (trainForm.profile === 'quality') {
    trainForm.epochs = 5
    trainForm.batchSize = 8
    trainForm.gradAccum = 4
    trainForm.learningRate = 0.00005
    trainForm.loraRank = 32
    trainForm.loraAlpha = 64
    trainForm.loraDropout = 0.1
    trainForm.evalEverySteps = 100
    trainForm.earlyStopping = true
  } else {
    trainForm.epochs = 3
    trainForm.batchSize = 8
    trainForm.gradAccum = 2
    trainForm.learningRate = 0.0001
    trainForm.loraRank = 16
    trainForm.loraAlpha = 32
    trainForm.loraDropout = 0.05
    trainForm.evalEverySteps = 100
    trainForm.earlyStopping = true
  }
}

function buildRunInputPayload() {
  const targetModules = String(trainForm.targetModulesCsv || '')
    .split(',')
    .map(item => item.trim())
    .filter(Boolean)

  return {
    profile: trainForm.profile,
    datasetVersion: String(trainForm.datasetVersion || '').trim(),
    model: {
      baseModel: String(trainForm.baseModel || '').trim(),
      precision: trainForm.precision,
      quantization: trainForm.quantization,
      maxSeqLen: Number(trainForm.maxSeqLen || 0)
    },
    lora: {
      rank: Number(trainForm.loraRank || 0),
      alpha: Number(trainForm.loraAlpha || 0),
      dropout: Number(trainForm.loraDropout || 0),
      targetModules
    },
    optimization: {
      epochs: Number(trainForm.epochs || 0),
      batchSize: Number(trainForm.batchSize || 0),
      gradAccum: Number(trainForm.gradAccum || 0),
      learningRate: Number(trainForm.learningRate || 0),
      warmupRatio: Number(trainForm.warmupRatio || 0),
      gradientCheckpointing: Boolean(trainForm.gradientCheckpointing)
    },
    evaluation: {
      everySteps: Number(trainForm.evalEverySteps || 0),
      earlyStopping: Boolean(trainForm.earlyStopping)
    },
    output: {
      adapterName: String(trainForm.adapterName || '').trim(),
      registryTag: String(trainForm.registryTag || '').trim() || null,
      exportCompact: Boolean(trainForm.exportCompact)
    },
    overrides: extraParsed.value.ok ? (extraParsed.value.value || {}) : {}
  }
}

function suggestDefaultsFromProject() {
  if (!selectedProject.value) return

  if (!String(trainForm.adapterName || '').trim()) {
    const slug = selectedProject.value.Slug || selectedProject.value.slug || `project-${selectedProjectId.value}`
    trainForm.adapterName = `${slug}-lora-v1`
  }

  if (!String(trainForm.registryTag || '').trim()) {
    const slug = selectedProject.value.Slug || selectedProject.value.slug || `project-${selectedProjectId.value}`
    trainForm.registryTag = `${slug}:v1`
  }

  if (!String(trainForm.datasetVersion || '').trim()) {
    const latestDatasetRun = runs.value.find(run =>
      String(run.RunType || run.runType || '').toLowerCase() === 'dataset_build'
      && String(run.Status || run.status || '').toLowerCase() === 'completed'
    )
    const parsedInput = parseJson(latestDatasetRun?.Inputjson || latestDatasetRun?.inputjson || '{}', {})
    const version = parsedInput.value?.datasetVersion
    trainForm.datasetVersion = version || `v${new Date().toISOString().slice(0, 10).replaceAll('-', '')}`
  }

  if (selectedTemplate.value && !String(trainForm.baseModel || '').trim()) {
    trainForm.baseModel = selectedTemplate.value.Basemodel || selectedTemplate.value.baseModel || 'qwen2.5:7b'
  }
}

async function loadProjects() {
  const [projectsRes, templatesRes] = await Promise.all([
    runtimeApi.list('projects'),
    runtimeApi.list('templates')
  ])

  projects.value = Array.isArray(projectsRes?.data) ? projectsRes.data.map(normalizeRecord) : []
  templates.value = Array.isArray(templatesRes?.data) ? templatesRes.data.map(normalizeRecord) : []
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
    error.value = errorText(err, 'No se pudo cargar la etapa Train.')
  } finally {
    loadingData.value = false
  }
}

async function triggerTrain() {
  if (!canRun.value) return

  running.value = true
  runError.value = ''
  runMessage.value = ''

  try {
    const payload = buildRunInputPayload()
    await runtimeApi.triggerProjectRun(
      selectedProjectId.value,
      'train_lora',
      JSON.stringify(payload)
    )
    runMessage.value = 'Train LoRA ejecutado. Revisa la salida en runs.'
    await Promise.all([loadWorkflow(selectedProjectId.value), loadRuns()])
  } catch (err) {
    runError.value = errorText(err, 'No se pudo ejecutar el entrenamiento.')
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
  router.push({ path: '/stage/eval', query: { projectId: selectedProjectId.value } })
}

watch(() => trainForm.profile, () => {
  applyProfileDefaults()
})

watch(selectedProjectId, async id => {
  if (!id) {
    runs.value = []
    projectWorkflow.value = null
    return
  }
  runMessage.value = ''
  runError.value = ''
  trainForm.datasetVersion = ''
  trainForm.adapterName = ''
  trainForm.registryTag = ''
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
