<template>
  <v-container fluid class="run-stage-page">
    <v-card class="card sb-page-header">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center">
          <div class="sb-page-icon">
            <v-icon color="primary" size="24">{{ icon }}</v-icon>
          </div>
          <div>
            <h2>{{ title }}</h2>
            <p class="sb-page-subtitle">{{ subtitle }}</p>
          </div>
        </div>

        <div class="d-flex align-center ga-2 flex-wrap">
          <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-home" @click="router.push('/home')">
            Flujo
          </v-btn>
          <v-btn v-if="prevRoute" class="sb-btn" variant="tonal" prepend-icon="mdi-arrow-left" @click="goTo(prevRoute)">
            Etapa anterior
          </v-btn>
          <v-btn v-if="nextRoute" class="sb-btn primary" prepend-icon="mdi-arrow-right" @click="goTo(nextRoute)">
            Siguiente etapa
          </v-btn>
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

    <v-row class="mt-4" dense>
      <v-col cols="12" md="7">
        <v-card class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            Ejecutar {{ stageLabel }}
            <v-chip size="small" variant="tonal" :color="workflowStatusColor(currentStageStatus)">
              {{ workflowStatusLabel(currentStageStatus) }}
            </v-chip>
          </v-card-title>
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
                  :loading="loadingProjects"
                  :disabled="loadingProjects || !projectItems.length"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  :model-value="selectedProjectTemplate"
                  label="Template"
                  density="comfortable"
                  readonly
                />
              </v-col>
            </v-row>

            <v-textarea
              v-model="runInputJson"
              label="Input opcional (JSON)"
              auto-grow
              rows="3"
              density="comfortable"
              :placeholder="samplePlaceholder"
            />

            <div class="d-flex align-center ga-2 mt-1 flex-wrap">
              <v-btn
                class="sb-btn primary"
                :loading="running"
                :disabled="!selectedProjectId || running || !canRunStage"
                prepend-icon="mdi-play"
                @click="triggerRun"
              >
                Ejecutar etapa
              </v-btn>

              <span v-if="!canRunStage" class="text-caption text-medium-emphasis">
                Etapa bloqueada por dependencias previas o no aplica al template.
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
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="5">
        <v-card class="card">
          <v-card-title>Checklist rápida</v-card-title>
          <v-divider />
          <v-card-text>
            <ul class="hint-list">
              <li v-for="item in hints" :key="item">{{ item }}</li>
            </ul>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title class="d-flex align-center justify-space-between">
            Workflow del proyecto
            <v-chip v-if="workflow" size="small" variant="tonal" :color="projectStatusColor(projectStatus)">
              {{ projectStatus || 'draft' }}
            </v-chip>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loadingWorkflow" class="sb-skeleton" style="height: 110px;"></div>
            <div v-else-if="!workflowSteps.length" class="text-caption text-medium-emphasis">
              Selecciona proyecto para ver estado.
            </div>
            <div v-else class="workflow-list">
              <div v-for="step in workflowSteps" :key="step.RunType || step.runType" class="workflow-item">
                <span>{{ step.Order || step.order }} · {{ step.Label || step.label }}</span>
                <v-chip size="small" variant="tonal" :color="workflowStatusColor(step.Status || step.status)">
                  {{ workflowStatusLabel(step.Status || step.status) }}
                </v-chip>
              </div>
            </div>
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
import runtimeApi from '../../api/runtime.service'

const props = defineProps({
  runType: { type: String, required: true },
  title: { type: String, required: true },
  subtitle: { type: String, required: true },
  icon: { type: String, default: 'mdi-play-network-outline' },
  prevRoute: { type: String, default: '' },
  nextRoute: { type: String, default: '' },
  hints: { type: Array, default: () => [] },
  samplePlaceholder: { type: String, default: '{"notes":"corrida"}' }
})

const router = useRouter()
const route = useRoute()

const loadingProjects = ref(false)
const loadingWorkflow = ref(false)
const loadingRuns = ref(false)
const error = ref('')

const projects = ref([])
const templates = ref([])
const selectedProjectId = ref(null)
const workflow = ref(null)
const projectRuns = ref([])

const runInputJson = ref('')
const running = ref(false)
const pipelineMessage = ref('')
const pipelineError = ref('')

const runTypeOptions = [
  { value: 'dataset_build', label: 'Build Dataset' },
  { value: 'rag_index', label: 'Indexar RAG' },
  { value: 'train_lora', label: 'Entrenar LoRA' },
  { value: 'eval_run', label: 'Evaluar' },
  { value: 'deploy_service', label: 'Deploy' }
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
  return runTypeOptions.find(item => item.value === key)?.label || runType || 'run'
}

function formatDate(value) {
  if (!value) return 'Sin fecha'
  return new Date(value).toLocaleString('es-AR')
}

const stageLabel = computed(() => runTypeLabel(props.runType))

const projectItems = computed(() => projects.value.map(project => ({
  title: project.Name || project.name || `Proyecto #${project.Id || project.id}`,
  value: project.Id || project.id
})))

const selectedProject = computed(() =>
  projects.value.find(project => String(project.Id || project.id) === String(selectedProjectId.value)) || null
)

const selectedProjectTemplate = computed(() => {
  if (!selectedProject.value) return '—'
  const templateId = selectedProject.value.TemplateId || selectedProject.value.Templateid
  const template = templates.value.find(t => String(t.Id || t.id) === String(templateId))
  return template?.Name || template?.name || `Template #${templateId}`
})

const workflowSteps = computed(() => {
  const raw = workflow.value?.Steps || workflow.value?.steps
  return Array.isArray(raw) ? raw : []
})

const projectStatus = computed(() => workflow.value?.ProjectStatus || workflow.value?.projectStatus || '')

const currentWorkflowStep = computed(() => workflowSteps.value.find(step =>
  String(step.RunType || step.runType || '').toLowerCase() === props.runType
))

const currentStageStatus = computed(() => String(currentWorkflowStep.value?.Status || currentWorkflowStep.value?.status || 'pending').toLowerCase())

const canRunStage = computed(() => {
  const step = currentWorkflowStep.value
  if (!step) return false
  const enabled = Boolean(step.Enabled ?? step.enabled)
  const available = Boolean(step.Available ?? step.available)
  const status = String(step.Status || step.status || '').toLowerCase()
  return enabled && (available || status === 'completed')
})

function goTo(path) {
  router.push({ path, query: { projectId: selectedProjectId.value || undefined } })
}

async function loadProjects() {
  loadingProjects.value = true
  try {
    const [projectsRes, templatesRes] = await Promise.all([
      runtimeApi.list('projects'),
      runtimeApi.list('templates')
    ])

    projects.value = Array.isArray(projectsRes?.data) ? projectsRes.data.map(normalizeRecord) : []
    templates.value = Array.isArray(templatesRes?.data) ? templatesRes.data.map(normalizeRecord) : []

    const routeProjectId = route.query.projectId
    if (routeProjectId && projects.value.some(p => String(p.Id || p.id) === String(routeProjectId))) {
      selectedProjectId.value = routeProjectId
    } else if (!selectedProjectId.value && projects.value.length) {
      selectedProjectId.value = projects.value[0].Id || projects.value[0].id
    }
  } catch (err) {
    projects.value = []
    templates.value = []
    error.value = errorText(err, 'No se pudieron cargar proyectos/templates.')
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
    error.value = errorText(err, 'No se pudieron cargar runs.')
  } finally {
    loadingRuns.value = false
  }
}

async function triggerRun() {
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

  running.value = true
  try {
    const response = await runtimeApi.triggerProjectRun(selectedProjectId.value, props.runType, inputJson)
    const run = normalizeRecord(response?.data || {})
    const runId = run.RunId || run.runId || run.Id || run.id
    const status = run.Status || run.status || 'unknown'
    pipelineMessage.value = `Run #${runId} finalizó con estado: ${status}.`
    await Promise.all([loadWorkflow(), loadProjectRuns()])
  } catch (err) {
    pipelineError.value = errorText(err, 'No se pudo ejecutar la etapa.')
  } finally {
    running.value = false
  }
}

async function load() {
  error.value = ''
  await loadProjects()
  await Promise.all([loadWorkflow(), loadProjectRuns()])
}

watch(selectedProjectId, async value => {
  router.replace({ path: route.path, query: { projectId: value || undefined } })
  await Promise.all([loadWorkflow(), loadProjectRuns()])
})

onMounted(load)
</script>

<style scoped>
.run-stage-page {
  padding-bottom: 30px;
}

.hint-list {
  margin: 0;
  padding-left: 18px;
  display: grid;
  gap: 8px;
}

.hint-list li {
  color: var(--sb-text-soft, var(--sb-muted));
  font-size: 0.9rem;
}

.workflow-list {
  display: grid;
  gap: 8px;
}

.workflow-item {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  padding: 8px 10px;
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
</style>
