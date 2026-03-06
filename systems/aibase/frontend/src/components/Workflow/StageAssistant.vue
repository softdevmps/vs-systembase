<template>
  <v-card class="card stage-assistant">
    <v-card-title class="d-flex align-center justify-space-between">
      <div class="d-flex align-center ga-2">
        <v-icon color="primary">mdi-robot-excited-outline</v-icon>
        Asistente de configuración
      </div>
      <v-chip size="small" variant="tonal" color="primary">{{ stageLabel }}</v-chip>
    </v-card-title>
    <v-divider />
    <v-card-text>
      <p class="text-caption text-medium-emphasis mb-2">
        Describe el modelo que quieres crear y te propongo valores para esta etapa.
      </p>

      <v-textarea
        v-model="prompt"
        :placeholder="placeholder"
        auto-grow
        rows="2"
        density="comfortable"
        hide-details
      />

      <div class="d-flex align-center ga-2 mt-2 flex-wrap">
        <v-btn class="sb-btn primary" prepend-icon="mdi-send" :loading="loading" :disabled="!prompt.trim()" @click="sendPrompt">
          Sugerir
        </v-btn>
        <v-btn
          v-if="currentSuggestion"
          class="sb-btn"
          variant="tonal"
          prepend-icon="mdi-auto-fix"
          @click="applySuggestion"
        >
          Aplicar sugerencias
        </v-btn>
      </div>

      <v-alert
        v-if="error"
        class="mt-2"
        type="warning"
        variant="tonal"
        density="comfortable"
        :text="error"
      />

      <div v-if="messages.length" class="assistant-log mt-3">
        <div v-for="(msg, idx) in messages" :key="idx" class="log-item" :class="`is-${msg.role}`">
          <div class="log-role">{{ msg.role === 'user' ? 'Tú' : 'Asistente' }}</div>
          <div class="log-text">{{ msg.text }}</div>
        </div>
      </div>

      <div v-if="currentSuggestion" class="suggestion-box mt-3">
        <div class="text-caption text-medium-emphasis mb-1">Preview de sugerencias para esta etapa</div>
        <pre>{{ suggestionPreview }}</pre>
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup>
import { computed, ref } from 'vue'
import runtimeApi from '../../api/runtime.service'

const props = defineProps({
  stage: {
    type: String,
    required: true
  },
  projectId: {
    type: [String, Number],
    default: null
  }
})

const emit = defineEmits(['apply'])

const prompt = ref('')
const loading = ref(false)
const error = ref('')
const messages = ref([])
const currentSuggestion = ref(null)

const stageLabels = {
  template: 'Etapa 1 · Template',
  project: 'Etapa 2 · Proyecto',
  dataset: 'Etapa 3 · Dataset',
  rag: 'Etapa 4 · RAG',
  train: 'Etapa 5 · Train',
  eval: 'Etapa 6 · Evaluar',
  deploy: 'Etapa 7 · Deploy',
  playground: 'Etapa 8 · Playground'
}

const stageLabel = computed(() => stageLabels[props.stage] || props.stage)

const placeholderByStage = {
  template: 'Ej: Quiero un modelo para transcribir audios policiales y extraer tipo de hecho + ubicación.',
  project: 'Ej: Quiero un proyecto en español, tono formal, para soporte técnico interno.',
  dataset: 'Ej: Dataset en CSV de incidentes, split 80/10/10, deduplicado y normalización de texto.'
}

const placeholder = computed(() => placeholderByStage[props.stage] || 'Describe el caso de uso y objetivo del modelo.')

const suggestionPreview = computed(() => {
  if (!currentSuggestion.value?.fields) return ''
  return JSON.stringify(currentSuggestion.value.fields, null, 2)
})

function normalizeSuggestion(item) {
  return {
    stage: item?.stage || item?.Stage || '',
    title: item?.title || item?.Title || '',
    description: item?.description || item?.Description || '',
    fields: item?.fields || item?.Fields || {}
  }
}

async function sendPrompt() {
  if (!prompt.value.trim()) return

  loading.value = true
  error.value = ''
  try {
    messages.value.push({ role: 'user', text: prompt.value.trim() })

    const response = await runtimeApi.assistantSuggest(prompt.value.trim(), props.stage, props.projectId || null)
    const data = response?.data || {}

    const assistantMessage = data?.message || data?.Message || 'Listo. Te preparé sugerencias para esta etapa.'
    messages.value.push({ role: 'assistant', text: assistantMessage })

    const rawSuggestions = Array.isArray(data?.suggestions)
      ? data.suggestions
      : (Array.isArray(data?.Suggestions) ? data.Suggestions : [])

    const suggestions = rawSuggestions.map(normalizeSuggestion)
    const selected = suggestions.find(item => String(item.stage).toLowerCase() === String(props.stage).toLowerCase()) || suggestions[0] || null
    currentSuggestion.value = selected

    if (!selected) {
      error.value = 'No llegaron sugerencias aplicables para esta etapa.'
    }
  } catch (err) {
    const message = err?.response?.data?.error || err?.response?.data?.message || err?.message
    error.value = message || 'No se pudo obtener sugerencias del asistente.'
  } finally {
    loading.value = false
  }
}

function applySuggestion() {
  if (!currentSuggestion.value?.fields) return
  emit('apply', currentSuggestion.value.fields)
}
</script>

<style scoped>
.stage-assistant {
  border: 1px solid rgba(120, 140, 170, 0.22);
}

.assistant-log {
  display: grid;
  gap: 8px;
  max-height: 220px;
  overflow: auto;
}

.log-item {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  padding: 8px 10px;
}

.log-item.is-user {
  background: rgba(37, 99, 235, 0.06);
}

.log-role {
  font-size: 0.72rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--sb-text-soft, var(--sb-muted));
}

.log-text {
  font-size: 0.9rem;
  white-space: pre-wrap;
}

.suggestion-box {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  padding: 10px;
  background: rgba(37, 99, 235, 0.03);
  max-height: 230px;
  overflow: auto;
}

.suggestion-box pre {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
  font-size: 0.78rem;
}
</style>
