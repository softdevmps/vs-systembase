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
        <StageAssistant stage="template" class="mb-4" @apply="applyAssistantSuggestion" />

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
                :class="{ active: String(editingId || selectedExistingTemplateId || '') === String(item.Id || item.id) }"
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
              <div class="check-item" :class="{ ok: hasModelServiceConfig }">
                <v-icon size="16">{{ hasModelServiceConfig ? 'mdi-check-circle' : 'mdi-alert-circle-outline' }}</v-icon>
                <span>Servicio de inferencia configurado</span>
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
            <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-refresh" :disabled="saving" @click="startNewTemplate">Nuevo</v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <v-row dense class="mb-1">
              <v-col cols="12" md="8">
                <v-select
                  v-model="selectedExistingTemplateId"
                  :items="templatePickerItems"
                  item-title="title"
                  item-value="value"
                  label="Seleccionar template existente para editar"
                  density="comfortable"
                  clearable
                  :disabled="loading || saving"
                />
              </v-col>
              <v-col cols="12" md="4" class="d-flex align-center ga-2 flex-wrap">
                <v-btn
                  class="sb-btn"
                  variant="tonal"
                  prepend-icon="mdi-pencil"
                  :disabled="!selectedExistingTemplateId || saving"
                  @click="loadSelectedTemplate"
                >
                  Cargar edición
                </v-btn>
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="3">
                <v-text-field v-model="form.key" label="Key" density="comfortable" :rules="[rules.required, rules.max80]" />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field v-model="form.name" label="Nombre" density="comfortable" :rules="[rules.required, rules.max200]" />
              </v-col>
              <v-col cols="12" md="2">
                <v-text-field v-model="form.version" label="Versión" density="comfortable" :rules="[rules.required, rules.max20]" />
              </v-col>
              <v-col cols="12" md="3">
                <v-select
                  v-model="form.playgroundProfile"
                  :items="playgroundProfileOptions"
                  item-title="title"
                  item-value="value"
                  label="Perfil Playground"
                  density="comfortable"
                />
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
            <div class="section-title">Servicio de inferencia</div>
            <p class="text-caption text-medium-emphasis mb-2">
              Configura aquí dónde corre tu modelo para que el Playground pueda responder preguntas reales.
            </p>

            <v-row dense>
              <v-col cols="12" md="3">
                <v-switch
                  v-model="form.modelEnabled"
                  color="primary"
                  inset
                  density="comfortable"
                  label="Habilitado"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-select
                  v-model="form.modelProvider"
                  :items="modelProviderOptions"
                  item-title="title"
                  item-value="value"
                  label="Proveedor"
                  density="comfortable"
                  :disabled="!form.modelEnabled"
                />
              </v-col>
              <v-col cols="12" md="5">
                <v-text-field
                  v-model="form.modelName"
                  label="Modelo"
                  density="comfortable"
                  :rules="[form.modelEnabled && form.modelProvider !== 'engine' ? rules.required : true, rules.max120]"
                  :disabled="!form.modelEnabled"
                  placeholder="assistant-general-v1 / hf:google/flan-t5-base"
                />
              </v-col>
            </v-row>

            <v-row dense v-if="form.modelEnabled && form.modelProvider !== 'engine'">
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="form.modelBaseUrl"
                  label="Base URL"
                  density="comfortable"
                  :rules="[rules.required, rules.max300]"
                  placeholder="http://localhost:11434"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="form.modelPath"
                  label="Path API"
                  density="comfortable"
                  :rules="[rules.required, rules.max120]"
                  placeholder="/api/chat"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.modelTemperature"
                  label="Temperature"
                  type="number"
                  min="0"
                  max="2"
                  step="0.1"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.modelMaxTokens"
                  label="Max Tokens"
                  type="number"
                  min="64"
                  max="4096"
                  step="1"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="6" v-if="form.modelProvider === 'openai'">
                <v-text-field
                  v-model="form.modelApiKeyEnv"
                  label="API Key Env"
                  density="comfortable"
                  :rules="[rules.max120]"
                  placeholder="OPENAI_API_KEY"
                />
              </v-col>
              <v-col cols="12" v-if="form.modelProvider === 'openai'">
                <v-text-field
                  v-model="form.modelApiKey"
                  label="API Key (opcional)"
                  density="comfortable"
                  type="password"
                  autocomplete="off"
                  :rules="[rules.max300]"
                  placeholder="Si queda vacío usa API Key Env"
                />
              </v-col>
            </v-row>

            <v-row dense v-if="form.modelEnabled && form.modelProvider === 'engine'">
              <v-col cols="12" md="3">
                <v-select
                  v-model="form.modelTask"
                  :items="modelTaskOptions"
                  item-title="title"
                  item-value="value"
                  label="Task HF"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.modelTopP"
                  label="Top P"
                  type="number"
                  min="0.05"
                  max="1"
                  step="0.01"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.modelRepetitionPenalty"
                  label="Rep penalty"
                  type="number"
                  min="0.8"
                  max="2.5"
                  step="0.05"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="3" class="d-flex align-center">
                <v-switch
                  v-model="form.modelLocalFilesOnly"
                  color="primary"
                  inset
                  density="comfortable"
                  label="Solo pesos locales"
                />
              </v-col>
              <v-col cols="12" md="3" class="d-flex align-center">
                <v-switch
                  v-model="form.autoLearnEnabled"
                  color="primary"
                  inset
                  density="comfortable"
                  label="Auto-learning ML"
                />
              </v-col>
              <v-col cols="12" md="3" class="d-flex align-center">
                <v-switch
                  v-model="form.autoLearnCaptureFallback"
                  color="primary"
                  inset
                  density="comfortable"
                  label="Aprender también fallback"
                  :disabled="!form.autoLearnEnabled"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.autoLearnMinQualityScore"
                  label="Min quality para aprender"
                  type="number"
                  min="0"
                  max="1"
                  step="0.05"
                  density="comfortable"
                  :disabled="!form.autoLearnEnabled"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.autoLearnMaxRecords"
                  label="Max memoria aprendida"
                  type="number"
                  min="50"
                  max="100000"
                  step="50"
                  density="comfortable"
                  :disabled="!form.autoLearnEnabled"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.autoLearnMaxDocsForRetrieval"
                  label="Max docs ML en retrieval"
                  type="number"
                  min="20"
                  max="5000"
                  step="10"
                  density="comfortable"
                  :disabled="!form.autoLearnEnabled"
                />
              </v-col>
              <v-col cols="12" md="3" class="d-flex align-center">
                <v-switch
                  v-model="form.reasoningEnabled"
                  color="primary"
                  inset
                  density="comfortable"
                  label="Modo razonamiento"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.reasoningPasses"
                  label="Pasadas de razonamiento"
                  type="number"
                  min="1"
                  max="3"
                  step="1"
                  density="comfortable"
                  :disabled="!form.reasoningEnabled"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.reasoningMaxPlanSteps"
                  label="Max pasos de plan"
                  type="number"
                  min="2"
                  max="8"
                  step="1"
                  density="comfortable"
                  :disabled="!form.reasoningEnabled"
                />
              </v-col>
              <v-col cols="12" md="3" class="d-flex align-center">
                <v-switch
                  v-model="form.reasoningUseVerifier"
                  color="primary"
                  inset
                  density="comfortable"
                  label="Verificador final"
                  :disabled="!form.reasoningEnabled"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.reasoningMinSelfScore"
                  label="Min score verificador"
                  type="number"
                  min="0"
                  max="1"
                  step="0.05"
                  density="comfortable"
                  :disabled="!form.reasoningEnabled || !form.reasoningUseVerifier"
                />
              </v-col>
              <v-col cols="12" md="3" class="d-flex align-center">
                <v-switch
                  v-model="form.qualityGateEnabled"
                  color="primary"
                  inset
                  density="comfortable"
                  label="Quality gate"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.qualityGateMinSamples"
                  label="Min muestras"
                  type="number"
                  min="1"
                  max="500"
                  step="1"
                  density="comfortable"
                  :disabled="!form.qualityGateEnabled"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.qualityGateMinSuccessRate"
                  label="Min success rate"
                  type="number"
                  min="0"
                  max="1"
                  step="0.05"
                  density="comfortable"
                  :disabled="!form.qualityGateEnabled"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.qualityGateMaxFallbackRate"
                  label="Max fallback rate"
                  type="number"
                  min="0"
                  max="1"
                  step="0.05"
                  density="comfortable"
                  :disabled="!form.qualityGateEnabled"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="form.qualityGateMaxAvgLatencyMs"
                  label="Max avg latency ms"
                  type="number"
                  min="0"
                  max="1800000"
                  step="100"
                  density="comfortable"
                  :disabled="!form.qualityGateEnabled"
                />
              </v-col>
            </v-row>

            <v-textarea
              v-if="form.modelEnabled"
              v-model="form.modelSystemPrompt"
              label="System Prompt base"
              auto-grow
              rows="2"
              density="comfortable"
              :rules="[rules.max2000]"
            />

            <v-alert
              v-if="form.modelEnabled && form.modelProvider === 'engine'"
              class="mt-2"
              type="info"
              variant="tonal"
              density="comfortable"
              text="Provider Engine local: usará AIBASE_ENGINE_URL (motor propio AIBase)."
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

            <div class="section-subtitle mt-4 d-flex align-center justify-space-between flex-wrap ga-2">
              <span>Constructor visual del contrato</span>
              <div class="d-flex align-center ga-2 flex-wrap">
                <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-plus" @click="addContractField()">
                  Campo
                </v-btn>
                <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-sync" @click="syncContractFieldsFromJson">
                  Traer desde JSON
                </v-btn>
                <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-code-json" @click="applyBuilderToContractJson">
                  Autogenerar JSON
                </v-btn>
                <v-btn class="sb-btn" variant="tonal" size="small" prepend-icon="mdi-auto-fix" @click="autoFixContractJson">
                  Autocorregir JSON
                </v-btn>
              </div>
            </div>

            <div class="contract-builder mt-2">
              <div v-if="!contractFields.length" class="text-caption text-medium-emphasis">
                Agrega campos o sincroniza desde el JSON para construir el esquema visualmente.
              </div>
              <div v-else class="contract-field-grid">
                <div v-for="field in contractFields" :key="field.id" class="contract-field-row">
                  <v-text-field
                    v-model="field.name"
                    label="Campo"
                    density="compact"
                    hide-details
                    placeholder="ej. tipoHecho"
                  />
                  <v-select
                    v-model="field.type"
                    :items="schemaTypeOptions"
                    item-title="title"
                    item-value="value"
                    label="Tipo"
                    density="compact"
                    hide-details
                  />
                  <v-text-field
                    v-model="field.enumCsv"
                    label="Enum (csv)"
                    density="compact"
                    hide-details
                    placeholder="robo, hurto, arrebato"
                  />
                  <v-checkbox v-model="field.required" hide-details density="compact" label="Req." />
                  <v-btn icon variant="text" color="error" @click="removeContractField(field.id)">
                    <v-icon>mdi-delete-outline</v-icon>
                  </v-btn>
                </div>
              </div>
            </div>

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
              <v-btn
                v-if="editingId"
                class="sb-btn"
                variant="tonal"
                color="error"
                prepend-icon="mdi-delete-outline"
                :loading="deleting"
                :disabled="!canDelete"
                @click="deleteTemplate"
              >
                Eliminar template
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
import StageAssistant from '../../components/Workflow/StageAssistant.vue'

const router = useRouter()

const loading = ref(false)
const saving = ref(false)
const deleting = ref(false)
const error = ref('')
const templates = ref([])
const editingId = ref(null)
const selectedExistingTemplateId = ref(null)
const form = reactive({
  presetKey: 'chatbot_rag',
  key: '',
  name: '',
  version: '1.0.0',
  playgroundProfile: 'chat',
  modelEnabled: true,
  modelProvider: 'engine',
  modelBaseUrl: '',
  modelPath: '',
  modelName: 'assistant-general-v1',
  modelApiKey: '',
  modelApiKeyEnv: 'OPENAI_API_KEY',
  modelSystemPrompt: '',
  modelTask: '',
  modelTemperature: 0.2,
  modelMaxTokens: 512,
  modelTopP: 0.95,
  modelRepetitionPenalty: 1.05,
  modelLocalFilesOnly: true,
  autoLearnEnabled: true,
  autoLearnCaptureFallback: true,
  autoLearnMinQualityScore: 0.35,
  autoLearnMaxRecords: 3000,
  autoLearnMaxDocsForRetrieval: 300,
  reasoningEnabled: true,
  reasoningPasses: 1,
  reasoningUseVerifier: true,
  reasoningMinSelfScore: 0.55,
  reasoningMaxPlanSteps: 4,
  qualityGateEnabled: true,
  qualityGateMinSamples: 3,
  qualityGateMinSuccessRate: 0.6,
  qualityGateMaxFallbackRate: 0.4,
  qualityGateMaxAvgLatencyMs: 25000,
  description: '',
  objective: '',
  outputSchemaJson: '{}',
  taxonomyJson: '{}',
  validationRulesJson: '{}',
  annotationGuide: '',
  steps: ['dataset_build', 'rag_index', 'eval_run', 'deploy_service'],
  createdAt: null
})
const contractFields = ref([])

const schemaTypeOptions = [
  { title: 'string', value: 'string' },
  { title: 'number', value: 'number' },
  { title: 'integer', value: 'integer' },
  { title: 'boolean', value: 'boolean' },
  { title: 'array', value: 'array' },
  { title: 'object', value: 'object' }
]

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
    description: 'Asistente conversacional 1 a 1 en español, optimizado para chat general.',
    name: 'Chatbot ES 1 a 1',
    baseKey: 'chatbot-rag',
    steps: ['dataset_build', 'train_lora', 'eval_run', 'deploy_service'],
    profile: 'chat',
    objective: 'Conversar 1 a 1 en español con respuestas claras, útiles y estables.',
    outputSchema: { answer: '' },
    taxonomy: { intents: ['saludo', 'pregunta_general', 'seguimiento', 'otro'], tone: ['profesional', 'amigable'] },
    validationRules: { required: ['answer'] },
    annotationGuide: 'No repetir la pregunta ni mostrar instrucciones internas. Priorizar claridad y acción.',
    modelService: {
      provider: 'engine',
      baseUrl: '',
      path: '',
      model: 'hf:Qwen/Qwen2.5-1.5B-Instruct',
      task: 'text-generation',
      localFilesOnly: false,
      temperature: 0.35,
      maxTokens: 384,
      topP: 0.9,
      repetitionPenalty: 1.1,
      autoLearning: {
        enabled: true,
        captureFallback: true,
        minQualityScore: 0.35,
        maxRecords: 3000,
        maxDocsForRetrieval: 300
      },
      reasoning: {
        enabled: true,
        passes: 1,
        useVerifier: true,
        minSelfScore: 0.55,
        maxPlanSteps: 4
      },
      qualityGate: {
        enabled: true,
        minSamples: 10,
        minSuccessRate: 0.7,
        maxFallbackRate: 0.2,
        maxAvgLatencyMs: 30000
      },
      systemPrompt: 'Sos un asistente conversacional 1 a 1 en español. Respondé claro, breve y útil. Si no sabés algo, decilo y proponé cómo resolverlo. No repitas la pregunta del usuario ni muestres instrucciones internas.'
    }
  },
  {
    key: 'extractor_json',
    title: 'Extractor JSON',
    icon: 'mdi-file-code-outline',
    description: 'Extrae campos estructurados desde texto o transcripción.',
    name: 'Extractor JSON',
    baseKey: 'extractor-json',
    steps: ['dataset_build', 'train_lora', 'eval_run', 'deploy_service'],
    profile: 'extraction',
    objective: 'Transformar texto libre en un JSON consistente y validable.',
    outputSchema: { tipoHecho: '', lugarTexto: '', fechaHora: '', confidence: 0 },
    taxonomy: { tipoHecho: ['robo', 'arrebato', 'hurto', 'otro'] },
    validationRules: { required: ['tipoHecho', 'lugarTexto'], confidenceMin: 0.7 },
    annotationGuide: 'Si falta un dato, devolver null. No inventar valores.',
    modelService: {
      provider: 'engine',
      baseUrl: '',
      path: '',
      model: 'extractor-json-v1',
      temperature: 0.1,
      maxTokens: 384,
      systemPrompt: 'Extrae solo campos estructurados. No agregues texto fuera del formato pedido.'
    }
  },
  {
    key: 'transcriptor_audio',
    title: 'Transcripción audio',
    icon: 'mdi-microphone-message',
    description: 'Convierte audio a texto y aplica limpieza de salida.',
    name: 'Transcriptor Audio',
    baseKey: 'transcriptor-audio',
    steps: ['dataset_build', 'train_lora', 'eval_run', 'deploy_service'],
    profile: 'transcription',
    objective: 'Transcribir audios con alta legibilidad y detección de entidades clave.',
    outputSchema: { text: '', entities: [] },
    taxonomy: { entities: ['persona', 'lugar', 'hora'] },
    validationRules: { required: ['text'] },
    annotationGuide: 'Conservar sentido del discurso, corregir errores ortográficos menores.',
    modelService: {
      provider: 'engine',
      baseUrl: '',
      path: '',
      model: 'transcriptor-audio-v1',
      temperature: 0.1,
      maxTokens: 512,
      systemPrompt: 'Transcribe y resume con claridad. Si no hay audio, indica limitaciones.'
    }
  },
  {
    key: 'vision_ocr',
    title: 'OCR / visión',
    icon: 'mdi-image-text',
    description: 'Reconoce texto y entidades desde imágenes.',
    name: 'OCR Vision',
    baseKey: 'ocr-vision',
    steps: ['dataset_build', 'train_lora', 'eval_run', 'deploy_service'],
    profile: 'vision',
    objective: 'Detectar texto en imagen y devolverlo estructurado.',
    outputSchema: { text: '', blocks: [] },
    taxonomy: { blockTypes: ['line', 'paragraph', 'table'] },
    validationRules: { required: ['text'] },
    annotationGuide: 'Usar coordenadas en píxeles cuando se detecten bloques.',
    modelService: {
      provider: 'engine',
      baseUrl: '',
      path: '',
      model: 'vision-ocr-v1',
      temperature: 0.1,
      maxTokens: 512,
      systemPrompt: 'Analiza imágenes y responde en formato estructurado.'
    }
  },
  {
    key: 'custom',
    title: 'Personalizado',
    icon: 'mdi-tune-variant',
    description: 'Define manualmente el pipeline según tu caso.',
    name: 'Template Custom',
    baseKey: 'custom-template',
    steps: ['dataset_build', 'rag_index', 'train_lora', 'eval_run', 'deploy_service'],
    profile: 'generic',
    objective: 'Define objetivo y contrato según tu caso.',
    outputSchema: {},
    taxonomy: {},
    validationRules: {},
    annotationGuide: '',
    modelService: {
      provider: 'engine',
      baseUrl: '',
      path: '',
      model: 'custom-local-v1',
      temperature: 0.2,
      maxTokens: 512,
      systemPrompt: 'Eres un asistente configurable para casos generales.'
    }
  }
]

const playgroundProfileOptions = [
  { title: 'Chat', value: 'chat' },
  { title: 'Extracción JSON', value: 'extraction' },
  { title: 'Transcripción audio', value: 'transcription' },
  { title: 'Visión / OCR', value: 'vision' },
  { title: 'Genérico', value: 'generic' }
]

const modelProviderOptions = [
  { title: 'Engine local (propio)', value: 'engine' },
  { title: 'Ollama (local)', value: 'ollama' },
  { title: 'OpenAI compatible', value: 'openai' }
]

const modelTaskOptions = [
  { title: 'auto', value: '' },
  { title: 'text2text-generation', value: 'text2text-generation' },
  { title: 'text-generation', value: 'text-generation' }
]

const rules = {
  required: v => !!String(v ?? '').trim() || 'Campo requerido',
  max80: v => String(v ?? '').length <= 80 || 'Máximo 80 caracteres',
  max120: v => String(v ?? '').length <= 120 || 'Máximo 120 caracteres',
  max200: v => String(v ?? '').length <= 200 || 'Máximo 200 caracteres',
  max300: v => String(v ?? '').length <= 300 || 'Máximo 300 caracteres',
  max500: v => String(v ?? '').length <= 500 || 'Máximo 500 caracteres',
  max800: v => String(v ?? '').length <= 800 || 'Máximo 800 caracteres',
  max2000: v => String(v ?? '').length <= 2000 || 'Máximo 2000 caracteres',
  max20: v => String(v ?? '').length <= 20 || 'Máximo 20 caracteres'
}

const optionGuideItems = [
  { label: 'Tipo de modelo', description: 'Selecciona un preset para cargar un contrato base y pipeline sugerido.' },
  { label: 'Key / Nombre / Versión', description: 'Identifican el template. La key debe ser única y estable.' },
  { label: 'Perfil Playground', description: 'Controla la interfaz de prueba del modelo desde la UI.' },
  { label: 'Servicio de inferencia', description: 'Define proveedor (Ollama/OpenAI/Engine), endpoint, modelo y prompt base.' },
  { label: 'Parámetros de inferencia', description: 'Ajusta temperature/maxTokens/topP/task y quality gate antes de deploy.' },
  { label: 'Razonamiento multi-paso', description: 'Activa plan + refinamiento + verificador para respuestas más consistentes.' },
  { label: 'Auto-learning incremental', description: 'Permite que el engine aprenda de conversaciones y reutilice respuestas sin rebuild manual.' },
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

function errorText(err, fallback) {
  return err?.response?.data?.error
    || err?.response?.data?.message
    || (typeof err?.response?.data === 'string' ? err.response.data : null)
    || err?.message
    || fallback
}

function parseEnumCsv(value) {
  return String(value || '')
    .split(',')
    .map(item => item.trim())
    .filter(Boolean)
}

function defaultValueByType(type) {
  const key = String(type || '').toLowerCase()
  if (key === 'number' || key === 'integer') return 0
  if (key === 'boolean') return false
  if (key === 'array') return []
  if (key === 'object') return {}
  return ''
}

function inferTypeFromValue(value) {
  if (Array.isArray(value)) return 'array'
  if (value && typeof value === 'object') return 'object'
  if (typeof value === 'number') return Number.isInteger(value) ? 'integer' : 'number'
  if (typeof value === 'boolean') return 'boolean'
  return 'string'
}

function createContractField(seed = {}) {
  return {
    id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    name: String(seed.name || '').trim(),
    type: String(seed.type || 'string').toLowerCase(),
    required: Boolean(seed.required),
    enumCsv: Array.isArray(seed.enum) ? seed.enum.join(', ') : String(seed.enumCsv || '')
  }
}

function addContractField(seed = {}) {
  contractFields.value.push(createContractField(seed))
}

function removeContractField(fieldId) {
  contractFields.value = contractFields.value.filter(field => field.id !== fieldId)
}

function syncContractFieldsFromJson() {
  if (!parsedOutputSchema.value.ok || !parsedValidationRules.value.ok || !parsedTaxonomy.value.ok) return

  const schema = parsedOutputSchema.value.value && typeof parsedOutputSchema.value.value === 'object'
    ? parsedOutputSchema.value.value
    : {}
  const validationRules = parsedValidationRules.value.value && typeof parsedValidationRules.value.value === 'object'
    ? parsedValidationRules.value.value
    : {}
  const taxonomy = parsedTaxonomy.value.value && typeof parsedTaxonomy.value.value === 'object'
    ? parsedTaxonomy.value.value
    : {}
  const requiredSet = new Set(Array.isArray(validationRules.required) ? validationRules.required.map(String) : [])

  const keys = Object.keys(schema)
  const next = keys.map(name => {
    const schemaValue = schema[name]
    const taxonomyValue = taxonomy[name]
    return createContractField({
      name,
      type: inferTypeFromValue(schemaValue),
      required: requiredSet.has(name),
      enum: Array.isArray(taxonomyValue) ? taxonomyValue : []
    })
  })

  contractFields.value = next
}

function applyBuilderToContractJson() {
  const usableFields = contractFields.value
    .map(field => ({
      name: String(field.name || '').trim(),
      type: String(field.type || 'string').toLowerCase(),
      required: Boolean(field.required),
      enum: parseEnumCsv(field.enumCsv)
    }))
    .filter(field => field.name)

  if (!usableFields.length) return

  const outputSchema = {}
  const taxonomy = parsedTaxonomy.value.ok && parsedTaxonomy.value.value && typeof parsedTaxonomy.value.value === 'object'
    ? { ...parsedTaxonomy.value.value }
    : {}
  const validationRules = parsedValidationRules.value.ok && parsedValidationRules.value.value && typeof parsedValidationRules.value.value === 'object'
    ? { ...parsedValidationRules.value.value }
    : {}

  for (const field of usableFields) {
    outputSchema[field.name] = defaultValueByType(field.type)
    if (field.enum.length) taxonomy[field.name] = field.enum
    else if (Array.isArray(taxonomy[field.name])) delete taxonomy[field.name]
  }

  const required = usableFields.filter(field => field.required).map(field => field.name)
  if (required.length) validationRules.required = required
  else if (Array.isArray(validationRules.required)) delete validationRules.required

  form.outputSchemaJson = JSON.stringify(outputSchema, null, 2)
  form.taxonomyJson = JSON.stringify(taxonomy, null, 2)
  form.validationRulesJson = JSON.stringify(validationRules, null, 2)
}

function autoFixJsonText(text) {
  const raw = String(text || '').trim()
  if (!raw) return '{}'
  const direct = parseJson(raw, null)
  if (direct.ok) return JSON.stringify(direct.value ?? {}, null, 2)
  const starts = ['{', '[']
    .map(char => raw.indexOf(char))
    .filter(index => index >= 0)
  const start = starts.length ? Math.min(...starts) : -1
  const endBrace = raw.lastIndexOf('}')
  const endBracket = raw.lastIndexOf(']')
  const end = Math.max(endBrace, endBracket)
  let candidate = start >= 0 && end > start ? raw.slice(start, end + 1) : raw

  candidate = candidate
    .replace(/,\s*([}\]])/g, '$1')
    .replace(/([{,]\s*)([A-Za-z_][A-Za-z0-9_]*)(\s*:)/g, '$1"$2"$3')
    .replace(/'/g, '"')

  const fixed = parseJson(candidate, null)
  if (fixed.ok) return JSON.stringify(fixed.value ?? {}, null, 2)
  return text
}

function autoFixContractJson() {
  form.outputSchemaJson = autoFixJsonText(form.outputSchemaJson)
  form.taxonomyJson = autoFixJsonText(form.taxonomyJson)
  form.validationRulesJson = autoFixJsonText(form.validationRulesJson)
  syncContractFieldsFromJson()
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
const hasModelServiceConfig = computed(() => {
  if (!form.modelEnabled) return true
  const provider = normalizeModelProvider(form.modelProvider)
  if (provider === 'engine') return true
  return Boolean(String(form.modelBaseUrl).trim()) && Boolean(String(form.modelName).trim())
})

const canSave = computed(() => {
  return (
    String(form.key).trim().length > 0 &&
    String(form.name).trim().length > 0 &&
    String(form.version).trim().length > 0 &&
    form.steps.length > 0 &&
    String(form.objective).trim().length > 0 &&
    jsonErrors.value.length === 0 &&
    hasModelServiceConfig.value
  )
})
const canDelete = computed(() => Boolean(editingId.value) && !saving.value && !deleting.value)

const pipelinePreview = computed(() => {
  const payload = buildPipelinePayload()
  return JSON.stringify(payload, null, 2)
})

const templatePickerItems = computed(() =>
  templates.value.map(item => ({
    title: `${item.Name || item.name} (v${item.Version || item.version || '1'}) · ${item.Key || item.key || 'sin-key'}`,
    value: item.Id || item.id
  }))
)

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

function normalizeModelProvider(value) {
  const key = String(value || '').trim().toLowerCase()
  if (['openai-compatible', 'openai_compatible', 'chatgpt'].includes(key)) return 'openai'
  if (['openai', 'ollama', 'engine'].includes(key)) return key
  return 'ollama'
}

function defaultModelService(preset = null) {
  const basePrompt = preset?.objective || 'Responde de forma útil, breve y basada en evidencia.'
  return {
    enabled: true,
    provider: 'engine',
    baseUrl: '',
    path: '',
    model: 'assistant-general-v1',
    apiKey: '',
    apiKeyEnv: 'OPENAI_API_KEY',
    systemPrompt: basePrompt,
    task: '',
    localFilesOnly: true,
    temperature: 0.2,
    maxTokens: 512,
    topP: 0.95,
    repetitionPenalty: 1.05,
    autoLearning: {
      enabled: false,
      captureFallback: true,
      minQualityScore: 0.35,
      maxRecords: 3000,
      maxDocsForRetrieval: 300
    },
    reasoning: {
      enabled: true,
      passes: 1,
      useVerifier: true,
      minSelfScore: 0.55,
      maxPlanSteps: 4
    },
    qualityGate: {
      enabled: true,
      minSamples: 3,
      minSuccessRate: 0.6,
      maxFallbackRate: 0.4,
      maxAvgLatencyMs: 25000
    }
  }
}

function applyModelServiceToForm(config = null, preset = null) {
  const defaults = defaultModelService(preset)
  const provider = normalizeModelProvider(config?.provider ?? defaults.provider)

  form.modelEnabled = config?.enabled !== false
  form.modelProvider = provider
  form.modelBaseUrl = String(config?.baseUrl || config?.url || config?.endpoint || defaults.baseUrl)
  form.modelPath = String(config?.path || config?.apiPath || (provider === 'openai' ? '/v1/chat/completions' : defaults.path))
  form.modelName = String(config?.model || config?.modelName || (provider === 'openai' ? 'gpt-4o-mini' : defaults.model))
  form.modelApiKey = String(config?.apiKey || '')
  form.modelApiKeyEnv = String(config?.apiKeyEnv || defaults.apiKeyEnv)
  form.modelSystemPrompt = String(config?.systemPrompt || config?.prompt || defaults.systemPrompt)
  form.modelTask = String(config?.task || config?.hfTask || config?.inferenceTask || defaults.task || '').trim().toLowerCase()
  form.modelLocalFilesOnly = Boolean(
    config?.localFilesOnly
      ?? config?.hfLocalFilesOnly
      ?? config?.local_files_only
      ?? defaults.localFilesOnly
  )

  const temperature = Number(config?.temperature)
  form.modelTemperature = Number.isFinite(temperature) ? Math.max(0, Math.min(2, temperature)) : defaults.temperature

  const maxTokens = Number(config?.maxTokens)
  form.modelMaxTokens = Number.isFinite(maxTokens) ? Math.max(64, Math.min(4096, Math.round(maxTokens))) : defaults.maxTokens

  const topP = Number(config?.topP ?? config?.top_p)
  form.modelTopP = Number.isFinite(topP) ? Math.max(0.05, Math.min(1, topP)) : defaults.topP

  const repetitionPenalty = Number(config?.repetitionPenalty ?? config?.repetition_penalty)
  form.modelRepetitionPenalty = Number.isFinite(repetitionPenalty)
    ? Math.max(0.8, Math.min(2.5, repetitionPenalty))
    : defaults.repetitionPenalty

  const autoLearning = config?.autoLearning && typeof config.autoLearning === 'object'
    ? config.autoLearning
    : {}
  const autoDefaults = defaults.autoLearning || {}
  form.autoLearnEnabled = Boolean(
    autoLearning.enabled
      ?? config?.autoLearningEnabled
      ?? autoDefaults.enabled
  )
  form.autoLearnCaptureFallback = Boolean(
    autoLearning.captureFallback
      ?? autoLearning.allowFallback
      ?? config?.autoLearningCaptureFallback
      ?? autoDefaults.captureFallback
  )
  const autoMinQuality = Number(autoLearning.minQualityScore ?? config?.autoLearningMinQualityScore)
  form.autoLearnMinQualityScore = Number.isFinite(autoMinQuality)
    ? Math.max(0, Math.min(1, autoMinQuality))
    : Number(autoDefaults.minQualityScore ?? 0.35)

  const autoMaxRecords = Number(autoLearning.maxRecords ?? config?.autoLearningMaxRecords)
  form.autoLearnMaxRecords = Number.isFinite(autoMaxRecords)
    ? Math.max(50, Math.min(100000, Math.round(autoMaxRecords)))
    : Number(autoDefaults.maxRecords ?? 3000)

  const autoMaxDocs = Number(autoLearning.maxDocsForRetrieval ?? config?.autoLearningMaxDocsForRetrieval)
  form.autoLearnMaxDocsForRetrieval = Number.isFinite(autoMaxDocs)
    ? Math.max(20, Math.min(form.autoLearnMaxRecords, Math.round(autoMaxDocs)))
    : Number(autoDefaults.maxDocsForRetrieval ?? 300)

  const reasoning = config?.reasoning && typeof config.reasoning === 'object'
    ? config.reasoning
    : {}
  const reasoningDefaults = defaults.reasoning || {}
  form.reasoningEnabled = Boolean(
    reasoning.enabled
      ?? config?.reasoningEnabled
      ?? reasoningDefaults.enabled
  )
  const reasoningPasses = Number(reasoning.passes ?? config?.reasoningPasses)
  form.reasoningPasses = Number.isFinite(reasoningPasses)
    ? Math.max(1, Math.min(3, Math.round(reasoningPasses)))
    : Number(reasoningDefaults.passes ?? 1)
  form.reasoningUseVerifier = Boolean(
    reasoning.useVerifier
      ?? config?.reasoningUseVerifier
      ?? reasoningDefaults.useVerifier
  )
  const reasoningMinSelfScore = Number(reasoning.minSelfScore ?? config?.reasoningMinSelfScore)
  form.reasoningMinSelfScore = Number.isFinite(reasoningMinSelfScore)
    ? Math.max(0, Math.min(1, reasoningMinSelfScore))
    : Number(reasoningDefaults.minSelfScore ?? 0.55)
  const reasoningMaxPlanSteps = Number(reasoning.maxPlanSteps ?? config?.reasoningMaxPlanSteps)
  form.reasoningMaxPlanSteps = Number.isFinite(reasoningMaxPlanSteps)
    ? Math.max(2, Math.min(8, Math.round(reasoningMaxPlanSteps)))
    : Number(reasoningDefaults.maxPlanSteps ?? 4)

  const qualityGate = config?.qualityGate && typeof config.qualityGate === 'object'
    ? config.qualityGate
    : {}
  form.qualityGateEnabled = Boolean(
    qualityGate.enabled
      ?? config?.qualityGateEnabled
      ?? defaults.qualityGate.enabled
  )
  const qMinSamples = Number(qualityGate.minSamples ?? config?.qualityGateMinSamples)
  form.qualityGateMinSamples = Number.isFinite(qMinSamples)
    ? Math.max(1, Math.min(500, Math.round(qMinSamples)))
    : defaults.qualityGate.minSamples

  const qMinSuccessRate = Number(qualityGate.minSuccessRate ?? config?.qualityGateMinSuccessRate)
  form.qualityGateMinSuccessRate = Number.isFinite(qMinSuccessRate)
    ? Math.max(0, Math.min(1, qMinSuccessRate))
    : defaults.qualityGate.minSuccessRate

  const qMaxFallbackRate = Number(qualityGate.maxFallbackRate ?? config?.qualityGateMaxFallbackRate)
  form.qualityGateMaxFallbackRate = Number.isFinite(qMaxFallbackRate)
    ? Math.max(0, Math.min(1, qMaxFallbackRate))
    : defaults.qualityGate.maxFallbackRate

  const qMaxAvgLatencyMs = Number(qualityGate.maxAvgLatencyMs ?? config?.qualityGateMaxAvgLatencyMs)
  form.qualityGateMaxAvgLatencyMs = Number.isFinite(qMaxAvgLatencyMs)
    ? Math.max(0, Math.min(1800000, Math.round(qMaxAvgLatencyMs)))
    : defaults.qualityGate.maxAvgLatencyMs
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
      description: form.description || null,
      playgroundProfile: form.playgroundProfile || 'generic',
      modelService: {
        enabled: Boolean(form.modelEnabled),
        provider: normalizeModelProvider(form.modelProvider),
        baseUrl: String(form.modelBaseUrl || '').trim(),
        path: String(form.modelPath || '').trim(),
        model: String(form.modelName || '').trim(),
        apiKey: String(form.modelApiKey || '').trim() || null,
        apiKeyEnv: String(form.modelApiKeyEnv || '').trim() || null,
        systemPrompt: String(form.modelSystemPrompt || '').trim() || null,
        task: String(form.modelTask || '').trim() || null,
        localFilesOnly: Boolean(form.modelLocalFilesOnly),
        temperature: Math.max(0, Math.min(2, Number(form.modelTemperature || 0))),
        maxTokens: Math.max(64, Math.min(4096, Math.round(Number(form.modelMaxTokens || 512)))),
        topP: Math.max(0.05, Math.min(1, Number(form.modelTopP || 0.95))),
        repetitionPenalty: Math.max(0.8, Math.min(2.5, Number(form.modelRepetitionPenalty || 1.05))),
        autoLearning: {
          enabled: Boolean(form.autoLearnEnabled),
          captureFallback: Boolean(form.autoLearnCaptureFallback),
          minQualityScore: Math.max(0, Math.min(1, Number(form.autoLearnMinQualityScore || 0))),
          maxRecords: Math.max(50, Math.min(100000, Math.round(Number(form.autoLearnMaxRecords || 3000)))),
          maxDocsForRetrieval: Math.max(
            20,
            Math.min(
              Math.max(50, Math.min(100000, Math.round(Number(form.autoLearnMaxRecords || 3000)))),
              Math.round(Number(form.autoLearnMaxDocsForRetrieval || 300))
            )
          )
        },
        reasoning: {
          enabled: Boolean(form.reasoningEnabled),
          passes: Math.max(1, Math.min(3, Math.round(Number(form.reasoningPasses || 1)))),
          useVerifier: Boolean(form.reasoningUseVerifier),
          minSelfScore: Math.max(0, Math.min(1, Number(form.reasoningMinSelfScore || 0))),
          maxPlanSteps: Math.max(2, Math.min(8, Math.round(Number(form.reasoningMaxPlanSteps || 4))))
        },
        qualityGate: {
          enabled: Boolean(form.qualityGateEnabled),
          minSamples: Math.max(1, Math.min(500, Math.round(Number(form.qualityGateMinSamples || 3)))),
          minSuccessRate: Math.max(0, Math.min(1, Number(form.qualityGateMinSuccessRate || 0.6))),
          maxFallbackRate: Math.max(0, Math.min(1, Number(form.qualityGateMaxFallbackRate || 0.4))),
          maxAvgLatencyMs: Math.max(0, Math.min(1800000, Math.round(Number(form.qualityGateMaxAvgLatencyMs || 25000))))
        }
      }
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
  form.playgroundProfile = preset.profile || 'generic'
  form.outputSchemaJson = JSON.stringify(preset.outputSchema || {}, null, 2)
  form.taxonomyJson = JSON.stringify(preset.taxonomy || {}, null, 2)
  form.validationRulesJson = JSON.stringify(preset.validationRules || {}, null, 2)
  form.annotationGuide = preset.annotationGuide || ''
  form.steps = [...preset.steps]
  applyModelServiceToForm(preset.modelService || null, preset)
  syncContractFieldsFromJson()
}

function applyAssistantSuggestion(fields) {
  if (!fields || typeof fields !== 'object') return

  const setIfString = key => {
    const value = fields[key]
    if (typeof value === 'string') form[key] = value
  }

  setIfString('presetKey')
  setIfString('key')
  setIfString('name')
  setIfString('version')
  setIfString('playgroundProfile')
  setIfString('modelProvider')
  setIfString('modelBaseUrl')
  setIfString('modelPath')
  setIfString('modelName')
  setIfString('modelApiKey')
  setIfString('modelApiKeyEnv')
  setIfString('modelSystemPrompt')
  setIfString('modelTask')
  setIfString('description')
  setIfString('objective')
  setIfString('outputSchemaJson')
  setIfString('taxonomyJson')
  setIfString('validationRulesJson')
  setIfString('annotationGuide')

  if (typeof fields.modelEnabled === 'boolean') form.modelEnabled = fields.modelEnabled
  if (Number.isFinite(Number(fields.modelTemperature))) form.modelTemperature = Number(fields.modelTemperature)
  if (Number.isFinite(Number(fields.modelMaxTokens))) form.modelMaxTokens = Number(fields.modelMaxTokens)
  if (Number.isFinite(Number(fields.modelTopP))) form.modelTopP = Number(fields.modelTopP)
  if (Number.isFinite(Number(fields.modelRepetitionPenalty))) form.modelRepetitionPenalty = Number(fields.modelRepetitionPenalty)
  if (typeof fields.modelLocalFilesOnly === 'boolean') form.modelLocalFilesOnly = fields.modelLocalFilesOnly
  if (typeof fields.autoLearnEnabled === 'boolean') form.autoLearnEnabled = fields.autoLearnEnabled
  if (typeof fields.autoLearnCaptureFallback === 'boolean') form.autoLearnCaptureFallback = fields.autoLearnCaptureFallback
  if (Number.isFinite(Number(fields.autoLearnMinQualityScore))) form.autoLearnMinQualityScore = Number(fields.autoLearnMinQualityScore)
  if (Number.isFinite(Number(fields.autoLearnMaxRecords))) form.autoLearnMaxRecords = Number(fields.autoLearnMaxRecords)
  if (Number.isFinite(Number(fields.autoLearnMaxDocsForRetrieval))) form.autoLearnMaxDocsForRetrieval = Number(fields.autoLearnMaxDocsForRetrieval)
  if (typeof fields.reasoningEnabled === 'boolean') form.reasoningEnabled = fields.reasoningEnabled
  if (Number.isFinite(Number(fields.reasoningPasses))) form.reasoningPasses = Number(fields.reasoningPasses)
  if (typeof fields.reasoningUseVerifier === 'boolean') form.reasoningUseVerifier = fields.reasoningUseVerifier
  if (Number.isFinite(Number(fields.reasoningMinSelfScore))) form.reasoningMinSelfScore = Number(fields.reasoningMinSelfScore)
  if (Number.isFinite(Number(fields.reasoningMaxPlanSteps))) form.reasoningMaxPlanSteps = Number(fields.reasoningMaxPlanSteps)
  if (typeof fields.qualityGateEnabled === 'boolean') form.qualityGateEnabled = fields.qualityGateEnabled
  if (Number.isFinite(Number(fields.qualityGateMinSamples))) form.qualityGateMinSamples = Number(fields.qualityGateMinSamples)
  if (Number.isFinite(Number(fields.qualityGateMinSuccessRate))) form.qualityGateMinSuccessRate = Number(fields.qualityGateMinSuccessRate)
  if (Number.isFinite(Number(fields.qualityGateMaxFallbackRate))) form.qualityGateMaxFallbackRate = Number(fields.qualityGateMaxFallbackRate)
  if (Number.isFinite(Number(fields.qualityGateMaxAvgLatencyMs))) form.qualityGateMaxAvgLatencyMs = Number(fields.qualityGateMaxAvgLatencyMs)

  if (Array.isArray(fields.steps)) {
    form.steps = fields.steps.map(step => String(step || '').trim().toLowerCase()).filter(Boolean)
  }

  syncContractFieldsFromJson()
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

function parsePipelineMeta(raw) {
  if (!raw) return null
  try {
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw
    return parsed?.meta || null
  } catch {
    return null
  }
}

function parseModelService(meta) {
  if (!meta || typeof meta !== 'object') return null
  return meta.modelService || meta.modelservice || null
}

function inferProfileFromKey(key) {
  const value = String(key || '').toLowerCase()
  if (value.includes('chat') || value.includes('rag') || value.includes('assistant')) return 'chat'
  if (value.includes('transcrib') || value.includes('whisper') || value.includes('audio')) return 'transcription'
  if (value.includes('vision') || value.includes('image') || value.includes('ocr')) return 'vision'
  if (value.includes('extract')) return 'extraction'
  return 'generic'
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
  selectedExistingTemplateId.value = null
  form.presetKey = 'chatbot_rag'
  form.key = ''
  form.name = ''
  form.version = '1.0.0'
  form.playgroundProfile = 'chat'
  form.description = ''
  form.objective = ''
  form.outputSchemaJson = '{}'
  form.taxonomyJson = '{}'
  form.validationRulesJson = '{}'
  form.annotationGuide = ''
  form.steps = ['dataset_build', 'rag_index', 'eval_run', 'deploy_service']
  form.createdAt = null
  form.modelTask = ''
  form.modelTopP = 0.95
  form.modelRepetitionPenalty = 1.05
  form.modelLocalFilesOnly = true
  form.autoLearnEnabled = true
  form.autoLearnCaptureFallback = true
  form.autoLearnMinQualityScore = 0.35
  form.autoLearnMaxRecords = 3000
  form.autoLearnMaxDocsForRetrieval = 300
  form.reasoningEnabled = true
  form.reasoningPasses = 1
  form.reasoningUseVerifier = true
  form.reasoningMinSelfScore = 0.55
  form.reasoningMaxPlanSteps = 4
  form.qualityGateEnabled = true
  form.qualityGateMinSamples = 3
  form.qualityGateMinSuccessRate = 0.6
  form.qualityGateMaxFallbackRate = 0.4
  form.qualityGateMaxAvgLatencyMs = 25000
  applyPreset(presets[0])
}

function loadTemplate(item) {
  const record = normalizeRecord(item)
  const parsedSteps = parsePipelineSteps(record.Pipelinejson)

  editingId.value = record.Id || record.id
  selectedExistingTemplateId.value = editingId.value
  form.key = record.Key || ''
  form.name = record.Name || ''
  form.version = record.Version || '1.0.0'
  form.description = record.Description || ''
  form.steps = parsedSteps.length ? parsedSteps : ['dataset_build', 'eval_run', 'deploy_service']
  form.presetKey = getPresetFromSteps(form.steps)
  form.createdAt = record.Createdat || record.createdAt || null

  const contract = parseContract(record.Pipelinejson)
  const meta = parsePipelineMeta(record.Pipelinejson)
  const modelService = parseModelService(meta)
  form.objective = contract?.objective || ''
  form.playgroundProfile = String(meta?.playgroundProfile || inferProfileFromKey(record.Key || record.key)).toLowerCase()
  form.outputSchemaJson = JSON.stringify(contract?.outputSchema || {}, null, 2)
  form.taxonomyJson = JSON.stringify(contract?.taxonomy || {}, null, 2)
  form.validationRulesJson = JSON.stringify(contract?.validationRules || {}, null, 2)
  form.annotationGuide = contract?.annotationGuide || ''
  applyModelServiceToForm(modelService, presets.find(x => x.key === form.presetKey) || null)
  syncContractFieldsFromJson()
}

function loadSelectedTemplate() {
  if (!selectedExistingTemplateId.value) return
  const selected = templates.value.find(item => String(item.Id || item.id) === String(selectedExistingTemplateId.value))
  if (!selected) return
  loadTemplate(selected)
}

function startNewTemplate() {
  resetForm()
}

async function loadTemplates() {
  loading.value = true
  error.value = ''
  try {
    const response = await runtimeApi.list('templates')
    templates.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
  } catch (err) {
    error.value = errorText(err, 'No se pudieron cargar templates.')
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
    error.value = errorText(err, 'No se pudo guardar el template.')
  } finally {
    saving.value = false
  }
}

async function deleteTemplate() {
  if (!editingId.value || !canDelete.value) return

  const targetName = String(form.name || '').trim() || `#${editingId.value}`
  const ok = window.confirm(`¿Eliminar template "${targetName}"? Esta acción no se puede deshacer.`)
  if (!ok) return

  deleting.value = true
  error.value = ''
  try {
    await runtimeApi.remove('templates', editingId.value)
    await loadTemplates()
    resetForm()
  } catch (err) {
    error.value = errorText(err, 'No se pudo eliminar el template.')
  } finally {
    deleting.value = false
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

watch(() => form.modelProvider, provider => {
  const normalized = normalizeModelProvider(provider)
  if (normalized === 'openai') {
    if (!String(form.modelPath || '').trim() || form.modelPath === '/api/chat') {
      form.modelPath = '/v1/chat/completions'
    }
    if (!String(form.modelName || '').trim() || form.modelName === 'llama3.2:3b') {
      form.modelName = 'gpt-4o-mini'
    }
    return
  }

  if (normalized === 'ollama') {
    if (!String(form.modelPath || '').trim() || form.modelPath === '/v1/chat/completions') {
      form.modelPath = '/api/chat'
    }
    if (!String(form.modelName || '').trim() || form.modelName === 'gpt-4o-mini') {
      form.modelName = 'llama3.2:3b'
    }
    return
  }

  if (normalized === 'engine') {
    if (form.modelPath === '/api/chat' || form.modelPath === '/v1/chat/completions') {
      form.modelPath = ''
    }
    if (!String(form.modelName || '').trim() || form.modelName === 'llama3.2:3b' || form.modelName === 'gpt-4o-mini') {
      form.modelName = 'assistant-general-v1'
    }
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

.template-btn.active {
  border-color: rgba(37, 99, 235, 0.45);
  box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.12);
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

.section-subtitle {
  font-weight: 600;
  font-size: 0.9rem;
}

.contract-builder {
  border: 1px solid rgba(120, 140, 170, 0.22);
  border-radius: 12px;
  padding: 10px;
  background: rgba(37, 99, 235, 0.02);
}

.contract-field-grid {
  display: grid;
  gap: 8px;
}

.contract-field-row {
  display: grid;
  grid-template-columns: minmax(0, 2fr) minmax(0, 1.2fr) minmax(0, 2fr) auto auto;
  gap: 8px;
  align-items: center;
}

.json-error-list {
  margin: 8px 0 0;
  padding-left: 20px;
}

@media (max-width: 960px) {
  .step-grid {
    grid-template-columns: 1fr;
  }

  .contract-field-row {
    grid-template-columns: 1fr;
  }
}
</style>
