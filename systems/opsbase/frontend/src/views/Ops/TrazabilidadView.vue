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
      <v-col cols="auto" class="d-flex ga-2 flex-wrap justify-end">
        <v-chip color="primary" variant="tonal" size="small">Paso 6/6</v-chip>
        <v-btn variant="tonal" color="primary" @click="goTo('/kardex')">
          <v-icon start>mdi-notebook-outline</v-icon>
          Kardex
        </v-btn>
        <v-btn variant="tonal" color="success" @click="goTo('/home')">
          <v-icon start>mdi-arrow-right-circle-outline</v-icon>
          Fin: Volver a centro
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
          <v-col cols="12" md="3">
            <v-select
              v-model="traceMode"
              :items="traceModeItems"
              item-title="title"
              item-value="value"
              label="Modo de trazabilidad"
              variant="outlined"
              density="comfortable"
            />
          </v-col>
          <v-col cols="12" md="5" v-if="traceMode === 'resource'">
            <v-select
              v-model="selectedResourceInstanceId"
              :items="resourceItems"
              item-title="title"
              item-value="value"
              label="Instancia de recurso"
              :loading="loadingCatalogs"
              :menu-props="{ maxHeight: 420 }"
              variant="outlined"
              density="comfortable"
              clearable
            />
          </v-col>
          <v-col cols="12" md="5" v-else>
            <v-select
              v-model="selectedLocationId"
              :items="locationItems"
              item-title="title"
              item-value="value"
              label="Depósito"
              :loading="loadingCatalogs"
              variant="outlined"
              density="comfortable"
              clearable
            />
          </v-col>
          <v-col cols="12" md="4" class="d-flex align-center">
            <v-btn
              color="primary"
              variant="tonal"
              :disabled="traceMode === 'resource' ? !selectedResourceInstanceId : !selectedLocationId"
              :loading="loadingTimeline"
              @click="loadTimeline"
            >
              <v-icon start>mdi-timeline-text-outline</v-icon>
              {{ traceMode === 'resource' ? 'Cargar trazabilidad' : 'Cargar timeline depósito' }}
            </v-btn>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-row dense class="mb-4" v-if="selectedResource || (traceMode === 'location' && selectedLocationId)">
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
          <v-alert
            v-if="traceMode === 'location' && stockLoadWarning"
            type="warning"
            density="comfortable"
            variant="tonal"
            class="mx-4 mt-4 mb-0"
          >
            {{ stockLoadWarning }}
          </v-alert>

          <v-data-table
            class="traza-table"
            :headers="stockHeaders"
            :items="stockRows"
            :items-per-page="8"
            density="comfortable"
            :loading="traceMode === 'location' && loadingStockByLocation"
            :no-data-text="stockNoDataText"
          >
            <template #item.primaryLabel="{ item }">
              <div class="table-name">{{ item.primaryLabel }}</div>
              <div class="table-sub">{{ item.secondaryLabel }}</div>
            </template>
            <template #item.stockDisponible="{ item }">
              <v-chip size="x-small" :color="item.stockDisponible <= 0 ? 'red' : 'green'" variant="tonal">
                {{ formatNumber(item.stockDisponible) }}
              </v-chip>
            </template>
            <template #item.actions="{ item }">
              <v-btn size="x-small" variant="text" color="primary" @click="openStockRow(item)">
                {{ item.actionType === 'resource' ? 'Ver recurso' : 'Abrir depósito' }}
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
            <v-chip size="small" variant="tonal">{{ timelineRowsFiltered.length }}</v-chip>
          </v-card-title>
          <v-divider />

          <v-card-text>
            <v-row dense class="mb-2">
              <v-col cols="12" md="4">
                <v-select
                  v-model="timelineStatusFilter"
                  :items="timelineStatusItems"
                  item-title="title"
                  item-value="value"
                  label="Estado"
                  variant="outlined"
                  density="comfortable"
                  clearable
                />
              </v-col>
              <v-col cols="6" md="3">
                <v-text-field
                  v-model="timelineDateFrom"
                  label="Desde"
                  type="date"
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="6" md="3">
                <v-text-field
                  v-model="timelineDateTo"
                  label="Hasta"
                  type="date"
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="2" class="d-flex align-center justify-end">
                <v-chip size="small" variant="tonal">{{ timelineRowsFiltered.length }} eventos</v-chip>
              </v-col>
            </v-row>
            <div v-if="traceMode === 'resource' && !selectedResourceInstanceId" class="text-caption text-medium-emphasis">
              Selecciona una instancia para cargar el timeline.
            </div>
            <div v-else-if="traceMode === 'location' && !selectedLocationId" class="text-caption text-medium-emphasis">
              Selecciona un depósito para cargar el timeline.
            </div>
            <div v-else-if="loadingTimeline" class="text-caption text-medium-emphasis">
              Cargando timeline...
            </div>
            <div v-else-if="timelineRowsFiltered.length === 0" class="text-caption text-medium-emphasis">
              Sin eventos registrados para el criterio seleccionado.
            </div>
            <div v-else class="timeline-list">
              <div v-for="row in timelineRowsFiltered" :key="row.id" class="timeline-item">
                <v-chip size="x-small" :color="statusColor(row.result)" variant="tonal">{{ row.result || 'evento' }}</v-chip>
                <div class="timeline-body">
                  <strong>{{ row.operationName }}</strong>
                  <div class="text-caption text-medium-emphasis">
                    {{ row.entityName }}#{{ row.entityId ?? '-' }} · {{ row.actor || 'system' }} · {{ formatDate(row.executedAt) }}
                  </div>
                  <div v-if="row.message" class="text-caption">{{ row.message }}</div>
                  <div class="mt-1" v-if="row.movementId">
                    <v-btn size="x-small" variant="text" color="primary" @click="goTo(`/movement?focus=${row.movementId}`)">
                      Abrir movimiento origen
                    </v-btn>
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
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import runtimeApi from '../../api/runtime.service'

const router = useRouter()

const loadingCatalogs = ref(false)
const loadingTimeline = ref(false)
const error = ref('')

const resources = ref([])
const resourceDefinitions = ref([])
const locations = ref([])
const stock = ref([])
const timeline = ref([])
const movements = ref([])
const stockByLocation = ref([])

const traceMode = ref('resource')
const selectedLocationId = ref(null)
const selectedResourceInstanceId = ref(null)
const timelineStatusFilter = ref(null)
const timelineDateFrom = ref('')
const timelineDateTo = ref('')
const loadingStockByLocation = ref(false)
const stockLoadWarning = ref('')

const traceModeItems = [
  { value: 'resource', title: 'Por recurso' },
  { value: 'location', title: 'Por depósito' }
]

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

function truncateText(value, max = 30) {
  const text = String(value || '').trim()
  if (!text || text.length <= max) return text
  return `${text.slice(0, Math.max(1, max - 1))}…`
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

const resourceDefinitionMap = computed(() => {
  const map = {}
  resourceDefinitions.value.forEach(row => {
    const id = toNumber(readField(row, 'Id'))
    if (id == null) return
    const code = String(readField(row, 'Codigo') || '').trim()
    const name = String(readField(row, 'Nombre') || '').trim()
    map[id] = {
      code,
      name,
      display: name || code || `Def#${id}`,
      rubroNombre: String(readField(row, 'Rubronombre') || '').trim()
    }
  })
  return map
})

const resourceItems = computed(() => resources.value
  .map(row => {
    const id = toNumber(readField(row, 'Id'))
    const definitionId = toNumber(readField(row, 'Resourcedefinitionid') ?? readField(row, 'ResourceDefinitionId'))
    const definition = resourceDefinitionMap.value[definitionId || -1]
    const definitionDisplay = truncateText(definition?.display || '', 34)
    const rubroNombre = String(readField(row, 'Rubronombre') || definition?.rubroNombre || '').trim()
    const rubroLabel = normalizeKey(rubroNombre) === 'general' ? '' : truncateText(rubroNombre, 20)
    const title = [definitionDisplay, rubroLabel].filter(Boolean).join(' · ')
    return {
      value: id,
      title: title || (definitionDisplay || `Recurso #${id}`)
    }
  })
  .filter(item => item.value != null)
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

const locationItems = computed(() => locations.value
  .map(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = readField(row, 'Codigo')
    const name = readField(row, 'Nombre')
    const title = code && name ? `${code} · ${name}` : (name || code || `#${id}`)
    return { value: id, title }
  })
  .filter(item => item.value != null)
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

const selectedResource = computed(() => resources.value.find(row => toNumber(readField(row, 'Id')) === toNumber(selectedResourceInstanceId.value)) || null)
const selectedLocation = computed(() => locations.value.find(row => toNumber(readField(row, 'Id')) === toNumber(selectedLocationId.value)) || null)

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

const resourceMap = computed(() => {
  const map = {}
  resources.value.forEach(row => {
    const id = toNumber(readField(row, 'Id'))
    if (id == null) return
    const code = String(readField(row, 'Codigointerno') || '').trim() || `#${id}`
    const estado = String(readField(row, 'Estado') || '').trim()
    const definitionId = toNumber(readField(row, 'Resourcedefinitionid') ?? readField(row, 'ResourceDefinitionId'))
    const definition = resourceDefinitionMap.value[definitionId || -1]
    const definitionDisplay = truncateText(definition?.display || '', 28)
    map[id] = [code, definitionDisplay, normalizeKey(estado) === 'activo' ? '' : estado].filter(Boolean).join(' · ')
  })
  return map
})

const stockRowsByResource = computed(() => {
  const resourceId = toNumber(selectedResourceInstanceId.value)
  if (resourceId == null) return []

  return stock.value
    .filter(row => toNumber(readField(row, 'ResourceInstanceId')) === resourceId)
    .map(row => {
      const locationId = toNumber(readField(row, 'LocationId'))
      return {
        locationId,
        resourceInstanceId: resourceId,
        primaryLabel: locationMap.value[locationId] || `#${locationId}`,
        secondaryLabel: `ID depósito ${locationId ?? '-'}`,
        stockReal: toNumber(readField(row, 'StockReal')) || 0,
        stockReservado: toNumber(readField(row, 'StockReservado')) || 0,
        stockDisponible: toNumber(readField(row, 'StockDisponible')) || 0,
        actionType: 'location'
      }
    })
    .sort((a, b) => a.stockDisponible - b.stockDisponible)
})

const stockRowsByLocationFromBalances = computed(() => {
  const locationId = toNumber(selectedLocationId.value)
  if (locationId == null) return []

  return stock.value
    .filter(row => toNumber(readField(row, 'LocationId')) === locationId)
    .map(row => {
      const resourceInstanceId = toNumber(readField(row, 'ResourceInstanceId'))
      const locationCode = readField(selectedLocation.value, 'Codigo') || ''
      const locationName = readField(selectedLocation.value, 'Nombre') || ''
      const secondaryLabel = `${locationCode} · ${locationName}`.replace(/^ · | · $/g, '') || `Depósito #${locationId}`
      return {
        locationId,
        resourceInstanceId,
        primaryLabel: resourceMap.value[resourceInstanceId || -1] || `#${resourceInstanceId ?? '-'}`,
        secondaryLabel,
        stockReal: toNumber(readField(row, 'StockReal')) || 0,
        stockReservado: toNumber(readField(row, 'StockReservado')) || 0,
        stockDisponible: toNumber(readField(row, 'StockDisponible')) || 0,
        actionType: 'resource'
      }
    })
    .sort((a, b) => a.stockDisponible - b.stockDisponible)
})

const stockRowsByLocationFallback = computed(() => {
  const locationId = toNumber(selectedLocationId.value)
  if (locationId == null) return []
  return stockByLocation.value
    .filter(row => toNumber(row.locationId) === locationId)
    .sort((a, b) => a.stockDisponible - b.stockDisponible)
})

const stockRows = computed(() => {
  if (traceMode.value === 'resource') return stockRowsByResource.value
  if (stockRowsByLocationFromBalances.value.length > 0) return stockRowsByLocationFromBalances.value
  return stockRowsByLocationFallback.value
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
    executedAt: readField(row, 'ExecutedAt') || null,
    movementId: normalizeKey(readField(row, 'EntityName')) === 'movement'
      ? toNumber(readField(row, 'EntityId'))
      : null
  }))
  .sort((a, b) => new Date(b.executedAt || 0).getTime() - new Date(a.executedAt || 0).getTime()))

const depositTimelineRows = computed(() => {
  const locationId = toNumber(selectedLocationId.value)
  if (locationId == null) return []

  return toArray(movements.value)
    .map(row => {
      const id = toNumber(readField(row, 'Id'))
      const sourceLocationId = toNumber(readField(row, 'Sourcelocationid') ?? readField(row, 'SourceLocationId'))
      const targetLocationId = toNumber(readField(row, 'Targetlocationid') ?? readField(row, 'TargetLocationId'))
      const movementType = String(readField(row, 'Movementtype') || readField(row, 'MovementType') || '').trim().toLowerCase()
      const status = String(readField(row, 'Status') || '').trim().toLowerCase()
      const isRelated = sourceLocationId === locationId || targetLocationId === locationId
      return {
        id,
        operationName: `movement.${movementType || 'evento'}`,
        entityName: 'Movement',
        entityId: id,
        result: status,
        message: `${locationMap.value[sourceLocationId || -1] || '—'} → ${locationMap.value[targetLocationId || -1] || '—'}`,
        actor: readField(row, 'Createdby') || readField(row, 'CreatedBy') || 'system',
        executedAt: readField(row, 'Operationat') || readField(row, 'OperationAt') || readField(row, 'Createdat') || readField(row, 'CreatedAt') || null,
        movementId: id,
        isRelated
      }
    })
    .filter(item => item.isRelated && item.id != null)
    .sort((a, b) => new Date(b.executedAt || 0).getTime() - new Date(a.executedAt || 0).getTime())
})

const baseTimelineRows = computed(() => (traceMode.value === 'resource' ? timelineRows.value : depositTimelineRows.value))

const timelineStatusItems = computed(() => {
  const set = new Set(baseTimelineRows.value.map(item => normalizeKey(item.result)).filter(Boolean))
  return Array.from(set)
    .sort((a, b) => a.localeCompare(b, 'es'))
    .map(value => ({ value, title: pretty(value) }))
})

function toDateStart(value) {
  if (!value) return null
  const d = new Date(`${value}T00:00:00`)
  return Number.isNaN(d.getTime()) ? null : d
}

function toDateEnd(value) {
  if (!value) return null
  const d = new Date(`${value}T23:59:59`)
  return Number.isNaN(d.getTime()) ? null : d
}

const timelineRowsFiltered = computed(() => {
  const status = normalizeKey(timelineStatusFilter.value)
  const from = toDateStart(timelineDateFrom.value)
  const to = toDateEnd(timelineDateTo.value)
  return baseTimelineRows.value.filter(item => {
    if (status && normalizeKey(item.result) !== status) return false
    const dt = item.executedAt ? new Date(item.executedAt) : null
    if (from && dt && dt < from) return false
    if (to && dt && dt > to) return false
    return true
  })
})

const kpiCards = computed(() => {
  const totalStock = stockRows.value.reduce((acc, row) => {
    acc.real += row.stockReal
    acc.res += row.stockReservado
    acc.disp += row.stockDisponible
    return acc
  }, { real: 0, res: 0, disp: 0 })

  return [
    { label: 'Eventos', value: timelineRowsFiltered.value.length, icon: 'mdi-history' },
    { label: 'Depósitos con stock', value: stockRows.value.length, icon: 'mdi-warehouse', color: 'teal' },
    { label: 'Stock real', value: formatNumber(totalStock.real), icon: 'mdi-cube-outline', color: 'blue' },
    { label: 'Disponible', value: formatNumber(totalStock.disp), icon: 'mdi-scale-balance', color: totalStock.disp > 0 ? 'green' : 'red' }
  ]
})

const stockHeaders = computed(() => ([
  { title: traceMode.value === 'resource' ? 'Depósito' : 'Recurso', key: 'primaryLabel' },
  { title: 'Disp', key: 'stockDisponible', align: 'end' },
  { title: 'Real', key: 'stockReal', align: 'end' },
  { title: 'Reservado', key: 'stockReservado', align: 'end' },
  { title: 'Acción', key: 'actions', sortable: false, align: 'end' }
]))

const stockNoDataText = computed(() => {
  if (traceMode.value === 'resource') return 'Sin stock distribuido para esta instancia.'
  if (loadingStockByLocation.value) return 'Cargando stock del depósito...'
  return 'Sin stock para el depósito seleccionado.'
})

function openStockRow(row) {
  if (!row) return
  if (row.actionType === 'location' && row.locationId != null) {
    goTo(`/depositos/${row.locationId}`)
    return
  }
  if (row.actionType === 'resource' && row.resourceInstanceId != null) {
    traceMode.value = 'resource'
    selectedResourceInstanceId.value = row.resourceInstanceId
  }
}

async function loadCatalogs() {
  loadingCatalogs.value = true
  error.value = ''
  stockLoadWarning.value = ''

  try {
    const withTimeout = (promise, ms, label) =>
      Promise.race([
        promise,
        new Promise((_, reject) => setTimeout(() => reject(new Error(`Timeout en ${label}`)), ms))
      ])

    const [resourcesRes, resourceDefsRes, locationsRes, stockRes, movementsRes] = await Promise.allSettled([
      withTimeout(runtimeApi.list('resource-instance'), 15000, 'resource-instance'),
      withTimeout(runtimeApi.list('resource-definition'), 15000, 'resource-definition'),
      withTimeout(runtimeApi.list('location'), 15000, 'location'),
      withTimeout(runtimeApi.list('stock-balance'), 25000, 'stock-balance'),
      withTimeout(runtimeApi.list('movement'), 15000, 'movement')
    ])

    const warnings = []
    const takeData = (result, label) => {
      if (result.status === 'fulfilled') return toArray(result.value?.data)
      warnings.push(label)
      return []
    }

    resources.value = takeData(resourcesRes, 'instancias')
    resourceDefinitions.value = takeData(resourceDefsRes, 'tipos de recurso')
    locations.value = takeData(locationsRes, 'depósitos')
    stock.value = takeData(stockRes, 'saldos')
    movements.value = takeData(movementsRes, 'movimientos')

    if (stockRes.status !== 'fulfilled') {
      stockLoadWarning.value = 'No se pudieron leer saldos directos. Se usará contexto operativo por depósito.'
    }

    if (warnings.length > 0) {
      error.value = `Carga parcial: no se pudieron obtener ${warnings.join(', ')}.`
    }
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || 'No se pudo cargar catálogo de trazabilidad.'
  } finally {
    loadingCatalogs.value = false
  }
}

async function loadStockByLocationContext(locationId) {
  const id = toNumber(locationId)
  if (id == null) {
    stockByLocation.value = []
    return
  }

  loadingStockByLocation.value = true
  try {
    const { data } = await runtimeApi.getOpsDepositoContext(id, 200)
    const stockItems = toArray(readField(data, 'StockItems'))
    const locationCode = readField(readField(data, 'Location'), 'Codigo') || readField(selectedLocation.value, 'Codigo') || ''
    const locationName = readField(readField(data, 'Location'), 'Nombre') || readField(selectedLocation.value, 'Nombre') || ''
    const secondaryLabel = `${locationCode} · ${locationName}`.replace(/^ · | · $/g, '') || `Depósito #${id}`

    stockByLocation.value = stockItems.map(row => {
      const resourceInstanceId = toNumber(readField(row, 'ResourceInstanceId'))
      const resourceCode = readField(row, 'ResourceCode')
      const resourceName = readField(row, 'ResourceName')
      const instanceCode = readField(row, 'InstanceCode')
      const estado = readField(row, 'Estado')

      const resourceLabel = [resourceCode, resourceName].filter(Boolean).join(' · ')
      const subLabel = [instanceCode, estado].filter(Boolean).join(' · ')

      return {
        locationId: id,
        resourceInstanceId,
        primaryLabel: resourceLabel || resourceMap.value[resourceInstanceId || -1] || `#${resourceInstanceId ?? '-'}`,
        secondaryLabel: subLabel || secondaryLabel,
        stockReal: toNumber(readField(row, 'StockReal')) || 0,
        stockReservado: toNumber(readField(row, 'StockReservado')) || 0,
        stockDisponible: toNumber(readField(row, 'StockDisponible')) || 0,
        actionType: 'resource'
      }
    })
  } catch (err) {
    const payload = err?.response?.data
    stockByLocation.value = []
    stockLoadWarning.value = payload?.message || payload?.error || 'No se pudo cargar stock por depósito.'
  } finally {
    loadingStockByLocation.value = false
  }
}

async function loadTimeline() {
  error.value = ''

  const resourceId = toNumber(selectedResourceInstanceId.value)
  const locationId = toNumber(selectedLocationId.value)
  if (traceMode.value === 'resource' && resourceId == null) {
    timeline.value = []
    return
  }
  if (traceMode.value === 'location' && locationId == null) return

  loadingTimeline.value = true
  try {
    if (traceMode.value === 'resource') {
      const { data } = await runtimeApi.getResourceTimeline(resourceId)
      timeline.value = toArray(data)
    } else {
      timeline.value = []
      await loadStockByLocationContext(locationId)
    }
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || 'No se pudo cargar timeline de trazabilidad.'
  } finally {
    loadingTimeline.value = false
  }
}

watch(selectedResourceInstanceId, () => {
  if (traceMode.value !== 'resource') return
  if (!selectedResourceInstanceId.value) {
    timeline.value = []
    return
  }
  loadTimeline()
})

watch(selectedLocationId, () => {
  if (traceMode.value !== 'location') return
  loadTimeline()
})

watch(traceMode, mode => {
  timelineStatusFilter.value = null
  timelineDateFrom.value = ''
  timelineDateTo.value = ''
  if (mode === 'resource') {
    selectedLocationId.value = null
    stockByLocation.value = []
    if (selectedResourceInstanceId.value) loadTimeline()
    return
  }
  timeline.value = []
  if (selectedLocationId.value) {
    loadTimeline()
  }
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
