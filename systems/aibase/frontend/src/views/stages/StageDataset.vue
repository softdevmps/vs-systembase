<template>
  <v-container fluid class="stage-page">
    <v-card class="card sb-page-header">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center">
          <div class="sb-page-icon"><v-icon color="primary" size="24">mdi-database-cog</v-icon></div>
          <div>
            <h2>Etapa 3 · Dataset</h2>
            <p class="sb-page-subtitle">Configura la construcción del dataset y ejecuta la corrida de preparación.</p>
          </div>
        </div>
        <div class="d-flex align-center ga-2 flex-wrap">
          <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-arrow-left" @click="router.push('/stage/project')">Volver</v-btn>
          <v-btn class="sb-btn primary" prepend-icon="mdi-arrow-right" :disabled="!selectedProjectId" @click="goNext">Ir a RAG</v-btn>
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
        <StageAssistant
          stage="dataset"
          class="mb-4"
          :project-id="selectedProjectId"
          @apply="applyAssistantSuggestion"
        />

        <v-card class="card mb-4">
          <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-2">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-wizard-hat</v-icon>
              Dataset Wizard
            </div>
            <v-chip size="x-small" variant="tonal" color="primary">Data Ops</v-chip>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div class="text-caption text-medium-emphasis mb-2">Generar dataset por tópicos (Wikipedia)</div>
            <v-textarea
              v-model="generatorForm.topicsText"
              label="Tópicos (uno por línea o separados por coma)"
              auto-grow
              rows="3"
              density="comfortable"
              placeholder="python&#10;docker&#10;inteligencia artificial"
            />
            <v-row dense>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="generatorForm.datasetName"
                  label="Nombre dataset generado (opcional)"
                  density="comfortable"
                  placeholder="chat-es-base"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="generatorForm.maxWikipediaResults"
                  type="number"
                  min="1"
                  max="50"
                  step="1"
                  label="Resultados Wiki"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-text-field
                  v-model.number="generatorForm.maxExpandedQueries"
                  type="number"
                  min="1"
                  max="200"
                  step="1"
                  label="Consultas por tópico"
                  density="comfortable"
                />
              </v-col>
            </v-row>
            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="generatorForm.chunkSize"
                  type="number"
                  min="200"
                  max="8000"
                  step="50"
                  label="Chunk size"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="generatorForm.chunkOverlap"
                  type="number"
                  min="0"
                  max="3000"
                  step="10"
                  label="Chunk overlap"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="generatorForm.sleepSeconds"
                  type="number"
                  min="0"
                  max="10"
                  step="0.1"
                  label="Sleep entre consultas (seg)"
                  density="comfortable"
                />
              </v-col>
            </v-row>
            <div class="d-flex align-center justify-space-between flex-wrap ga-2 mb-3">
              <v-checkbox
                v-model="generatorForm.resetTopicFolders"
                hide-details
                density="compact"
                label="Resetear carpeta del tópico antes de generar"
              />
              <v-btn
                class="sb-btn"
                variant="tonal"
                prepend-icon="mdi-auto-fix"
                :loading="generatingDatasetByTopics"
                :disabled="!canGenerateByTopics"
                @click="generateDatasetByTopics"
              >
                Generar dataset por tópicos
              </v-btn>
            </div>

            <div v-if="generatingDatasetByTopics || generatorProgressPct > 0" class="generator-progress mb-3">
              <div class="d-flex align-center justify-space-between text-caption text-medium-emphasis mb-1">
                <span>
                  {{ generatingDatasetByTopics ? `Generando dataset... ${generatorProgressPct}%` : `Generación completada (${generatorProgressPct}%)` }}
                </span>
                <span>{{ generatorElapsedLabel }} / {{ generatorEstimateLabel }}</span>
              </div>
              <v-progress-linear
                :model-value="generatorProgressPct"
                :indeterminate="generatingDatasetByTopics && generatorEstimatedMs <= 0"
                height="8"
                rounded
                color="primary"
                bg-color="grey-lighten-3"
              />
            </div>

            <v-alert
              v-if="generatorMessage"
              class="mb-2"
              type="success"
              variant="tonal"
              density="comfortable"
              :text="generatorMessage"
            />
            <v-alert
              v-if="generatorError"
              class="mb-2"
              type="error"
              variant="tonal"
              density="comfortable"
              :text="generatorError"
            />

            <v-divider class="mb-3" />

            <v-file-input
              v-model="wizardFile"
              label="Cargar dataset local (CSV/JSON/JSONL)"
              density="comfortable"
              accept=".csv,.json,.jsonl,.txt"
              prepend-icon="mdi-upload"
              show-size
              clearable
              :loading="uploadingDatasetFile"
              @update:model-value="onDatasetFilePicked"
            />

            <v-alert
              v-if="uploadedDatasetMessage"
              class="mt-2"
              type="success"
              variant="tonal"
              density="comfortable"
              :text="uploadedDatasetMessage"
            />

            <v-alert
              v-if="wizardNotice"
              class="mt-2"
              type="info"
              variant="tonal"
              density="comfortable"
              :text="wizardNotice"
            />

            <div
              class="wizard-drop"
              :class="{ active: wizardDragActive }"
              @dragover.prevent="wizardDragActive = true"
              @dragleave.prevent="wizardDragActive = false"
              @drop.prevent="onWizardDrop"
            >
              <v-icon size="20" color="primary">mdi-tray-arrow-up</v-icon>
              <span>Arrastra un archivo aquí para inferir mapeo y calidad.</span>
            </div>

            <v-alert
              v-if="wizardError"
              class="mt-2"
              type="error"
              variant="tonal"
              density="comfortable"
              :text="wizardError"
            />

            <template v-if="wizardRows.length">
              <div class="mt-3 d-flex align-center ga-2 flex-wrap">
                <v-chip size="small" variant="tonal">Rows: {{ wizardRows.length }}</v-chip>
                <v-chip size="small" variant="tonal">Cols: {{ wizardColumns.length }}</v-chip>
                <v-chip size="small" variant="tonal" :color="wizardQuality.duplicatePct > 10 ? 'warning' : 'success'">
                  Duplicados: {{ wizardQuality.duplicatePct.toFixed(1) }}%
                </v-chip>
              </div>

              <div class="mt-3 text-caption text-medium-emphasis">Mapeo automático al contrato</div>
              <div class="wizard-mapping-grid mt-2">
                <div
                  v-for="targetField in outputSchemaFields"
                  :key="targetField"
                  class="wizard-mapping-row"
                >
                  <div class="text-body-2"><strong>{{ targetField }}</strong></div>
                  <v-select
                    v-model="wizardMappings[targetField]"
                    :items="wizardColumns"
                    label="Columna origen"
                    density="compact"
                    clearable
                    hide-details
                  />
                  <v-chip
                    size="x-small"
                    variant="tonal"
                    :color="mappedFieldQuality(targetField).nullPct > Number(datasetForm.maxNullPct || 5) ? 'warning' : 'success'"
                  >
                    Nulos {{ mappedFieldQuality(targetField).nullPct.toFixed(1) }}%
                  </v-chip>
                </div>
              </div>

              <div class="d-flex align-center ga-2 flex-wrap mt-3">
                <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-auto-fix" @click="autoMapColumns">
                  Auto-mapeo
                </v-btn>
                <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-content-save-edit-outline" @click="applyWizardToDatasetForm">
                  Aplicar al formulario
                </v-btn>
                <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-table-eye" @click="toggleWizardPreview">
                  {{ wizardPreviewVisible ? 'Ocultar preview' : 'Ver preview' }}
                </v-btn>
              </div>

              <div v-if="wizardPreviewVisible" class="preview-box mt-3">
                <pre>{{ wizardPreviewJson }}</pre>
              </div>
            </template>
          </v-card-text>
        </v-card>

        <v-card class="card">
          <v-card-title>Configuración de dataset</v-card-title>
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
                  density="comfortable"
                  :loading="loadingData"
                />
              </v-col>
              <v-col cols="12" md="5">
                <v-text-field
                  v-model="datasetForm.datasetVersion"
                  label="Versión dataset"
                  density="comfortable"
                  :rules="[rules.required, rules.max30]"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="4">
                <v-select
                  v-model="datasetForm.sourceType"
                  :items="sourceTypeOptions"
                  item-title="title"
                  item-value="value"
                  label="Fuente"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="8">
                <v-text-field
                  v-model="datasetForm.sourcePath"
                  label="Path/URI de fuente"
                  density="comfortable"
                  placeholder="/data/incidentes.csv o s3://bucket/file.jsonl"
                  :rules="[rules.required, rules.max300]"
                />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12">
                <v-select
                  v-model="selectedSavedSourcePath"
                  :items="savedSourceItems"
                  item-title="title"
                  item-value="value"
                  label="Fuentes guardadas en engine (seleccionable)"
                  density="comfortable"
                  clearable
                  :loading="loadingSavedSources"
                  @update:model-value="applySavedSourceSelection"
                />
              </v-col>
            </v-row>

            <div class="source-library mb-3">
              <div class="d-flex align-center justify-space-between flex-wrap ga-2 mb-2">
                <div class="text-caption text-medium-emphasis">
                  Datasets guardados del proyecto (subidos + generados)
                </div>
                <v-btn
                  class="sb-btn"
                  size="small"
                  variant="text"
                  prepend-icon="mdi-refresh"
                  :loading="loadingSavedSources"
                  @click="loadSavedSources"
                >
                  Refrescar
                </v-btn>
              </div>

              <div v-if="!savedSources.length" class="text-caption text-medium-emphasis">
                No hay datasets guardados todavía.
              </div>

              <div v-else class="source-library-list">
                <div
                  v-for="source in savedSources"
                  :key="source.sourcePath"
                  class="source-library-item"
                  :class="{ active: selectedSavedSourcePath === source.sourcePath }"
                >
                  <div class="source-library-main">
                    <div class="source-library-title">{{ source.fileName || 'dataset' }}</div>
                    <div class="source-library-meta">
                      {{ String(source.origin || 'fuente') }} · {{ formatBytes(source.sizeBytes) }} · {{ source.updatedAt ? formatDate(source.updatedAt) : 'sin fecha' }}
                    </div>
                    <div class="source-library-path">{{ source.sourcePath }}</div>
                  </div>
                  <div class="source-library-actions">
                    <v-btn
                      class="sb-btn"
                      size="small"
                      variant="tonal"
                      prepend-icon="mdi-check-circle-outline"
                      @click="applySavedSourceSelection(source.sourcePath)"
                    >
                      Usar
                    </v-btn>
                    <v-checkbox
                      v-model="mergeForm.sourcePaths"
                      :value="source.sourcePath"
                      density="compact"
                      hide-details
                      class="source-merge-checkbox"
                    />
                  </div>
                </div>
              </div>

              <div class="source-merge-bar mt-3">
                <v-text-field
                  v-model="mergeForm.datasetName"
                  label="Nombre unificación (opcional)"
                  density="comfortable"
                  placeholder="chat-es-combinado"
                  hide-details
                />
                <v-checkbox
                  v-model="mergeForm.deduplicate"
                  hide-details
                  density="compact"
                  label="Quitar duplicados"
                />
                <v-btn
                  class="sb-btn"
                  variant="tonal"
                  prepend-icon="mdi-source-merge"
                  :loading="mergingSources"
                  :disabled="!canMergeSources"
                  @click="mergeSelectedSources"
                >
                  Unificar seleccionados ({{ mergeForm.sourcePaths.length }})
                </v-btn>
              </div>
              <div class="text-caption text-medium-emphasis mt-1">
                Selecciona 2 o más datasets y crea una fuente combinada para usarla en Build Dataset.
              </div>
            </div>

            <v-alert
              v-if="mergeMessage"
              class="mb-2"
              type="success"
              variant="tonal"
              density="comfortable"
              :text="mergeMessage"
            />
            <v-alert
              v-if="mergeError"
              class="mb-2"
              type="error"
              variant="tonal"
              density="comfortable"
              :text="mergeError"
            />

            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="datasetForm.splitTrainPct"
                  type="number"
                  label="Train %"
                  density="comfortable"
                  min="0"
                  max="100"
                  step="1"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="datasetForm.splitValPct"
                  type="number"
                  label="Validation %"
                  density="comfortable"
                  min="0"
                  max="100"
                  step="1"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="datasetForm.splitTestPct"
                  type="number"
                  label="Test %"
                  density="comfortable"
                  min="0"
                  max="100"
                  step="1"
                />
              </v-col>
            </v-row>

            <v-alert
              v-if="splitTotal !== 100"
              class="mb-2"
              type="warning"
              variant="tonal"
              density="comfortable"
              :text="`El split debe sumar 100%. Actual: ${splitTotal}%`"
            />

            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="datasetForm.minRecords"
                  type="number"
                  label="Mínimo de registros"
                  density="comfortable"
                  min="1"
                  step="10"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="datasetForm.maxNullPct"
                  type="number"
                  label="% máximo nulos"
                  density="comfortable"
                  min="0"
                  max="100"
                  step="1"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model="datasetForm.tagsCsv"
                  label="Tags (csv)"
                  density="comfortable"
                  placeholder="baseline, v1, demo"
                  :rules="[rules.max200]"
                />
              </v-col>
            </v-row>

            <div class="toggle-grid mb-2">
              <v-checkbox v-model="datasetForm.deduplicate" hide-details density="compact" label="Deduplicar" />
              <v-checkbox v-model="datasetForm.normalizeText" hide-details density="compact" label="Normalizar texto" />
              <v-checkbox v-model="datasetForm.dropNullTarget" hide-details density="compact" label="Excluir target nulo" />
            </div>

            <v-textarea
              v-model="datasetForm.sampleRowsJson"
              label="Muestra opcional de registros (JSON array)"
              auto-grow
              rows="4"
              density="comfortable"
              :error-messages="sampleJsonError ? [sampleJsonError] : []"
              placeholder='[{"input":"texto 1","target":"json esperado"}]'
            />

            <div class="d-flex align-center ga-2 mt-3 flex-wrap">
              <v-btn
                class="sb-btn primary"
                prepend-icon="mdi-play"
                :loading="running"
                :disabled="!canRun"
                @click="triggerDatasetBuild"
              >
                Ejecutar Build Dataset
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
            Últimos runs de dataset
            <v-btn icon variant="text" :disabled="loadingRuns" @click="loadRuns">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loadingRuns" class="sb-skeleton" style="height: 120px;"></div>
            <div v-else-if="!datasetRuns.length" class="text-caption text-medium-emphasis">No hay corridas de dataset.</div>
            <div v-else class="run-list">
              <div v-for="run in datasetRuns" :key="run.Id || run.id" class="run-item">
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
          <v-card-title class="d-flex align-center justify-space-between">
            Contrato activo del template
            <v-chip v-if="selectedTemplateName" size="x-small" variant="tonal">{{ selectedTemplateName }}</v-chip>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <template v-if="templateContract">
              <div class="contract-item">
                <div class="contract-title">Objetivo</div>
                <div class="contract-body">{{ templateContract.objective || '—' }}</div>
              </div>
              <div class="contract-item">
                <div class="contract-title">Campos esperados</div>
                <div class="contract-body">
                  <v-chip
                    v-for="field in outputSchemaFields"
                    :key="field"
                    class="mr-1 mb-1"
                    size="x-small"
                    variant="tonal"
                    color="primary"
                  >
                    {{ field }}
                  </v-chip>
                  <span v-if="!outputSchemaFields.length" class="text-caption text-medium-emphasis">No definidos.</span>
                </div>
              </div>
              <div class="contract-item">
                <div class="contract-title">Reglas requeridas</div>
                <div class="contract-body">
                  <v-chip
                    v-for="field in requiredFields"
                    :key="field"
                    class="mr-1 mb-1"
                    size="x-small"
                    variant="tonal"
                    color="teal"
                  >
                    {{ field }}
                  </v-chip>
                  <span v-if="!requiredFields.length" class="text-caption text-medium-emphasis">Sin campos obligatorios definidos.</span>
                </div>
              </div>
            </template>
            <div v-else class="text-caption text-medium-emphasis">
              El template no tiene contrato explícito o el JSON de pipeline no es válido.
            </div>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title>Checklist de etapa</v-card-title>
          <v-divider />
          <v-card-text>
            <ul class="hint-list">
              <li :class="{ ok: !!selectedProjectId }">Proyecto seleccionado</li>
              <li :class="{ ok: !!datasetForm.sourcePath.trim() }">Fuente de datos definida</li>
              <li :class="{ ok: splitTotal === 100 }">Split train/val/test correcto</li>
              <li :class="{ ok: !sampleJsonError }">JSON de muestra válido</li>
              <li :class="{ ok: canRunStage }">Etapa habilitada por workflow</li>
            </ul>
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
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import runtimeApi from '../../api/runtime.service'
import OptionGuide from '../../components/Workflow/OptionGuide.vue'
import StageAssistant from '../../components/Workflow/StageAssistant.vue'

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

const datasetForm = reactive({
  datasetVersion: defaultDatasetVersion(),
  sourceType: 'csv',
  sourcePath: '',
  splitTrainPct: 80,
  splitValPct: 10,
  splitTestPct: 10,
  minRecords: 200,
  maxNullPct: 5,
  deduplicate: true,
  normalizeText: true,
  dropNullTarget: true,
  tagsCsv: 'baseline,v1',
  sampleRowsJson: ''
})
const wizardFile = ref(null)
const wizardDragActive = ref(false)
const wizardError = ref('')
const wizardRows = ref([])
const wizardColumns = ref([])
const wizardMappings = reactive({})
const wizardPreviewVisible = ref(false)
const uploadingDatasetFile = ref(false)
const uploadedDataset = ref(null)
const wizardNotice = ref('')
const loadingSavedSources = ref(false)
const savedSources = ref([])
const selectedSavedSourcePath = ref(null)
const mergingSources = ref(false)
const mergeMessage = ref('')
const mergeError = ref('')
const mergeForm = reactive({
  sourcePaths: [],
  datasetName: '',
  deduplicate: true
})
const generatingDatasetByTopics = ref(false)
const generatorMessage = ref('')
const generatorError = ref('')
const generatorProgressPct = ref(0)
const generatorElapsedMs = ref(0)
const generatorEstimatedMs = ref(0)
const generatorProgressStartMs = ref(0)
let generatorProgressTimer = null
const generatorForm = reactive({
  topicsText: '',
  datasetName: '',
  maxWikipediaResults: 8,
  maxExpandedQueries: 24,
  chunkSize: 1500,
  chunkOverlap: 200,
  sleepSeconds: 0.5,
  resetTopicFolders: true
})
const MAX_WIZARD_PREVIEW_BYTES = 2 * 1024 * 1024
const MAX_WIZARD_PREVIEW_ROWS = 2000

const rules = {
  required: v => !!String(v ?? '').trim() || 'Campo requerido',
  max30: v => String(v ?? '').length <= 30 || 'Máximo 30 caracteres',
  max200: v => String(v ?? '').length <= 200 || 'Máximo 200 caracteres',
  max300: v => String(v ?? '').length <= 300 || 'Máximo 300 caracteres'
}

const optionGuideItems = [
  { label: 'Generador por tópicos', description: 'Genera CSVs desde Wikipedia por tema y los guarda automáticamente en el engine.' },
  { label: 'Fuentes guardadas', description: 'Permite reusar datasets ya subidos o generados sin volver a cargarlos manualmente.' },
  { label: 'Unificar datasets', description: 'Selecciona 2+ fuentes guardadas para combinarlas en un único dataset reutilizable.' },
  { label: 'Versión dataset', description: 'Etiqueta reproducible del dataset para auditoría y comparación de corridas.' },
  { label: 'Fuente / Path', description: 'Indica de dónde se importan los datos crudos para construir el dataset.' },
  { label: 'Split train/validation/test', description: 'Define la partición del dataset para entrenar y evaluar.' },
  { label: 'Deduplicar / Normalizar / Drop null', description: 'Aplica limpieza automática antes de generar el dataset final.' },
  { label: 'Mínimo registros / % nulos', description: 'Quality gates para evitar entrenar con datos insuficientes o sucios.' },
  { label: 'Sample rows (JSON)', description: 'Muestra ejemplos de registros para validar mapeo y contrato antes de correr.' }
]

const sourceTypeOptions = [
  { title: 'CSV tabular', value: 'csv' },
  { title: 'JSONL (registro por línea)', value: 'jsonl' },
  { title: 'Transcripciones', value: 'transcripts' },
  { title: 'Manifest audio', value: 'audio_manifest' }
]

const savedSourceItems = computed(() => savedSources.value.map(source => {
  const origin = String(source?.origin || '').toLowerCase()
  const originLabel = origin === 'generated'
    ? 'generado'
    : (origin === 'upload' ? 'upload' : (origin || 'fuente'))
  const fileName = String(source?.fileName || source?.sourcePath || 'dataset')
  const sizeBytes = Number(source?.sizeBytes || 0)
  const updatedAt = source?.updatedAt ? formatDate(source.updatedAt) : 'sin fecha'
  const active = Boolean(source?.isActive)
  const title = `${active ? '★ ' : ''}${fileName} · ${originLabel} · ${formatBytes(sizeBytes)} · ${updatedAt}`
  return {
    title,
    value: source?.sourcePath || '',
  }
}))

const canGenerateByTopics = computed(() =>
  Boolean(selectedProjectId.value) &&
  parseTopicsInput(generatorForm.topicsText).length > 0 &&
  !generatingDatasetByTopics.value &&
  !uploadingDatasetFile.value
)
const canMergeSources = computed(() =>
  Boolean(selectedProjectId.value) &&
  Array.isArray(mergeForm.sourcePaths) &&
  mergeForm.sourcePaths.length >= 2 &&
  !mergingSources.value
)
const generatorElapsedLabel = computed(() => formatDuration(generatorElapsedMs.value))
const generatorEstimateLabel = computed(() => formatDuration(generatorEstimatedMs.value))

function defaultDatasetVersion() {
  const now = new Date()
  const mm = String(now.getMonth() + 1).padStart(2, '0')
  const dd = String(now.getDate()).padStart(2, '0')
  return `v${now.getFullYear()}${mm}${dd}`
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
    'Id', 'Name', 'TemplateId', 'Templateid', 'Pipelinejson',
    'Status', 'RunType', 'ProgressPct', 'CreatedAt', 'UpdatedAt'
  ].forEach(ensure)
  return copy
}

function errorText(err, fallback = 'Error de comunicación.') {
  const status = Number(err?.response?.status || 0)
  if (status === 401) {
    return 'Sesión expirada o inválida. Inicia sesión nuevamente.'
  }
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

function normalizeText(value) {
  return String(value || '')
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]/g, '')
}

function formatBytes(value) {
  const bytes = Number(value || 0)
  if (!Number.isFinite(bytes) || bytes <= 0) return '0 B'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`
}

function formatDuration(ms) {
  const safeMs = Math.max(0, Number(ms || 0))
  const totalSeconds = Math.round(safeMs / 1000)
  if (totalSeconds < 60) return `${totalSeconds}s`
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${minutes}m ${String(seconds).padStart(2, '0')}s`
}

function estimateGeneratorDurationMs(topicCount) {
  const topics = Math.max(1, Number(topicCount || 1))
  const maxExpandedQueries = Math.max(1, Number(generatorForm.maxExpandedQueries || 1))
  const maxWikipediaResults = Math.max(1, Number(generatorForm.maxWikipediaResults || 1))
  const sleepMs = Math.max(0, Number(generatorForm.sleepSeconds || 0) * 1000)

  const perQueryMs = 220 + sleepMs + Math.min(900, maxWikipediaResults * 18)
  const rawMs = (topics * maxExpandedQueries * perQueryMs) + (topics * 1200) + 2500
  return Math.max(4000, Math.min(20 * 60 * 1000, Math.round(rawMs)))
}

function startGeneratorProgress(topicCount) {
  stopGeneratorProgress()
  generatorProgressStartMs.value = Date.now()
  generatorElapsedMs.value = 0
  generatorEstimatedMs.value = estimateGeneratorDurationMs(topicCount)
  generatorProgressPct.value = 1

  generatorProgressTimer = setInterval(() => {
    if (!generatorProgressStartMs.value) return
    const elapsedMs = Date.now() - generatorProgressStartMs.value
    generatorElapsedMs.value = elapsedMs
    const estimatedMs = Math.max(1, generatorEstimatedMs.value)
    const rawPct = Math.round((elapsedMs / estimatedMs) * 100)

    if (rawPct <= 95) {
      generatorProgressPct.value = Math.max(1, rawPct)
      return
    }

    const overflowMs = Math.max(0, elapsedMs - estimatedMs)
    const extra = Math.min(4, Math.floor(overflowMs / 4000) + 1)
    generatorProgressPct.value = Math.min(99, 95 + extra)
  }, 250)
}

function stopGeneratorProgress(options = {}) {
  const { completed = false, reset = false } = options
  if (generatorProgressTimer) {
    clearInterval(generatorProgressTimer)
    generatorProgressTimer = null
  }

  if (generatorProgressStartMs.value) {
    generatorElapsedMs.value = Date.now() - generatorProgressStartMs.value
  }

  if (completed) {
    generatorProgressPct.value = 100
    return
  }

  if (reset) {
    generatorProgressPct.value = 0
    generatorElapsedMs.value = 0
    generatorEstimatedMs.value = 0
    generatorProgressStartMs.value = 0
  }
}

function parseTopicsInput(value) {
  return String(value || '')
    .split(/[\n,;]+/)
    .map(item => item.trim())
    .filter(Boolean)
    .filter((item, index, arr) => arr.findIndex(x => x.toLowerCase() === item.toLowerCase()) === index)
}

function parseCsvRow(line) {
  const output = []
  let current = ''
  let inQuotes = false
  for (let i = 0; i < line.length; i += 1) {
    const char = line[i]
    if (char === '"') {
      if (inQuotes && line[i + 1] === '"') {
        current += '"'
        i += 1
      } else {
        inQuotes = !inQuotes
      }
      continue
    }
    if (char === ',' && !inQuotes) {
      output.push(current)
      current = ''
      continue
    }
    current += char
  }
  output.push(current)
  return output.map(item => item.trim())
}

function parseDatasetText(text, fileName = '', maxRows = MAX_WIZARD_PREVIEW_ROWS) {
  const trimmed = String(text || '').trim()
  if (!trimmed) return []

  const lowerName = String(fileName || '').toLowerCase()
  const isCsvFile = lowerName.endsWith('.csv')
  const isJsonlFile = lowerName.endsWith('.jsonl')
  const isJsonFile = lowerName.endsWith('.json')
  const looksJson = isJsonFile || trimmed.startsWith('[') || trimmed.startsWith('{')
  const looksJsonl = isJsonlFile

  if (!isCsvFile && looksJson && !looksJsonl) {
    const parsed = parseJson(trimmed, [])
    if (!parsed.ok) throw new Error('JSON inválido.')
    if (Array.isArray(parsed.value)) return parsed.value.slice(0, maxRows)
    if (parsed.value && typeof parsed.value === 'object') return [parsed.value]
    return []
  }

  if (!isCsvFile && looksJsonl) {
    return trimmed
      .split(/\r?\n/)
      .map(line => line.trim())
      .filter(Boolean)
      .slice(0, maxRows)
      .map((line, index) => {
        const parsed = parseJson(line, null)
        if (!parsed.ok || typeof parsed.value !== 'object' || Array.isArray(parsed.value)) {
          throw new Error(`JSONL inválido en línea ${index + 1}.`)
        }
        return parsed.value
      })
  }

  const lines = trimmed.split(/\r?\n/).filter(line => line.trim().length > 0)
  if (!lines.length) return []
  const headers = parseCsvRow(lines[0])
  return lines.slice(1, maxRows + 1).map(line => {
    const values = parseCsvRow(line)
    const row = {}
    headers.forEach((header, idx) => {
      row[header || `col_${idx + 1}`] = values[idx] ?? ''
    })
    return row
  })
}

function detectSourceTypeByName(name = '') {
  const lower = String(name || '').toLowerCase()
  if (lower.endsWith('.jsonl')) return 'jsonl'
  if (lower.endsWith('.json')) return 'jsonl'
  return 'csv'
}

async function onDatasetFilePicked(fileValue) {
  const file = Array.isArray(fileValue) ? fileValue[0] : fileValue
  if (!file) {
    wizardRows.value = []
    wizardColumns.value = []
    wizardError.value = ''
    wizardNotice.value = ''
    uploadedDataset.value = null
    return
  }

  if (!selectedProjectId.value) {
    wizardError.value = 'Selecciona un proyecto antes de cargar el dataset.'
    return
  }

  try {
    wizardError.value = ''
    wizardNotice.value = ''
    uploadingDatasetFile.value = true

    const detectedSourceType = detectSourceTypeByName(file.name)
    const uploadResponse = await runtimeApi.uploadProjectDatasetFile(
      selectedProjectId.value,
      file,
      detectedSourceType
    )
    const uploaded = uploadResponse?.data || {}
    const sourcePath = uploaded.sourcePath || uploaded.SourcePath || ''
    const sourceType = uploaded.sourceType || uploaded.SourceType || detectedSourceType
    const fileName = uploaded.fileName || uploaded.FileName || file.name
    const sizeBytes = Number(uploaded.sizeBytes || uploaded.SizeBytes || file.size || 0)
    const storedPath = uploaded.storedPath || uploaded.StoredPath || ''

    datasetForm.sourceType = String(sourceType || detectedSourceType)
    datasetForm.sourcePath = String(sourcePath || '').trim() || `local://${file.name}`
    uploadedDataset.value = {
      fileName,
      sizeBytes,
      storedPath,
      sourcePath: datasetForm.sourcePath
    }
    selectedSavedSourcePath.value = datasetForm.sourcePath
    await loadSavedSources()

    try {
      const previewText = await file.slice(0, Math.min(file.size, MAX_WIZARD_PREVIEW_BYTES)).text()
      const rows = parseDatasetText(previewText, file.name, MAX_WIZARD_PREVIEW_ROWS)
      wizardRows.value = rows
      const first = rows.find(item => item && typeof item === 'object') || {}
      wizardColumns.value = Object.keys(first)
      if (rows.length) autoMapColumns()

      if (file.size > MAX_WIZARD_PREVIEW_BYTES) {
        wizardNotice.value = 'Archivo grande: el wizard muestra solo una vista previa parcial para mantener fluidez.'
      } else if (!rows.length) {
        wizardNotice.value = 'Archivo cargado al motor. No se pudo generar vista previa, pero ya puedes ejecutar Build Dataset.'
      }
    } catch {
      wizardRows.value = []
      wizardColumns.value = []
      wizardNotice.value = 'Archivo cargado al motor. El preview no se pudo inferir automáticamente; puedes ejecutar Build Dataset igual.'
    }
  } catch (err) {
    wizardRows.value = []
    wizardColumns.value = []
    wizardError.value = errorText(err, 'No se pudo leer o subir el archivo.')
    uploadedDataset.value = null
  } finally {
    wizardDragActive.value = false
    uploadingDatasetFile.value = false
  }
}

async function onWizardDrop(event) {
  const file = event?.dataTransfer?.files?.[0] || null
  if (!file) return
  wizardFile.value = file
  await onDatasetFilePicked(file)
}

async function loadSavedSources() {
  if (!selectedProjectId.value) {
    savedSources.value = []
    selectedSavedSourcePath.value = null
    return
  }

  loadingSavedSources.value = true
  try {
    const response = await runtimeApi.listProjectDatasetSources(selectedProjectId.value)
    const items = Array.isArray(response?.data?.sources) ? response.data.sources : []
    savedSources.value = items
    const validPaths = new Set(items.map(item => String(item?.sourcePath || '')))
    mergeForm.sourcePaths = (Array.isArray(mergeForm.sourcePaths) ? mergeForm.sourcePaths : [])
      .map(item => String(item || '').trim())
      .filter(item => validPaths.has(item))

    const activePath = String(response?.data?.activeSourcePath || '').trim()
    if (activePath) {
      selectedSavedSourcePath.value = activePath
    } else if (datasetForm.sourcePath && items.some(item => item?.sourcePath === datasetForm.sourcePath)) {
      selectedSavedSourcePath.value = datasetForm.sourcePath
    } else {
      selectedSavedSourcePath.value = null
    }
  } catch {
    savedSources.value = []
    selectedSavedSourcePath.value = null
    mergeForm.sourcePaths = []
  } finally {
    loadingSavedSources.value = false
  }
}

function applySavedSourceSelection(sourcePath) {
  const value = String(sourcePath || '').trim()
  if (!value) return
  const selected = savedSources.value.find(item => String(item?.sourcePath || '') === value)
  if (!selected) return

  datasetForm.sourcePath = value
  datasetForm.sourceType = String(selected.sourceType || datasetForm.sourceType || 'csv')
  uploadedDataset.value = {
    fileName: selected.fileName || 'dataset',
    sizeBytes: Number(selected.sizeBytes || 0),
    storedPath: '',
    sourcePath: value
  }
}

async function mergeSelectedSources() {
  if (!selectedProjectId.value) {
    mergeError.value = 'Selecciona un proyecto antes de unificar datasets.'
    return
  }

  const sourcePaths = Array.isArray(mergeForm.sourcePaths)
    ? mergeForm.sourcePaths.map(item => String(item || '').trim()).filter(Boolean)
    : []

  if (sourcePaths.length < 2) {
    mergeError.value = 'Selecciona al menos 2 datasets para unificar.'
    return
  }

  mergingSources.value = true
  mergeError.value = ''
  mergeMessage.value = ''

  try {
    const payload = {
      sourcePaths,
      datasetName: String(mergeForm.datasetName || '').trim() || null,
      deduplicate: Boolean(mergeForm.deduplicate)
    }
    const response = await runtimeApi.mergeProjectDatasets(selectedProjectId.value, payload)
    const merged = response?.data || {}
    const sourcePath = String(merged.sourcePath || '').trim()
    if (!sourcePath) throw new Error('El engine no devolvió sourcePath para el dataset unificado.')

    datasetForm.sourcePath = sourcePath
    datasetForm.sourceType = String(merged.sourceType || 'csv')
    selectedSavedSourcePath.value = sourcePath
    uploadedDataset.value = {
      fileName: merged.fileName || merged.storedFileName || 'dataset-merged.csv',
      sizeBytes: Number(merged.sizeBytes || 0),
      storedPath: merged.storedPath || '',
      sourcePath
    }
    mergeMessage.value = `Dataset unificado: ${uploadedDataset.value.fileName} (${merged.records ?? 0} registros).`
    mergeForm.sourcePaths = []
    await loadSavedSources()
  } catch (err) {
    mergeError.value = errorText(err, 'No se pudieron unificar los datasets seleccionados.')
  } finally {
    mergingSources.value = false
  }
}

async function generateDatasetByTopics() {
  if (!selectedProjectId.value) {
    generatorError.value = 'Selecciona un proyecto antes de generar dataset.'
    return
  }

  const topics = parseTopicsInput(generatorForm.topicsText)
  if (!topics.length) {
    generatorError.value = 'Ingresa al menos un tópico.'
    return
  }

  generatingDatasetByTopics.value = true
  generatorError.value = ''
  generatorMessage.value = ''
  wizardError.value = ''
  startGeneratorProgress(topics.length)
  let completed = false

  try {
    const payload = {
      topics,
      datasetName: String(generatorForm.datasetName || '').trim() || null,
      maxWikipediaResults: Number(generatorForm.maxWikipediaResults || 0) || null,
      maxExpandedQueries: Number(generatorForm.maxExpandedQueries || 0) || null,
      chunkSize: Number(generatorForm.chunkSize || 0) || null,
      chunkOverlap: Number(generatorForm.chunkOverlap || 0) || null,
      sleepSeconds: Number(generatorForm.sleepSeconds || 0),
      resetTopicFolders: Boolean(generatorForm.resetTopicFolders)
    }

    const response = await runtimeApi.generateProjectDataset(selectedProjectId.value, payload)
    const generated = response?.data || {}
    const sourcePath = String(generated.sourcePath || '').trim()
    const sourceType = String(generated.sourceType || 'csv')
    if (!sourcePath) throw new Error('El engine no devolvió sourcePath para el dataset generado.')

    datasetForm.sourcePath = sourcePath
    datasetForm.sourceType = sourceType
    selectedSavedSourcePath.value = sourcePath
    wizardRows.value = []
    wizardColumns.value = []
    wizardNotice.value = 'Dataset generado en engine. Si deseas, ejecuta Build Dataset directamente.'
    uploadedDataset.value = {
      fileName: generated.fileName || generated.storedFileName || 'dataset-generado.csv',
      sizeBytes: Number(generated.sizeBytes || 0),
      storedPath: generated.storedPath || '',
      sourcePath
    }

    generatorMessage.value = `Dataset generado: ${uploadedDataset.value.fileName} (${generated.records ?? 0} registros).`
    await loadSavedSources()
    completed = true
  } catch (err) {
    generatorError.value = errorText(err, 'No se pudo generar dataset por tópicos.')
  } finally {
    stopGeneratorProgress({ completed, reset: !completed })
    generatingDatasetByTopics.value = false
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

const selectedTemplateName = computed(() => selectedTemplate.value?.Name || selectedTemplate.value?.name || '')

const templateContract = computed(() => {
  const raw = selectedTemplate.value?.Pipelinejson || selectedTemplate.value?.pipelinejson
  if (!raw) return null
  const parsed = parseJson(raw, null)
  return parsed.ok ? (parsed.value?.contract || null) : null
})

const outputSchemaFields = computed(() => {
  const schema = templateContract.value?.outputSchema
  return schema && typeof schema === 'object' && !Array.isArray(schema)
    ? Object.keys(schema)
    : []
})

const requiredFields = computed(() => {
  const required = templateContract.value?.validationRules?.required
  return Array.isArray(required) ? required : []
})

const wizardQuality = computed(() => {
  const rows = wizardRows.value
  if (!rows.length) return { duplicatePct: 0, duplicated: 0 }

  const seen = new Set()
  let duplicated = 0
  for (const row of rows) {
    const key = JSON.stringify(row)
    if (seen.has(key)) duplicated += 1
    else seen.add(key)
  }
  return {
    duplicated,
    duplicatePct: (duplicated / rows.length) * 100
  }
})

function mappedFieldQuality(targetField) {
  const rows = wizardRows.value
  const column = wizardMappings[targetField]
  if (!rows.length || !column) return { nullPct: 100, nullCount: rows.length || 0 }

  let nullCount = 0
  rows.forEach(row => {
    const raw = row?.[column]
    if (raw === null || raw === undefined || String(raw).trim() === '') nullCount += 1
  })

  return {
    nullCount,
    nullPct: rows.length ? (nullCount / rows.length) * 100 : 0
  }
}

const wizardPreviewRows = computed(() => {
  const rows = wizardRows.value.slice(0, 8)
  const targets = outputSchemaFields.value
  if (!targets.length) return rows

  return rows.map(row => {
    const mapped = {}
    targets.forEach(target => {
      const sourceColumn = wizardMappings[target]
      mapped[target] = sourceColumn ? (row?.[sourceColumn] ?? null) : null
    })
    return mapped
  })
})

const wizardPreviewJson = computed(() => JSON.stringify(wizardPreviewRows.value, null, 2))

const splitTotal = computed(() =>
  Number(datasetForm.splitTrainPct || 0) + Number(datasetForm.splitValPct || 0) + Number(datasetForm.splitTestPct || 0)
)

const sampleRowsParsed = computed(() => parseJson(datasetForm.sampleRowsJson, []))
const sampleJsonError = computed(() => (sampleRowsParsed.value.ok ? '' : 'La muestra JSON no es válida'))

const workflowSteps = computed(() => {
  const raw = projectWorkflow.value?.Steps || projectWorkflow.value?.steps
  return Array.isArray(raw) ? raw : []
})

const datasetStep = computed(() => workflowSteps.value.find(step =>
  String(step.RunType || step.runType || '').toLowerCase() === 'dataset_build'
))

const canRunStage = computed(() => {
  if (!datasetStep.value) return false
  const enabled = Boolean(datasetStep.value.Enabled ?? datasetStep.value.enabled)
  const available = Boolean(datasetStep.value.Available ?? datasetStep.value.available)
  const status = String(datasetStep.value.Status || datasetStep.value.status || '').toLowerCase()
  return enabled && (available || status === 'completed')
})

const canRun = computed(() =>
  Boolean(selectedProjectId.value) &&
  Boolean(String(datasetForm.datasetVersion || '').trim()) &&
  Boolean(String(datasetForm.sourcePath || '').trim()) &&
  splitTotal.value === 100 &&
  !sampleJsonError.value &&
  !uploadingDatasetFile.value &&
  canRunStage.value
)

const payloadPreview = computed(() => JSON.stringify(buildRunInputPayload(), null, 2))

const uploadedDatasetMessage = computed(() => {
  if (!uploadedDataset.value) return ''
  const fileName = uploadedDataset.value.fileName || 'archivo'
  const sourcePath = uploadedDataset.value.sourcePath || ''
  const sizeBytes = Number(uploadedDataset.value.sizeBytes || 0)
  const mb = sizeBytes > 0 ? `${(sizeBytes / (1024 * 1024)).toFixed(2)} MB` : 'tamaño desconocido'
  return `Archivo cargado al engine: ${fileName} (${mb}). Fuente activa: ${sourcePath}`
})

const datasetRuns = computed(() => runs.value.filter(run =>
  String(run.RunType || run.runType || '').toLowerCase() === 'dataset_build'
))

function applyAssistantSuggestion(fields) {
  if (!fields || typeof fields !== 'object') return

  const setIfString = key => {
    const value = fields[key]
    if (typeof value === 'string') datasetForm[key] = value
  }
  const setIfNumber = key => {
    const value = Number(fields[key])
    if (Number.isFinite(value)) datasetForm[key] = value
  }
  const setIfBool = key => {
    const value = fields[key]
    if (typeof value === 'boolean') datasetForm[key] = value
  }

  setIfString('datasetVersion')
  setIfString('sourceType')
  setIfString('sourcePath')
  setIfString('tagsCsv')
  setIfString('sampleRowsJson')
  setIfNumber('splitTrainPct')
  setIfNumber('splitValPct')
  setIfNumber('splitTestPct')
  setIfNumber('minRecords')
  setIfNumber('maxNullPct')
  setIfBool('deduplicate')
  setIfBool('normalizeText')
  setIfBool('dropNullTarget')
}

function autoMapColumns() {
  if (!wizardColumns.value.length) return
  const normalizedColumns = wizardColumns.value.map(column => ({
    original: column,
    normalized: normalizeText(column)
  }))

  outputSchemaFields.value.forEach(targetField => {
    const target = normalizeText(targetField)
    const direct = normalizedColumns.find(col => col.normalized === target)
    if (direct) {
      wizardMappings[targetField] = direct.original
      return
    }

    const partial = normalizedColumns.find(col =>
      col.normalized.includes(target) || target.includes(col.normalized)
    )
    wizardMappings[targetField] = partial ? partial.original : null
  })
}

function applyWizardToDatasetForm() {
  if (!wizardRows.value.length) return
  const sample = wizardPreviewRows.value.slice(0, 10)
  datasetForm.sampleRowsJson = JSON.stringify(sample, null, 2)
  if (!datasetForm.sourcePath.trim() && wizardFile.value?.name) {
    datasetForm.sourcePath = `local://${wizardFile.value.name}`
  }
}

function toggleWizardPreview() {
  wizardPreviewVisible.value = !wizardPreviewVisible.value
}

function buildRunInputPayload() {
  const tags = String(datasetForm.tagsCsv || '')
    .split(',')
    .map(item => item.trim())
    .filter(Boolean)

  return {
    datasetVersion: String(datasetForm.datasetVersion || '').trim(),
    source: {
      type: datasetForm.sourceType,
      path: String(datasetForm.sourcePath || '').trim()
    },
    split: {
      trainPct: Number(datasetForm.splitTrainPct || 0),
      valPct: Number(datasetForm.splitValPct || 0),
      testPct: Number(datasetForm.splitTestPct || 0)
    },
    transforms: {
      deduplicate: Boolean(datasetForm.deduplicate),
      normalizeText: Boolean(datasetForm.normalizeText),
      dropNullTarget: Boolean(datasetForm.dropNullTarget)
    },
    qualityGates: {
      minRecords: Number(datasetForm.minRecords || 1),
      maxNullPct: Number(datasetForm.maxNullPct || 0)
    },
    expectedOutputFields: outputSchemaFields.value,
    requiredFields: requiredFields.value,
    tags,
    sampleRows: sampleRowsParsed.value.ok ? sampleRowsParsed.value.value : []
  }
}

async function loadProjects() {
  const response = await runtimeApi.list('projects')
  projects.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
}

async function loadTemplates() {
  const response = await runtimeApi.list('templates')
  templates.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
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
    await Promise.all([loadProjects(), loadTemplates()])
    if (!selectedProjectId.value && projects.value.length) {
      selectedProjectId.value = projects.value[0].Id || projects.value[0].id
    }
    await Promise.all([loadWorkflow(selectedProjectId.value), loadRuns(), loadSavedSources()])
  } catch (err) {
    error.value = errorText(err, 'No se pudo cargar la etapa de dataset.')
  } finally {
    loadingData.value = false
  }
}

async function triggerDatasetBuild() {
  if (!canRun.value) return

  running.value = true
  runError.value = ''
  runMessage.value = ''

  try {
    const payload = buildRunInputPayload()
    await runtimeApi.triggerProjectRun(
      selectedProjectId.value,
      'dataset_build',
      JSON.stringify(payload)
    )
    runMessage.value = 'Build Dataset ejecutado. Revisa la salida en runs.'
    await Promise.all([loadWorkflow(selectedProjectId.value), loadRuns()])
  } catch (err) {
    runError.value = errorText(err, 'No se pudo ejecutar el build de dataset.')
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
  router.push({ path: '/stage/rag', query: { projectId: selectedProjectId.value } })
}

watch(selectedProjectId, async id => {
  if (!id) return
  wizardRows.value = []
  wizardColumns.value = []
  wizardError.value = ''
  wizardNotice.value = ''
  uploadedDataset.value = null
  generatorMessage.value = ''
  generatorError.value = ''
  mergeMessage.value = ''
  mergeError.value = ''
  mergeForm.sourcePaths = []
  stopGeneratorProgress({ reset: true })
  selectedSavedSourcePath.value = null
  await Promise.all([loadWorkflow(id), loadRuns(), loadSavedSources()])
})

watch(() => route.query.projectId, value => {
  if (!value) return
  selectedProjectId.value = Number(value)
}, { immediate: true })

watch(() => datasetForm.sourcePath, value => {
  const path = String(value || '').trim()
  if (!path) return
  if (savedSources.value.some(item => String(item?.sourcePath || '') === path)) {
    selectedSavedSourcePath.value = path
  }
})

watch(outputSchemaFields, () => {
  autoMapColumns()
})

onMounted(loadData)
onBeforeUnmount(() => {
  stopGeneratorProgress({ reset: true })
})
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

.wizard-drop {
  border: 1px dashed rgba(37, 99, 235, 0.45);
  border-radius: 12px;
  padding: 12px;
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--sb-text-soft, var(--sb-muted));
}

.wizard-drop.active {
  background: rgba(37, 99, 235, 0.08);
}

.generator-progress {
  border: 1px solid rgba(120, 140, 170, 0.22);
  border-radius: 10px;
  padding: 10px;
  background: rgba(37, 99, 235, 0.03);
}

.source-library {
  border: 1px solid rgba(120, 140, 170, 0.22);
  border-radius: 12px;
  padding: 10px;
  background: rgba(148, 163, 184, 0.05);
}

.source-library-list {
  display: grid;
  gap: 8px;
  max-height: 260px;
  overflow: auto;
}

.source-library-item {
  border: 1px solid rgba(120, 140, 170, 0.22);
  border-radius: 10px;
  padding: 8px;
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 8px;
  align-items: start;
  background: #fff;
}

.source-library-item.active {
  border-color: rgba(37, 99, 235, 0.45);
  box-shadow: inset 0 0 0 1px rgba(37, 99, 235, 0.22);
}

.source-library-main {
  min-width: 0;
}

.source-library-title {
  font-weight: 600;
  line-height: 1.2;
  word-break: break-word;
}

.source-library-meta {
  font-size: 0.8rem;
  color: var(--sb-text-soft, var(--sb-muted));
  margin-top: 2px;
}

.source-library-path {
  font-size: 0.75rem;
  color: var(--sb-text-soft, var(--sb-muted));
  margin-top: 4px;
  word-break: break-all;
}

.source-library-actions {
  display: flex;
  align-items: center;
  gap: 4px;
}

.source-merge-checkbox {
  margin-inline-start: 0;
}

.source-merge-bar {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto;
  gap: 10px;
  align-items: center;
}

.wizard-mapping-grid {
  display: grid;
  gap: 8px;
}

.wizard-mapping-row {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 10px;
  padding: 8px;
  display: grid;
  grid-template-columns: minmax(0, 1.2fr) minmax(0, 2fr) auto;
  gap: 8px;
  align-items: center;
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

.contract-item + .contract-item {
  margin-top: 10px;
}

.contract-title {
  font-size: 0.78rem;
  color: var(--sb-text-soft, var(--sb-muted));
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.contract-body {
  margin-top: 4px;
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

@media (max-width: 960px) {
  .toggle-grid {
    grid-template-columns: 1fr;
  }

  .wizard-mapping-row {
    grid-template-columns: 1fr;
  }

  .source-library-item {
    grid-template-columns: 1fr;
  }

  .source-library-actions {
    justify-content: space-between;
  }

  .source-merge-bar {
    grid-template-columns: 1fr;
  }
}
</style>
