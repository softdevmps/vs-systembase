<template>
  <v-container fluid class="stage-page">
    <v-card class="card sb-page-header">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center">
          <div class="sb-page-icon"><v-icon color="primary" size="24">mdi-shape-plus</v-icon></div>
          <div>
            <h2>Etapa 1 · Definir Template</h2>
            <p class="sb-page-subtitle">Define el contrato del modelo y el pipeline base del proyecto.</p>
          </div>
        </div>
        <div class="d-flex align-center ga-2 flex-wrap">
          <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-home" @click="router.push('/home')">Flujo</v-btn>
          <v-btn class="sb-btn primary" prepend-icon="mdi-arrow-right" @click="goNext">Ir a etapa Proyecto</v-btn>
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

    <v-alert
      v-if="jsonErrors.length"
      class="mt-2"
      type="error"
      variant="tonal"
      border="start"
      density="comfortable"
    >
      <div class="text-body-2">Revisa el contrato antes de guardar:</div>
      <ul class="json-error-list">
        <li v-for="item in jsonErrors" :key="item">{{ item }}</li>
      </ul>
    </v-alert>

    <OptionGuide class="mt-4" :items="optionGuideItems" />

    <v-row class="mt-4" dense>
      <v-col cols="12" md="4">
        <v-card class="card">
          <v-card-title>Tipo de modelo</v-card-title>
          <v-divider />
          <v-card-text>
            <div class="preset-list">
              <button
                v-for="preset in presets"
                :key="preset.key"
                class="preset-btn"
                type="button"
                :class="{ active: form.presetKey === preset.key }"
                @click="applyPreset(preset)"
              >
                <div class="d-flex align-center ga-2">
                  <v-icon color="primary" size="18">{{ preset.icon }}</v-icon>
                  <strong>{{ preset.title }}</strong>
                </div>
                <div class="text-caption text-medium-emphasis mt-1">{{ preset.description }}</div>
              </button>
            </div>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title>Templates existentes</v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loading" class="sb-skeleton" style="height: 140px;"></div>
            <div v-else-if="!templates.length" class="text-caption text-medium-emphasis">No hay templates activos.</div>
            <div v-else class="template-list">
              <button
                v-for="item in templates"
                :key="item.Id || item.id"
                type="button"
                class="template-btn"
                @click="loadTemplate(item)"
              >
                <div class="d-flex align-center justify-space-between ga-2">
                  <strong>{{ item.Name || item.name }}</strong>
                  <v-chip size="x-small" color="primary" variant="tonal">v{{ item.Version || item.version }}</v-chip>
                </div>
                <div class="text-caption text-medium-emphasis mt-1">{{ item.Key || item.key }}</div>
              </button>
            </div>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title>Checklist de etapa</v-card-title>
          <v-divider />
          <v-card-text>
            <div class="checklist-grid">
              <div class="check-item" :class="{ ok: hasBasicData }">
                <v-icon size="16">{{ hasBasicData ? 'mdi-check-circle' : 'mdi-circle-outline' }}</v-icon>
                <span>Datos base completos</span>
              </div>
              <div class="check-item" :class="{ ok: hasPipeline }">
                <v-icon size="16">{{ hasPipeline ? 'mdi-check-circle' : 'mdi-circle-outline' }}</v-icon>
                <span>Pipeline definido</span>
              </div>
              <div class="check-item" :class="{ ok: hasContract }">
                <v-icon size="16">{{ hasContract ? 'mdi-check-circle' : 'mdi-circle-outline' }}</v-icon>
                <span>Contrato de salida</span>
              </div>
              <div class="check-item" :class="{ ok: jsonErrors.length === 0 }">
                <v-icon size="16">{{ jsonErrors.length === 0 ? 'mdi-check-circle' : 'mdi-alert-circle-outline' }}</v-icon>
                <span>JSON válido</span>
              </div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="8">
        <v-card class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            <span>{{ editingId ? 'Editar template' : 'Nuevo template' }}</span>
            <v-btn v-if="editingId" class="sb-btn ghost" variant="text" prepend-icon="mdi-refresh" @click="resetForm">Nuevo</v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field v-model="form.key" label="Key" density="comfortable" :rules="[rules.required, rules.max80]" />
              </v-col>
              <v-col cols="12" md="5">
                <v-text-field v-model="form.name" label="Nombre" density="comfortable" :rules="[rules.required, rules.max200]" />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field v-model="form.version" label="Versión" density="comfortable" :rules="[rules.required, rules.max20]" />
              </v-col>
            </v-row>

            <v-textarea
              v-model="form.description"
              label="Descripción"
              auto-grow
              rows="2"
              density="comfortable"
              :rules="[rules.max500]"
            />

            <v-divider class="my-4" />
            <div class="section-title">Contrato del modelo</div>
            <p class="text-caption text-medium-emphasis mb-2">
              Define cómo debe responder el modelo para que Dataset, Evaluación y Playground usen el mismo contrato.
            </p>

            <v-textarea
              v-model="form.objective"
              label="Objetivo del modelo"
              auto-grow
              rows="2"
              density="comfortable"
              placeholder="Ej: extraer tipo de hecho, hora y lugar en JSON estructurado."
              :rules="[rules.required, rules.max800]"
            />

            <v-row dense>
              <v-col cols="12" md="6">
                <v-textarea
                  v-model="form.outputSchemaJson"
                  label="Output Schema (JSON)"
                  auto-grow
                  rows="5"
                  density="comfortable"
                  :error-messages="outputSchemaError ? [outputSchemaError] : []"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-textarea
                  v-model="form.taxonomyJson"
                  label="Taxonomía / Labels (JSON)"
                  auto-grow
                  rows="5"
                  density="comfortable"
                  :error-messages="taxonomyError ? [taxonomyError] : []"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-textarea
                  v-model="form.validationRulesJson"
                  label="Reglas de validación (JSON)"
                  auto-grow
                  rows="5"
                  density="comfortable"
                  :error-messages="validationRulesError ? [validationRulesError] : []"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-textarea
                  v-model="form.annotationGuide"
                  label="Guía de anotación"
                  auto-grow
                  rows="5"
                  density="comfortable"
                  placeholder="Criterios operativos para etiquetar o validar ejemplos."
                  :rules="[rules.max2000]"
                />
              </v-col>
            </v-row>

            <div class="mt-1 text-caption text-medium-emphasis">Pasos del pipeline</div>
            <div class="step-grid mt-2">
              <v-checkbox
                v-for="step in stageOptions"
                :key="step.value"
                :model-value="form.steps.includes(step.value)"
                :label="step.label"
                density="compact"
                hide-details
                @update:model-value="toggleStep(step.value)"
              />
            </div>

            <v-alert
              v-if="form.steps.length === 0"
              class="mt-2"
              type="warning"
              variant="tonal"
              density="comfortable"
              text="Selecciona al menos una etapa del pipeline."
            />

            <v-divider class="my-4" />
            <div class="d-flex align-center justify-space-between flex-wrap ga-2">
              <div class="text-caption text-medium-emphasis">Preview PipelineJson</div>
              <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-content-copy" @click="copyPreview">
                Copiar preview
              </v-btn>
            </div>
            <div class="preview-box mt-2">
              <pre>{{ pipelinePreview }}</pre>
            </div>

            <div class="d-flex align-center ga-2 mt-4 flex-wrap">
              <v-btn
                class="sb-btn primary"
                :loading="saving"
                :disabled="!canSave"
                prepend-icon="mdi-content-save"
                @click="saveTemplate"
              >
                {{ editingId ? 'Guardar cambios' : 'Crear template' }}
              </v-btn>
              <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-arrow-right" @click="goNext">Continuar a Proyecto</v-btn>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import runtimeApi from '../../api/runtime.service'
import OptionGuide from '../../components/Workflow/OptionGuide.vue'

const router = useRouter()

const loading = ref(false)
const saving = ref(false)
const error = ref('')
const templates = ref([])
const editingId = ref(null)
const form = reactive({
  presetKey: 'chatbot_rag',
  key: '',
  name: '',
  version: '1.0.0',
  description: '',
  objective: '',
  outputSchemaJson: '{}',
  taxonomyJson: '{}',
  validationRulesJson: '{}',
  annotationGuide: '',
  steps: ['dataset_build', 'rag_index', 'eval_run', 'deploy_service'],
  createdAt: null
})

const stageOptions = [
  { value: 'dataset_build', label: '3. Dataset' },
  { value: 'rag_index', label: '4. RAG Index' },
  { value: 'train_lora', label: '5. Train LoRA' },
  { value: 'eval_run', label: '6. Evaluar' },
  { value: 'deploy_service', label: '7. Deploy' }
]

const presets = [
  {
    key: 'chatbot_rag',
    title: 'Chatbot con RAG',
    icon: 'mdi-robot-excited-outline',
    description: 'Asistente de preguntas y respuestas sobre documentos.',
    name: 'Chatbot RAG',
    baseKey: 'chatbot-rag',
    steps: ['dataset_build', 'rag_index', 'eval_run', 'deploy_service'],
    objective: 'Responder preguntas con evidencia de documentos indexados.',
    outputSchema: { answer: '', sources: [] },
    taxonomy: { intents: ['consulta', 'seguimiento', 'otro'], tone: ['formal', 'neutral'] },
    validationRules: { required: ['answer'], minSources: 1 },
    annotationGuide: 'Toda respuesta debe incluir resumen y fuentes utilizadas.'
  },
  {
    key: 'extractor_json',
    title: 'Extractor JSON',
    icon: 'mdi-file-code-outline',
    description: 'Extrae campos estructurados desde texto o transcripción.',
    name: 'Extractor JSON',
    baseKey: 'extractor-json',
    steps: ['dataset_build', 'train_lora', 'eval_run', 'deploy_service'],
    objective: 'Transformar texto libre en un JSON consistente y validable.',
    outputSchema: { tipoHecho: '', lugarTexto: '', fechaHora: '', confidence: 0 },
    taxonomy: { tipoHecho: ['robo', 'arrebato', 'hurto', 'otro'] },
    validationRules: { required: ['tipoHecho', 'lugarTexto'], confidenceMin: 0.7 },
    annotationGuide: 'Si falta un dato, devolver null. No inventar valores.'
  },
  {
    key: 'transcriptor_audio',
    title: 'Transcripción audio',
    icon: 'mdi-microphone-message',
    description: 'Convierte audio a texto y aplica limpieza de salida.',
    name: 'Transcriptor Audio',
    baseKey: 'transcriptor-audio',
    steps: ['dataset_build', 'train_lora', 'eval_run', 'deploy_service'],
    objective: 'Transcribir audios con alta legibilidad y detección de entidades clave.',
    outputSchema: { text: '', entities: [] },
    taxonomy: { entities: ['persona', 'lugar', 'hora'] },
    validationRules: { required: ['text'] },
    annotationGuide: 'Conservar sentido del discurso, corregir errores ortográficos menores.'
  },
  {
    key: 'vision_ocr',
    title: 'OCR / visión',
    icon: 'mdi-image-text',
    description: 'Reconoce texto y entidades desde imágenes.',
    name: 'OCR Vision',
    baseKey: 'ocr-vision',
    steps: ['dataset_build', 'train_lora', 'eval_run', 'deploy_service'],
    objective: 'Detectar texto en imagen y devolverlo estructurado.',
    outputSchema: { text: '', blocks: [] },
    taxonomy: { blockTypes: ['line', 'paragraph', 'table'] },
    validationRules: { required: ['text'] },
    annotationGuide: 'Usar coordenadas en píxeles cuando se detecten bloques.'
  },
  {
    key: 'custom',
    title: 'Personalizado',
    icon: 'mdi-tune-variant',
    description: 'Define manualmente el pipeline según tu caso.',
    name: 'Template Custom',
    baseKey: 'custom-template',
    steps: ['dataset_build', 'rag_index', 'train_lora', 'eval_run', 'deploy_service'],
    objective: 'Define objetivo y contrato según tu caso.',
    outputSchema: {},
    taxonomy: {},
    validationRules: {},
    annotationGuide: ''
  }
]

const rules = {
  required: v => !!String(v ?? '').trim() || 'Campo requerido',
  max80: v => String(v ?? '').length <= 80 || 'Máximo 80 caracteres',
  max200: v => String(v ?? '').length <= 200 || 'Máximo 200 caracteres',
  max500: v => String(v ?? '').length <= 500 || 'Máximo 500 caracteres',
  max800: v => String(v ?? '').length <= 800 || 'Máximo 800 caracteres',
  max2000: v => String(v ?? '').length <= 2000 || 'Máximo 2000 caracteres',
  max20: v => String(v ?? '').length <= 20 || 'Máximo 20 caracteres'
}

const optionGuideItems = [
  { label: 'Tipo de modelo', description: 'Selecciona un preset para cargar un contrato base y pipeline sugerido.' },
  { label: 'Key / Nombre / Versión', description: 'Identifican el template. La key debe ser única y estable.' },
  { label: 'Objetivo del modelo', description: 'Describe claramente qué problema resuelve y qué salida debe entregar.' },
  { label: 'Output Schema (JSON)', description: 'Contrato de salida consumido por dataset, evaluación y playground.' },
  { label: 'Taxonomía / Labels', description: 'Define clases, etiquetas o categorías que el modelo puede usar.' },
  { label: 'Reglas de validación', description: 'Restringe formatos y campos obligatorios para asegurar calidad.' },
  { label: 'Etapas del pipeline', description: 'Activa o desactiva fases del workflow según el tipo de modelo.' }
]

function parseJson(text, fallback = null) {
  try {
    return { ok: true, value: JSON.parse(String(text || '').trim() || '{}') }
  } catch (err) {
    return { ok: false, value: fallback, message: err?.message || 'JSON inválido' }
  }
}

const parsedOutputSchema = computed(() => parseJson(form.outputSchemaJson, {}))
const parsedTaxonomy = computed(() => parseJson(form.taxonomyJson, {}))
const parsedValidationRules = computed(() => parseJson(form.validationRulesJson, {}))

const outputSchemaError = computed(() => (parsedOutputSchema.value.ok ? '' : 'Output Schema JSON inválido'))
const taxonomyError = computed(() => (parsedTaxonomy.value.ok ? '' : 'Taxonomía JSON inválida'))
const validationRulesError = computed(() => (parsedValidationRules.value.ok ? '' : 'Reglas JSON inválidas'))

const jsonErrors = computed(() => {
  const errors = []
  if (outputSchemaError.value) errors.push(outputSchemaError.value)
  if (taxonomyError.value) errors.push(taxonomyError.value)
  if (validationRulesError.value) errors.push(validationRulesError.value)
  return errors
})

const hasBasicData = computed(() =>
  Boolean(String(form.key).trim() && String(form.name).trim() && String(form.version).trim())
)
const hasPipeline = computed(() => form.steps.length > 0)
const hasContract = computed(() => Boolean(String(form.objective).trim()) && jsonErrors.value.length === 0)

const canSave = computed(() => {
  return (
    String(form.key).trim().length > 0 &&
    String(form.name).trim().length > 0 &&
    String(form.version).trim().length > 0 &&
    form.steps.length > 0 &&
    String(form.objective).trim().length > 0 &&
    jsonErrors.value.length === 0
  )
})

const pipelinePreview = computed(() => {
  const payload = buildPipelinePayload()
  return JSON.stringify(payload, null, 2)
})

function slugify(value) {
  return String(value || '')
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
}

function modelDescriptionFromPreset(preset) {
  if (!preset) return 'Template para modelo de IA.'
  return `${preset.title}: ${preset.description}`
}

function buildPipelinePayload() {
  return {
    version: form.version,
    preset: form.presetKey || 'custom',
    steps: form.steps.map(step => ({ name: step })),
    contract: {
      objective: form.objective?.trim() || null,
      outputSchema: parsedOutputSchema.value.value || {},
      taxonomy: parsedTaxonomy.value.value || {},
      validationRules: parsedValidationRules.value.value || {},
      annotationGuide: form.annotationGuide?.trim() || null
    },
    meta: {
      description: form.description || null
    }
  }
}

function applyPreset(preset) {
  form.presetKey = preset.key
  if (!editingId.value) {
    form.name = preset.name
    form.description = modelDescriptionFromPreset(preset)
    form.key = `${preset.baseKey}-v${String(form.version || '1.0.0').replace(/\./g, '-')}`
  }
  form.objective = preset.objective || ''
  form.outputSchemaJson = JSON.stringify(preset.outputSchema || {}, null, 2)
  form.taxonomyJson = JSON.stringify(preset.taxonomy || {}, null, 2)
  form.validationRulesJson = JSON.stringify(preset.validationRules || {}, null, 2)
  form.annotationGuide = preset.annotationGuide || ''
  form.steps = [...preset.steps]
}

function toggleStep(step) {
  if (form.steps.includes(step)) {
    form.steps = form.steps.filter(s => s !== step)
  } else {
    form.steps = [...form.steps, step]
  }
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
  ;['Id', 'Key', 'Name', 'Description', 'Pipelinejson', 'Version', 'Createdat', 'Updatedat'].forEach(ensure)
  return copy
}

function parsePipelineSteps(raw) {
  if (!raw) return []
  try {
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw
    const steps = Array.isArray(parsed?.steps) ? parsed.steps : []
    return steps
      .map(step => String(step?.name || step || '').trim().toLowerCase())
      .filter(Boolean)
  } catch {
    return []
  }
}

function parseContract(raw) {
  if (!raw) return null
  try {
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw
    return parsed?.contract || null
  } catch {
    return null
  }
}

function getPresetFromSteps(steps) {
  const normalized = [...steps].sort().join('|')
  for (const preset of presets) {
    const key = [...preset.steps].sort().join('|')
    if (key === normalized) return preset.key
  }
  return 'custom'
}

function resetForm() {
  editingId.value = null
  form.presetKey = 'chatbot_rag'
  form.key = ''
  form.name = ''
  form.version = '1.0.0'
  form.description = ''
  form.objective = ''
  form.outputSchemaJson = '{}'
  form.taxonomyJson = '{}'
  form.validationRulesJson = '{}'
  form.annotationGuide = ''
  form.steps = ['dataset_build', 'rag_index', 'eval_run', 'deploy_service']
  form.createdAt = null
  applyPreset(presets[0])
}

function loadTemplate(item) {
  const record = normalizeRecord(item)
  const parsedSteps = parsePipelineSteps(record.Pipelinejson)

  editingId.value = record.Id || record.id
  form.key = record.Key || ''
  form.name = record.Name || ''
  form.version = record.Version || '1.0.0'
  form.description = record.Description || ''
  form.steps = parsedSteps.length ? parsedSteps : ['dataset_build', 'eval_run', 'deploy_service']
  form.presetKey = getPresetFromSteps(form.steps)
  form.createdAt = record.Createdat || record.createdAt || null

  const contract = parseContract(record.Pipelinejson)
  form.objective = contract?.objective || ''
  form.outputSchemaJson = JSON.stringify(contract?.outputSchema || {}, null, 2)
  form.taxonomyJson = JSON.stringify(contract?.taxonomy || {}, null, 2)
  form.validationRulesJson = JSON.stringify(contract?.validationRules || {}, null, 2)
  form.annotationGuide = contract?.annotationGuide || ''
}

async function loadTemplates() {
  loading.value = true
  error.value = ''
  try {
    const response = await runtimeApi.list('templates')
    templates.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
  } catch (err) {
    const message = err?.response?.data?.error || err?.response?.data?.message || err?.message
    error.value = message || 'No se pudieron cargar templates.'
  } finally {
    loading.value = false
  }
}

async function saveTemplate() {
  if (!canSave.value) return

  saving.value = true
  error.value = ''
  const nowIso = new Date().toISOString()
  const payload = {
    key: form.key.trim(),
    name: form.name.trim(),
    description: form.description?.trim() || null,
    pipelinejson: JSON.stringify(buildPipelinePayload()),
    isactive: true,
    version: form.version.trim(),
    createdat: form.createdAt || nowIso,
    updatedat: nowIso
  }

  try {
    if (editingId.value) {
      await runtimeApi.update('templates', editingId.value, payload)
    } else {
      await runtimeApi.create('templates', payload)
    }
    await loadTemplates()
    resetForm()
  } catch (err) {
    const message = err?.response?.data?.error || err?.response?.data?.message || err?.message
    error.value = message || 'No se pudo guardar el template.'
  } finally {
    saving.value = false
  }
}

async function copyPreview() {
  try {
    await navigator.clipboard.writeText(pipelinePreview.value)
  } catch {
    // no-op
  }
}

function goNext() {
  router.push('/stage/project')
}

watch(() => form.name, value => {
  if (editingId.value) return
  if (!String(form.key || '').trim()) {
    form.key = slugify(value)
  }
})

onMounted(async () => {
  await loadTemplates()
  resetForm()
})
</script>

<style scoped>
.stage-page {
  padding-bottom: 30px;
}

.preset-list {
  display: grid;
  gap: 8px;
}

.preset-btn {
  border: 1px solid rgba(120, 140, 170, 0.22);
  border-radius: 12px;
  padding: 10px;
  text-align: left;
  background: transparent;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.preset-btn.active {
  border-color: rgba(37, 99, 235, 0.45);
  box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.12);
}

.template-list {
  display: grid;
  gap: 8px;
  max-height: 290px;
  overflow: auto;
}

.template-btn {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  padding: 10px;
  text-align: left;
  background: transparent;
}

.step-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0 8px;
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

.checklist-grid {
  display: grid;
  gap: 8px;
}

.check-item {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--sb-text-soft, var(--sb-muted));
}

.check-item.ok {
  color: var(--sb-primary, #2563eb);
}

.section-title {
  font-weight: 700;
  font-size: 1rem;
}

.json-error-list {
  margin: 8px 0 0;
  padding-left: 20px;
}

@media (max-width: 960px) {
  .step-grid {
    grid-template-columns: 1fr;
  }
}
</style>
