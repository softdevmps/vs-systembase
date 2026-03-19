<template>
  <v-container fluid class="pendientes-view">
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="head-wrap">
          <div class="head-icon">
            <v-icon size="24" color="warning">mdi-alert-circle-outline</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Pendientes e incidencias</h2>
            <div class="text-body-2 text-medium-emphasis">
              Resolver primero borradores y alertas para no cortar el flujo operativo.
            </div>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2 flex-wrap justify-end">
        <v-btn variant="tonal" color="primary" @click="goTo('/operaciones')">
          <v-icon start>mdi-transit-transfer</v-icon>
          Operaciones
        </v-btn>
        <v-btn variant="text" color="primary" :loading="loading" @click="loadData">
          <v-icon start>mdi-refresh</v-icon>
          Actualizar
        </v-btn>
      </v-col>
    </v-row>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">{{ error }}</v-alert>

    <v-row dense class="mb-4">
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
      <v-col cols="12" lg="7">
        <v-card class="panel-card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="orange">mdi-file-document-edit-outline</v-icon>
              Borradores pendientes
            </div>
            <v-chip size="small" variant="tonal">{{ draftRows.length }}</v-chip>
          </v-card-title>
          <v-divider />
          <v-data-table
            :headers="draftHeaders"
            :items="draftRows"
            :loading="loading"
            :items-per-page="8"
            no-data-text="No hay movimientos en borrador."
            class="ops-table"
          >
            <template #item.movementType="{ item }">
              <v-chip size="x-small" :color="movementTypeColor(item.movementType)" variant="tonal">
                {{ pretty(item.movementType) }}
              </v-chip>
            </template>
            <template #item.operationAt="{ item }">
              {{ formatDate(item.operationAt) }}
            </template>
            <template #item.actions="{ item }">
              <v-btn size="x-small" variant="text" color="primary" @click="goTo(`/movement?focus=${item.id}`)">
                Resolver
              </v-btn>
            </template>
          </v-data-table>
        </v-card>
      </v-col>

      <v-col cols="12" lg="5">
        <v-card class="panel-card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="red">mdi-alert-outline</v-icon>
              Stock crítico
            </div>
            <v-chip size="small" color="red" variant="tonal">{{ criticalStockRows.length }}</v-chip>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="criticalStockRows.length === 0" class="text-caption text-medium-emphasis">
              No hay saldos críticos.
            </div>
            <div v-else class="alert-list">
              <div v-for="row in criticalStockRows.slice(0, 8)" :key="row.id" class="alert-item">
                <div>
                  <strong>{{ row.locationLabel }}</strong>
                  <div class="text-caption">{{ row.resourceLabel }}</div>
                </div>
                <div class="text-right">
                  <div class="text-caption">Disp: {{ formatNumber(row.stockDisponible) }}</div>
                  <div class="text-caption">Res: {{ formatNumber(row.stockReservado) }}</div>
                </div>
              </div>
            </div>
            <div class="mt-3 d-flex justify-end">
              <v-btn size="small" variant="tonal" color="primary" @click="goTo('/stock-balance')">
                Ver saldos
              </v-btn>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-row dense class="mt-4">
      <v-col cols="12">
        <v-card class="panel-card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="indigo">mdi-bug-outline</v-icon>
              Incidencias recientes
            </div>
            <v-chip size="small" variant="tonal">{{ incidentRows.length }}</v-chip>
          </v-card-title>
          <v-divider />
          <v-data-table
            :headers="incidentHeaders"
            :items="incidentRows"
            :loading="loading"
            :items-per-page="6"
            no-data-text="No hay incidencias recientes."
            class="ops-table"
          >
            <template #item.result="{ item }">
              <v-chip size="x-small" :color="resultColor(item.result)" variant="tonal">
                {{ item.result || 'evento' }}
              </v-chip>
            </template>
            <template #item.executedAt="{ item }">
              {{ formatDate(item.executedAt) }}
            </template>
            <template #item.actions="{ item }">
              <v-btn size="x-small" variant="text" color="primary" @click="goTo(`/operation-audit?focus=${item.id}`)">
                Revisar
              </v-btn>
            </template>
          </v-data-table>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import runtimeApi from '../../api/runtime.service'

const router = useRouter()

const loading = ref(false)
const error = ref('')

const movementRows = ref([])
const movementLineRows = ref([])
const stockRows = ref([])
const locationRows = ref([])
const resourceRows = ref([])
const auditRows = ref([])

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

function pretty(value) {
  return String(value || '')
    .replace(/[_-]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/\b\w/g, c => c.toUpperCase())
}

function formatDate(value) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return String(value)
  return date.toLocaleString('es-AR')
}

function formatNumber(value) {
  const num = toNumber(value)
  if (num == null) return '0'
  return new Intl.NumberFormat('es-AR', { maximumFractionDigits: 3 }).format(num)
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

function resultColor(result) {
  const key = normalizeKey(result)
  if (key === 'ok' || key === 'confirmado') return 'green'
  if (key === 'warning' || key === 'borrador') return 'orange'
  if (key === 'error' || key === 'anulado') return 'red'
  return 'grey'
}

function goTo(path) {
  if (!path) return
  router.push(path)
}

const lineCountByMovement = computed(() => {
  const map = {}
  movementLineRows.value.forEach(row => {
    const movementId = toNumber(readField(row, 'MovementId'))
    if (movementId == null) return
    map[movementId] = (map[movementId] || 0) + 1
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

const resourceMap = computed(() => {
  const map = {}
  resourceRows.value.forEach(row => {
    const id = toNumber(readField(row, 'Id'))
    if (id == null) return
    const code = readField(row, 'Codigointerno')
    const state = readField(row, 'Estado')
    map[id] = state ? `${code || `#${id}`} · ${state}` : (code || `#${id}`)
  })
  return map
})

const draftRows = computed(() => {
  return movementRows.value
    .map(row => {
      const id = toNumber(readField(row, 'Id'))
      return {
        id,
        movementType: readField(row, 'MovementType') || '',
        status: readField(row, 'Status') || '',
        referenceNo: readField(row, 'ReferenceNo') || `MOV-${id ?? 'X'}`,
        operationAt: readField(row, 'OperationAt') || readField(row, 'CreatedAt'),
        createdBy: readField(row, 'CreatedBy') || 'system',
        lineCount: id == null ? 0 : (lineCountByMovement.value[id] || 0)
      }
    })
    .filter(row => normalizeKey(row.status) === 'borrador')
    .sort((a, b) => new Date(b.operationAt || 0).getTime() - new Date(a.operationAt || 0).getTime())
})

const criticalStockRows = computed(() => {
  return stockRows.value
    .map(row => {
      const id = toNumber(readField(row, 'Id'))
      const locationId = toNumber(readField(row, 'LocationId'))
      const resourceInstanceId = toNumber(readField(row, 'ResourceInstanceId'))
      return {
        id,
        locationLabel: locationMap.value[locationId || -1] || `#${locationId ?? '-'}`,
        resourceLabel: resourceMap.value[resourceInstanceId || -1] || `#${resourceInstanceId ?? '-'}`,
        stockDisponible: toNumber(readField(row, 'StockDisponible')) || 0,
        stockReservado: toNumber(readField(row, 'StockReservado')) || 0
      }
    })
    .filter(row => row.stockDisponible <= 0)
    .sort((a, b) => a.stockDisponible - b.stockDisponible)
})

const incidentRows = computed(() => {
  return auditRows.value
    .map(row => ({
      id: toNumber(readField(row, 'Id')),
      operationName: readField(row, 'OperationName') || '-',
      entityName: readField(row, 'EntityName') || '-',
      entityId: toNumber(readField(row, 'EntityId')),
      actor: readField(row, 'Actor') || 'system',
      result: String(readField(row, 'Result') || '').toLowerCase(),
      message: readField(row, 'Message') || '',
      executedAt: readField(row, 'ExecutedAt')
    }))
    .filter(row => ['error', 'warning', 'anulado'].includes(normalizeKey(row.result)))
    .sort((a, b) => new Date(b.executedAt || 0).getTime() - new Date(a.executedAt || 0).getTime())
    .slice(0, 20)
})

const kpiCards = computed(() => [
  { label: 'Borradores', value: draftRows.value.length, icon: 'mdi-file-document-edit-outline', color: draftRows.value.length ? 'orange' : 'green' },
  { label: 'Incidencias', value: incidentRows.value.length, icon: 'mdi-alert-outline', color: incidentRows.value.length ? 'red' : 'green' },
  { label: 'Stock crítico', value: criticalStockRows.value.length, icon: 'mdi-scale-balance', color: criticalStockRows.value.length ? 'red' : 'green' },
  { label: 'Movimientos total', value: movementRows.value.length, icon: 'mdi-swap-horizontal-bold' }
])

const draftHeaders = [
  { title: 'Referencia', key: 'referenceNo' },
  { title: 'Tipo', key: 'movementType' },
  { title: 'Fecha', key: 'operationAt' },
  { title: 'Usuario', key: 'createdBy' },
  { title: 'Líneas', key: 'lineCount', align: 'end' },
  { title: 'Acción', key: 'actions', sortable: false }
]

const incidentHeaders = [
  { title: 'Resultado', key: 'result', sortable: false },
  { title: 'Operación', key: 'operationName' },
  { title: 'Entidad', key: 'entityName' },
  { title: 'Fecha', key: 'executedAt' },
  { title: 'Acción', key: 'actions', sortable: false }
]

async function loadData() {
  loading.value = true
  error.value = ''

  try {
    const [movementsRes, linesRes, stockRes, locationsRes, resourcesRes, auditRes] = await Promise.all([
      runtimeApi.list('movement'),
      runtimeApi.list('movement-line'),
      runtimeApi.list('stock-balance'),
      runtimeApi.list('location'),
      runtimeApi.list('resource-instance'),
      runtimeApi.list('operation-audit')
    ])

    movementRows.value = toArray(movementsRes?.data)
    movementLineRows.value = toArray(linesRes?.data)
    stockRows.value = toArray(stockRes?.data)
    locationRows.value = toArray(locationsRes?.data)
    resourceRows.value = toArray(resourcesRes?.data)
    auditRows.value = toArray(auditRes?.data)
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || 'No se pudo cargar pendientes e incidencias.'
  } finally {
    loading.value = false
  }
}

onMounted(loadData)
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
  background: color-mix(in srgb, #f59e0b 15%, transparent);
}

.kpi-card,
.panel-card {
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

.alert-list {
  display: grid;
  gap: 10px;
  max-height: 340px;
  overflow: auto;
}

.alert-item {
  border: 1px solid var(--sb-border-soft);
  border-radius: 12px;
  padding: 10px;
  display: flex;
  justify-content: space-between;
  gap: 10px;
}

.pendientes-view :deep(.v-data-table th),
.pendientes-view :deep(.v-data-table td),
.pendientes-view :deep(.v-data-table .v-data-table__th),
.pendientes-view :deep(.v-data-table .v-data-table__td),
.pendientes-view :deep(.v-card-title),
.pendientes-view :deep(.v-label),
.pendientes-view :deep(.v-field__input) {
  color: var(--sb-text, #0f172a) !important;
}
</style>
