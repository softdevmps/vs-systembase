<template>
  <v-container fluid class="aibase-page">
    <v-row class="mb-4">
      <v-col cols="12">
        <v-card class="card hero-card">
          <div class="hero-content">
            <div class="hero-text">
              <div class="hero-icon">
                <v-icon color="primary" size="26">mdi-brain</v-icon>
              </div>
              <div>
                <h1>AIBase</h1>
                <p>Proyectos de IA metadata-driven (bootstrap V1).</p>
              </div>
            </div>
            <div class="hero-actions">
              <v-btn class="sb-btn primary" @click="openCreate = true">Nuevo proyecto</v-btn>
            </div>
          </div>
        </v-card>
      </v-col>
    </v-row>

    <v-row dense>
      <v-col cols="12" md="5">
        <v-card class="card h-100">
          <v-card-title class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-shape-plus</v-icon>
            Templates disponibles
          </v-card-title>
          <v-divider />
          <v-card-text>
            <v-alert v-if="templateError" type="warning" variant="tonal" density="compact" class="mb-3">
              {{ templateError }}
            </v-alert>

            <v-list v-if="templates.length" class="py-0" density="comfortable">
              <v-list-item v-for="tpl in templates" :key="tpl.id" class="px-0">
                <template #prepend>
                  <v-icon color="primary">mdi-cube-outline</v-icon>
                </template>
                <v-list-item-title>{{ tpl.name }}</v-list-item-title>
                <v-list-item-subtitle>
                  {{ tpl.key }} · v{{ tpl.version }}
                </v-list-item-subtitle>
              </v-list-item>
            </v-list>

            <div v-else class="text-caption text-medium-emphasis">No hay templates activos.</div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="7">
        <v-card class="card h-100">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-folder-multiple-outline</v-icon>
              Proyectos AIBase
            </div>
            <v-btn icon variant="text" @click="loadAll">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <v-alert v-if="projectError" type="warning" variant="tonal" density="compact" class="mb-3">
              {{ projectError }}
            </v-alert>

            <v-table v-if="projects.length" density="compact">
              <thead>
                <tr>
                  <th>Proyecto</th>
                  <th>Template</th>
                  <th>Estado</th>
                  <th>Idioma</th>
                  <th>Creado</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in projects" :key="item.id">
                  <td>
                    <div class="font-weight-medium">{{ item.name }}</div>
                    <div class="text-caption text-medium-emphasis">{{ item.slug }}</div>
                  </td>
                  <td>{{ item.templateName }}</td>
                  <td>
                    <v-chip size="small" variant="tonal" color="primary">{{ item.status }}</v-chip>
                  </td>
                  <td>{{ item.language }}</td>
                  <td>{{ formatDate(item.createdAt) }}</td>
                </tr>
              </tbody>
            </v-table>
            <div v-else class="text-caption text-medium-emphasis">Todavía no hay proyectos.</div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-dialog v-model="openCreate" max-width="560">
      <v-card>
        <v-card-title>Nuevo proyecto AI</v-card-title>
        <v-divider />
        <v-card-text>
          <v-form @submit.prevent="submitCreate" class="d-grid" style="gap: 12px;">
            <v-text-field v-model="form.slug" label="Slug" placeholder="mi_ia" required />
            <v-text-field v-model="form.name" label="Nombre" required />
            <v-text-field v-model="form.description" label="Descripción" />
            <v-select
              v-model="form.templateId"
              :items="templateOptions"
              item-title="name"
              item-value="id"
              label="Template"
              required
            />
            <v-text-field v-model="form.language" label="Idioma" />
            <v-text-field v-model="form.tone" label="Tono" />
          </v-form>
          <v-alert v-if="createError" type="error" variant="tonal" density="compact" class="mt-2">
            {{ createError }}
          </v-alert>
        </v-card-text>
        <v-divider />
        <v-card-actions class="justify-end">
          <v-btn variant="text" @click="openCreate = false">Cancelar</v-btn>
          <v-btn :loading="creating" color="primary" @click="submitCreate">Crear</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import aibaseService from '../../api/aibase.service'

const templates = ref([])
const projects = ref([])
const templateError = ref('')
const projectError = ref('')
const createError = ref('')
const creating = ref(false)
const openCreate = ref(false)

const form = ref({
  slug: '',
  name: '',
  description: '',
  templateId: null,
  language: 'es',
  tone: ''
})

const templateOptions = computed(() => templates.value.map(t => ({
  id: t.id,
  name: `${t.name} (${t.key})`
})))

function formatDate(value) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleString('es-AR')
}

function resetForm() {
  form.value = {
    slug: '',
    name: '',
    description: '',
    templateId: templates.value[0]?.id ?? null,
    language: 'es',
    tone: ''
  }
}

async function loadTemplates() {
  templateError.value = ''
  try {
    const { data } = await aibaseService.getTemplates()
    templates.value = Array.isArray(data) ? data : []
  } catch {
    templates.value = []
    templateError.value = 'No se pudieron cargar los templates de AIBase.'
  }
}

async function loadProjects() {
  projectError.value = ''
  try {
    const { data } = await aibaseService.getProjects()
    projects.value = Array.isArray(data) ? data : []
  } catch {
    projects.value = []
    projectError.value = 'No se pudieron cargar los proyectos AIBase.'
  }
}

async function loadAll() {
  await Promise.all([loadTemplates(), loadProjects()])
  if (!form.value.templateId && templates.value.length) {
    form.value.templateId = templates.value[0].id
  }
}

async function submitCreate() {
  if (creating.value) return

  createError.value = ''
  creating.value = true
  try {
    await aibaseService.createProject(form.value)
    openCreate.value = false
    resetForm()
    await loadProjects()
  } catch (error) {
    createError.value = error?.response?.data?.message || 'No se pudo crear el proyecto.'
  } finally {
    creating.value = false
  }
}

onMounted(async () => {
  await loadAll()
  resetForm()
})
</script>

<style scoped>
.aibase-page {
  padding-bottom: 32px;
}

.hero-card {
  padding: 20px 24px;
}

.hero-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.hero-text {
  display: flex;
  gap: 14px;
  align-items: center;
}

.hero-text h1 {
  margin: 0;
  font-size: 1.4rem;
}

.hero-text p {
  margin: 4px 0 0;
  color: var(--sb-text-soft, var(--sb-muted));
}

.hero-icon {
  width: 44px;
  height: 44px;
  border-radius: 14px;
  background: var(--sb-primary-soft);
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>
