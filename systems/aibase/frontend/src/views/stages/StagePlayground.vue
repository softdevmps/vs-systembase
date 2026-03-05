<template>
  <v-container fluid class="stage-page">
    <v-card class="card sb-page-header">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center">
          <div class="sb-page-icon">
            <v-icon color="primary" size="24">mdi-flask-outline</v-icon>
          </div>
          <div>
            <h2>Etapa 8 · Playground</h2>
            <p class="sb-page-subtitle">
              Prueba el servicio desplegado con texto, audio o imagen y valida la salida en tiempo real.
            </p>
          </div>
        </div>
        <div class="d-flex align-center ga-2 flex-wrap">
          <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-arrow-left" @click="openDeployStage">
            Volver a Deploy
          </v-btn>
          <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-home" @click="router.push('/home')">
            Flujo
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

    <OptionGuide class="mt-4" :items="optionGuideItems" />

    <v-row class="mt-4" dense>
      <v-col cols="12" md="7">
        <v-card class="card">
          <v-card-title>Prueba interactiva</v-card-title>
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
                  clearable
                />
              </v-col>
              <v-col cols="12" md="4" class="d-flex align-center">
                <v-chip v-if="selectedTemplateKey" size="small" variant="tonal" color="primary">
                  Template: {{ selectedTemplateKey }}
                </v-chip>
              </v-col>
            </v-row>

            <div class="input-mode mb-3">
              <div class="text-caption text-medium-emphasis mb-1">Tipo de input</div>
              <v-btn-toggle v-model="inputMode" mandatory density="comfortable" variant="outlined">
                <v-btn value="text" prepend-icon="mdi-text">Texto</v-btn>
                <v-btn value="audio" prepend-icon="mdi-microphone">Audio</v-btn>
                <v-btn value="image" prepend-icon="mdi-image">Imagen</v-btn>
              </v-btn-toggle>
            </div>

            <v-textarea
              v-model="inferInput"
              label="Input textual"
              rows="5"
              auto-grow
              density="comfortable"
              placeholder="Escribe texto para inferencia o una descripción del archivo subido."
            />

            <v-card v-if="inputMode !== 'text'" class="media-card mb-3" variant="outlined">
              <v-card-text>
                <v-file-input
                  v-model="mediaFile"
                  :accept="mediaAccept"
                  :label="inputMode === 'audio' ? 'Adjuntar audio' : 'Adjuntar imagen'"
                  prepend-icon="mdi-paperclip"
                  show-size
                  clearable
                  density="comfortable"
                  @update:modelValue="onMediaChanged"
                />

                <v-alert
                  v-if="mediaError"
                  class="mt-2"
                  type="warning"
                  variant="tonal"
                  density="comfortable"
                  :text="mediaError"
                />

                <template v-if="mediaPayload">
                  <div class="d-flex align-center ga-2 mt-1 flex-wrap">
                    <v-chip size="x-small" variant="tonal" color="primary">{{ mediaPayload.fileName }}</v-chip>
                    <v-chip size="x-small" variant="tonal">{{ mediaPayload.mime || 'mime n/a' }}</v-chip>
                    <v-chip size="x-small" variant="tonal">{{ prettyBytes(mediaPayload.sizeBytes) }}</v-chip>
                    <v-btn class="sb-btn ghost" variant="text" size="small" prepend-icon="mdi-close" @click="clearMediaSelection">
                      Quitar
                    </v-btn>
                  </div>

                  <audio
                    v-if="mediaPayload.type === 'audio'"
                    class="media-preview mt-3"
                    :src="mediaPayload.dataUrl"
                    controls
                  />

                  <v-img
                    v-else-if="mediaPayload.type === 'image'"
                    class="media-image mt-3"
                    :src="mediaPayload.dataUrl"
                    cover
                  />
                </template>
              </v-card-text>
            </v-card>

            <v-checkbox
              v-model="includeMediaInContext"
              hide-details
              density="compact"
              label="Incluir archivo en contextJson"
              class="mb-2"
            />

            <v-textarea
              v-model="inferContextJson"
              label="Contexto opcional (JSON)"
              rows="2"
              auto-grow
              density="comfortable"
              :error-messages="contextError ? [contextError] : []"
              placeholder='{"temperature":0,"maxTokens":256}'
            />

            <v-text-field
              v-model="inferExpectedContains"
              label="Validación rápida (debe contener)"
              density="comfortable"
              placeholder="Ej: arrebato"
            />

            <div class="d-flex align-center ga-2 mt-2 flex-wrap">
              <v-btn
                class="sb-btn primary"
                :loading="inferLoading"
                :disabled="!canRunInfer"
                prepend-icon="mdi-send"
                @click="runInfer()"
              >
                Probar modelo
              </v-btn>

              <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-broom" @click="clearForm">
                Limpiar
              </v-btn>

              <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-content-copy" @click="copyText(inferInput)">
                Copiar input
              </v-btn>

              <span v-if="!canInfer" class="text-caption text-medium-emphasis">
                Completa deploy para habilitar inferencia.
              </span>
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

        <v-card class="card mt-4">
          <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-2">
            <span>Casos de demo</span>
            <v-btn
              class="sb-btn"
              variant="tonal"
              prepend-icon="mdi-playlist-play"
              :disabled="!selectedProjectId || !demoCases.length || inferLoading || !canInfer"
              @click="runAllDemoCases"
            >
              Ejecutar todos
            </v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="!demoCases.length" class="text-caption text-medium-emphasis">
              Selecciona un proyecto para sugerir casos de prueba.
            </div>
            <div v-else class="demo-case-list">
              <div v-for="testCase in demoCases" :key="testCase.id" class="demo-case-item">
                <div class="demo-case-head">
                  <strong>{{ testCase.title }}</strong>
                  <v-chip
                    v-if="caseResults[testCase.id]"
                    size="x-small"
                    variant="tonal"
                    :color="caseResults[testCase.id].ok ? 'success' : 'warning'"
                  >
                    {{ caseResults[testCase.id].ok ? 'ok' : 'revisar' }}
                  </v-chip>
                </div>
                <div class="text-caption text-medium-emphasis mb-2">{{ testCase.description }}</div>
                <div class="text-caption mb-2"><strong>Esperado:</strong> {{ testCase.expectedContains }}</div>
                <div class="d-flex align-center ga-2 flex-wrap">
                  <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-pencil" @click="loadDemoCase(testCase)">
                    Cargar
                  </v-btn>
                  <v-btn
                    class="sb-btn"
                    variant="tonal"
                    size="small"
                    prepend-icon="mdi-play"
                    :disabled="!canInfer || inferLoading"
                    @click="runDemoCase(testCase)"
                  >
                    Ejecutar
                  </v-btn>
                </div>
              </div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="5">
        <v-card class="card">
          <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-2">
            <span>Resultado</span>
            <div class="d-flex align-center ga-2 flex-wrap">
              <v-chip v-if="inferResult" size="small" variant="tonal" :color="isMockResult ? 'warning' : 'success'">
                {{ isMockResult ? 'mock' : 'engine' }}
              </v-chip>
              <v-chip v-if="quickVerdict" size="small" variant="tonal" :color="quickVerdict.ok ? 'success' : 'warning'">
                {{ quickVerdict.ok ? 'validado' : 'revisar' }}
              </v-chip>
            </div>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="!inferResult" class="text-caption text-medium-emphasis">
              Ejecuta una prueba para ver la salida del modelo.
            </div>
            <template v-else>
              <div class="result-meta">
                <div><strong>Proyecto:</strong> {{ selectedProjectName || 'N/A' }}</div>
                <div><strong>Timestamp:</strong> {{ formatDate(new Date().toISOString()) }}</div>
                <div v-if="lastInferDurationMs"><strong>Duración:</strong> {{ lastInferDurationMs }} ms</div>
                <div><strong>Input mode:</strong> {{ inputMode }}</div>
              </div>

              <div class="d-flex align-center ga-2 mt-3 flex-wrap">
                <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-content-copy" @click="copyText(inferOutputRaw)">
                  Copiar raw
                </v-btn>
                <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-code-json" :disabled="!inferOutputJsonPretty" @click="copyText(inferOutputJsonPretty)">
                  Copiar JSON
                </v-btn>
              </div>

              <v-btn-toggle v-model="outputMode" class="mt-3" mandatory density="compact" variant="outlined">
                <v-btn value="json">JSON</v-btn>
                <v-btn value="raw">Raw</v-btn>
              </v-btn-toggle>

              <div class="preview-box mt-3">
                <pre v-if="outputMode === 'json'">{{ inferOutputJsonPretty || 'No hay JSON parseable.' }}</pre>
                <pre v-else>{{ inferOutputRaw || 'Sin salida.' }}</pre>
              </div>
            </template>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title>Gate de etapa</v-card-title>
          <v-divider />
          <v-card-text>
            <ul class="hint-list">
              <li :class="{ ok: !!selectedProjectId }">Proyecto seleccionado</li>
              <li :class="{ ok: canInfer }">Deploy completado (canInfer)</li>
              <li :class="{ ok: !contextError }">Contexto JSON válido</li>
              <li :class="{ ok: !!inferInput.trim() || !!mediaPayload }">Input cargado (texto o archivo)</li>
            </ul>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title class="d-flex align-center justify-space-between">
            Deploy activo
            <v-btn icon variant="text" :disabled="loadingDeployRuns" @click="loadLatestDeployInfo">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loadingDeployRuns" class="sb-skeleton" style="height: 72px;" />
            <div v-else-if="!latestDeploySummary.status" class="text-caption text-medium-emphasis">
              No hay deploy_service para este proyecto.
            </div>
            <template v-else>
              <div class="result-meta">
                <div>
                  <strong>Estado:</strong>
                  <v-chip size="x-small" variant="tonal" :color="statusColor(latestDeploySummary.status)">
                    {{ latestDeploySummary.status }}
                  </v-chip>
                </div>
                <div v-if="latestDeploySummary.service"><strong>Servicio:</strong> {{ latestDeploySummary.service }}</div>
                <div v-if="latestDeploySummary.endpoint"><strong>Endpoint:</strong> {{ latestDeploySummary.endpoint }}</div>
                <div v-if="latestDeploySummary.health"><strong>Health:</strong> {{ latestDeploySummary.health }}</div>
              </div>
              <div class="d-flex align-center ga-2 mt-2 flex-wrap">
                <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-open-in-new" :disabled="!latestDeploySummary.endpoint" @click="openUrl(latestDeploySummary.endpoint)">
                  Abrir endpoint
                </v-btn>
                <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-heart-pulse" :disabled="!latestDeploySummary.health" @click="openUrl(latestDeploySummary.health)">
                  Abrir health
                </v-btn>
              </div>
            </template>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title>Historial de sesión</v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="!sessionHistory.length" class="text-caption text-medium-emphasis">
              Aún no hay pruebas ejecutadas en esta sesión.
            </div>
            <div v-else class="session-list">
              <div v-for="item in sessionHistory" :key="item.localId" class="session-item">
                <div class="session-head">
                  <strong>{{ item.title }}</strong>
                  <v-chip size="x-small" variant="tonal" :color="item.ok ? 'success' : 'warning'">
                    {{ item.ok ? 'ok' : 'revisar' }}
                  </v-chip>
                </div>
                <div class="session-meta">
                  {{ item.when }} · {{ item.durationMs }} ms · {{ item.mode }}
                </div>
                <div class="session-text">{{ item.preview }}</div>
              </div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import runtimeApi from '../../api/runtime.service'
import OptionGuide from '../../components/Workflow/OptionGuide.vue'

const router = useRouter()
const route = useRoute()

const MEDIA_MAX_BYTES = 8 * 1024 * 1024

const optionGuideItems = [
  { label: 'Tipo de input', description: 'Elige si probarás el modelo con texto, audio o imagen.' },
  { label: 'Adjuntar archivo', description: 'Carga un archivo para pruebas multimodales y visualiza su preview.' },
  { label: 'Incluir en contextJson', description: 'Inserta metadata/base64 del archivo en el contexto enviado al modelo.' },
  { label: 'Contexto opcional JSON', description: 'Parámetros extra de inferencia (temperatura, maxTokens, etc.).' },
  { label: 'Validación rápida', description: 'Chequea si la respuesta contiene un texto esperado para demo/gate rápido.' },
  { label: 'Casos de demo', description: 'Permiten ejecutar escenarios predefinidos para mostrar resultados consistentes.' }
]

const loadingProjects = ref(false)
const loadingWorkflow = ref(false)
const loadingDeployRuns = ref(false)
const inferLoading = ref(false)

const error = ref('')
const inferError = ref('')
const mediaError = ref('')

const projects = ref([])
const selectedProjectId = ref(null)
const selectedTemplate = ref(null)
const workflow = ref(null)
const latestDeployRun = ref(null)

const inputMode = ref('text')
const includeMediaInContext = ref(true)
const mediaFile = ref(null)
const mediaPayload = ref(null)

const inferInput = ref('')
const inferContextJson = ref('')
const inferExpectedContains = ref('')
const inferResult = ref(null)
const lastInferDurationMs = ref(0)
const outputMode = ref('json')

const sessionHistory = ref([])
const caseResults = ref({})

let localHistoryId = 1

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
  ;[
    'Id', 'Name', 'TemplateId', 'Templateid', 'RunType', 'Status', 'Inputjson', 'Outputjson',
    'CreatedAt', 'ProjectId', 'Projectid', 'CanInfer', 'ProjectStatus', 'NextRunType'
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

function parseJson(text, fallback = null) {
  try {
    const raw = String(text || '').trim()
    if (!raw) return { ok: true, value: fallback }
    return { ok: true, value: JSON.parse(raw) }
  } catch (err) {
    return { ok: false, value: fallback, message: err?.message || 'JSON inválido' }
  }
}

function pickFirst(...values) {
  for (const value of values) {
    if (value === null || value === undefined) continue
    const text = String(value).trim()
    if (text) return text
  }
  return ''
}

function statusColor(status) {
  const value = String(status || '').toLowerCase()
  if (value === 'completed' || value === 'deployed' || value === 'ready') return 'success'
  if (value === 'running') return 'warning'
  if (value === 'error' || value === 'failed') return 'error'
  if (value === 'blocked') return 'grey'
  if (value === 'na') return 'blue-grey'
  return 'primary'
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

function openUrl(url) {
  if (!url) return
  window.open(url, '_blank', 'noopener')
}

function prettyBytes(bytes) {
  const value = Number(bytes || 0)
  if (!Number.isFinite(value) || value <= 0) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB']
  let size = value
  let idx = 0
  while (size >= 1024 && idx < units.length - 1) {
    size /= 1024
    idx += 1
  }
  return `${size.toFixed(size >= 10 || idx === 0 ? 0 : 1)} ${units[idx]}`
}

async function readAsDataUrl(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(String(reader.result || ''))
    reader.onerror = () => reject(new Error('No se pudo leer el archivo'))
    reader.readAsDataURL(file)
  })
}

function normalizePickedFile(value) {
  if (!value) return null
  if (Array.isArray(value)) return value[0] || null
  return value
}

function clearMediaSelection() {
  mediaFile.value = null
  mediaPayload.value = null
  mediaError.value = ''
}

async function onMediaChanged(value) {
  mediaError.value = ''
  mediaPayload.value = null

  const file = normalizePickedFile(value)
  if (!file) return

  const maxSize = MEDIA_MAX_BYTES
  if (Number(file.size || 0) > maxSize) {
    mediaError.value = `El archivo supera ${prettyBytes(maxSize)}.`
    return
  }

  const mime = String(file.type || '').toLowerCase()
  const expected = inputMode.value
  if (expected === 'audio' && !mime.startsWith('audio/')) {
    mediaError.value = 'Selecciona un archivo de audio válido.'
    return
  }
  if (expected === 'image' && !mime.startsWith('image/')) {
    mediaError.value = 'Selecciona una imagen válida.'
    return
  }

  try {
    const dataUrl = await readAsDataUrl(file)
    mediaPayload.value = {
      type: expected,
      fileName: file.name,
      sizeBytes: Number(file.size || 0),
      mime: mime || '',
      dataUrl
    }
  } catch (err) {
    mediaError.value = err?.message || 'No se pudo procesar el archivo.'
  }
}

const mediaAccept = computed(() => {
  if (inputMode.value === 'audio') return 'audio/*'
  if (inputMode.value === 'image') return 'image/*'
  return ''
})

const projectItems = computed(() => projects.value.map(project => ({
  title: project.Name || project.name || `Proyecto #${project.Id || project.id}`,
  value: project.Id || project.id
})))

const selectedProject = computed(() => projects.value.find(p => String(p.Id || p.id) === String(selectedProjectId.value)) || null)
const selectedProjectName = computed(() => selectedProject.value?.Name || selectedProject.value?.name || '')
const selectedTemplateKey = computed(() => String(selectedTemplate.value?.Key || selectedTemplate.value?.key || '').toLowerCase())

const canInfer = computed(() => Boolean(workflow.value?.CanInfer ?? workflow.value?.canInfer))
const projectStatus = computed(() => workflow.value?.ProjectStatus || workflow.value?.projectStatus || '')
const nextRunType = computed(() => String(workflow.value?.NextRunType || workflow.value?.nextRunType || '').toLowerCase())
const nextRunLabel = computed(() => runTypeLabels[nextRunType.value] || nextRunType.value || '—')

const contextParsed = computed(() => parseJson(inferContextJson.value, null))
const contextError = computed(() => {
  const raw = String(inferContextJson.value || '').trim()
  if (!raw) return ''
  return contextParsed.value.ok ? '' : 'Contexto JSON inválido.'
})

const canRunInfer = computed(() =>
  Boolean(selectedProjectId.value) &&
  (Boolean(inferInput.value.trim()) || Boolean(mediaPayload.value)) &&
  !inferLoading.value &&
  !contextError.value &&
  !mediaError.value &&
  canInfer.value
)

const inferOutputRaw = computed(() => {
  if (!inferResult.value) return ''
  return String(inferResult.value.Output || inferResult.value.output || inferResult.value.OutputJson || inferResult.value.outputJson || '')
})

const inferOutputJsonPretty = computed(() => {
  if (!inferResult.value) return ''

  const outputJson = inferResult.value.OutputJson || inferResult.value.outputJson
  if (outputJson) {
    const parsed = parseJson(outputJson, null)
    if (parsed.ok && parsed.value && typeof parsed.value === 'object') {
      return JSON.stringify(parsed.value, null, 2)
    }
  }

  const parsedRaw = parseJson(inferOutputRaw.value, null)
  if (parsedRaw.ok && parsedRaw.value && typeof parsedRaw.value === 'object') {
    return JSON.stringify(parsedRaw.value, null, 2)
  }

  return ''
})

const isMockResult = computed(() => Boolean(inferResult.value?.IsMock || inferResult.value?.isMock))

const quickVerdict = computed(() => {
  if (!inferResult.value) return null
  const expected = String(inferExpectedContains.value || '').trim()
  if (!expected) return null

  const output = `${inferOutputRaw.value}\n${inferOutputJsonPretty.value}`.toLowerCase()
  const ok = output.includes(expected.toLowerCase())
  return { ok, expected }
})

const latestDeploySummary = computed(() => {
  if (!latestDeployRun.value) {
    return { status: '', service: '', endpoint: '', health: '' }
  }

  const outputParsed = parseJson(latestDeployRun.value.Outputjson || latestDeployRun.value.outputjson || '', null)
  const inputParsed = parseJson(latestDeployRun.value.Inputjson || latestDeployRun.value.inputjson || '', null)
  const outputObj = outputParsed.ok ? outputParsed.value : null
  const inputObj = inputParsed.ok ? inputParsed.value : null
  const deploy = outputObj?.deploy || {}

  return {
    status: String(outputObj?.status || deploy?.status || latestDeployRun.value.Status || latestDeployRun.value.status || '').toLowerCase(),
    service: pickFirst(deploy?.service, outputObj?.service, deploy?.stackName, outputObj?.stackName, inputObj?.stackName),
    endpoint: pickFirst(deploy?.endpoint, outputObj?.endpoint, inputObj?.endpoint),
    health: pickFirst(deploy?.health, outputObj?.health, inputObj?.healthUrl)
  }
})

const demoCases = computed(() => {
  if (!selectedProjectId.value) return []

  const key = selectedTemplateKey.value
  if (key.includes('extractor')) {
    return [
      {
        id: 'ext-1',
        title: 'Hecho simple con dirección',
        description: 'Debe detectar tipo de hecho y lugar.',
        expectedContains: 'arrebato',
        input: 'Hoy 5 de marzo a las 21:10 en Avenida Colon 1250, barrio Alberdi, ocurrió un arrebato con moto.'
      },
      {
        id: 'ext-2',
        title: 'Robo de auto',
        description: 'Debe detectar robo y referencia de zona.',
        expectedContains: 'robo',
        input: 'A las 23:40 en calle Rondeau 300, Nueva Córdoba, rompieron el vidrio de un auto y robaron mochila.'
      },
      {
        id: 'ext-3',
        title: 'Hurto sin violencia',
        description: 'Debe inferir hurto/hecho según template.',
        expectedContains: 'hurto',
        input: 'En Duarte Quiros 2100 se denunció hurto de bicicleta durante la tarde.'
      }
    ]
  }

  if (key.includes('chat') || key.includes('rag')) {
    return [
      {
        id: 'rag-1',
        title: 'Pregunta de soporte',
        description: 'Debe responder de forma contextual.',
        expectedContains: 'respuesta',
        input: 'Cómo conecto mi dataset al proyecto para entrenar el modelo?'
      },
      {
        id: 'rag-2',
        title: 'Política operativa',
        description: 'Debe devolver respuesta útil y concreta.',
        expectedContains: 'respuesta',
        input: 'Qué necesito para desplegar en ambiente staging?'
      }
    ]
  }

  return [
    {
      id: 'gen-1',
      title: 'Prueba funcional 1',
      description: 'Caso base para validar salida del servicio.',
      expectedContains: 'respuesta',
      input: 'Devuelve una respuesta breve sobre el estado del modelo.'
    },
    {
      id: 'gen-2',
      title: 'Prueba funcional 2',
      description: 'Caso con texto más largo.',
      expectedContains: 'respuesta',
      input: 'Analiza este texto de ejemplo y responde en una sola salida útil.'
    }
  ]
})

function openDeployStage() {
  router.push({ path: '/stage/deploy', query: { projectId: selectedProjectId.value || undefined } })
}

function clearForm() {
  inferInput.value = ''
  inferContextJson.value = ''
  inferExpectedContains.value = ''
  inferResult.value = null
  inferError.value = ''
  outputMode.value = 'json'
  clearMediaSelection()
  inputMode.value = 'text'
}

async function copyText(value) {
  try {
    await navigator.clipboard.writeText(String(value || ''))
  } catch {
    // no-op
  }
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

async function loadProjectTemplate() {
  selectedTemplate.value = null
  if (!selectedProject.value) return

  const templateId = selectedProject.value.Templateid ?? selectedProject.value.TemplateId
  if (!templateId) return

  try {
    const response = await runtimeApi.get('templates', templateId)
    selectedTemplate.value = normalizeRecord(response?.data || null)
  } catch {
    selectedTemplate.value = null
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

async function loadLatestDeployInfo() {
  latestDeployRun.value = null
  if (!selectedProjectId.value) return

  loadingDeployRuns.value = true
  try {
    const response = await runtimeApi.listProjectRuns(selectedProjectId.value, 40)
    const allRuns = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
    latestDeployRun.value = allRuns.find(run => String(run.RunType || run.runType || '').toLowerCase() === 'deploy_service') || null
  } catch {
    latestDeployRun.value = null
  } finally {
    loadingDeployRuns.value = false
  }
}

function pushHistoryEntry({ mode, expectedContains = '', ok = false }) {
  const outputPreview = (inferOutputJsonPretty.value || inferOutputRaw.value || '').slice(0, 220)
  sessionHistory.value.unshift({
    localId: localHistoryId++,
    title: selectedProjectName.value || 'Proyecto',
    mode,
    when: formatDate(new Date().toISOString()),
    durationMs: lastInferDurationMs.value,
    expectedContains,
    ok: expectedContains ? ok : true,
    preview: outputPreview || 'Sin salida'
  })
  if (sessionHistory.value.length > 20) {
    sessionHistory.value = sessionHistory.value.slice(0, 20)
  }
}

function buildContextJson() {
  const base = contextParsed.value.ok ? (contextParsed.value.value || {}) : {}
  const ctx = typeof base === 'object' && base !== null ? { ...base } : {}

  ctx.inputType = inputMode.value

  if (includeMediaInContext.value && mediaPayload.value) {
    ctx.multimodal = true
    ctx.media = [
      {
        type: mediaPayload.value.type,
        fileName: mediaPayload.value.fileName,
        mime: mediaPayload.value.mime,
        sizeBytes: mediaPayload.value.sizeBytes,
        dataUrl: mediaPayload.value.dataUrl
      }
    ]
  }

  return Object.keys(ctx).length ? JSON.stringify(ctx) : null
}

async function runInfer(options = {}) {
  const mode = options.mode || 'manual'
  const expectedContains = String(options.expectedContains ?? inferExpectedContains.value ?? '').trim()

  inferError.value = ''
  inferResult.value = null
  outputMode.value = 'json'

  if (!selectedProjectId.value) {
    inferError.value = 'Selecciona un proyecto.'
    return false
  }

  if (!canInfer.value) {
    inferError.value = 'El proyecto todavía no habilitó inferencia (falta deploy).'
    return false
  }

  if (!inferInput.value.trim() && !mediaPayload.value) {
    inferError.value = 'Carga texto o un archivo para inferir.'
    return false
  }

  if (contextError.value) {
    inferError.value = contextError.value
    return false
  }

  if (mediaError.value) {
    inferError.value = mediaError.value
    return false
  }

  const payloadInput = inferInput.value.trim() || `[${inputMode.value.toUpperCase()}] ${mediaPayload.value?.fileName || 'input'}`
  const contextJson = buildContextJson()

  const startedAt = performance.now()
  inferLoading.value = true
  try {
    const response = await runtimeApi.inferProject(selectedProjectId.value, payloadInput, contextJson)
    inferResult.value = normalizeRecord(response?.data || {})
    lastInferDurationMs.value = Math.round(performance.now() - startedAt)

    const outputText = `${inferOutputRaw.value}\n${inferOutputJsonPretty.value}`.toLowerCase()
    const ok = expectedContains ? outputText.includes(expectedContains.toLowerCase()) : true
    pushHistoryEntry({ mode, expectedContains, ok })
    return ok
  } catch (err) {
    inferError.value = errorText(err, 'No se pudo ejecutar la inferencia.')
    lastInferDurationMs.value = Math.round(performance.now() - startedAt)
    pushHistoryEntry({ mode, expectedContains, ok: false })
    return false
  } finally {
    inferLoading.value = false
  }
}

function loadDemoCase(testCase) {
  inputMode.value = 'text'
  clearMediaSelection()
  inferInput.value = testCase.input
  inferExpectedContains.value = testCase.expectedContains
}

async function runDemoCase(testCase) {
  loadDemoCase(testCase)
  const ok = await runInfer({ mode: `demo:${testCase.id}`, expectedContains: testCase.expectedContains })
  caseResults.value = {
    ...caseResults.value,
    [testCase.id]: {
      ok,
      when: formatDate(new Date().toISOString())
    }
  }
}

async function runAllDemoCases() {
  for (const testCase of demoCases.value) {
    // eslint-disable-next-line no-await-in-loop
    await runDemoCase(testCase)
  }
}

async function load() {
  error.value = ''
  await loadProjects()
  await Promise.all([loadProjectTemplate(), loadWorkflow(), loadLatestDeployInfo()])
}

watch(selectedProjectId, async value => {
  router.replace({ path: '/stage/playground', query: { projectId: value || undefined } })
  inferError.value = ''
  inferResult.value = null
  caseResults.value = {}
  await Promise.all([loadProjectTemplate(), loadWorkflow(), loadLatestDeployInfo()])
})

watch(inputMode, value => {
  mediaError.value = ''
  if (value === 'text') {
    clearMediaSelection()
    return
  }
  if (mediaPayload.value && mediaPayload.value.type !== value) {
    clearMediaSelection()
  }
})

onMounted(load)
</script>

<style scoped>
.stage-page {
  padding-bottom: 30px;
}

.media-card {
  border-radius: 14px;
}

.media-preview {
  width: 100%;
}

.media-image {
  border-radius: 10px;
  border: 1px solid rgba(120, 140, 170, 0.2);
  height: 220px;
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

.result-meta {
  display: grid;
  gap: 6px;
  font-size: 0.9rem;
}

.preview-box {
  border: 1px solid rgba(120, 140, 170, 0.22);
  border-radius: 12px;
  padding: 10px;
  max-height: 280px;
  overflow: auto;
}

.preview-box pre {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
  font-size: 0.8rem;
}

.demo-case-list {
  display: grid;
  gap: 10px;
  max-height: 360px;
  overflow: auto;
}

.demo-case-item {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  padding: 10px;
}

.demo-case-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.session-list {
  display: grid;
  gap: 10px;
  max-height: 320px;
  overflow: auto;
}

.session-item {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  padding: 10px;
}

.session-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.session-meta {
  margin-top: 4px;
  color: var(--sb-text-soft, var(--sb-muted));
  font-size: 0.8rem;
}

.session-text {
  margin-top: 6px;
  font-size: 0.86rem;
  line-height: 1.35;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

@media (max-width: 960px) {
  .input-mode :deep(.v-btn-toggle) {
    width: 100%;
  }
}
</style>
