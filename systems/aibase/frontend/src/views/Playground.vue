<template>
  <v-container fluid class="playground-page">
    <v-card class="card sb-page-header">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center">
          <div class="sb-page-icon">
            <v-icon color="primary" size="24">mdi-flask-outline</v-icon>
          </div>
          <div>
            <h2>Playground del modelo</h2>
            <p class="sb-page-subtitle">Prueba entradas reales contra el modelo desplegado.</p>
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
          <v-card-title>Entrada de prueba</v-card-title>
          <v-divider />
          <v-card-text>
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

            <v-textarea
              v-model="inferInput"
              label="Entrada"
              rows="6"
              auto-grow
              density="comfortable"
              placeholder="Escribe texto para inferencia o pega una transcripción."
            />

            <v-textarea
              v-model="inferContextJson"
              label="Contexto opcional (JSON)"
              rows="2"
              auto-grow
              density="comfortable"
              placeholder='{"maxTokens":256}'
            />

            <div class="d-flex align-center ga-2">
              <v-btn
                class="sb-btn primary"
                :loading="inferLoading"
                :disabled="!selectedProjectId || !inferInput.trim() || inferLoading || !canInfer"
                prepend-icon="mdi-send"
                @click="runInfer"
              >
                Probar modelo
              </v-btn>

              <v-btn
                class="sb-btn"
                variant="tonal"
                prepend-icon="mdi-play-network"
                :disabled="canInfer"
                @click="openDeployStage"
              >
                Ir a etapa Deploy
              </v-btn>
            </div>

            <div v-if="!canInfer" class="text-caption text-medium-emphasis mt-3">
              Este proyecto aún no está desplegado. Completa la etapa <strong>Deploy</strong> para habilitar inferencia.
            </div>

            <v-alert
              v-if="inferError"
              class="mt-3"
              type="error"
              variant="tonal"
              density="comfortable"
              :text="inferError"
            />
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="5">
        <v-card class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            Resultado
            <v-chip
              v-if="inferResult"
              size="small"
              variant="tonal"
              :color="inferResult.IsMock || inferResult.isMock ? 'warning' : 'success'"
            >
              {{ inferResult.IsMock || inferResult.isMock ? 'mock' : 'engine' }}
            </v-chip>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="!inferResult" class="text-caption text-medium-emphasis">
              Ejecuta una prueba para ver la salida del modelo.
            </div>
            <div v-else class="infer-output">
              <pre>{{ inferOutputText }}</pre>
            </div>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title class="d-flex align-center justify-space-between">
            Estado del proyecto
            <v-chip size="small" variant="tonal" :color="projectStatusColor(projectStatus)">
              {{ projectStatus || 'draft' }}
            </v-chip>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loadingWorkflow" class="sb-skeleton" style="height: 72px;"></div>
            <template v-else>
              <div class="mb-2 text-body-2">
                Siguiente etapa: <strong>{{ nextRunLabel || '—' }}</strong>
              </div>
              <div class="text-caption text-medium-emphasis">
                Can infer: {{ canInfer ? 'sí' : 'no' }}
              </div>
            </template>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
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
const error = ref('')

const projects = ref([])
const selectedProjectId = ref(null)
const workflow = ref(null)

const inferInput = ref('')
const inferContextJson = ref('')
const inferResult = ref(null)
const inferLoading = ref(false)
const inferError = ref('')

const runTypeLabels = {
  dataset_build: 'Build Dataset',
  rag_index: 'Indexar RAG',
  train_lora: 'Entrenar LoRA',
  eval_run: 'Evaluar',
  deploy_service: 'Deploy'
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
  ;['Id', 'Name', 'TemplateId', 'Templateid'].forEach(ensure)
  return copy
}

function errorText(err, fallback = 'Error de comunicación.') {
  const msg = err?.response?.data?.error
    || err?.response?.data?.message
    || (typeof err?.response?.data === 'string' ? err.response.data : null)
    || err?.message
  return String(msg || fallback)
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

const projectItems = computed(() => projects.value.map(project => ({
  title: project.Name || project.name || `Proyecto #${project.Id || project.id}`,
  value: project.Id || project.id
})))

const canInfer = computed(() => Boolean(workflow.value?.CanInfer ?? workflow.value?.canInfer))
const projectStatus = computed(() => workflow.value?.ProjectStatus || workflow.value?.projectStatus || '')
const nextRunType = computed(() => String(workflow.value?.NextRunType || workflow.value?.nextRunType || '').toLowerCase())
const nextRunLabel = computed(() => runTypeLabels[nextRunType.value] || nextRunType.value || '—')

const inferOutputText = computed(() => {
  if (!inferResult.value) return ''
  const outputJson = inferResult.value.OutputJson || inferResult.value.outputJson
  if (outputJson) {
    try {
      return JSON.stringify(JSON.parse(outputJson), null, 2)
    } catch {
      return String(outputJson)
    }
  }
  return String(inferResult.value.Output || inferResult.value.output || '')
})

function openDeployStage() {
  router.push({ path: '/pipeline', query: { projectId: selectedProjectId.value || undefined, stage: 'deploy_service' } })
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
  } catch (err) {
    workflow.value = null
    error.value = errorText(err, 'No se pudo cargar el workflow del proyecto.')
  } finally {
    loadingWorkflow.value = false
  }
}

async function runInfer() {
  inferError.value = ''
  inferResult.value = null

  if (!selectedProjectId.value) {
    inferError.value = 'Selecciona un proyecto.'
    return
  }

  if (!inferInput.value.trim()) {
    inferError.value = 'Escribe un input para inferir.'
    return
  }

  let contextJson = null
  const contextText = inferContextJson.value.trim()
  if (contextText) {
    try {
      contextJson = JSON.stringify(JSON.parse(contextText))
    } catch {
      inferError.value = 'Contexto JSON inválido.'
      return
    }
  }

  inferLoading.value = true
  try {
    const response = await runtimeApi.inferProject(selectedProjectId.value, inferInput.value.trim(), contextJson)
    inferResult.value = normalizeRecord(response?.data || {})
  } catch (err) {
    inferError.value = errorText(err, 'No se pudo ejecutar la inferencia.')
  } finally {
    inferLoading.value = false
  }
}

async function load() {
  error.value = ''
  await loadProjects()
  await loadWorkflow()
}

watch(selectedProjectId, async value => {
  router.replace({ path: '/playground', query: { projectId: value || undefined } })
  await loadWorkflow()
})

onMounted(load)
</script>

<style scoped>
.playground-page {
  padding-bottom: 30px;
}

.infer-output {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  padding: 10px;
  max-height: 420px;
  overflow: auto;
}

.infer-output pre {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
  font-size: 0.8rem;
}
</style>
