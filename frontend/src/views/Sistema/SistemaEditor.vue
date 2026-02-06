<template>
  <v-container fluid>
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="d-flex align-center">
          <v-icon class="mr-2" color="primary" size="28">mdi-vector-square</v-icon>
          <div>
            <h2 class="mb-1">Disenador</h2>
            <span class="grey--text text-body-2">
              {{ sistema?.name || 'Sistema' }} ({{ sistema?.slug || '-' }})
            </span>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2">
        <v-btn variant="text" @click="volver">
          <v-icon left>mdi-arrow-left</v-icon>
          Volver
        </v-btn>
        <v-btn color="green" @click="publicarSistema">
          <v-icon left>mdi-rocket-launch</v-icon>
          Publicar
        </v-btn>
      </v-col>
    </v-row>

    <v-row>
      <v-col cols="12" md="6">
        <v-card elevation="2" class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-table</v-icon>
              <span class="text-h6 font-weight-medium">Entidades</span>
            </div>
            <v-btn color="primary" size="small" @click="nuevaEntidad">
              <v-icon left>mdi-plus</v-icon>
              Nueva entidad
            </v-btn>
          </v-card-title>

          <v-divider />

          <v-data-table :headers="headersEntidades" :items="entidades" class="table" density="compact" hover>
            <template #item.isActive="{ item }">
              <v-chip size="small" :color="item.isActive ? 'green' : 'grey'">
                {{ item.isActive ? 'Activo' : 'Inactivo' }}
              </v-chip>
            </template>

            <template #item.actions="{ item }">
              <v-tooltip text="Seleccionar">
                <template #activator="{ props }">
                  <v-btn v-bind="props" icon size="small" color="secondary" variant="text"
                    @click="seleccionarEntidad(item)">
                    <v-icon>mdi-database-search</v-icon>
                  </v-btn>
                </template>
              </v-tooltip>

              <v-tooltip text="Datos">
                <template #activator="{ props }">
                  <v-btn
                    v-bind="props"
                    icon
                    size="small"
                    color="teal"
                    variant="text"
                    @click="verDatos(item)"
                  >
                    <v-icon>mdi-table</v-icon>
                  </v-btn>
                </template>
              </v-tooltip>

              <v-tooltip text="Editar">
                <template #activator="{ props }">
                  <v-btn v-bind="props" icon size="small" color="primary" variant="text"
                    @click="editarEntidad(item)">
                    <v-icon>mdi-pencil</v-icon>
                  </v-btn>
                </template>
              </v-tooltip>
            </template>
          </v-data-table>
        </v-card>
      </v-col>

      <v-col cols="12" md="6">
        <v-card elevation="2" class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-form-textbox</v-icon>
              <span class="text-h6 font-weight-medium">Campos</span>
            </div>
            <v-btn color="primary" size="small" :disabled="!entidadSeleccionada" @click="nuevoCampo">
              <v-icon left>mdi-plus</v-icon>
              Nuevo campo
            </v-btn>
          </v-card-title>

          <v-divider />

          <div v-if="!entidadSeleccionada" class="empty-state">
            Selecciona una entidad para ver sus campos.
          </div>

          <v-data-table v-else :headers="headersCampos" :items="campos" class="table" density="compact" hover>
            <template #item.required="{ item }">
              <v-chip size="small" :color="item.required ? 'green' : 'grey'">
                {{ item.required ? 'Si' : 'No' }}
              </v-chip>
            </template>

            <template #item.isPrimaryKey="{ item }">
              <v-chip size="small" :color="item.isPrimaryKey ? 'primary' : 'grey'">
                {{ item.isPrimaryKey ? 'PK' : '-' }}
              </v-chip>
            </template>

            <template #item.actions="{ item }">
              <v-tooltip text="Editar">
                <template #activator="{ props }">
                  <v-btn v-bind="props" icon size="small" color="primary" variant="text"
                    @click="editarCampo(item)">
                    <v-icon>mdi-pencil</v-icon>
                  </v-btn>
                </template>
              </v-tooltip>
            </template>
          </v-data-table>
        </v-card>
      </v-col>
    </v-row>

    <v-row class="mt-4">
      <v-col cols="12">
        <v-card elevation="2" class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-link-variant</v-icon>
              <span class="text-h6 font-weight-medium">Relaciones</span>
            </div>
            <v-btn color="primary" size="small" @click="nuevaRelacion">
              <v-icon left>mdi-plus</v-icon>
              Nueva relacion
            </v-btn>
          </v-card-title>

          <v-divider />

          <v-data-table :headers="headersRelaciones" :items="relaciones" class="table" density="compact" hover>
            <template #item.sourceEntityId="{ item }">
              {{ entidadNombre(item.sourceEntityId) }}
            </template>

            <template #item.targetEntityId="{ item }">
              {{ entidadNombre(item.targetEntityId) }}
            </template>

            <template #item.cascadeDelete="{ item }">
              <v-chip size="small" :color="item.cascadeDelete ? 'red' : 'grey'">
                {{ item.cascadeDelete ? 'Si' : 'No' }}
              </v-chip>
            </template>

            <template #item.actions="{ item }">
              <v-tooltip text="Editar">
                <template #activator="{ props }">
                  <v-btn v-bind="props" icon size="small" color="primary" variant="text"
                    @click="editarRelacion(item)">
                    <v-icon>mdi-pencil</v-icon>
                  </v-btn>
                </template>
              </v-tooltip>
            </template>
          </v-data-table>
        </v-card>
      </v-col>
    </v-row>

    <EntidadDialog v-model="mostrarEntidadDialog" :entidad="entidadSeleccionadaEdicion" :system-id="systemId"
      @guardado="cargarEntidades" />

    <CampoDialog v-model="mostrarCampoDialog" :campo="campoSeleccionado" :system-id="systemId"
      :entity-id="entidadSeleccionada?.id" @guardado="cargarCampos" />

    <RelacionDialog v-model="mostrarRelacionDialog" :relacion="relacionSeleccionada" :entidades="entidades"
      :system-id="systemId" @guardado="cargarRelaciones" />
  </v-container>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import sistemaService from '../../api/sistema.service.js'
import entidadService from '../../api/entidad.service.js'
import campoService from '../../api/campo.service.js'
import relacionService from '../../api/relacion.service.js'
import EntidadDialog from '../../components/sistemas/EntidadDialog.vue'
import CampoDialog from '../../components/sistemas/CampoDialog.vue'
import RelacionDialog from '../../components/sistemas/RelacionDialog.vue'
import { toKebab } from '../../utils/slug.js'
import { useMenuStore } from '../../store/menu.store.js'

const route = useRoute()
const router = useRouter()
const systemId = Number(route.params.id)
const { cargarMenuTree } = useMenuStore()

const sistema = ref(null)
const entidades = ref([])
const campos = ref([])
const entidadSeleccionada = ref(null)
const relaciones = ref([])

const mostrarEntidadDialog = ref(false)
const entidadSeleccionadaEdicion = ref(null)

const mostrarCampoDialog = ref(false)
const campoSeleccionado = ref(null)

const mostrarRelacionDialog = ref(false)
const relacionSeleccionada = ref(null)

const headersEntidades = [
  { title: 'Nombre', key: 'name' },
  { title: 'TableName', key: 'tableName' },
  { title: 'Activo', key: 'isActive' },
  { title: 'Acciones', key: 'actions', sortable: false }
]

const headersCampos = [
  { title: 'Nombre', key: 'name' },
  { title: 'ColumnName', key: 'columnName' },
  { title: 'DataType', key: 'dataType' },
  { title: 'Required', key: 'required' },
  { title: 'PK', key: 'isPrimaryKey' },
  { title: 'Acciones', key: 'actions', sortable: false }
]

const headersRelaciones = [
  { title: 'Origen', key: 'sourceEntityId' },
  { title: 'FK', key: 'foreignKey' },
  { title: 'Destino', key: 'targetEntityId' },
  { title: 'Tipo', key: 'relationType' },
  { title: 'Cascade', key: 'cascadeDelete' },
  { title: 'Acciones', key: 'actions', sortable: false }
]

async function cargarSistema() {
  const { data } = await sistemaService.getById(systemId)
  sistema.value = data
}

async function cargarEntidades() {
  const { data } = await entidadService.getBySystem(systemId)
  entidades.value = data
  if (entidadSeleccionada.value) {
    const match = data.find(e => e.id === entidadSeleccionada.value.id)
    entidadSeleccionada.value = match ?? null
    if (entidadSeleccionada.value) await cargarCampos()
  }
}

async function cargarCampos() {
  if (!entidadSeleccionada.value) {
    campos.value = []
    return
  }

  const { data } = await campoService.getByEntity(systemId, entidadSeleccionada.value.id)
  campos.value = data
}

async function cargarRelaciones() {
  const { data } = await relacionService.getBySystem(systemId)
  relaciones.value = data
}

function seleccionarEntidad(item) {
  entidadSeleccionada.value = item
  cargarCampos()
}

function nuevaEntidad() {
  entidadSeleccionadaEdicion.value = null
  mostrarEntidadDialog.value = true
}

function editarEntidad(item) {
  entidadSeleccionadaEdicion.value = item
  mostrarEntidadDialog.value = true
}

function verDatos(item) {
  if (!sistema.value?.slug) {
    window.alert('Sistema sin slug.')
    return
  }
  router.push(`/s/${sistema.value.slug}/${toKebab(item.name)}`)
}

function nuevoCampo() {
  campoSeleccionado.value = null
  mostrarCampoDialog.value = true
}

function editarCampo(item) {
  campoSeleccionado.value = item
  mostrarCampoDialog.value = true
}

function nuevaRelacion() {
  relacionSeleccionada.value = null
  mostrarRelacionDialog.value = true
}

function editarRelacion(item) {
  relacionSeleccionada.value = item
  mostrarRelacionDialog.value = true
}

function volver() {
  router.push('/sistemas')
}

async function publicarSistema() {
  const ok = window.confirm('Publicar sistema?')
  if (!ok) return

  try {
    await sistemaService.publicar(systemId)
    await cargarSistema()
    await cargarMenuTree()
  } catch (error) {
    const message =
      error?.response?.data?.message ||
      error?.response?.data?.Message ||
      'Error al publicar el sistema.'
    window.alert(message)
  }
}

onMounted(async () => {
  await cargarSistema()
  await cargarEntidades()
  await cargarRelaciones()
})

function entidadNombre(id) {
  const entidad = entidades.value.find(e => e.id === id)
  return entidad?.displayName || entidad?.name || `#${id}`
}
</script>

<style scoped>
.card {
  border-radius: 12px;
}

.table :deep(thead th) {
  font-weight: 600;
  text-transform: uppercase;
  font-size: 0.7rem;
  color: #6b7280;
}

.empty-state {
  padding: 24px;
  color: #6b7280;
}
</style>
