<template>
  <v-container fluid>
    <v-row class="mb-6 align-center">
      <v-col>
        <div class="d-flex align-center">
          <v-icon class="mr-2" color="primary" size="28">
            mdi-apps
          </v-icon>
          <div>
            <h2 class="mb-1">Sistemas</h2>
            <span class="grey--text text-body-2">
              Administracion de sistemas generados
            </span>
          </div>
        </div>
      </v-col>

      <v-col cols="auto">
        <v-btn color="primary" @click="nuevoSistema">
          <v-icon left>mdi-plus</v-icon>
          Nuevo sistema
        </v-btn>
      </v-col>
    </v-row>

    <v-card elevation="2" class="sistemas-card">
      <v-card-title class="d-flex align-center justify-space-between">
        <div class="d-flex align-center">
          <v-icon class="mr-2" color="primary">mdi-apps</v-icon>
          <span class="text-h6 font-weight-medium">Listado de sistemas</span>
        </div>
        <v-chip color="primary" variant="outlined" size="small">
          {{ sistemas.length }} sistemas
        </v-chip>
      </v-card-title>

      <v-divider />

      <v-data-table :headers="headers" :items="sistemas" class="sistemas-table" density="comfortable" hover>
        <template #item.status="{ item }">
          <v-chip size="small" :color="item.status === 'published' ? 'green' : 'grey'">
            {{ item.status }}
          </v-chip>
        </template>

        <template #item.isActive="{ item }">
          <v-chip size="small" :color="item.isActive ? 'green' : 'grey'">
            {{ item.isActive ? 'Activo' : 'Inactivo' }}
          </v-chip>
        </template>

        <template #item.actions="{ item }">
          <v-tooltip text="Editar">
            <template #activator="{ props }">
              <v-btn v-bind="props" icon size="small" color="primary" variant="text" @click="editarSistema(item)">
                <v-icon>mdi-pencil</v-icon>
              </v-btn>
            </template>
          </v-tooltip>

          <v-tooltip text="Disenar">
            <template #activator="{ props }">
              <v-btn v-bind="props" icon size="small" color="secondary" variant="text" @click="disenar(item)">
                <v-icon>mdi-vector-square</v-icon>
              </v-btn>
            </template>
          </v-tooltip>

          <v-tooltip text="Publicar">
            <template #activator="{ props }">
              <v-btn v-bind="props" icon size="small" color="green" variant="text" @click="publicar(item)">
                <v-icon>mdi-rocket-launch</v-icon>
              </v-btn>
            </template>
          </v-tooltip>
        </template>
      </v-data-table>
    </v-card>

    <SistemaDialog v-model="mostrarDialog" :sistema="sistemaSeleccionado" @guardado="cargarSistemas" />
  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import sistemaService from '../../api/sistema.service.js'
import SistemaDialog from '../../components/sistemas/SistemaDialog.vue'
import { useMenuStore } from '../../store/menu.store.js'

const router = useRouter()
const { cargarMenuTree } = useMenuStore()

const sistemas = ref([])
const mostrarDialog = ref(false)
const sistemaSeleccionado = ref(null)

const headers = [
  { title: 'Slug', key: 'slug' },
  { title: 'Nombre', key: 'name' },
  { title: 'Namespace', key: 'namespace' },
  { title: 'Status', key: 'status' },
  { title: 'Version', key: 'version' },
  { title: 'Activo', key: 'isActive' },
  { title: 'Acciones', key: 'actions', sortable: false }
]

async function cargarSistemas() {
  const { data } = await sistemaService.getAll()
  sistemas.value = data
}

function nuevoSistema() {
  sistemaSeleccionado.value = null
  mostrarDialog.value = true
}

function editarSistema(item) {
  sistemaSeleccionado.value = item
  mostrarDialog.value = true
}

function disenar(item) {
  router.push(`/sistemas/${item.id}`)
}

async function publicar(item) {
  const ok = window.confirm(`Publicar sistema ${item.name}?`)
  if (!ok) return

  try {
    await sistemaService.publicar(item.id)
    await cargarSistemas()
    await cargarMenuTree()
  } catch (error) {
    const message =
      error?.response?.data?.message ||
      error?.response?.data?.Message ||
      'Error al publicar el sistema.'
    window.alert(message)
  }
}

onMounted(cargarSistemas)
</script>

<style scoped>
.sistemas-card {
  border-radius: 12px;
}

.sistemas-table :deep(thead th) {
  font-weight: 600;
  text-transform: uppercase;
  font-size: 0.75rem;
  color: #6b7280;
}
</style>
