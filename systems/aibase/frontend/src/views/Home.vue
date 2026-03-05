<template>
  <v-container fluid class="home-workflow">
    <v-card class="card sb-page-header">
      <div class="header-row">
        <div class="header-left">
          <div class="sb-page-icon">
            <v-icon color="primary" size="24">mdi-source-branch</v-icon>
          </div>
          <div>
            <h2>Flujo de creación de modelo</h2>
            <p class="sb-page-subtitle">Selecciona proyecto y avanza etapa por etapa. Cada bloque te lleva a su pantalla de trabajo.</p>
          </div>
        </div>

        <div class="header-right">
          <v-select
            v-model="selectedProjectId"
            :items="projectItems"
            item-title="title"
            item-value="value"
            label="Proyecto activo"
            :loading="loadingProjects"
            :disabled="loadingProjects || !projectItems.length"
            density="comfortable"
            clearable
            hide-details
          />
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

    <v-card class="card mt-4">
      <v-card-title class="d-flex align-center justify-space-between">
        <div class="d-flex align-center">
          <v-icon class="mr-2" color="primary">mdi-chart-sankey</v-icon>
          Etapas del workflow
        </div>
        <v-chip v-if="workflow" size="small" variant="tonal" :color="projectStatusColor(projectStatus)">
          {{ projectStatus || 'draft' }}
        </v-chip>
      </v-card-title>
      <v-divider />
      <v-card-text>
        <div v-if="loadingWorkflow" class="sb-skeleton" style="height: 150px;"></div>
        <div v-else-if="!stageNodes.length" class="text-caption text-medium-emphasis">
          Crea o selecciona un proyecto para habilitar el flujo.
        </div>
        <div v-else class="workflow-scroller">
          <div class="workflow-chain">
            <template v-for="(node, index) in stageNodes" :key="node.id">
              <button
                class="stage-card"
                type="button"
                :class="[
                  `state-${node.status}`,
                  { 'is-next': node.isNext, 'is-clickable': node.clickable }
                ]"
                @click="goToStage(node)"
              >
                <div class="stage-top">
                  <span class="stage-index">{{ index + 1 }}</span>
                  <v-chip size="x-small" variant="tonal" :color="workflowStatusColor(node.status)">
                    {{ workflowStatusLabel(node.status) }}
                  </v-chip>
                </div>
                <div class="stage-title">
                  <v-icon size="18" color="primary" class="mr-1">{{ node.icon }}</v-icon>
                  {{ node.label }}
                </div>
                <p class="stage-description">{{ node.description }}</p>
                <div class="stage-footer">
                  <span>{{ node.hint }}</span>
                  <v-icon size="16">mdi-arrow-right</v-icon>
                </div>
              </button>

              <div v-if="index < stageNodes.length - 1" class="stage-arrow">
                <v-icon size="20" color="primary">mdi-arrow-right</v-icon>
              </div>
            </template>
          </div>
        </div>
      </v-card-text>
    </v-card>

    <v-row class="mt-4" dense>
      <v-col v-for="card in statCards" :key="card.label" cols="12" sm="6" md="3">
        <v-card class="card stat-card">
          <div class="stat-icon">
            <v-icon :color="card.color">{{ card.icon }}</v-icon>
          </div>
          <div>
            <div class="stat-label">{{ card.label }}</div>
            <div class="stat-value">{{ card.value }}</div>
          </div>
        </v-card>
      </v-col>
    </v-row>

    <v-row class="mt-4" dense>
      <v-col cols="12" md="7">
        <v-card class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-timeline-clock-outline</v-icon>
              Últimos runs del proyecto
            </div>
            <v-btn class="sb-btn ghost" variant="text" @click="router.push('/runs')">Ver Runs</v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loadingRuns" class="sb-skeleton" style="height: 120px;"></div>
            <div v-else-if="!projectRuns.length" class="text-caption text-medium-emphasis">
              No hay ejecuciones para el proyecto seleccionado.
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
              </div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="5">
        <v-card class="card quick-actions">
          <v-card-title class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-lightning-bolt-outline</v-icon>
            Accesos rápidos
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div class="quick-grid">
              <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-shape-plus" @click="router.push('/templates')">
                Templates
              </v-btn>
              <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-folder-cog" @click="router.push('/projects')">
                Proyectos
              </v-btn>
              <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-play-network" @click="openPipeline()">
                Pipeline
              </v-btn>
              <v-btn class="sb-btn primary" prepend-icon="mdi-flask-outline" :disabled="!canInfer" @click="openPlayground()">
                Playground
              </v-btn>
            </div>
            <div v-if="!canInfer" class="text-caption text-medium-emphasis mt-3">
              El playground se habilita al completar la etapa de deploy.
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import runtimeApi from '../api/runtime.service'

const router = useRouter()

const loadingProjects = ref(false)
const loadingWorkflow = ref(false)
const loadingRuns = ref(false)
const error = ref('')

const overview = ref(null)
const projects = ref([])
const selectedProjectId = ref(null)
const workflow = ref(null)
const projectRuns = ref([])

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

function runTypeLabel(runType) {
  const key = String(runType || '').toLowerCase()
  return runTypeOptions.find(item => item.value === key)?.label || runType || 'run'
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

function formatDate(value) {
  if (!value) return 'Sin fecha'
  return new Date(value).toLocaleString('es-AR')
}

const projectItems = computed(() => projects.value.map(project => ({
  title: project.Name || project.name || `Proyecto #${project.Id || project.id}`,
  value: project.Id || project.id
})))

const templatesCount = computed(() => overview.value?.TemplatesCount ?? 0)
const projectsCount = computed(() => overview.value?.ProjectsCount ?? 0)
const runsCount = computed(() => overview.value?.RunsCount ?? 0)
const runningCount = computed(() => overview.value?.RunningCount ?? 0)

const statCards = computed(() => ([
  { label: 'Templates', value: templatesCount.value, icon: 'mdi-shape-plus', color: 'primary' },
  { label: 'Proyectos', value: projectsCount.value, icon: 'mdi-folder-cog', color: 'teal' },
  { label: 'Runs', value: runsCount.value, icon: 'mdi-timeline', color: 'indigo' },
  { label: 'En ejecución', value: runningCount.value, icon: 'mdi-progress-clock', color: 'warning' }
]))

const workflowSteps = computed(() => {
  const raw = workflow.value?.Steps || workflow.value?.steps
  return Array.isArray(raw) ? raw : []
})

const projectStatus = computed(() => workflow.value?.ProjectStatus || workflow.value?.projectStatus || '')

const canInfer = computed(() => Boolean(workflow.value?.CanInfer ?? workflow.value?.canInfer))

const stageNodes = computed(() => {
  if (!projectItems.value.length) return []

  const stepsByRunType = new Map(
    workflowSteps.value.map(step => [String(step.RunType || step.runType || '').toLowerCase(), step])
  )

  const nextRunType = String(workflow.value?.NextRunType || workflow.value?.nextRunType || '').toLowerCase()

  const runStage = (runType, label, description, icon, route) => {
    const step = stepsByRunType.get(runType)
    const status = selectedProjectId.value
      ? String(step?.Status || step?.status || 'pending').toLowerCase()
      : 'blocked'
    const enabled = Boolean(step?.Enabled ?? step?.enabled)
    const available = Boolean(step?.Available ?? step?.available)

    return {
      id: runType,
      label,
      description,
      icon,
      status: enabled ? status : 'na',
      isNext: selectedProjectId.value && runType === nextRunType,
      clickable: true,
      hint: enabled
        ? (available || status === 'completed' ? 'Abrir etapa' : 'Requiere etapa previa')
        : 'No aplica en este template',
      route,
      runType
    }
  }

  return [
    {
      id: 'templates',
      label: 'Template',
      description: 'Define el contrato del modelo (schema, reglas y objetivos).',
      icon: 'mdi-shape-plus',
      status: templatesCount.value > 0 ? 'completed' : 'pending',
      isNext: false,
      clickable: true,
      hint: 'Abrir etapa',
      route: '/stage/template'
    },
    {
      id: 'projects',
      label: 'Proyecto',
      description: 'Crea el proyecto y asocia un template de trabajo.',
      icon: 'mdi-folder-cog',
      status: selectedProjectId.value ? 'completed' : 'pending',
      isNext: false,
      clickable: true,
      hint: 'Abrir etapa',
      route: '/stage/project'
    },
    runStage('dataset_build', 'Dataset', 'Construcción y versionado del dataset de entrenamiento.', 'mdi-database-cog', '/stage/dataset'),
    runStage('rag_index', 'RAG Index', 'Indexación vectorial para recuperación contextual.', 'mdi-vector-link', '/stage/rag'),
    runStage('train_lora', 'Train LoRA', 'Entrenamiento del adapter para el caso de uso.', 'mdi-brain', '/stage/train'),
    runStage('eval_run', 'Evaluar', 'Validación de calidad antes de publicar.', 'mdi-chart-line', '/stage/eval'),
    runStage('deploy_service', 'Deploy', 'Publicación del servicio ejecutable.', 'mdi-rocket-launch', '/stage/deploy'),
    {
      id: 'playground',
      label: 'Playground',
      description: 'Prueba interactiva con entradas reales del usuario.',
      icon: 'mdi-flask-outline',
      status: canInfer.value ? 'completed' : 'blocked',
      isNext: false,
      clickable: true,
      hint: canInfer.value ? 'Abrir playground' : 'Completa deploy',
      route: '/stage/playground'
    }
  ]
})

function goToStage(node) {
  if (!node?.route) return
  if (node.route.startsWith('/stage/')) {
    router.push({
      path: node.route,
      query: { projectId: selectedProjectId.value || undefined }
    })
    return
  }
  router.push({ path: node.route })
}

function openPipeline() {
  router.push({ path: '/stage/dataset', query: { projectId: selectedProjectId.value || undefined } })
}

function openPlayground() {
  router.push({ path: '/stage/playground', query: { projectId: selectedProjectId.value || undefined } })
}

async function loadOverview() {
  try {
    const response = await runtimeApi.getOverview(12)
    overview.value = response?.data || null
  } catch {
    overview.value = null
  }
}

async function loadProjects() {
  loadingProjects.value = true
  try {
    const response = await runtimeApi.list('projects')
    projects.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
    if (!selectedProjectId.value && projects.value.length) {
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
    const response = await runtimeApi.listProjectRuns(selectedProjectId.value, 8)
    projectRuns.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
  } catch (err) {
    projectRuns.value = []
    error.value = errorText(err, 'No se pudieron cargar runs.')
  } finally {
    loadingRuns.value = false
  }
}

async function load() {
  error.value = ''
  await Promise.all([loadOverview(), loadProjects()])
  await Promise.all([loadWorkflow(), loadProjectRuns()])
}

watch(selectedProjectId, async () => {
  await Promise.all([loadWorkflow(), loadProjectRuns()])
})

onMounted(load)
</script>

<style scoped>
.home-workflow {
  padding-bottom: 30px;
}

.header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-right {
  width: 320px;
  max-width: 100%;
}

.workflow-scroller {
  overflow-x: auto;
  padding-bottom: 8px;
}

.workflow-chain {
  display: inline-flex;
  align-items: stretch;
  gap: 10px;
  min-width: 100%;
}

.stage-arrow {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 28px;
}

.stage-card {
  min-width: 230px;
  max-width: 230px;
  border: 1px solid rgba(120, 140, 170, 0.22);
  border-radius: 14px;
  padding: 10px;
  background: var(--sb-surface);
  text-align: left;
  display: flex;
  flex-direction: column;
  gap: 8px;
  transition: transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
}

.stage-card.is-clickable:hover {
  transform: translateY(-1px);
  border-color: rgba(37, 99, 235, 0.35);
  box-shadow: 0 8px 18px rgba(37, 99, 235, 0.12);
}

.stage-card.is-next {
  border-color: rgba(37, 99, 235, 0.52);
  box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.12);
}

.stage-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.stage-index {
  width: 22px;
  height: 22px;
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: var(--sb-primary-soft);
  color: var(--sb-primary);
  font-size: 0.75rem;
  font-weight: 700;
}

.stage-title {
  display: inline-flex;
  align-items: center;
  font-weight: 700;
  font-size: 0.92rem;
}

.stage-description {
  margin: 0;
  font-size: 0.78rem;
  color: var(--sb-text-soft, var(--sb-muted));
  min-height: 48px;
}

.stage-footer {
  margin-top: auto;
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 0.75rem;
  color: var(--sb-text-soft, var(--sb-muted));
}

.stat-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
}

.stat-icon {
  width: 36px;
  height: 36px;
  border-radius: 12px;
  background: var(--sb-primary-soft);
  display: flex;
  align-items: center;
  justify-content: center;
}

.stat-label {
  font-size: 0.72rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--sb-text-soft, var(--sb-muted));
}

.stat-value {
  font-size: 1.2rem;
  font-weight: 700;
}

.run-list {
  display: grid;
  gap: 10px;
  max-height: 330px;
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

.quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
}

@media (max-width: 960px) {
  .header-row {
    flex-direction: column;
    align-items: flex-start;
  }

  .header-right {
    width: 100%;
  }

  .workflow-chain {
    min-width: 0;
    flex-direction: column;
  }

  .stage-card {
    min-width: 100%;
    max-width: 100%;
  }

  .stage-arrow {
    transform: rotate(90deg);
    min-height: 20px;
  }

  .quick-grid {
    grid-template-columns: 1fr;
  }
}
</style>
