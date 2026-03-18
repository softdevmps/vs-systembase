<template>
  <v-container fluid class="traza-view">
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="head-wrap">
          <div class="head-icon">
            <v-icon size="24" color="primary">mdi-timeline-clock-outline</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Trazabilidad operativa</h2>
            <div class="text-body-2 text-medium-emphasis">
              Seguimiento por recurso/instancia: ubicación actual, stock y eventos de auditoría.
            </div>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2">
        <v-btn variant="tonal" color="primary" @click="goTo('/kardex')">
          <v-icon start>mdi-notebook-outline</v-icon>
          Kardex
        </v-btn>
        <v-btn variant="text" color="primary" :loading="loadingCatalogs" @click="loadCatalogs">
          <v-icon start>mdi-refresh</v-icon>
          Recargar
        </v-btn>
      </v-col>
    </v-row>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">{{ error }}</v-alert>

    <v-card class="mb-4 panel-card">
      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="8">
            <v-select
              v-model="selectedResourceInstanceId"
              :items="resourceItems"
              item-title="title"
              item-value="value"
              label="Instancia de recurso"
              :loading="loadingCatalogs"
              variant="outlined"
              density="comfortable"
              clearable
            />
          </v-col>
          <v-col cols="12" md="4" class="d-flex align-center">
            <v-btn color="primary" variant="tonal" :disabled="!selectedResourceInstanceId" :loading="loadingTimeline" @click="loadTimeline">
              <v-icon start>mdi-timeline-text-outline</v-icon>
              Cargar trazabilidad
            </v-btn>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-row dense class="mb-4" v-if="selectedResource">
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

    <v-row dense>
      <v-col cols="12" lg="5">
        <v-card class="panel-card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="teal">mdi-warehouse</v-icon>
              Stock por depósito
            </div>
            <v-chip size="small" variant="tonal">{{ stockRows.length }}</v-chip>
          </v-card-title>
          <v-divider />

          <v-data-table
            class="traza-table"
            :headers="stockHeaders"
            :items="stockRows"
            :items-per-page="8"
            density="comfortable"
            no-data-text="Sin stock distribuido para esta instancia."
          >
            <template #item.locationLabel="{ item }">
              <div class="table-name">{{ item.locationLabel }}</div>
              <div class="table-sub">ID {{ item.locationId }}</div>
            </template>
            <template #item.stockDisponible="{ item }">
              <v-chip size="x-small" :color="item.stockDisponible <= 0 ? 'red' : 'green'" variant="tonal">
                {{ formatNumber(item.stockDisponible) }}
              </v-chip>
            </template>
            <template #item.actions="{ item }">
              <v-btn size="x-small" variant="text" color="primary" @click="goTo(`/depositos/${item.locationId}`)">
                Abrir
              </v-btn>
            </template>
          </v-data-table>
        </v-card>
      </v-col>

      <v-col cols="12" lg="7">
        <v-card class="panel-card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="indigo">mdi-timeline-text-outline</v-icon>
              Timeline de auditoría
            </div>
            <v-chip size="small" variant="tonal">{{ timelineRows.length }}</v-chip>
          </v-card-title>
          <v-divider />

          <v-card-text>
            <div v-if="!selectedResourceInstanceId" class="text-caption text-medium-emphasis">
              Selecciona una instancia para cargar el timeline.
            </div>
            <div v-else-if="loadingTimeline" class="text-caption text-medium-emphasis">
              Cargando timeline...
            </div>
            <div v-else-if="timelineRows.length === 0" class="text-caption text-medium-emphasis">
              Sin eventos registrados para esta instancia.
            </div>
            <div v-else class="timeline-list">
              <div v-for="row in timelineRows" :key="row.id" class="timeline-item">
                <v-chip size="x-small" :color="statusColor(row.result)" variant="tonal">{{ row.result || 'evento' }}</v-chip>
                <div class="timeline-body">
                  <strong>{{ row.operationName }}</strong>
                  <div class="text-caption text-medium-emphasis">
                    {{ row.entityName }}#{{ row.entityId ?? '-' }} · {{ row.actor || 'system' }} · {{ formatDate(row.executedAt) }}
                  </div>
                  <div v-if="row.message" class="text-caption">{{ row.message }}</div>
                </div>
              </div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import runtimeApi from '../../api/runtime.service'

const router = useRouter()

const loadingCatalogs = ref(false)
const loadingTimeline = ref(false)
const error = ref('')

const resources = ref([])
const locations = ref([])
const stock = ref([])
const timeline = ref([])

const selectedResourceInstanceId = ref(null)

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

function formatDate(value) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return String(value)
  return date.toLocaleString('es-AR')
}

function pretty(value) {
  return String(value || '')
    .replace(/[_-]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/\b\w/g, c => c.toUpperCase())
}

function goTo(path) {
  if (!path) return
  router.push(path)
}

function statusColor(status) {
  const key = normalizeKey(status)
  if (key === 'ok' || key === 'confirmado') return 'green'
  if (key === 'warning' || key === 'borrador') return 'orange'
  if (key === 'error' || key === 'anulado') return 'red'
  return 'grey'
}

const resourceItems = computed(() => resources.value
  .map(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = readField(row, 'Codigointerno')
    const state = readField(row, 'Estado')
    const title = state ? `${code || `#${id}`} · ${state}` : (code || `#${id}`)
    return { value: id, title }
  })
  .filter(item => item.value != null)
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

const selectedResource = computed(() => resources.value.find(row => toNumber(readField(row, 'Id')) === toNumber(selectedResourceInstanceId.value)) || null)

const locationMap = computed(() => {
  const map = {}
  locations.value.forEach(row => {
    const id = toNumber(readField(row, 'Id'))
    if (id == null) return
    const code = readField(row, 'Codigo')
    const name = readField(row, 'Nombre')
    map[id] = code && name ? `${code} · ${name}` : (name || code || `#${id}`)
  })
  return map
})

const stockRows = computed(() => {
  const resourceId = toNumber(selectedResourceInstanceId.value)
  if (resourceId == null) return []

  return stock.value
    .filter(row => toNumber(readField(row, 'ResourceInstanceId')) === resourceId)
    .map(row => {
      const locationId = toNumber(readField(row, 'LocationId'))
      return {
        locationId,
        locationLabel: locationMap.value[locationId] || `#${locationId}`,
        stockReal: toNumber(readField(row, 'StockReal')) || 0,
        stockReservado: toNumber(readField(row, 'StockReservado')) || 0,
        stockDisponible: toNumber(readField(row, 'StockDisponible')) || 0
      }
    })
    .sort((a, b) => a.stockDisponible - b.stockDisponible)
})

const timelineRows = computed(() => toArray(timeline.value)
  .map(row => ({
    id: toNumber(readField(row, 'Id')),
    operationName: readField(row, 'OperationName') || 'evento',
    entityName: readField(row, 'EntityName') || '-',
    entityId: toNumber(readField(row, 'EntityId')),
    result: readField(row, 'Result') || '',
    message: readField(row, 'Message') || null,
    actor: readField(row, 'Actor') || null,
    executedAt: readField(row, 'ExecutedAt') || null
  }))
  .sort((a, b) => new Date(b.executedAt || 0).getTime() - new Date(a.executedAt || 0).getTime()))

const kpiCards = computed(() => {
  const totalStock = stockRows.value.reduce((acc, row) => {
    acc.real += row.stockReal
    acc.res += row.stockReservado
    acc.disp += row.stockDisponible
    return acc
  }, { real: 0, res: 0, disp: 0 })

  return [
    { label: 'Eventos', value: timelineRows.value.length, icon: 'mdi-history' },
    { label: 'Depósitos con stock', value: stockRows.value.length, icon: 'mdi-warehouse', color: 'teal' },
    { label: 'Stock real', value: formatNumber(totalStock.real), icon: 'mdi-cube-outline', color: 'blue' },
    { label: 'Disponible', value: formatNumber(totalStock.disp), icon: 'mdi-scale-balance', color: totalStock.disp > 0 ? 'green' : 'red' }
  ]
})

const stockHeaders = [
  { title: 'Depósito', key: 'locationLabel' },
  { title: 'Disp', key: 'stockDisponible', align: 'end' },
  { title: 'Real', key: 'stockReal', align: 'end' },
  { title: 'Reservado', key: 'stockReservado', align: 'end' },
  { title: 'Acción', key: 'actions', sortable: false, align: 'end' }
]

async function loadCatalogs() {
  loadingCatalogs.value = true
  error.value = ''

  try {
    const [resourcesRes, locationsRes, stockRes] = await Promise.all([
      runtimeApi.list('resource-instance'),
      runtimeApi.list('location'),
      runtimeApi.list('stock-balance')
    ])

    resources.value = toArray(resourcesRes?.data)
    locations.value = toArray(locationsRes?.data)
    stock.value = toArray(stockRes?.data)
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || 'No se pudo cargar catálogo de trazabilidad.'
  } finally {
    loadingCatalogs.value = false
  }
}

async function loadTimeline() {
  error.value = ''

  const resourceId = toNumber(selectedResourceInstanceId.value)
  if (resourceId == null) {
    timeline.value = []
    return
  }

  loadingTimeline.value = true
  try {
    const { data } = await runtimeApi.getResourceTimeline(resourceId)
    timeline.value = toArray(data)
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || 'No se pudo cargar timeline de trazabilidad.'
  } finally {
    loadingTimeline.value = false
  }
}

watch(selectedResourceInstanceId, () => {
  if (!selectedResourceInstanceId.value) {
    timeline.value = []
    return
  }
  loadTimeline()
})

onMounted(loadCatalogs)
</script>

<style scoped>
.head-wrap {
  display: flex;
  align-items: center;
  gap: 12px;
}

.head-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--sb-primary-soft, rgba(37,99,235,0.1));
}

.panel-card,
.kpi-card {
  border: 1px solid var(--sb-border-soft);
  border-radius: 14px;
}

.kpi-card {
  padding: 10px 12px;
}

.kpi-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.kpi-label {
  font-size: 0.74rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--sb-text-soft, #64748b);
}

.kpi-value {
  margin-top: 4px;
  font-size: 1.2rem;
  font-weight: 700;
}

.timeline-list {
  display: grid;
  gap: 8px;
  max-height: 520px;
  overflow: auto;
}

.timeline-item {
  border: 1px solid var(--sb-border-soft);
  border-radius: 12px;
  padding: 10px;
  display: flex;
  gap: 8px;
  align-items: flex-start;
}

.timeline-body {
  min-width: 0;
}

.table-name {
  font-weight: 600;
}

.table-sub {
  font-size: 0.8rem;
  color: var(--sb-text-soft, #64748b);
}

.traza-view :deep(.v-data-table th),
.traza-view :deep(.v-data-table td),
.traza-view :deep(.v-data-table .v-data-table__th),
.traza-view :deep(.v-data-table .v-data-table__td),
.traza-view :deep(.v-card-title),
.traza-view :deep(.v-label),
.traza-view :deep(.v-field__input) {
  color: var(--sb-text, #0f172a) !important;
}
</style>
