<template>
  <v-container fluid class="ops-home">
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="hero-box">
          <div class="hero-title-wrap">
            <div class="hero-icon">
              <v-icon color="primary" size="26">mdi-monitor-dashboard</v-icon>
            </div>
            <div>
              <h1 class="hero-title">Centro operativo</h1>
              <p class="hero-subtitle">Primero resuelve pendientes, luego ejecuta operación y finalmente valida trazabilidad.</p>
            </div>
          </div>
          <div class="hero-actions">
            <v-btn color="primary" @click="goTo(operacionesRoute)">
              <v-icon start>mdi-transit-transfer</v-icon>
              Iniciar operación
            </v-btn>
            <v-btn variant="tonal" color="warning" @click="goTo(pendientesRoute)">
              <v-icon start>mdi-alert-circle-outline</v-icon>
              Resolver pendientes
            </v-btn>
            <v-btn variant="tonal" color="primary" @click="goTo(depositosRoute)">
              <v-icon start>mdi-map-marker-multiple-outline</v-icon>
              Ver depósitos
            </v-btn>
            <v-btn variant="tonal" color="primary" @click="goTo(kardexRoute)">
              <v-icon start>mdi-notebook-outline</v-icon>
              Ver movimientos
            </v-btn>
            <v-btn variant="text" color="primary" :loading="loading" @click="loadData">
              <v-icon start>mdi-refresh</v-icon>
              Actualizar
            </v-btn>
          </div>
        </div>
      </v-col>
    </v-row>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">{{ error }}</v-alert>

    <v-row dense>
      <v-col v-for="card in kpiCards" :key="card.label" cols="12" sm="6" md="4" lg="2">
        <v-card class="kpi-card">
          <div class="kpi-head">
            <span class="kpi-label">{{ card.label }}</span>
            <v-icon :color="card.color || 'primary'" size="18">{{ card.icon }}</v-icon>
          </div>
          <div class="kpi-value">{{ card.value }}</div>
        </v-card>
      </v-col>
    </v-row>

    <v-row class="mt-4" dense>
      <v-col cols="12" md="7">
        <v-card class="panel-card">
          <v-card-title class="panel-title">
            <v-icon class="mr-2" color="orange">mdi-timer-sand</v-icon>
            Cola operativa (movimientos en borrador)
          </v-card-title>
          <v-divider />
          <v-data-table
            class="ops-table"
            :headers="draftHeaders"
            :items="draftMovements"
            :loading="loading"
            :items-per-page="5"
            no-data-text="No hay movimientos en borrador."
          >
            <template #item.movementType="{ item }">
              <v-chip size="small" :color="movementTypeColor(item.movementType)">
                {{ pretty(item.movementType) }}
              </v-chip>
            </template>
            <template #item.operationAt="{ item }">
              {{ formatDate(item.operationAt) }}
            </template>
            <template #item.actions="{ item }">
              <v-btn size="x-small" variant="text" color="primary" @click="goTo(`${movementListRoute}?focus=${item.id}`)">
                Abrir
              </v-btn>
            </template>
          </v-data-table>
        </v-card>
      </v-col>

      <v-col cols="12" md="5">
        <v-card class="panel-card">
          <v-card-title class="panel-title">
            <v-icon class="mr-2" color="teal">mdi-warehouse</v-icon>
            Stock por ubicación
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="locationStockRows.length === 0" class="text-caption text-medium-emphasis">
              Sin datos de stock.
            </div>
            <div v-else class="stock-list">
              <div v-for="row in locationStockRows.slice(0, 6)" :key="row.locationId" class="stock-item">
                <div>
                  <div class="stock-title">{{ row.locationLabel }}</div>
                  <div class="stock-meta">Items: {{ row.items }}</div>
                </div>
                <div class="stock-values">
                  <span>Real: {{ formatNumber(row.stockReal) }}</span>
                  <span>Disp: {{ formatNumber(row.stockDisponible) }}</span>
                  <span>Res: {{ formatNumber(row.stockReservado) }}</span>
                </div>
              </div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-row class="mt-4" dense>
      <v-col cols="12" md="6">
        <v-card class="panel-card">
          <v-card-title class="panel-title d-flex justify-space-between align-center">
            <div>
              <v-icon class="mr-2" color="primary">mdi-map-marker-path</v-icon>
              Flujo operativo
            </div>
            <v-btn size="small" variant="text" color="primary" @click="goTo(operacionesRoute)">Abrir operaciones</v-btn>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div class="flow-grid">
              <button class="flow-step" @click="goTo('/home')">
                <span class="step-index">1</span>
                <div>
                  <strong>Centro operativo</strong>
                  <p>Ver prioridad diaria de trabajo.</p>
                </div>
              </button>
              <button class="flow-step" @click="goTo(operacionesRoute)">
                <span class="step-index">2</span>
                <div>
                  <strong>Operaciones</strong>
                  <p>Recepcionar, despachar o trasladar.</p>
                </div>
              </button>
              <button class="flow-step" @click="goTo(pendientesRoute)">
                <span class="step-index">3</span>
                <div>
                  <strong>Pendientes</strong>
                  <p>Corregir borradores e incidencias.</p>
                </div>
              </button>
              <button class="flow-step" @click="goTo(depositosRoute)">
                <span class="step-index">4</span>
                <div>
                  <strong>Depósitos</strong>
                  <p>Validar impacto por ubicación.</p>
                </div>
              </button>
              <button class="flow-step" @click="goTo(kardexRoute)">
                <span class="step-index">5</span>
                <div>
                  <strong>Movimientos</strong>
                  <p>Control de kardex del turno.</p>
                </div>
              </button>
              <button class="flow-step" @click="goTo(trazabilidadRoute)">
                <span class="step-index">6</span>
                <div>
                  <strong>Trazabilidad</strong>
                  <p>Auditar historia completa.</p>
                </div>
              </button>
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="6">
        <v-card class="panel-card">
          <v-card-title class="panel-title">
            <v-icon class="mr-2" color="indigo">mdi-history</v-icon>
            Auditoría reciente
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="recentAudit.length === 0" class="text-caption text-medium-emphasis">
              Sin eventos recientes.
            </div>
            <div v-else class="audit-list">
              <div v-for="evt in recentAudit" :key="evt.id" class="audit-item">
                <v-chip size="x-small" :color="statusColor(evt.result)">
                  {{ evt.result || 'evento' }}
                </v-chip>
                <div class="audit-body">
                  <strong>{{ evt.operationName }}</strong>
                  <div class="text-caption text-medium-emphasis">
                    {{ evt.entityName }}#{{ evt.entityId ?? '-' }} · {{ evt.actor || 'system' }} · {{ formatDate(evt.executedAt) }}
                  </div>
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
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import runtimeApi from '../api/runtime.service'
import frontendConfig from '../config/frontend-config.json'
import { toKebab } from '../utils/slug'

const router = useRouter()
const loading = ref(false)
const error = ref('')

const movementRows = ref([])
const movementLineRows = ref([])
const stockRows = ref([])
const resourceRows = ref([])
const locationRows = ref([])
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

function formatNumber(value) {
  const num = toNumber(value)
  if (num == null) return '0'
  return new Intl.NumberFormat('es-AR', { maximumFractionDigits: 3 }).format(num)
}

function formatDate(value) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return String(value)
  return date.toLocaleString('es-AR')
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

function routeFor(entityKey, fallback) {
  const entities = frontendConfig?.entities || []
  const match = entities.find(entity => normalizeKey(entity?.routeSlug || entity?.name) === normalizeKey(entityKey))
  return `/${toKebab(match?.routeSlug || match?.name || fallback)}`
}

const movementListRoute = computed(() => routeFor('movement', 'movement'))
const operacionesRoute = computed(() => '/operaciones')
const pendientesRoute = computed(() => '/pendientes')
const depositosRoute = computed(() => '/depositos')
const trazabilidadRoute = computed(() => '/trazabilidad')
const kardexRoute = computed(() => '/kardex')

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

const draftMovements = computed(() => {
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

const locationStockRows = computed(() => {
  const grouped = {}
  stockRows.value.forEach(row => {
    const locationId = toNumber(readField(row, 'LocationId'))
    if (locationId == null) return
    if (!grouped[locationId]) {
      grouped[locationId] = {
        locationId,
        locationLabel: locationMap.value[locationId] || `#${locationId}`,
        items: 0,
        stockReal: 0,
        stockReservado: 0,
        stockDisponible: 0
      }
    }
    grouped[locationId].items += 1
    grouped[locationId].stockReal += toNumber(readField(row, 'StockReal')) || 0
    grouped[locationId].stockReservado += toNumber(readField(row, 'StockReservado')) || 0
    grouped[locationId].stockDisponible += toNumber(readField(row, 'StockDisponible')) || 0
  })

  return Object.values(grouped).sort((a, b) => a.stockDisponible - b.stockDisponible)
})

const recentAudit = computed(() => {
  return auditRows.value
    .map(row => ({
      id: toNumber(readField(row, 'Id')),
      operationName: readField(row, 'OperationName') || '-',
      entityName: readField(row, 'EntityName') || '-',
      entityId: toNumber(readField(row, 'EntityId')),
      actor: readField(row, 'Actor') || 'system',
      result: String(readField(row, 'Result') || '').toLowerCase(),
      executedAt: readField(row, 'ExecutedAt')
    }))
    .sort((a, b) => new Date(b.executedAt || 0).getTime() - new Date(a.executedAt || 0).getTime())
    .slice(0, 8)
})

const stockTotals = computed(() => stockRows.value.reduce((acc, row) => {
  acc.real += toNumber(readField(row, 'StockReal')) || 0
  acc.reservado += toNumber(readField(row, 'StockReservado')) || 0
  acc.disponible += toNumber(readField(row, 'StockDisponible')) || 0
  return acc
}, { real: 0, reservado: 0, disponible: 0 }))

const confirmedToday = computed(() => {
  const today = new Date()
  return movementRows.value.filter(row => {
    const status = normalizeKey(readField(row, 'Status'))
    if (status !== 'confirmado') return false
    const rawDate = readField(row, 'OperationAt') || readField(row, 'CreatedAt')
    const date = new Date(rawDate)
    return !Number.isNaN(date.getTime())
      && date.getFullYear() === today.getFullYear()
      && date.getMonth() === today.getMonth()
      && date.getDate() === today.getDate()
  }).length
})

const criticalStockCount = computed(() => stockRows.value.filter(row => {
  const available = toNumber(readField(row, 'StockDisponible')) || 0
  return available <= 0
}).length)

const kpiCards = computed(() => ([
  { label: 'Recursos', value: resourceRows.value.length, icon: 'mdi-cube-outline' },
  { label: 'Ubicaciones', value: locationRows.value.length, icon: 'mdi-map-marker-outline' },
  { label: 'Mov. borrador', value: draftMovements.value.length, icon: 'mdi-timer-sand', color: 'orange' },
  { label: 'Confirmados hoy', value: confirmedToday.value, icon: 'mdi-check-circle-outline', color: 'green' },
  { label: 'Stock disponible', value: formatNumber(stockTotals.value.disponible), icon: 'mdi-scale-balance', color: 'teal' },
  { label: 'Stock crítico', value: criticalStockCount.value, icon: 'mdi-alert-outline', color: criticalStockCount.value ? 'red' : 'green' }
]))

const draftHeaders = [
  { title: 'Referencia', key: 'referenceNo' },
  { title: 'Tipo', key: 'movementType' },
  { title: 'Fecha', key: 'operationAt' },
  { title: 'Usuario', key: 'createdBy' },
  { title: 'Líneas', key: 'lineCount', align: 'end' },
  { title: 'Acción', key: 'actions', sortable: false }
]

async function loadData() {
  loading.value = true
  error.value = ''

  try {
    const [movementsRes, linesRes, stockRes, resourcesRes, locationsRes, auditRes] = await Promise.all([
      runtimeApi.list('movement'),
      runtimeApi.list('movement-line'),
      runtimeApi.list('stock-balance'),
      runtimeApi.list('resource-instance'),
      runtimeApi.list('location'),
      runtimeApi.list('operation-audit')
    ])

    movementRows.value = toArray(movementsRes?.data)
    movementLineRows.value = toArray(linesRes?.data)
    stockRows.value = toArray(stockRes?.data)
    resourceRows.value = toArray(resourcesRes?.data)
    locationRows.value = toArray(locationsRes?.data)
    auditRows.value = toArray(auditRes?.data)
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || 'No se pudo cargar el centro operativo.'
  } finally {
    loading.value = false
  }
}

onMounted(loadData)
</script>

<style scoped>
.hero-box {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: center;
  border: 1px solid var(--sb-border-soft);
  border-radius: 16px;
  padding: 16px;
  background: color-mix(in srgb, var(--sb-surface) 96%, transparent);
}

.hero-title-wrap {
  display: flex;
  align-items: center;
  gap: 12px;
}

.hero-icon {
  width: 46px;
  height: 46px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--sb-primary-soft, rgba(37, 99, 235, 0.1));
}

.hero-title {
  margin: 0;
  font-size: 1.5rem;
}

.hero-subtitle {
  margin: 4px 0 0;
  color: var(--sb-text-soft, #64748b);
}

.hero-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  justify-content: flex-end;
}

.kpi-card {
  border: 1px solid var(--sb-border-soft);
  border-radius: 14px;
  padding: 10px 12px;
  background: color-mix(in srgb, var(--sb-surface) 94%, transparent);
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
  font-size: 1.25rem;
  font-weight: 700;
}

.panel-card {
  border: 1px solid var(--sb-border-soft);
  border-radius: 16px;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.panel-title {
  font-weight: 600;
  color: var(--sb-text, #0f172a);
}

.stock-list,
.audit-list {
  display: grid;
  gap: 10px;
  max-height: 420px;
  overflow: auto;
  padding-right: 4px;
}

.stock-item,
.audit-item {
  border: 1px solid var(--sb-border-soft);
  border-radius: 12px;
  padding: 10px;
  background: color-mix(in srgb, var(--sb-surface) 94%, transparent);
}

.stock-item {
  display: flex;
  justify-content: space-between;
  gap: 10px;
}

.stock-title {
  font-weight: 600;
}

.stock-meta {
  font-size: 0.8rem;
  color: var(--sb-text-soft, #64748b);
}

.stock-values {
  display: flex;
  flex-direction: column;
  gap: 2px;
  font-size: 0.82rem;
  text-align: right;
}

.flow-grid {
  display: grid;
  gap: 8px;
}

.flow-step {
  border: 1px solid var(--sb-border-soft);
  border-radius: 12px;
  padding: 10px;
  text-align: left;
  display: flex;
  gap: 10px;
  background: color-mix(in srgb, var(--sb-surface) 96%, transparent);
  cursor: pointer;
}

.flow-step:hover {
  border-color: var(--sb-primary, #2563eb);
  background: color-mix(in srgb, var(--sb-primary-soft, rgba(37,99,235,0.1)) 70%, var(--sb-surface));
}

.step-index {
  width: 22px;
  height: 22px;
  border-radius: 999px;
  background: var(--sb-primary-soft, rgba(37,99,235,0.1));
  color: var(--sb-primary, #2563eb);
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-top: 1px;
}

.flow-step p {
  margin: 2px 0 0;
  font-size: 0.82rem;
  color: var(--sb-text-soft, #64748b);
}

.audit-item {
  display: flex;
  gap: 8px;
  align-items: flex-start;
}

.audit-body {
  min-width: 0;
}

.ops-home :deep(.v-data-table),
.ops-home :deep(.v-data-table .v-table__wrapper table) {
  background: transparent;
}

.ops-home :deep(.v-data-table th),
.ops-home :deep(.v-data-table td),
.ops-home :deep(.v-data-table .v-data-table__td),
.ops-home :deep(.v-data-table .v-data-table__th) {
  color: var(--sb-text, #0f172a) !important;
}

.ops-home :deep(.v-data-table thead th) {
  background: color-mix(in srgb, var(--sb-primary, #2563eb) 5%, transparent);
  border-bottom: 1px solid var(--sb-border-soft);
  font-weight: 600;
}

.ops-home :deep(.v-data-table tbody tr:hover) {
  background: color-mix(in srgb, var(--sb-primary-soft, rgba(37,99,235,0.1)) 70%, transparent);
}

@media (max-width: 960px) {
  .hero-box {
    flex-direction: column;
    align-items: flex-start;
  }

  .hero-actions {
    width: 100%;
    justify-content: flex-start;
  }
}
</style>
