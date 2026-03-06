<template>
  <v-container fluid class="stage-page">
    <v-card class="card sb-page-header">
      <div class="d-flex align-center justify-space-between flex-wrap ga-3">
        <div class="d-flex align-center">
          <div class="sb-page-icon"><v-icon color="primary" size="24">mdi-folder-cog</v-icon></div>
          <div>
            <h2>Etapa 2 · Proyecto</h2>
            <p class="sb-page-subtitle">Crea el proyecto operativo y asociá su template antes de construir el dataset.</p>
          </div>
        </div>
        <div class="d-flex align-center ga-2 flex-wrap">
          <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-arrow-left" @click="router.push('/stage/template')">Volver</v-btn>
          <v-btn class="sb-btn primary" prepend-icon="mdi-arrow-right" :disabled="!selectedProjectId" @click="goNext">
            Ir a Dataset
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
          <v-card-title class="d-flex align-center justify-space-between">
            <span>{{ editingId ? 'Editar proyecto' : 'Nuevo proyecto' }}</span>
            <v-btn v-if="editingId" class="sb-btn ghost" variant="text" prepend-icon="mdi-refresh" @click="resetForm">
              Nuevo
            </v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="8">
                <v-text-field v-model="form.name" label="Nombre del proyecto" density="comfortable" :rules="[rules.required, rules.max200]" />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field v-model="form.slug" label="Slug" density="comfortable" :rules="[rules.required, rules.max80]" />
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="6">
                <v-select
                  v-model="form.templateId"
                  :items="templateItems"
                  item-title="title"
                  item-value="value"
                  label="Template"
                  :rules="[rules.required]"
                  density="comfortable"
                  :loading="loading"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-select
                  v-model="form.language"
                  :items="languageOptions"
                  label="Idioma"
                  density="comfortable"
                  :rules="[rules.required, rules.max10]"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-select
                  v-model="form.status"
                  :items="statusOptions"
                  label="Estado"
                  density="comfortable"
                  :rules="[rules.required, rules.max30]"
                />
              </v-col>
            </v-row>

            <v-text-field
              v-model="form.tone"
              label="Tono (opcional)"
              density="comfortable"
              placeholder="neutral, formal, técnico..."
              :rules="[rules.max100]"
            />

            <v-textarea
              v-model="form.description"
              label="Descripción"
              rows="3"
              auto-grow
              density="comfortable"
              :rules="[rules.max500]"
            />

            <div class="d-flex align-center ga-2 mt-2 flex-wrap">
              <v-btn
                class="sb-btn primary"
                prepend-icon="mdi-content-save"
                :loading="saving"
                :disabled="!canSave"
                @click="saveProject"
              >
                {{ editingId ? 'Guardar proyecto' : 'Crear proyecto' }}
              </v-btn>
              <v-btn class="sb-btn" variant="tonal" prepend-icon="mdi-arrow-right" :disabled="!selectedProjectId" @click="goNext">
                Continuar a Dataset
              </v-btn>
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="5">
        <StageAssistant
          stage="project"
          class="mb-4"
          :project-id="selectedProjectId"
          @apply="applyAssistantSuggestion"
        />

        <v-card class="card">
          <v-card-title>Proyectos existentes</v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loading" class="sb-skeleton" style="height: 120px;"></div>
            <div v-else-if="!projects.length" class="text-caption text-medium-emphasis">No hay proyectos aún.</div>
            <template v-else>
              <v-select
                v-model="selectedProjectId"
                :items="projectItems"
                item-title="title"
                item-value="value"
                label="Proyecto activo"
                density="comfortable"
              />

              <v-card v-if="selectedProject" class="project-summary" variant="flat">
                <div class="summary-row"><span>Template</span><strong>{{ selectedTemplateName }}</strong></div>
                <div class="summary-row"><span>Idioma</span><strong>{{ selectedProject.Language || selectedProject.language || '-' }}</strong></div>
                <div class="summary-row"><span>Estado</span><v-chip size="small" variant="tonal">{{ selectedProject.Status || selectedProject.status || 'draft' }}</v-chip></div>
                <div class="summary-row"><span>Runs</span><strong>{{ selectedProjectRunsCount }}</strong></div>
              </v-card>

              <div class="d-flex align-center ga-2 mt-3">
                <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-pencil" :disabled="!selectedProject" @click="loadSelectedForEdit">
                  Editar seleccionado
                </v-btn>
                <v-btn class="sb-btn ghost" variant="text" prepend-icon="mdi-refresh" @click="loadData">
                  Recargar
                </v-btn>
              </div>
            </template>
          </v-card-text>
        </v-card>

        <v-card class="card mt-4">
          <v-card-title>Checklist de etapa</v-card-title>
          <v-divider />
          <v-card-text>
            <ul class="hint-list">
              <li :class="{ ok: hasTemplate }">Template seleccionado</li>
              <li :class="{ ok: hasBasicData }">Nombre y slug válidos</li>
              <li :class="{ ok: hasOperationalConfig }">Idioma/estado configurados</li>
              <li :class="{ ok: !!selectedProjectId }">Proyecto activo para continuar</li>
            </ul>
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
const error = ref('')
const projects = ref([])
const templates = ref([])
const selectedProjectId = ref(null)
const selectedProjectRunsCount = ref(0)
const editingId = ref(null)

const form = reactive({
  name: '',
  slug: '',
  description: '',
  language: 'es',
  tone: '',
  status: 'draft',
  templateId: null,
  createdAt: null,
  createdByUserId: null
})

const languageOptions = ['es', 'en', 'pt']
const statusOptions = ['draft', 'data_ready', 'index_ready', 'trained', 'evaluated', 'deployed', 'error']

const rules = {
  required: v => !!String(v ?? '').trim() || 'Campo requerido',
  max10: v => String(v ?? '').length <= 10 || 'Máximo 10 caracteres',
  max30: v => String(v ?? '').length <= 30 || 'Máximo 30 caracteres',
  max80: v => String(v ?? '').length <= 80 || 'Máximo 80 caracteres',
  max100: v => String(v ?? '').length <= 100 || 'Máximo 100 caracteres',
  max200: v => String(v ?? '').length <= 200 || 'Máximo 200 caracteres',
  max500: v => String(v ?? '').length <= 500 || 'Máximo 500 caracteres'
}

const optionGuideItems = [
  { label: 'Nombre y Slug', description: 'Definen la identidad del proyecto. El slug se usa en artefactos y rutas.' },
  { label: 'Template', description: 'Vincula el proyecto con su contrato de entrada/salida y workflow base.' },
  { label: 'Idioma', description: 'Idioma operativo esperado para prompts, datos y validaciones.' },
  { label: 'Estado', description: 'Estado funcional del proyecto (draft, data_ready, trained, deployed, etc.).' },
  { label: 'Tono', description: 'Orientación de estilo para respuestas del modelo (opcional).' }
]

function slugify(value) {
  return String(value || '')
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
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
    'Id', 'Name', 'Slug', 'Description', 'Language', 'Tone', 'Status', 'TemplateId', 'Templateid',
    'Createdat', 'Createdbyuserid'
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

function getUserIdFromToken() {
  try {
    const token = localStorage.getItem('token')
    if (!token) return 1
    const payload = JSON.parse(atob(token.split('.')[1]))
    const value = payload?.usuarioId || payload?.userId || payload?.sub
    const userId = Number(value)
    return Number.isFinite(userId) && userId > 0 ? userId : 1
  } catch {
    return 1
  }
}

const projectItems = computed(() => projects.value.map(project => ({
  title: project.Name || project.name || `Proyecto #${project.Id || project.id}`,
  value: project.Id || project.id
})))

const templateItems = computed(() => templates.value.map(template => ({
  title: `${template.Name || template.name} (v${template.Version || template.version || '1'})`,
  value: template.Id || template.id
})))

const selectedProject = computed(() =>
  projects.value.find(project => String(project.Id || project.id) === String(selectedProjectId.value)) || null
)

const selectedTemplateName = computed(() => {
  const templateId = selectedProject.value?.Templateid || selectedProject.value?.TemplateId
  if (!templateId) return '-'
  const template = templates.value.find(item => String(item.Id || item.id) === String(templateId))
  return template?.Name || template?.name || `Template #${templateId}`
})

const hasTemplate = computed(() => !!form.templateId)
const hasBasicData = computed(() => Boolean(String(form.name).trim()) && Boolean(String(form.slug).trim()))
const hasOperationalConfig = computed(() => Boolean(String(form.language).trim()) && Boolean(String(form.status).trim()))
const canSave = computed(() => hasTemplate.value && hasBasicData.value && hasOperationalConfig.value)

function resetForm() {
  editingId.value = null
  form.name = ''
  form.slug = ''
  form.description = ''
  form.language = 'es'
  form.tone = ''
  form.status = 'draft'
  form.templateId = templateItems.value[0]?.value ?? null
  form.createdAt = null
  form.createdByUserId = null
}

function loadProjectIntoForm(project) {
  const record = normalizeRecord(project)
  editingId.value = record.Id || record.id
  form.name = record.Name || ''
  form.slug = record.Slug || ''
  form.description = record.Description || ''
  form.language = record.Language || 'es'
  form.tone = record.Tone || ''
  form.status = record.Status || 'draft'
  form.templateId = record.Templateid || record.TemplateId || null
  form.createdAt = record.Createdat || record.createdAt || null
  form.createdByUserId = record.Createdbyuserid || record.createdByUserId || null
}

function loadSelectedForEdit() {
  if (!selectedProject.value) return
  loadProjectIntoForm(selectedProject.value)
}

function applyAssistantSuggestion(fields) {
  if (!fields || typeof fields !== 'object') return

  const setIfString = key => {
    const value = fields[key]
    if (typeof value === 'string') form[key] = value
  }

  setIfString('name')
  setIfString('slug')
  setIfString('description')
  setIfString('language')
  setIfString('tone')
  setIfString('status')
}

async function loadProjects() {
  const response = await runtimeApi.list('projects')
  projects.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
}

async function loadTemplates() {
  const response = await runtimeApi.list('templates')
  templates.value = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
}

async function loadRunsCount(projectId) {
  if (!projectId) {
    selectedProjectRunsCount.value = 0
    return
  }
  try {
    const response = await runtimeApi.listProjectRuns(projectId, 100)
    selectedProjectRunsCount.value = Array.isArray(response?.data) ? response.data.length : 0
  } catch {
    selectedProjectRunsCount.value = 0
  }
}

async function loadData() {
  loading.value = true
  error.value = ''
  try {
    await Promise.all([loadTemplates(), loadProjects()])
    if (!selectedProjectId.value && projects.value.length) {
      selectedProjectId.value = projects.value[0].Id || projects.value[0].id
    }
    if (!editingId.value) {
      form.templateId = form.templateId || templateItems.value[0]?.value || null
    }
    await loadRunsCount(selectedProjectId.value)
  } catch (err) {
    error.value = errorText(err, 'No se pudieron cargar proyectos/templates.')
  } finally {
    loading.value = false
  }
}

async function saveProject() {
  if (!canSave.value) return

  saving.value = true
  error.value = ''

  const nowIso = new Date().toISOString()
  const payload = {
    slug: form.slug.trim(),
    name: form.name.trim(),
    description: form.description?.trim() || null,
    language: form.language,
    tone: form.tone?.trim() || null,
    status: form.status,
    isactive: true,
    templateid: Number(form.templateId),
    tenantid: null,
    createdbyuserid: form.createdByUserId || getUserIdFromToken(),
    createdat: form.createdAt || nowIso,
    updatedat: nowIso
  }

  try {
    if (editingId.value) {
      await runtimeApi.update('projects', editingId.value, payload)
      selectedProjectId.value = editingId.value
    } else {
      await runtimeApi.create('projects', payload)
      await loadProjects()
      const created = [...projects.value].reverse().find(item => String(item.Slug || '').toLowerCase() === payload.slug.toLowerCase())
      selectedProjectId.value = created?.Id || created?.id || selectedProjectId.value
    }

    await loadData()
    if (!editingId.value) resetForm()
  } catch (err) {
    error.value = errorText(err, 'No se pudo guardar el proyecto.')
  } finally {
    saving.value = false
  }
}

function goNext() {
  if (!selectedProjectId.value) return
  router.push({ path: '/stage/dataset', query: { projectId: selectedProjectId.value } })
}

watch(() => form.name, value => {
  if (editingId.value) return
  if (!String(form.slug || '').trim()) {
    form.slug = slugify(value)
  }
})

watch(selectedProjectId, async id => {
  await loadRunsCount(id)
})

onMounted(async () => {
  await loadData()
  if (!projects.value.length) {
    resetForm()
  }
})
</script>

<style scoped>
.stage-page {
  padding-bottom: 30px;
}

.project-summary {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 12px;
  padding: 10px;
  background: rgba(37, 99, 235, 0.02);
}

.summary-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 5px 0;
}

.summary-row span {
  color: var(--sb-text-soft, var(--sb-muted));
  font-size: 0.85rem;
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
</style>
