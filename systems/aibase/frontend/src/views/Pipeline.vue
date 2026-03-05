<template>
  <v-container fluid class="pipeline-page">
    <v-card class="card sb-page-header">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center">
          <div class="sb-page-icon">
            <v-icon color="primary" size="24">mdi-play-network-outline</v-icon>
          </div>
          <div>
            <h2>Pipeline de entrenamiento</h2>
            <p class="sb-page-subtitle">Ejecuta una etapa por vez con control de orden y estado.</p>
          </div>
        </div>
        <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-home" @click="router.push('/home')">
          Volver al flujo
        </v-btn>
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

    <v-row class="mt-4" dense>
      <v-col cols="12" md="7">
        <v-card class="card">
          <v-card-title>Configuración</v-card-title>
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
                  :loading="loadingProjects"
                  :disabled="loadingProjects || !projectItems.length"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="5">
                <v-text-field
                  :model-value="selectedProjectTemplate"
                  label="Template"
                  density="comfortable"
                  readonly
                />
              </v-col>
            </v-row>

            <div class="stage-grid mt-1">
              <button
                v-for="stage in stageOptions"
                :key="stage.value"
                type="button"
                class="stage-btn"
                :class="{
                  'is-active': selectedStage === stage.value,
                  'is-blocked': !isRunTypeAvailable(stage.value)
                }"
                @click="selectStage(stage.value)"
              >
                <v-icon size="16" class="mr-1" :color="isRunTypeAvailable(stage.value) ? 'primary' : 'grey'">
                  {{ stage.icon }}
                </v-icon>
                {{ stage.label }}
              </button>
            </div>

            <v-card class="stage-detail mt-3" variant="flat">
              <div class="stage-detail-head">
                <div>
                  <div class="text-subtitle-1 font-weight-bold">{{ selectedStageMeta.label }}</div>
                  <div class="text-caption text-medium-emphasis">{{ selectedStageMeta.description }}</div>
                </div>
                <v-chip size="small" variant="tonal" :color="workflowStatusColor(currentStageStatus)">
                  {{ workflowStatusLabel(currentStageStatus) }}
                </v-chip>
              </div>

              <v-textarea
                v-model="runInputJson"
                label="Input opcional (JSON)"
                auto-grow
                rows="3"
                density="comfortable"
                placeholder='{"datasetVersion":"v1","notes":"corrida inicial"}'
              />

              <div class="d-flex align-center ga-2 mt-1">
                <v-btn
                  class="sb-btn primary"
                  :loading="runningType === selectedStage"
                  :disabled="!selectedProjectId || runningType !== '' || !canRunSelectedStage"
                  prepend-icon="mdi-play"
                  @click="triggerRun(selectedStage)"
                >
                  Ejecutar etapa
                </v-btn>
                <span v-if="!canRunSelectedStage" class="text-caption text-medium-emphasis">
                  Etapa bloqueada por dependencia previa o no aplica al template.
                </span>
              </div>

              <v-alert
                v-if="pipelineError"
                class="mt-3"
                type="error"
                variant="tonal"
                density="comfortable"
                :text="pipelineError"
              />
              <v-alert
                v-if="pipelineMessage"
                class="mt-3"
                type="success"
                variant="tonal"
                density="comfortable"
                :text="pipelineMessage"
              />
            </v-card>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="5">
        <v-card class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            Workflow
            <v-chip v-if="workflow" size="small" variant="tonal" :color="projectStatusColor(projectStatus)">
              {{ projectStatus || 'draft' }}
            </v-chip>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loadingWorkflow" class="sb-skeleton" style="height: 140px;"></div>
            <div v-else-if="!workflowSteps.length" class="text-caption text-medium-emphasis">
              Selecciona un proyecto para ver el workflow.
            </div>
            <div v-else class="workflow-list">
              <div v-for="step in workflowSteps" :key="step.RunType || step.runType" class="workflow-item">
                <div>
                  <strong>{{ step.Order || step.order }} · {{ step.Label || step.label }}</strong>
                  <div class="text-caption text-medium-emphasis">
                    {{ (step.Available ?? step.available) ? 'Disponible' : 'Pendiente' }}
                  </div>
                </div>
                <v-chip size="small" variant="tonal" :color="workflowStatusColor(step.Status || step.status)">
                  {{ workflowStatusLabel(step.Status || step.status) }}
                </v-chip>
              </div>
            </div>

            <v-btn
              class="sb-btn mt-3"
              variant="tonal"
              prepend-icon="mdi-flask-outline"
              :disabled="!canInfer"
              @click="openPlayground"
            >
              Ir a playground
            </v-btn>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-card class="card mt-4">
      <v-card-title class="d-flex align-center justify-space-between">
        Últimas ejecuciones
        <v-btn icon variant="text" :disabled="loadingRuns" @click="loadProjectRuns">
          <v-icon>mdi-refresh</v-icon>
        </v-btn>
      </v-card-title>
      <v-divider />
      <v-card-text>
        <div v-if="loadingRuns" class="sb-skeleton" style="height: 120px;"></div>
        <div v-else-if="!projectRuns.length" class="text-caption text-medium-emphasis">
          No hay ejecuciones para este proyecto.
        </div>
        <div v-else class="run-list">
          <div v-for="run in projectRuns" :key="run.Id || run.id" class="run-item">
            <div class="run-head">
              <strong>#{{ run.Id || run.id }} · {{ runTypeLabel(run.RunType || run.runType) }}</strong>
              <v-chip size="small" variant="tonal" :color="workflowStatusColor(run.Status || run.status)">
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
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import runtimeApi from '../api/runtime.service'

const router = useRouter()
const route = useRoute()

const loadingProjects = ref(false)
const loadingWorkflow = ref(false)
const loadingRuns = ref(false)
const error = ref('')

const projects = ref([])
const selectedProjectId = ref(null)
const workflow = ref(null)
const projectRuns = ref([])

const selectedStage = ref('dataset_build')
const runInputJson = ref('')
const runningType = ref('')
const pipelineMessage = ref('')
const pipelineError = ref('')

const stageOptions = [
  { value: 'dataset_build', label: 'Dataset', icon: 'mdi-database-cog', description: 'Construye o actualiza la versión de dataset del proyecto.' },
  { value: 'rag_index', label: 'RAG Index', icon: 'mdi-vector-link', description: 'Indexa documentos en la base vectorial para retrieval.' },
  { value: 'train_lora', label: 'Train LoRA', icon: 'mdi-brain', description: 'Entrena un adapter LoRA con el dataset actual.' },
  { value: 'eval_run', label: 'Evaluar', icon: 'mdi-chart-line', description: 'Corre suite de evaluación y guarda métricas.' },
  { value: 'deploy_service', label: 'Deploy', icon: 'mdi-rocket-launch', description: 'Publica el modelo como servicio consumible.' }
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
    'Id', 'Name', 'RunType', 'Status', 'ProgressPct', 'CreatedAt', 'UpdatedAt',
    'TemplateId', 'Templateid', 'ProjectId', 'Projectid'
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

function workflowStatusColor(status) {
  const value = String(status || '').toLowerCase()
  if (value === 'completed' || value === 'deployed') return 'success'
  if (value === 'running') return 'warning'
  if (value === 'error' || value === 'failed') return 'error'
  if (value === 'blocked') return 'grey'
  if (value === 'na') return 'blue-grey'
  return 'primary'
}

function workflowStatusLabel(status) {
  const value = String(status || '').toLowerCase()
  if (value === 'completed' || value === 'deployed') return 'Completado'
  if (value === 'running') return 'En curso'
  if (value === 'error' || value === 'failed') return 'Error'
  if (value === 'blocked') return 'Bloqueado'
  if (value === 'na') return 'No aplica'
  return 'Pendiente'
}

function projectStatusColor(status) {
  const value = String(status || '').toLowerCase()
  if (value === 'deployed') return 'success'
  if (value === 'evaluated') return 'primary'
  if (value === 'trained') return 'indigo'
  if (value === 'data_ready' || value === 'index_ready') return 'teal'
  if (value === 'error') return 'error'
  return 'blue-grey'
}

function runTypeLabel(runType) {
  const key = String(runType || '').toLowerCase()
  return stageOptions.find(item => item.value === key)?.label || runType || 'run'
}

function formatDate(value) {
  if (!value) return 'Sin fecha'
  return new Date(value).toLocaleString('es-AR')
}

const projectItems = computed(() => projects.value.map(project => ({
  title: project.Name || project.name || `Proyecto #${project.Id || project.id}`,
  value: project.Id || project.id
})))

const selectedProject = computed(() =>
  projects.value.find(project => String(project.Id || project.id) === String(selectedProjectId.value)) || null
)

const selectedProjectTemplate = computed(() => {
  if (!selectedProject.value) return '—'
  return selectedProject.value.TemplateName || selectedProject.value.Template || 'Template asociado'
})

const workflowSteps = computed(() => {
  const raw = workflow.value?.Steps || workflow.value?.steps
  return Array.isArray(raw) ? raw : []
})

const projectStatus = computed(() => workflow.value?.ProjectStatus || workflow.value?.projectStatus || '')

const canInfer = computed(() => Boolean(workflow.value?.CanInfer ?? workflow.value?.canInfer))

const selectedStageMeta = computed(() => stageOptions.find(s => s.value === selectedStage.value) || stageOptions[0])

const currentWorkflowStep = computed(() => workflowSteps.value.find(step =>
  String(step.RunType || step.runType || '').toLowerCase() === selectedStage.value
))

const currentStageStatus = computed(() => String(currentWorkflowStep.value?.Status || currentWorkflowStep.value?.status || 'pending').toLowerCase())

const canRunSelectedStage = computed(() => {
  const step = currentWorkflowStep.value
  if (!step) return false
  const enabled = Boolean(step.Enabled ?? step.enabled)
  const available = Boolean(step.Available ?? step.available)
  const status = String(step.Status || step.status || '').toLowerCase()
  return enabled && (available || status === 'completed')
})

function isRunTypeAvailable(runType) {
  const step = workflowSteps.value.find(s => String(s.RunType || s.runType || '').toLowerCase() === runType)
  if (!step) return false
  const enabled = Boolean(step.Enabled ?? step.enabled)
  const available = Boolean(step.Available ?? step.available)
  const status = String(step.Status || step.status || '').toLowerCase()
  return enabled && (available || status === 'completed')
}

function selectStage(stage) {
  selectedStage.value = stage
  router.replace({
    path: '/pipeline',
    query: {
      projectId: selectedProjectId.value || undefined,
      stage
    }
  })
}

function openPlayground() {
  router.push({ path: '/playground', query: { projectId: selectedProjectId.value || undefined } })
}

async function loadProjects() {
  loadingProjects.value = true
  try {
    const response = await runtimeApi.list('projects')
    projects.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []

    const routeProjectId = route.query.projectId
    if (routeProjectId && projects.value.some(p => String(p.Id || p.id) === String(routeProjectId))) {
      selectedProjectId.value = routeProjectId
    } else if (!selectedProjectId.value && projects.value.length) {
      selectedProjectId.value = projects.value[0].Id || projects.value[0].id
    }
  } catch (err) {
    projects.value = []
    error.value = errorText(err, 'No se pudieron cargar proyectos.')
  } finally {
    loadingProjects.value = false
  }
}

async function loadWorkflow() {
  if (!selectedProjectId.value) {
    workflow.value = null
    return
  }

  loadingWorkflow.value = true
  try {
    const response = await runtimeApi.getProjectWorkflow(selectedProjectId.value)
    workflow.value = response?.data || null

    if (!route.query.stage) {
      const suggested = String(workflow.value?.NextRunType || workflow.value?.nextRunType || '')
      if (suggested) selectedStage.value = suggested
    }
  } catch (err) {
    workflow.value = null
    error.value = errorText(err, 'No se pudo cargar el workflow del proyecto.')
  } finally {
    loadingWorkflow.value = false
  }
}

async function loadProjectRuns() {
  if (!selectedProjectId.value) {
    projectRuns.value = []
    return
  }

  loadingRuns.value = true
  try {
    const response = await runtimeApi.listProjectRuns(selectedProjectId.value, 20)
    projectRuns.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
  } catch (err) {
    projectRuns.value = []
    error.value = errorText(err, 'No se pudieron cargar runs del proyecto.')
  } finally {
    loadingRuns.value = false
  }
}

async function triggerRun(runType) {
  pipelineError.value = ''
  pipelineMessage.value = ''

  if (!selectedProjectId.value) {
    pipelineError.value = 'Selecciona un proyecto.'
    return
  }

  let inputJson = null
  const inputText = runInputJson.value.trim()
  if (inputText) {
    try {
      inputJson = JSON.stringify(JSON.parse(inputText))
    } catch {
      pipelineError.value = 'Input JSON inválido.'
      return
    }
  }

  runningType.value = runType
  try {
    const response = await runtimeApi.triggerProjectRun(selectedProjectId.value, runType, inputJson)
    const run = normalizeRecord(response?.data || {})
    const runId = run.RunId || run.runId || run.Id || run.id
    const status = run.Status || run.status || 'unknown'
    pipelineMessage.value = `Run #${runId} finalizó con estado: ${status}.`
    await Promise.all([loadWorkflow(), loadProjectRuns()])
  } catch (err) {
    pipelineError.value = errorText(err, 'No se pudo ejecutar la etapa.')
  } finally {
    runningType.value = ''
  }
}

async function load() {
  error.value = ''
  const queryStage = String(route.query.stage || '').toLowerCase()
  if (stageOptions.some(s => s.value === queryStage)) {
    selectedStage.value = queryStage
  }

  await loadProjects()
  await Promise.all([loadWorkflow(), loadProjectRuns()])
}

watch(selectedProjectId, async value => {
  router.replace({
    path: '/pipeline',
    query: {
      projectId: value || undefined,
      stage: selectedStage.value
    }
  })
  await Promise.all([loadWorkflow(), loadProjectRuns()])
})

watch(() => route.query.stage, value => {
  const stage = String(value || '').toLowerCase()
  if (stageOptions.some(s => s.value === stage)) {
    selectedStage.value = stage
  }
})

onMounted(load)
</script>

<style scoped>
.pipeline-page {
  padding-bottom: 30px;
}

.stage-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
}

.stage-btn {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  background: var(--sb-surface);
  padding: 9px 10px;
  text-align: left;
  font-weight: 600;
  font-size: 0.86rem;
}

.stage-btn.is-active {
  border-color: rgba(37, 99, 235, 0.48);
  box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.12);
}

.stage-btn.is-blocked {
  opacity: 0.65;
}

.stage-detail {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  padding: 12px;
  background: rgba(37, 99, 235, 0.02);
}

.stage-detail-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 10px;
}

.workflow-list {
  display: grid;
  gap: 8px;
}

.workflow-item {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  padding: 10px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.run-list {
  display: grid;
  gap: 10px;
  max-height: 360px;
  overflow: auto;
}

.run-item {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  padding: 10px;
}

.run-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
}

.run-meta {
  margin-top: 4px;
  font-size: 0.8rem;
  color: var(--sb-text-soft, var(--sb-muted));
}

.run-error {
  margin-top: 6px;
  color: #b42318;
  font-size: 0.82rem;
}

@media (max-width: 960px) {
  .stage-grid {
    grid-template-columns: 1fr;
  }
}
</style>
