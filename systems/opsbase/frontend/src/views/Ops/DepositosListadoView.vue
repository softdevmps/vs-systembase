<template>
  <v-container fluid class="depositos-listado-view">
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="head-wrap">
          <div class="head-icon">
            <v-icon size="24" color="primary">mdi-format-list-bulleted-square</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Listado de depósitos</h2>
            <div class="text-body-2 text-medium-emphasis">
              Acceso directo a configuración y contexto de cada depósito sin pasar por el mapa.
            </div>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2">
        <v-btn variant="tonal" color="primary" @click="goTo('/depositos')">
          <v-icon start>mdi-map-outline</v-icon>
          Ver mapa
        </v-btn>
        <v-btn color="primary" @click="goTo('/depositos/nuevo')">
          <v-icon start>mdi-map-marker-plus-outline</v-icon>
          Nuevo depósito
        </v-btn>
        <v-btn variant="text" color="primary" :loading="loading" @click="loadData">
          <v-icon start>mdi-refresh</v-icon>
          Actualizar
        </v-btn>
      </v-col>
    </v-row>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">{{ error }}</v-alert>

    <v-row dense class="mb-4">
      <v-col cols="12" md="5" lg="4">
        <v-text-field
          v-model="search"
          label="Buscar por código o nombre"
          variant="outlined"
          density="comfortable"
          clearable
          prepend-inner-icon="mdi-magnify"
        />
      </v-col>
      <v-col cols="12" md="4" lg="3">
        <v-select
          v-model="selectedRubroId"
          :items="rubroFilterItems"
          item-title="title"
          item-value="value"
          label="Filtrar por rubro"
          variant="outlined"
          density="comfortable"
          :loading="loading"
          clearable
        />
      </v-col>
      <v-col cols="12" md="3" lg="2">
        <v-select
          v-model="selectedOpsStatus"
          :items="opsStatusItems"
          item-title="title"
          item-value="value"
          label="Estado operativo"
          variant="outlined"
          density="comfortable"
          clearable
        />
      </v-col>
      <v-col cols="12" md="3" lg="2">
        <v-select
          v-model="selectedCoordsMode"
          :items="coordsModeItems"
          item-title="title"
          item-value="value"
          label="Coordenadas"
          variant="outlined"
          density="comfortable"
          clearable
        />
      </v-col>
      <v-col cols="12" md="3" lg="1" class="d-flex align-center">
        <v-chip size="small" color="primary" variant="tonal">
          {{ filteredDepositos.length }} resultados
        </v-chip>
      </v-col>
    </v-row>

    <v-row dense class="mb-3">
      <v-col cols="12" sm="6" md="3" v-for="card in kpiCards" :key="card.label">
        <v-card class="kpi-card">
          <div class="kpi-head">
            <span class="kpi-label">{{ card.label }}</span>
            <v-icon :color="card.color || 'primary'" size="18">{{ card.icon }}</v-icon>
          </div>
          <div class="kpi-value">{{ card.value }}</div>
        </v-card>
      </v-col>
    </v-row>

    <v-card class="table-card">
      <v-data-table
        :headers="tableHeaders"
        :items="filteredDepositos"
        :loading="loading"
        :items-per-page="10"
        density="comfortable"
        no-data-text="No hay depósitos para mostrar."
      >
        <template #item.codigo="{ item }">
          <span class="cell-code">{{ item.codigo }}</span>
        </template>

        <template #item.nombre="{ item }">
          <span class="cell-name">{{ item.nombre }}</span>
        </template>

        <template #item.rubro="{ item }">
          <v-chip size="x-small" color="primary" variant="tonal">
            {{ item.rubroNombre || 'Sin rubro' }}
          </v-chip>
        </template>

        <template #item.tipo="{ item }">
          <v-chip size="x-small" variant="tonal">
            {{ item.tipo }}
          </v-chip>
        </template>

        <template #item.coords="{ item }">
          <v-chip size="x-small" :color="item.coordinateMode === 'db' ? 'green' : 'orange'" variant="tonal">
            {{ item.coordinateMode === 'db' ? 'Reales' : 'Sintéticas' }}
          </v-chip>
        </template>

        <template #item.stockDisponible="{ item }">
          {{ formatNumber(item.stockDisponible) }}
        </template>

        <template #item.pendingMovements="{ item }">
          <v-chip size="x-small" :color="item.pendingMovements > 0 ? 'orange' : 'green'" variant="tonal">
            {{ item.pendingMovements }}
          </v-chip>
        </template>

        <template #item.actions="{ item }">
          <div class="list-actions">
            <v-btn size="small" variant="tonal" color="primary" @click="openContext(item.locationId)">
              <v-icon start size="16">mdi-warehouse</v-icon>
              Contexto
            </v-btn>
            <v-btn size="small" variant="text" color="primary" @click="openConfig(item.locationId)">
              <v-icon start size="16">mdi-cog-outline</v-icon>
              Configurar
            </v-btn>
            <v-btn size="small" variant="text" color="primary" @click="goTo('/depositos')">
              <v-icon start size="16">mdi-map-outline</v-icon>
              Mapa
            </v-btn>
          </div>
        </template>
      </v-data-table>
    </v-card>
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import runtimeApi from '../../api/runtime.service'

const router = useRouter()

const loading = ref(false)
const error = ref('')
const response = ref({})
const search = ref('')
const selectedRubroId = ref(null)
const selectedOpsStatus = ref(null)
const selectedCoordsMode = ref(null)

function normalizeKey(value) {
  return String(value || '').trim().toLowerCase().replace(/[^a-z0-9]/g, '')
}

function readField(item, name) {
  if (!item || typeof item !== 'object') return undefined
  if (item[name] !== undefined) return item[name]
  const target = normalizeKey(name)
  const key = Object.keys(item).find(k => normalizeKey(k) === target)
  return key ? item[key] : undefined
}

function toArray(data) {
  return Array.isArray(data) ? data : (Array.isArray(data?.items) ? data.items : [])
}

function toNumber(value) {
  if (value == null || value === '') return null
  const num = Number(String(value).replace(',', '.'))
  return Number.isFinite(num) ? num : null
}

function formatNumber(value) {
  const num = toNumber(value)
  if (num == null) return '0'
  return new Intl.NumberFormat('es-AR', { maximumFractionDigits: 2 }).format(num)
}

function goTo(path) {
  if (!path) return
  router.push(path)
}

function openContext(locationId) {
  if (!locationId) return
  router.push(`/depositos/${locationId}`)
}

function openConfig(locationId) {
  if (!locationId) return
  router.push(`/depositos/${locationId}/editar`)
}

function normalizeDeposito(row) {
  return {
    locationId: toNumber(readField(row, 'LocationId')),
    codigo: readField(row, 'Codigo') || '—',
    nombre: readField(row, 'Nombre') || 'Sin nombre',
    tipo: readField(row, 'Tipo') || 'n/a',
    rubroId: toNumber(readField(row, 'RubroId')),
    rubroNombre: String(readField(row, 'RubroNombre') || '').trim(),
    stockDisponible: toNumber(readField(row, 'StockDisponible')) || 0,
    pendingMovements: toNumber(readField(row, 'PendingMovements')) || 0,
    coordinateMode: (readField(row, 'CoordinateMode') || 'db').toString().toLowerCase()
  }
}

const rubroFilterItems = computed(() => {
  const rubros = toArray(readField(response.value, 'Rubros'))
    .map(row => {
      const id = toNumber(readField(row, 'Id'))
      const codigo = String(readField(row, 'Codigo') || '').trim()
      const nombre = String(readField(row, 'Nombre') || '').trim()
      const depositos = toNumber(readField(row, 'Depositos')) || 0
      return {
        value: id,
        title: `${nombre || codigo || `#${id}`} (${depositos})`,
        nombre: nombre || codigo || `#${id}`
      }
    })
    .filter(item => item.value != null)
    .sort((a, b) => String(a.nombre).localeCompare(String(b.nombre), 'es'))
  return [{ value: null, title: 'Todos los rubros', nombre: 'Todos los rubros' }, ...rubros]
})

const depositos = computed(() => toArray(readField(response.value, 'Depositos'))
  .map(normalizeDeposito)
  .filter(item => item.locationId != null))

const filteredDepositos = computed(() => {
  const query = String(search.value || '').trim().toLowerCase()
  return depositos.value.filter(item => {
    if (selectedCoordsMode.value && item.coordinateMode !== selectedCoordsMode.value) return false
    if (selectedOpsStatus.value === 'pendiente' && !(item.pendingMovements > 0)) return false
    if (selectedOpsStatus.value === 'critico' && !((item.stockDisponible || 0) <= 0)) return false
    if (selectedOpsStatus.value === 'ok' && ((item.pendingMovements > 0) || ((item.stockDisponible || 0) <= 0))) return false
    if (!query) return true
    return String(item.codigo || '').toLowerCase().includes(query) ||
      String(item.nombre || '').toLowerCase().includes(query) ||
      String(item.rubroNombre || '').toLowerCase().includes(query)
  })
})

const opsStatusItems = [
  { value: 'pendiente', title: 'Con pendientes' },
  { value: 'critico', title: 'Stock crítico' },
  { value: 'ok', title: 'Operativo OK' }
]

const coordsModeItems = [
  { value: 'db', title: 'Coordenadas reales' },
  { value: 'synthetic', title: 'Coordenadas sintéticas' }
]

const kpiCards = computed(() => {
  const total = filteredDepositos.value.length
  const pendientes = filteredDepositos.value.reduce((acc, item) => acc + (item.pendingMovements || 0), 0)
  const criticos = filteredDepositos.value.filter(item => (item.stockDisponible || 0) <= 0).length
  const stockDisponible = filteredDepositos.value.reduce((acc, item) => acc + (item.stockDisponible || 0), 0)
  return [
    { label: 'Depósitos', value: total, icon: 'mdi-warehouse' },
    { label: 'Pendientes', value: pendientes, icon: 'mdi-timer-sand', color: 'orange' },
    { label: 'Stock crítico', value: criticos, icon: 'mdi-alert-outline', color: criticos > 0 ? 'red' : 'green' },
    { label: 'Stock disp.', value: formatNumber(stockDisponible), icon: 'mdi-scale-balance', color: 'primary' }
  ]
})

const tableHeaders = [
  { title: 'Código', key: 'codigo' },
  { title: 'Nombre', key: 'nombre' },
  { title: 'Rubro', key: 'rubro', sortable: false },
  { title: 'Tipo', key: 'tipo' },
  { title: 'Coords', key: 'coords', align: 'center', sortable: false },
  { title: 'Stock disp', key: 'stockDisponible', align: 'end' },
  { title: 'Pend.', key: 'pendingMovements', align: 'center' },
  { title: 'Acciones', key: 'actions', sortable: false, align: 'end' }
]

async function loadData() {
  loading.value = true
  error.value = ''
  try {
    const rubroId = toNumber(selectedRubroId.value)
    const res = await runtimeApi.getOpsDepositosMapa(rubroId)
    response.value = res?.data || {}
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || (typeof payload === 'string' ? payload : '') || 'No se pudo cargar listado de depósitos.'
  } finally {
    loading.value = false
  }
}

watch(selectedRubroId, () => {
  loadData()
})

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.head-wrap {
  display: flex;
  align-items: center;
  gap: 12px;
}

.head-icon {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  background: var(--sb-primary-soft, rgba(37, 99, 235, 0.12));
  display: flex;
  align-items: center;
  justify-content: center;
}

.kpi-card {
  border-radius: 14px;
  border: 1px solid rgba(100, 116, 139, 0.2);
  padding: 10px 12px;
}

.kpi-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 4px;
}

.kpi-label {
  font-size: 0.78rem;
  color: var(--sb-text-muted, #64748b);
  text-transform: uppercase;
}

.kpi-value {
  font-size: 1.35rem;
  font-weight: 700;
}

.table-card {
  border-radius: 16px;
  border: 1px solid rgba(100, 116, 139, 0.2);
  overflow: hidden;
}

.cell-code {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace;
  font-weight: 700;
  color: var(--sb-text-main, #0f172a);
}

.cell-name {
  font-weight: 600;
  color: var(--sb-text-main, #0f172a);
}

.list-actions {
  display: flex;
  gap: 6px;
  justify-content: flex-end;
  flex-wrap: wrap;
}

.depositos-listado-view :deep(.v-data-table th),
.depositos-listado-view :deep(.v-data-table .v-data-table__th) {
  color: var(--sb-text-main, #334155) !important;
  font-weight: 700;
}

.depositos-listado-view :deep(.v-data-table td),
.depositos-listado-view :deep(.v-data-table .v-data-table__td) {
  color: var(--sb-text-main, #0f172a) !important;
}
</style>
