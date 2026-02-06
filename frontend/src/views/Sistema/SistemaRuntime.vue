<template>
  <v-container fluid>
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="d-flex align-center">
          <v-icon class="mr-2" color="primary" size="28">mdi-database</v-icon>
          <div>
            <h2 class="mb-1">{{ sistema?.name || 'Sistema' }}</h2>
            <span class="grey--text text-body-2">
              /s/{{ slug }}{{ entitySlug ? `/${entitySlug}` : '' }}
            </span>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2">
        <v-btn variant="text" @click="volver">
          <v-icon left>mdi-arrow-left</v-icon>
          Volver
        </v-btn>
        <v-btn color="primary" :disabled="!entidadSeleccionada" @click="nuevoRegistro">
          <v-icon left>mdi-plus</v-icon>
          Nuevo registro
        </v-btn>
      </v-col>
    </v-row>

    <v-row>
      <v-col cols="12" md="3">
        <v-card elevation="2" class="card">
          <v-card-title class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-folder-outline</v-icon>
            <span class="text-h6">Entidades</span>
          </v-card-title>
          <v-divider />
          <v-list density="compact">
            <v-list-item
              v-for="entidad in entidades"
              :key="entidad.id"
              :active="entidadSeleccionada?.id === entidad.id"
              @click="irEntidad(entidad)"
            >
              <v-list-item-title>{{ entidad.displayName || entidad.name }}</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-card>
      </v-col>

      <v-col cols="12" md="9">
        <v-card elevation="2" class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-table</v-icon>
              <span class="text-h6">{{ entidadSeleccionada?.displayName || entidadSeleccionada?.name || 'Registros' }}</span>
            </div>
            <v-btn icon variant="text" @click="cargarDatos" :disabled="!entidadSeleccionada">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>
          <v-divider />

          <v-alert v-if="error" type="error" variant="tonal" class="ma-4">
            {{ error }}
          </v-alert>

          <div v-if="loading" class="pa-4">
            Cargando...
          </div>

          <div v-else-if="!entidadSeleccionada" class="pa-4">
            Selecciona una entidad para ver los registros.
          </div>

          <v-data-table
            v-else
            :headers="headers"
            :items="registros"
            class="table"
            density="compact"
            hover
          >
            <template #item.actions="{ item }">
              <v-tooltip text="Editar">
                <template #activator="{ props }">
                  <v-btn
                    v-bind="props"
                    icon
                    size="small"
                    color="primary"
                    variant="text"
                    @click="editarRegistro(item)"
                  >
                    <v-icon>mdi-pencil</v-icon>
                  </v-btn>
                </template>
              </v-tooltip>

              <v-tooltip text="Eliminar">
                <template #activator="{ props }">
                  <v-btn
                    v-bind="props"
                    icon
                    size="small"
                    color="red"
                    variant="text"
                    @click="eliminarRegistro(item)"
                  >
                    <v-icon>mdi-delete</v-icon>
                  </v-btn>
                </template>
              </v-tooltip>
            </template>
          </v-data-table>
        </v-card>
      </v-col>
    </v-row>

    <RegistroDialog
      v-model="mostrarDialog"
      :record="registroSeleccionado"
      :fields="campos"
      :fk-options="fkOptions"
      :system-id="sistema?.id"
      :entity-id="entidadSeleccionada?.id"
      @guardado="cargarDatos"
    />
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import sistemaService from '../../api/sistema.service.js'
import entidadService from '../../api/entidad.service.js'
import campoService from '../../api/campo.service.js'
import datosService from '../../api/datos.service.js'
import relacionService from '../../api/relacion.service.js'
import RegistroDialog from '../../components/sistemas/RegistroDialog.vue'
import { toKebab } from '../../utils/slug.js'

const route = useRoute()
const router = useRouter()

const sistema = ref(null)
const entidades = ref([])
const entidadSeleccionada = ref(null)
const campos = ref([])
const registros = ref([])
const loading = ref(false)
const error = ref(null)
const relaciones = ref([])
const fkOptions = ref({})

const mostrarDialog = ref(false)
const registroSeleccionado = ref(null)

const slug = computed(() => String(route.params.slug || ''))
const entitySlug = computed(() => String(route.params.entity || ''))

const headers = computed(() => {
  if (!campos.value.length) return []
  const cols = campos.value.map(field => ({
    title: field.name || field.columnName,
    key: field.columnName
  }))
  cols.push({ title: 'Acciones', key: 'actions', sortable: false })
  return cols
})

const pkField = computed(() => campos.value.find(field => field.isPrimaryKey))

async function cargarSistema() {
  const { data } = await sistemaService.getBySlug(slug.value)
  sistema.value = data
}

async function cargarEntidades() {
  if (!sistema.value) return
  const { data } = await entidadService.getBySystem(sistema.value.id)
  entidades.value = data
}

async function cargarRelaciones() {
  if (!sistema.value) return
  const { data } = await relacionService.getBySystem(sistema.value.id)
  relaciones.value = data
}

function resolverEntidad() {
  if (!entidades.value.length) {
    entidadSeleccionada.value = null
    return
  }

  const target = entitySlug.value
    ? entidades.value.find(ent => toKebab(ent.name) === entitySlug.value || toKebab(ent.tableName) === entitySlug.value)
    : entidades.value[0]

  if (!target) {
    entidadSeleccionada.value = entidades.value[0]
    router.replace(`/s/${slug.value}/${toKebab(entidadSeleccionada.value.name)}`)
    return
  }

  entidadSeleccionada.value = target

  if (!entitySlug.value && entidadSeleccionada.value) {
    router.replace(`/s/${slug.value}/${toKebab(entidadSeleccionada.value.name)}`)
  }
}

async function cargarCampos() {
  if (!sistema.value || !entidadSeleccionada.value) return
  const { data } = await campoService.getByEntity(sistema.value.id, entidadSeleccionada.value.id)
  campos.value = data
}

async function cargarDatos() {
  if (!sistema.value || !entidadSeleccionada.value) return
  const { data } = await datosService.listar(sistema.value.id, entidadSeleccionada.value.id)
  registros.value = data
}

function elegirDisplayField(fields, pkField) {
  const byName = fields.find(f => {
    const name = f.columnName?.toLowerCase()
    return name === 'nombre' || name === 'name'
  })
  if (byName) return byName

  const firstString = fields.find(f => f.dataType?.toLowerCase() === 'string')
  if (firstString) return firstString

  return pkField || fields[0]
}

async function cargarFkOptions() {
  fkOptions.value = {}
  if (!sistema.value || !entidadSeleccionada.value) return

  const rels = relaciones.value.filter(r =>
    r.sourceEntityId === entidadSeleccionada.value.id && r.foreignKey
  )

  for (const rel of rels) {
    const target = entidades.value.find(e => e.id === rel.targetEntityId)
    if (!target) continue

    const { data: targetFields } = await campoService.getByEntity(sistema.value.id, target.id)
    if (!targetFields?.length) continue

    const pk = targetFields.find(f => f.isPrimaryKey) || targetFields[0]
    const display = elegirDisplayField(targetFields, pk)

    const { data: rows } = await datosService.listar(sistema.value.id, target.id)
    const options = (rows || []).map(row => ({
      value: row[pk.columnName],
      title: row[display.columnName] ?? row[pk.columnName]
    }))

    fkOptions.value[rel.foreignKey] = {
      options,
      targetEntityId: target.id,
      pkField: pk.columnName,
      displayField: display.columnName
    }
  }
}

async function inicializar() {
  loading.value = true
  error.value = null
  try {
    await cargarSistema()
    await cargarEntidades()
    await cargarRelaciones()
    resolverEntidad()
    await cargarCampos()
    await cargarDatos()
    await cargarFkOptions()
  } catch (err) {
    error.value =
      err?.response?.data?.message ||
      err?.response?.data?.Message ||
      'No se pudo cargar el sistema.'
  } finally {
    loading.value = false
  }
}

function irEntidad(entidad) {
  router.push(`/s/${slug.value}/${toKebab(entidad.name)}`)
}

function nuevoRegistro() {
  registroSeleccionado.value = null
  mostrarDialog.value = true
}

function editarRegistro(item) {
  registroSeleccionado.value = { ...item }
  mostrarDialog.value = true
}

async function eliminarRegistro(item) {
  if (!pkField.value) {
    window.alert('Entidad sin PK, no se puede eliminar.')
    return
  }

  const ok = window.confirm('Eliminar registro?')
  if (!ok) return

  try {
    await datosService.eliminar(sistema.value.id, entidadSeleccionada.value.id, item[pkField.value.columnName])
    await cargarDatos()
  } catch (err) {
    const message =
      err?.response?.data?.message ||
      err?.response?.data?.Message ||
      'No se pudo eliminar.'
    window.alert(message)
  }
}

function volver() {
  router.push('/sistemas')
}

watch(
  () => [slug.value, entitySlug.value],
  () => {
    inicializar()
  }
)

onMounted(() => {
  inicializar()
})
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
</style>
