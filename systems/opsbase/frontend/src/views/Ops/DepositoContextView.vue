<template>
  <v-container fluid class="deposito-context-view">
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="head-wrap">
          <div class="head-icon">
            <v-icon size="24" color="primary">mdi-warehouse</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Depósito {{ locationLabel }}</h2>
            <div class="text-body-2 text-medium-emphasis">
              Contexto operativo por depósito: stock, cola de trabajo, movimientos y auditoría.
            </div>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2 flex-wrap justify-end">
        <v-btn variant="tonal" color="primary" @click="goTo('/depositos')">
          <v-icon start>mdi-arrow-left</v-icon>
          Volver al mapa
        </v-btn>
        <v-btn
          color="primary"
          variant="tonal"
          :disabled="!location.id"
          @click="goTo(`/depositos/${location.id}/editar`)"
        >
          <v-icon start>mdi-pencil</v-icon>
          Editar depósito
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

    <v-row dense class="mb-4">
      <v-col cols="12" lg="8">
        <v-card class="panel-card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-cube-outline</v-icon>
              Stock del depósito
            </div>
            <v-chip size="small" variant="tonal">{{ stockItems.length }} ítems</v-chip>
          </v-card-title>
          <v-divider />

          <v-data-table
            class="ops-table"
            :headers="stockHeaders"
            :items="stockItems"
            :loading="loading"
            :items-per-page="8"
            density="comfortable"
            no-data-text="Sin stock en este depósito."
          >
            <template #item.resource="{ item }">
              <div class="table-name">{{ item.resourceCode }} · {{ item.resourceName }}</div>
              <div class="table-sub">{{ item.instanceCode }} · {{ item.estado }}</div>
            </template>
            <template #item.stockDisponible="{ item }">
              <v-chip size="x-small" :color="item.stockDisponible <= 0 ? 'red' : 'green'" variant="tonal">
                {{ formatNumber(item.stockDisponible) }}
              </v-chip>
            </template>
            <template #item.stockReal="{ item }">{{ formatNumber(item.stockReal) }}</template>
            <template #item.stockReservado="{ item }">{{ formatNumber(item.stockReservado) }}</template>
          </v-data-table>
        </v-card>
      </v-col>

      <v-col cols="12" lg="4">
        <v-card class="panel-card h-100">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-map-marker-outline</v-icon>
              Localización
            </div>
            <v-chip size="small" :color="location.coordinateMode === 'db' ? 'green' : 'orange'" variant="tonal">
              {{ location.coordinateMode === 'db' ? 'Real' : 'Sintética' }}
            </v-chip>
          </v-card-title>
          <v-divider />

          <v-card-text>
            <div class="mb-2"><strong>{{ locationLabel }}</strong></div>
            <div class="text-body-2 text-medium-emphasis mb-3">
              Tipo: {{ location.tipo || 'n/a' }} · Capacidad: {{ formatNumber(location.capacidad) }}
            </div>

            <v-alert v-if="location.coordinateMode !== 'db'" type="warning" variant="tonal" density="comfortable" class="mb-3">
              Este depósito aún no tiene coordenadas reales cargadas.
            </v-alert>

            <iframe
              v-if="locationMapEmbed"
              :src="locationMapEmbed"
              width="100%"
              height="250"
              style="border:0; border-radius: 12px;"
              loading="lazy"
              referrerpolicy="no-referrer-when-downgrade"
            />

            <div class="d-flex justify-end mt-3">
              <v-btn
                v-if="locationMapLink"
                size="small"
                color="primary"
                variant="tonal"
                :href="locationMapLink"
                target="_blank"
                rel="noopener"
              >
                Ver en OpenStreetMap
              </v-btn>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-row dense>
      <v-col cols="12" md="6">
        <v-card class="panel-card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="orange">mdi-timer-sand</v-icon>
              Cola pendiente
            </div>
            <v-chip size="small" variant="tonal">{{ pendingMovements.length }}</v-chip>
          </v-card-title>
          <v-divider />

          <v-data-table
            class="ops-table"
            :headers="movementHeaders"
            :items="pendingMovements"
            :loading="loading"
            :items-per-page="6"
            density="comfortable"
            no-data-text="No hay pendientes en este depósito."
          >
            <template #item.referenceNo="{ item }">
              {{ item.referenceNo || `MOV-${item.movementId}` }}
            </template>
            <template #item.direction="{ item }">
              <v-chip size="x-small" :color="directionColor(item.direction)" variant="tonal">{{ pretty(item.direction) }}</v-chip>
            </template>
            <template #item.totalQuantity="{ item }">{{ formatNumber(item.totalQuantity) }}</template>
          </v-data-table>
        </v-card>
      </v-col>

      <v-col cols="12" md="6">
        <v-card class="panel-card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="indigo">mdi-history</v-icon>
              Auditoría reciente
            </div>
            <v-chip size="small" variant="tonal">{{ recentAudit.length }}</v-chip>
          </v-card-title>
          <v-divider />

          <v-card-text>
            <div v-if="recentAudit.length === 0" class="text-caption text-medium-emphasis">
              Sin eventos recientes.
            </div>
            <div v-else class="audit-list">
              <div v-for="evt in recentAudit" :key="evt.id" class="audit-item">
                <v-chip size="x-small" :color="statusColor(evt.result)" variant="tonal">{{ evt.result || 'evento' }}</v-chip>
                <div class="audit-body">
                  <strong>{{ evt.operationName }}</strong>
                  <div class="text-caption text-medium-emphasis">
                    {{ evt.entityName }}#{{ evt.entityId ?? '-' }} · {{ evt.actor || 'system' }} · {{ formatDate(evt.executedAt) }}
                  </div>
                  <div v-if="evt.message" class="text-caption">{{ evt.message }}</div>
                </div>
              </div>
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
              <v-icon class="mr-2" color="primary">mdi-transit-transfer</v-icon>
              Movimientos recientes (confirmados/anulados)
            </div>
            <v-chip size="small" variant="tonal">{{ recentMovements.length }}</v-chip>
          </v-card-title>
          <v-divider />

          <v-data-table
            class="ops-table"
            :headers="recentHeaders"
            :items="recentMovements"
            :loading="loading"
            :items-per-page="8"
            density="comfortable"
            no-data-text="Sin movimientos recientes."
          >
            <template #item.referenceNo="{ item }">
              {{ item.referenceNo || `MOV-${item.movementId}` }}
            </template>
            <template #item.movementType="{ item }">
              <v-chip size="x-small" :color="movementTypeColor(item.movementType)" variant="tonal">{{ pretty(item.movementType) }}</v-chip>
            </template>
            <template #item.status="{ item }">
              <v-chip size="x-small" :color="statusColor(item.status)" variant="tonal">{{ pretty(item.status) }}</v-chip>
            </template>
            <template #item.totalQuantity="{ item }">{{ formatNumber(item.totalQuantity) }}</template>
            <template #item.operationAt="{ item }">{{ formatDate(item.operationAt) }}</template>
          </v-data-table>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import runtimeApi from '../../api/runtime.service'

const route = useRoute()
const router = useRouter()

const loading = ref(false)
const error = ref('')
const response = ref({})

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
  if (key === 'ok' || key === 'confirmado') return 'green'
  if (key === 'warning' || key === 'borrador') return 'orange'
  if (key === 'error' || key === 'anulado') return 'red'
  return 'grey'
}

function directionColor(direction) {
  const key = normalizeKey(direction)
  if (key === 'entrada') return 'green'
  if (key === 'salida') return 'red'
  if (key === 'interno') return 'blue'
  return 'grey'
}

const location = computed(() => {
  const src = readField(response.value, 'Location') || {}
  return {
    id: toNumber(readField(src, 'Id')),
    codigo: readField(src, 'Codigo') || '—',
    nombre: readField(src, 'Nombre') || 'Sin nombre',
    tipo: readField(src, 'Tipo') || 'n/a',
    capacidad: toNumber(readField(src, 'Capacidad')) || 0,
    coordinateMode: (readField(src, 'CoordinateMode') || 'db').toString().toLowerCase(),
    lat: toNumber(readField(src, 'Lat')),
    lng: toNumber(readField(src, 'Lng'))
  }
})

const locationLabel = computed(() => `${location.value.codigo} · ${location.value.nombre}`)

const kpis = computed(() => {
  const src = readField(response.value, 'Kpis') || {}
  return {
    totalMovements: toNumber(readField(src, 'TotalMovements')) || 0,
    pendingMovements: toNumber(readField(src, 'PendingMovements')) || 0,
    confirmedMovements: toNumber(readField(src, 'ConfirmedMovements')) || 0,
    stockCriticoItems: toNumber(readField(src, 'StockCriticoItems')) || 0,
    stockDisponibleTotal: toNumber(readField(src, 'StockDisponibleTotal')) || 0
  }
})

const stockItems = computed(() => toArray(readField(response.value, 'StockItems')).map(row => ({
  stockBalanceId: toNumber(readField(row, 'StockBalanceId')),
  resourceInstanceId: toNumber(readField(row, 'ResourceInstanceId')),
  resourceCode: readField(row, 'ResourceCode') || '—',
  resourceName: readField(row, 'ResourceName') || '—',
  instanceCode: readField(row, 'InstanceCode') || '—',
  estado: readField(row, 'Estado') || '—',
  stockReal: toNumber(readField(row, 'StockReal')) || 0,
  stockReservado: toNumber(readField(row, 'StockReservado')) || 0,
  stockDisponible: toNumber(readField(row, 'StockDisponible')) || 0
})))

function normalizeMovementRow(row) {
  return {
    movementId: toNumber(readField(row, 'MovementId')),
    movementType: readField(row, 'MovementType') || '',
    status: readField(row, 'Status') || '',
    referenceNo: readField(row, 'ReferenceNo') || null,
    operationAt: readField(row, 'OperationAt') || null,
    direction: readField(row, 'Direction') || '',
    totalQuantity: toNumber(readField(row, 'TotalQuantity')) || 0,
    lineCount: toNumber(readField(row, 'LineCount')) || 0,
    sourceLabel: readField(row, 'SourceLabel') || '—',
    targetLabel: readField(row, 'TargetLabel') || '—'
  }
}

const pendingMovements = computed(() => toArray(readField(response.value, 'PendingMovements')).map(normalizeMovementRow))
const recentMovements = computed(() => toArray(readField(response.value, 'RecentMovements')).map(normalizeMovementRow))

const recentAudit = computed(() => toArray(readField(response.value, 'RecentAudit')).map(row => ({
  id: toNumber(readField(row, 'Id')),
  operationName: readField(row, 'OperationName') || '',
  entityName: readField(row, 'EntityName') || '',
  entityId: toNumber(readField(row, 'EntityId')),
  result: readField(row, 'Result') || '',
  message: readField(row, 'Message') || null,
  actor: readField(row, 'Actor') || null,
  executedAt: readField(row, 'ExecutedAt') || null
})))

const kpiCards = computed(() => [
  { label: 'Movimientos', value: kpis.value.totalMovements, icon: 'mdi-transit-transfer' },
  { label: 'Pendientes', value: kpis.value.pendingMovements, icon: 'mdi-timer-sand', color: 'orange' },
  { label: 'Confirmados', value: kpis.value.confirmedMovements, icon: 'mdi-check-circle-outline', color: 'green' },
  { label: 'Stock crítico', value: kpis.value.stockCriticoItems, icon: 'mdi-alert-outline', color: kpis.value.stockCriticoItems > 0 ? 'red' : 'green' },
  { label: 'Stock disponible', value: formatNumber(kpis.value.stockDisponibleTotal), icon: 'mdi-scale-balance', color: 'primary' }
])

const stockHeaders = [
  { title: 'Recurso', key: 'resource', sortable: false },
  { title: 'Disp', key: 'stockDisponible', align: 'end' },
  { title: 'Real', key: 'stockReal', align: 'end' },
  { title: 'Reservado', key: 'stockReservado', align: 'end' }
]

const movementHeaders = [
  { title: 'Referencia', key: 'referenceNo' },
  { title: 'Tipo', key: 'movementType' },
  { title: 'Dirección', key: 'direction' },
  { title: 'Cant.', key: 'totalQuantity', align: 'end' }
]

const recentHeaders = [
  { title: 'Referencia', key: 'referenceNo' },
  { title: 'Tipo', key: 'movementType' },
  { title: 'Estado', key: 'status' },
  { title: 'Cant.', key: 'totalQuantity', align: 'end' },
  { title: 'Fecha', key: 'operationAt' }
]

const locationMapLink = computed(() => {
  const lat = toNumber(location.value.lat)
  const lng = toNumber(location.value.lng)
  if (lat == null || lng == null) return ''
  return `https://www.openstreetmap.org/?mlat=${lat.toFixed(6)}&mlon=${lng.toFixed(6)}#map=17/${lat.toFixed(6)}/${lng.toFixed(6)}`
})

const locationMapEmbed = computed(() => {
  const lat = toNumber(location.value.lat)
  const lng = toNumber(location.value.lng)
  if (lat == null || lng == null) return ''

  const delta = 0.01
  const minLng = (lng - delta).toFixed(6)
  const minLat = (lat - delta).toFixed(6)
  const maxLng = (lng + delta).toFixed(6)
  const maxLat = (lat + delta).toFixed(6)

  return `https://www.openstreetmap.org/export/embed.html?bbox=${minLng}%2C${minLat}%2C${maxLng}%2C${maxLat}&layer=mapnik&marker=${lat.toFixed(6)}%2C${lng.toFixed(6)}`
})

async function loadData() {
  loading.value = true
  error.value = ''

  try {
    const locationId = Number(route.params.locationId)
    const { data } = await runtimeApi.getOpsDepositoContext(locationId, 50)
    response.value = data || {}
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || (typeof payload === 'string' ? payload : 'No se pudo cargar el contexto del depósito.')
  } finally {
    loading.value = false
  }
}

watch(() => route.params.locationId, loadData, { immediate: true })
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

.table-name {
  font-weight: 600;
}

.table-sub {
  font-size: 0.8rem;
  color: var(--sb-text-soft, #64748b);
}

.audit-list {
  display: grid;
  gap: 8px;
  max-height: 360px;
  overflow: auto;
}

.audit-item {
  border: 1px solid var(--sb-border-soft);
  border-radius: 12px;
  padding: 10px;
  display: flex;
  gap: 8px;
  align-items: flex-start;
}

.audit-body {
  min-width: 0;
}

.deposito-context-view :deep(.v-data-table th),
.deposito-context-view :deep(.v-data-table td),
.deposito-context-view :deep(.v-data-table .v-data-table__th),
.deposito-context-view :deep(.v-data-table .v-data-table__td),
.deposito-context-view :deep(.v-card-title),
.deposito-context-view :deep(.v-label),
.deposito-context-view :deep(.v-field__input) {
  color: var(--sb-text, #0f172a) !important;
}
</style>
