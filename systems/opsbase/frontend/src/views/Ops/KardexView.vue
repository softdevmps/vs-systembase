<template>
  <v-container fluid class="kardex-view">
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="d-flex align-center ga-3">
          <div class="kardex-icon">
            <v-icon color="primary" size="24">mdi-notebook-outline</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Kardex operativo</h2>
            <div class="text-body-2 text-medium-emphasis">
              Libro de movimientos con filtros por recurso, ubicación y estado.
            </div>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2">
        <v-btn variant="tonal" color="primary" @click="goTo(movementRoute)">
          <v-icon start>mdi-swap-horizontal-bold</v-icon>
          Movimientos
        </v-btn>
        <v-btn color="primary" @click="loadData" :loading="loading">
          <v-icon start>mdi-refresh</v-icon>
          Actualizar
        </v-btn>
      </v-col>
    </v-row>

    <v-row dense class="mb-3">
      <v-col cols="12" md="3">
        <v-select
          v-model="selectedResource"
          :items="resourceItems"
          item-title="title"
          item-value="value"
          label="Recurso"
          clearable
          density="comfortable"
          variant="outlined"
        />
      </v-col>
      <v-col cols="12" md="3">
        <v-select
          v-model="selectedLocation"
          :items="locationItems"
          item-title="title"
          item-value="value"
          label="Ubicación"
          clearable
          density="comfortable"
          variant="outlined"
        />
      </v-col>
      <v-col cols="12" md="2">
        <v-select
          v-model="selectedType"
          :items="movementTypeItems"
          item-title="title"
          item-value="value"
          label="Tipo"
          clearable
          density="comfortable"
          variant="outlined"
        />
      </v-col>
      <v-col cols="12" md="2">
        <v-select
          v-model="selectedStatus"
          :items="movementStatusItems"
          item-title="title"
          item-value="value"
          label="Estado"
          clearable
          density="comfortable"
          variant="outlined"
        />
      </v-col>
      <v-col cols="12" md="2">
        <v-text-field
          v-model="search"
          label="Buscar"
          clearable
          density="comfortable"
          variant="outlined"
          prepend-inner-icon="mdi-magnify"
        />
      </v-col>
    </v-row>

    <v-row dense class="mb-4">
      <v-col cols="12" sm="6" md="2">
        <v-card class="metric-card">
          <span class="metric-label">Registros</span>
          <strong>{{ filteredRows.length }}</strong>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="2">
        <v-card class="metric-card">
          <span class="metric-label">Cantidad total</span>
          <strong>{{ formatNumber(totalQuantity) }}</strong>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="2">
        <v-card class="metric-card">
          <span class="metric-label">Costo total</span>
          <strong>{{ formatMoney(totalCost) }}</strong>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="2">
        <v-card class="metric-card">
          <span class="metric-label">Borrador</span>
          <strong>{{ statusCounts.borrador || 0 }}</strong>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="2">
        <v-card class="metric-card">
          <span class="metric-label">Confirmado</span>
          <strong>{{ statusCounts.confirmado || 0 }}</strong>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="2">
        <v-card class="metric-card">
          <span class="metric-label">Anulado</span>
          <strong>{{ statusCounts.anulado || 0 }}</strong>
        </v-card>
      </v-col>
    </v-row>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">{{ error }}</v-alert>

    <v-card>
      <v-data-table
        :headers="headers"
        :items="filteredRows"
        :loading="loading"
        :items-per-page="15"
        class="kardex-table"
        no-data-text="Sin movimientos para los filtros seleccionados."
      >
        <template #item.movementType="{ item }">
          <v-chip size="small" :color="movementTypeColor(item.movementType)">
            {{ pretty(item.movementType) }}
          </v-chip>
        </template>
        <template #item.status="{ item }">
          <v-chip size="small" :color="statusColor(item.status)">
            {{ pretty(item.status) }}
          </v-chip>
        </template>
        <template #item.quantity="{ item }">
          {{ formatNumber(item.quantity) }}
        </template>
        <template #item.unitCost="{ item }">
          {{ item.unitCost == null ? '—' : formatMoney(item.unitCost) }}
        </template>
        <template #item.totalCost="{ item }">
          {{ item.totalCost == null ? '—' : formatMoney(item.totalCost) }}
        </template>
      </v-data-table>
    </v-card>
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import runtimeApi from '../../api/runtime.service'
import frontendConfig from '../../config/frontend-config.json'
import { toKebab } from '../../utils/slug'

const router = useRouter()

const loading = ref(false)
const error = ref('')

const movementRows = ref([])
const movementLineRows = ref([])
const resourceRows = ref([])
const locationRows = ref([])

const selectedResource = ref(null)
const selectedLocation = ref(null)
const selectedType = ref(null)
const selectedStatus = ref(null)
const search = ref('')

const MOVEMENT_TYPES = ['ingreso', 'egreso', 'transferencia', 'ajuste', 'reserva', 'liberacion']
const MOVEMENT_STATUS = ['borrador', 'confirmado', 'anulado']

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

function toDate(value) {
  if (!value) return null
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

function pretty(value) {
  return String(value || '')
    .replace(/[_-]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/\b\w/g, c => c.toUpperCase())
}

function movementTypeColor(type) {
  const key = normalizeKey(type)
  if (key === 'ingreso') return 'green'
  if (key === 'egreso') return 'red'
  if (key === 'transferencia') return 'blue'
  if (key === 'reserva') return 'orange'
  if (key === 'liberacion') return 'teal'
  return 'grey'
}

function statusColor(status) {
  const key = normalizeKey(status)
  if (key === 'confirmado') return 'green'
  if (key === 'borrador') return 'orange'
  if (key === 'anulado') return 'red'
  return 'grey'
}

function formatNumber(value) {
  const num = toNumber(value)
  if (num == null) return '0'
  return new Intl.NumberFormat('es-AR', { maximumFractionDigits: 3 }).format(num)
}

function formatMoney(value) {
  const num = toNumber(value)
  if (num == null) return '—'
  return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(num)
}

const movementMap = computed(() => {
  const map = {}
  movementRows.value.forEach(row => {
    const id = toNumber(readField(row, 'Id'))
    if (id != null) map[id] = row
  })
  return map
})

const resourceMap = computed(() => {
  const map = {}
  resourceRows.value.forEach(row => {
    const id = toNumber(readField(row, 'Id'))
    if (id == null) return
    const code = readField(row, 'Codigointerno') || `#${id}`
    const state = readField(row, 'Estado')
    map[id] = state ? `${code} · ${state}` : String(code)
  })
  return map
})

const locationMap = computed(() => {
  const map = {}
  locationRows.value.forEach(row => {
    const id = toNumber(readField(row, 'Id'))
    if (id == null) return
    const code = readField(row, 'Codigo')
    const name = readField(row, 'Nombre')
    map[id] = code && name ? `${code} · ${name}` : (name || code || `#${id}`)
  })
  return map
})

const resourceItems = computed(() => Object.entries(resourceMap.value)
  .map(([value, title]) => ({ value: Number(value), title }))
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

const locationItems = computed(() => Object.entries(locationMap.value)
  .map(([value, title]) => ({ value: Number(value), title }))
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

const movementTypeItems = computed(() => MOVEMENT_TYPES.map(type => ({ value: type, title: pretty(type) })))
const movementStatusItems = computed(() => MOVEMENT_STATUS.map(status => ({ value: status, title: pretty(status) })))

const movementRoute = computed(() => {
  const entities = frontendConfig?.entities || []
  const movement = entities.find(entity => normalizeKey(entity?.routeSlug || entity?.name) === 'movement')
  return `/${toKebab(movement?.routeSlug || movement?.name || 'movement')}`
})

const joinedRows = computed(() => {
  return movementLineRows.value.map(line => {
    const movementId = toNumber(readField(line, 'MovementId'))
    const movement = movementMap.value[movementId || -1] || {}

    const resourceInstanceId = toNumber(readField(line, 'ResourceInstanceId'))
    const sourceLocationId = toNumber(readField(movement, 'SourceLocationId'))
    const targetLocationId = toNumber(readField(movement, 'TargetLocationId'))

    const quantity = toNumber(readField(line, 'Quantity')) || 0
    const unitCost = toNumber(readField(line, 'UnitCost'))
    const totalCost = unitCost == null ? null : quantity * unitCost

    const operationAt = readField(movement, 'OperationAt') || readField(line, 'CreatedAt')
    const operationDate = toDate(operationAt)

    return {
      id: `${movementId || 'x'}-${readField(line, 'Id') || Math.random()}`,
      lineId: toNumber(readField(line, 'Id')),
      movementId,
      movementType: String(readField(movement, 'MovementType') || '').toLowerCase(),
      status: String(readField(movement, 'Status') || '').toLowerCase(),
      referenceNo: readField(movement, 'ReferenceNo') || '',
      operationAt,
      operationDate,
      resourceInstanceId,
      resourceLabel: resourceMap.value[resourceInstanceId || -1] || `#${resourceInstanceId ?? '-'}`,
      sourceLocationId,
      targetLocationId,
      sourceLabel: sourceLocationId == null ? '—' : (locationMap.value[sourceLocationId] || `#${sourceLocationId}`),
      targetLabel: targetLocationId == null ? '—' : (locationMap.value[targetLocationId] || `#${targetLocationId}`),
      quantity,
      unitCost,
      totalCost,
      notes: readField(movement, 'Notes') || ''
    }
  }).sort((a, b) => {
    const ta = a.operationDate?.getTime() || 0
    const tb = b.operationDate?.getTime() || 0
    if (tb !== ta) return tb - ta
    return (b.lineId || 0) - (a.lineId || 0)
  })
})

const filteredRows = computed(() => {
  const term = String(search.value || '').trim().toLowerCase()
  return joinedRows.value.filter(row => {
    if (selectedResource.value != null && Number(selectedResource.value) !== Number(row.resourceInstanceId)) return false
    if (selectedLocation.value != null) {
      const loc = Number(selectedLocation.value)
      if (Number(row.sourceLocationId) !== loc && Number(row.targetLocationId) !== loc) return false
    }
    if (selectedType.value && normalizeKey(selectedType.value) !== normalizeKey(row.movementType)) return false
    if (selectedStatus.value && normalizeKey(selectedStatus.value) !== normalizeKey(row.status)) return false

    if (!term) return true
    const haystack = [
      row.referenceNo,
      row.resourceLabel,
      row.sourceLabel,
      row.targetLabel,
      row.notes,
      row.movementType,
      row.status
    ].join(' ').toLowerCase()
    return haystack.includes(term)
  })
})

const totalQuantity = computed(() => filteredRows.value.reduce((sum, row) => sum + (row.quantity || 0), 0))
const totalCost = computed(() => filteredRows.value.reduce((sum, row) => sum + (row.totalCost || 0), 0))

const statusCounts = computed(() => filteredRows.value.reduce((acc, row) => {
  const key = normalizeKey(row.status)
  if (!key) return acc
  acc[key] = (acc[key] || 0) + 1
  return acc
}, {}))

const headers = [
  { title: 'Fecha', key: 'operationAt' },
  { title: 'Tipo', key: 'movementType' },
  { title: 'Estado', key: 'status' },
  { title: 'Referencia', key: 'referenceNo' },
  { title: 'Recurso', key: 'resourceLabel' },
  { title: 'Origen', key: 'sourceLabel' },
  { title: 'Destino', key: 'targetLabel' },
  { title: 'Cantidad', key: 'quantity', align: 'end' },
  { title: 'Costo unitario', key: 'unitCost', align: 'end' },
  { title: 'Costo total', key: 'totalCost', align: 'end' }
]

function goTo(path) {
  if (!path) return
  router.push(path)
}

async function loadData() {
  loading.value = true
  error.value = ''
  try {
    const [movementsRes, linesRes, resourcesRes, locationsRes] = await Promise.all([
      runtimeApi.list('movement'),
      runtimeApi.list('movement-line'),
      runtimeApi.list('resource-instance'),
      runtimeApi.list('location')
    ])

    movementRows.value = toArray(movementsRes?.data)
    movementLineRows.value = toArray(linesRes?.data)
    resourceRows.value = toArray(resourcesRes?.data)
    locationRows.value = toArray(locationsRes?.data)
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || 'No se pudo cargar kardex.'
  } finally {
    loading.value = false
  }
}

onMounted(loadData)
</script>

<style scoped>
.kardex-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  background: var(--sb-primary-soft, rgba(37, 99, 235, 0.1));
  display: flex;
  align-items: center;
  justify-content: center;
}

.metric-card {
  border: 1px solid var(--sb-border-soft);
  border-radius: 14px;
  padding: 10px 12px;
  background: color-mix(in srgb, var(--sb-surface) 94%, transparent);
}

.metric-label {
  display: block;
  font-size: 0.72rem;
  text-transform: uppercase;
  color: var(--sb-text-soft, #64748b);
  letter-spacing: 0.06em;
}

.metric-card strong {
  font-size: 1.2rem;
}

.kardex-table :deep(th .v-data-table-header__content) {
  white-space: nowrap;
}

.kardex-view :deep(.v-data-table),
.kardex-view :deep(.v-data-table .v-table__wrapper table) {
  background: transparent;
}

.kardex-view :deep(.v-data-table th),
.kardex-view :deep(.v-data-table td),
.kardex-view :deep(.v-data-table .v-data-table__td),
.kardex-view :deep(.v-data-table .v-data-table__th) {
  color: var(--sb-text, #0f172a) !important;
}

.kardex-view :deep(.v-data-table thead th) {
  background: color-mix(in srgb, var(--sb-primary, #2563eb) 5%, transparent);
  border-bottom: 1px solid var(--sb-border-soft);
  font-weight: 600;
}

.kardex-view :deep(.v-data-table tbody tr:hover) {
  background: color-mix(in srgb, var(--sb-primary-soft, rgba(37,99,235,0.1)) 70%, transparent);
}
</style>
