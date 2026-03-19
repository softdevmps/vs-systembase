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
            <div class="d-flex align-center ga-2">
              <v-chip size="small" variant="tonal">{{ filteredStockItems.length }} / {{ stockItems.length }} ítems</v-chip>
              <v-btn size="small" color="primary" variant="tonal" @click="openNewStockDialog">
                <v-icon start>mdi-plus</v-icon>
                Nuevo ítem
              </v-btn>
            </div>
          </v-card-title>
          <v-divider />

          <v-card-text class="pb-0">
            <v-row dense>
              <v-col cols="12" md="5">
                <v-text-field
                  v-model="stockSearch"
                  label="Buscar recurso o instancia"
                  variant="outlined"
                  density="comfortable"
                  clearable
                  prepend-inner-icon="mdi-magnify"
                />
              </v-col>
              <v-col cols="12" md="3">
                <v-select
                  v-model="stockEstadoFilter"
                  :items="stockEstadoItems"
                  label="Estado"
                  variant="outlined"
                  density="comfortable"
                  clearable
                />
              </v-col>
              <v-col cols="12" md="2" class="d-flex align-center">
                <v-switch
                  v-model="stockOnlyCritical"
                  label="Solo críticos"
                  color="error"
                  hide-details
                  inset
                />
              </v-col>
              <v-col cols="12" md="2" class="d-flex align-center justify-md-end">
                <v-chip size="small" :color="stockCriticalCount > 0 ? 'red' : 'green'" variant="tonal">
                  Críticos: {{ stockCriticalCount }}
                </v-chip>
              </v-col>
            </v-row>
          </v-card-text>

          <v-data-table
            class="ops-table"
            :headers="stockHeaders"
            :items="filteredStockItems"
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
            <template #item.estado="{ item }">
              <v-chip size="x-small" :color="estadoColor(item.estado)" variant="tonal">
                {{ pretty(item.estado) }}
              </v-chip>
            </template>
            <template #item.stockReal="{ item }">{{ formatNumber(item.stockReal) }}</template>
            <template #item.stockReservado="{ item }">{{ formatNumber(item.stockReservado) }}</template>
            <template #item.updatedAt="{ item }">{{ formatDate(item.updatedAt) }}</template>
            <template #item.actions="{ item }">
              <div class="stock-actions">
                <v-btn size="small" variant="tonal" color="primary" @click="openAdjustStockDialog(item)">
                  <v-icon start size="16">mdi-tune-variant</v-icon>
                  Ajustar
                </v-btn>
                <v-btn size="small" variant="text" color="error" @click="openDeleteStockDialog(item)">
                  <v-icon start size="16">mdi-delete-outline</v-icon>
                  Eliminar
                </v-btn>
              </div>
            </template>
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
              Rubro: {{ location.rubroNombre || 'Sin rubro' }} · Tipo: {{ location.tipo || 'n/a' }} · Capacidad: {{ formatNumber(location.capacidad) }}
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

    <v-dialog v-model="stockAdjustDialog" max-width="760">
      <v-card class="stock-dialog sb-dialog">
        <div class="sb-dialog-title stock-dialog__header">
          <div class="sb-dialog-icon">
            <v-icon color="primary">mdi-tune-variant</v-icon>
          </div>
          <div>
            <div class="sb-dialog-title-text">Ajustar stock del depósito</div>
            <div class="sb-dialog-subtitle">{{ locationLabel }}</div>
          </div>
          <v-spacer />
          <v-chip size="small" variant="tonal">{{ stockAdjustForm.instanceCode || 'Ítem' }}</v-chip>
        </div>
        <v-divider />
        <v-card-text class="sb-dialog-body stock-dialog__body">
          <v-alert v-if="stockError" type="error" variant="tonal" class="mb-3">{{ stockError }}</v-alert>

          <div class="stock-resource-card mb-3">
            <div class="table-name">{{ stockAdjustForm.resourceCode }} · {{ stockAdjustForm.resourceName }}</div>
            <div class="table-sub">{{ stockAdjustForm.instanceCode }} · {{ stockAdjustForm.estado }}</div>
          </div>

          <v-row dense class="mb-1">
            <v-col cols="12" md="4">
              <div class="stock-metric">
                <span>Real actual</span>
                <strong>{{ formatNumber(stockAdjustForm.originalReal) }}</strong>
              </div>
            </v-col>
            <v-col cols="12" md="4">
              <div class="stock-metric">
                <span>Reservado actual</span>
                <strong>{{ formatNumber(stockAdjustForm.originalReservado) }}</strong>
              </div>
            </v-col>
            <v-col cols="12" md="4">
              <div class="stock-metric stock-metric--accent">
                <span>Disponible actual</span>
                <strong>{{ formatNumber((stockAdjustForm.originalReal || 0) - (stockAdjustForm.originalReservado || 0)) }}</strong>
              </div>
            </v-col>
          </v-row>

          <v-row dense>
            <v-col cols="12" md="6">
              <v-text-field
                v-model.number="stockAdjustForm.stockReal"
                label="Stock real"
                type="number"
                min="0"
                step="0.001"
                variant="outlined"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field
                v-model.number="stockAdjustForm.stockReservado"
                label="Stock reservado"
                type="number"
                min="0"
                step="0.001"
                variant="outlined"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12">
              <div class="stock-result">
                <v-icon size="18" color="primary">mdi-calculator-variant-outline</v-icon>
                <span>Stock disponible resultante: <strong>{{ formatNumber(adjustedStockDisponible) }}</strong></span>
              </div>
            </v-col>
          </v-row>
        </v-card-text>
        <v-divider />
        <v-card-actions class="sb-dialog-actions stock-dialog__actions d-flex justify-end ga-2">
          <v-btn class="sb-btn ghost" variant="text" @click="stockAdjustDialog = false">Cancelar</v-btn>
          <v-btn class="sb-btn primary" color="primary" :loading="stockSaving" @click="saveStockAdjust">
            <v-icon start>mdi-content-save-outline</v-icon>
            Guardar ajuste
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="stockCreateDialog" max-width="640">
      <v-card class="stock-dialog">
        <v-card-title class="d-flex align-center justify-space-between">
          <div class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-plus-box-outline</v-icon>
            Alta de ítem en depósito
          </div>
          <v-chip size="small" variant="tonal">{{ locationLabel }}</v-chip>
        </v-card-title>
        <v-divider />
        <v-card-text>
          <v-alert v-if="stockError" type="error" variant="tonal" class="mb-3">{{ stockError }}</v-alert>
          <v-row dense>
            <v-col cols="12">
              <v-select
                v-model="stockCreateForm.resourceInstanceId"
                :items="resourceInstanceItems"
                item-title="title"
                item-value="value"
                label="Instancia de recurso"
                variant="outlined"
                density="comfortable"
                clearable
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field
                v-model.number="stockCreateForm.stockReal"
                label="Stock real"
                type="number"
                min="0"
                step="0.001"
                variant="outlined"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-text-field
                v-model.number="stockCreateForm.stockReservado"
                label="Stock reservado"
                type="number"
                min="0"
                step="0.001"
                variant="outlined"
                density="comfortable"
              />
            </v-col>
            <v-col cols="12">
              <v-chip size="small" color="primary" variant="tonal">
                Stock disponible inicial: {{ formatNumber(createdStockDisponible) }}
              </v-chip>
            </v-col>
          </v-row>
        </v-card-text>
        <v-divider />
        <v-card-actions class="d-flex justify-end ga-2">
          <v-btn variant="text" @click="stockCreateDialog = false">Cancelar</v-btn>
          <v-btn color="primary" :loading="stockSaving" @click="saveStockCreate">Crear ítem</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="stockDeleteDialog" max-width="560">
      <v-card class="stock-dialog sb-dialog">
        <div class="sb-dialog-title stock-dialog__header">
          <div class="sb-dialog-icon stock-dialog__icon--danger">
            <v-icon color="error">mdi-alert-outline</v-icon>
          </div>
          <div>
            <div class="sb-dialog-title-text">Eliminar ítem de stock</div>
            <div class="sb-dialog-subtitle">Esta acción impacta el saldo del depósito actual.</div>
          </div>
        </div>
        <v-divider />
        <v-card-text class="sb-dialog-body stock-dialog__body">
          <v-alert v-if="stockError" type="error" variant="tonal" class="mb-3">{{ stockError }}</v-alert>
          <div class="stock-resource-card mb-3">
            <div class="table-name mb-1">
            {{ stockDeleteTarget.resourceCode }} · {{ stockDeleteTarget.resourceName }}
            </div>
            <div class="table-sub">{{ stockDeleteTarget.instanceCode }} · {{ stockDeleteTarget.estado }}</div>
          </div>
          <v-alert type="warning" variant="tonal" density="comfortable">
            Esta acción elimina el saldo de este recurso en el depósito actual.
          </v-alert>
        </v-card-text>
        <v-divider />
        <v-card-actions class="sb-dialog-actions stock-dialog__actions d-flex justify-end ga-2">
          <v-btn class="sb-btn ghost" variant="text" @click="stockDeleteDialog = false">Cancelar</v-btn>
          <v-btn class="sb-btn danger" color="error" :loading="stockSaving" @click="confirmDeleteStock">
            <v-icon start>mdi-delete-outline</v-icon>
            Eliminar ítem
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
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
const stockSearch = ref('')
const stockEstadoFilter = ref(null)
const stockOnlyCritical = ref(false)
const stockAdjustDialog = ref(false)
const stockCreateDialog = ref(false)
const stockDeleteDialog = ref(false)
const stockSaving = ref(false)
const stockError = ref('')
const resourceInstances = ref([])
const stockDeleteTarget = ref({
  stockBalanceId: null,
  resourceCode: '',
  resourceName: '',
  instanceCode: '',
  estado: ''
})

const stockAdjustForm = ref({
  stockBalanceId: null,
  resourceInstanceId: null,
  resourceCode: '',
  resourceName: '',
  instanceCode: '',
  estado: '',
  originalReal: 0,
  originalReservado: 0,
  stockReal: 0,
  stockReservado: 0,
  createdAt: null
})

const stockCreateForm = ref({
  resourceInstanceId: null,
  stockReal: 0,
  stockReservado: 0
})

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

function estadoColor(estado) {
  const key = normalizeKey(estado)
  if (key === 'activo') return 'green'
  if (key === 'bloqueado' || key === 'cuarentena' || key === 'baja') return 'red'
  if (key === 'inactivo' || key === 'reparacion') return 'orange'
  return 'grey'
}

const location = computed(() => {
  const src = readField(response.value, 'Location') || {}
  return {
    id: toNumber(readField(src, 'Id')),
    codigo: readField(src, 'Codigo') || '—',
    nombre: readField(src, 'Nombre') || 'Sin nombre',
    tipo: readField(src, 'Tipo') || 'n/a',
    rubroId: toNumber(readField(src, 'RubroId')),
    rubroCodigo: readField(src, 'RubroCodigo') || '',
    rubroNombre: readField(src, 'RubroNombre') || '',
    rubroColorHex: readField(src, 'RubroColorHex') || '',
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
  stockDisponible: toNumber(readField(row, 'StockDisponible')) || 0,
  updatedAt: readField(row, 'UpdatedAt') || null
})))

const stockEstadoItems = computed(() => {
  const unique = new Set()
  stockItems.value.forEach(item => {
    const estado = String(item.estado || '').trim()
    if (estado) unique.add(estado)
  })
  return Array.from(unique).sort((a, b) => String(a).localeCompare(String(b), 'es'))
})

const stockCriticalCount = computed(() =>
  stockItems.value.filter(item => (item.stockDisponible || 0) <= 0).length)

const filteredStockItems = computed(() => {
  const query = String(stockSearch.value || '').trim().toLowerCase()
  const estado = String(stockEstadoFilter.value || '').trim().toLowerCase()
  return stockItems.value.filter(item => {
    if (stockOnlyCritical.value && (item.stockDisponible || 0) > 0) return false
    if (estado && String(item.estado || '').trim().toLowerCase() !== estado) return false
    if (!query) return true
    return String(item.resourceCode || '').toLowerCase().includes(query) ||
      String(item.resourceName || '').toLowerCase().includes(query) ||
      String(item.instanceCode || '').toLowerCase().includes(query)
  })
})

const resourceInstanceItems = computed(() => resourceInstances.value
  .map(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = readField(row, 'CodigoInterno') || `#${id}`
    const estado = readField(row, 'Estado') || ''
    const rubroId = toNumber(readField(row, 'RubroId'))
    const rubroNombre = readField(row, 'RubroNombre') || ''
    return {
      value: id,
      title: estado ? `${code} · ${estado}` : code,
      rubroId,
      rubroNombre
    }
  })
  .filter(item => item.value != null)
  .filter(item => {
    if (location.value.rubroId == null) return true
    if (item.rubroId == null) return true
    return item.rubroId === location.value.rubroId
  })
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

const adjustedStockDisponible = computed(() => {
  const real = toNumber(stockAdjustForm.value.stockReal) ?? 0
  const reservado = toNumber(stockAdjustForm.value.stockReservado) ?? 0
  return real - reservado
})

const createdStockDisponible = computed(() => {
  const real = toNumber(stockCreateForm.value.stockReal) ?? 0
  const reservado = toNumber(stockCreateForm.value.stockReservado) ?? 0
  return real - reservado
})

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
  { title: 'Estado', key: 'estado' },
  { title: 'Disp', key: 'stockDisponible', align: 'end' },
  { title: 'Real', key: 'stockReal', align: 'end' },
  { title: 'Reservado', key: 'stockReservado', align: 'end' },
  { title: 'Actualizado', key: 'updatedAt' },
  { title: 'Acciones', key: 'actions', sortable: false, align: 'end' }
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
    const [contextRes, instancesRes] = await Promise.all([
      runtimeApi.getOpsDepositoContext(locationId, 50),
      runtimeApi.list('resource-instance')
    ])
    const data = contextRes?.data
    response.value = data || {}
    resourceInstances.value = toArray(instancesRes?.data)
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || (typeof payload === 'string' ? payload : 'No se pudo cargar el contexto del depósito.')
  } finally {
    loading.value = false
  }
}

function openAdjustStockDialog(item) {
  if (!item?.stockBalanceId) return
  stockError.value = ''
  stockAdjustForm.value = {
    stockBalanceId: item.stockBalanceId,
    resourceInstanceId: item.resourceInstanceId,
    resourceCode: item.resourceCode,
    resourceName: item.resourceName,
    instanceCode: item.instanceCode,
    estado: item.estado,
    originalReal: item.stockReal,
    originalReservado: item.stockReservado,
    stockReal: item.stockReal,
    stockReservado: item.stockReservado,
    createdAt: null
  }
  stockAdjustDialog.value = true
}

function openDeleteStockDialog(item) {
  if (!item?.stockBalanceId) return
  stockError.value = ''
  stockDeleteTarget.value = {
    stockBalanceId: item.stockBalanceId,
    resourceCode: item.resourceCode,
    resourceName: item.resourceName,
    instanceCode: item.instanceCode,
    estado: item.estado
  }
  stockDeleteDialog.value = true
}

function openNewStockDialog() {
  stockError.value = ''
  stockCreateForm.value = {
    resourceInstanceId: null,
    stockReal: 0,
    stockReservado: 0
  }
  stockCreateDialog.value = true
}

async function saveStockAdjust() {
  stockError.value = ''
  const stockBalanceId = toNumber(stockAdjustForm.value.stockBalanceId)
  const resourceInstanceId = toNumber(stockAdjustForm.value.resourceInstanceId)
  const locationId = toNumber(location.value.id)
  const stockReal = toNumber(stockAdjustForm.value.stockReal)
  const stockReservado = toNumber(stockAdjustForm.value.stockReservado)

  if (!stockBalanceId || !resourceInstanceId || !locationId) {
    stockError.value = 'No se pudo resolver el ítem de stock.'
    return
  }
  if (stockReal == null || stockReal < 0) {
    stockError.value = 'Stock real inválido.'
    return
  }
  if (stockReservado == null || stockReservado < 0) {
    stockError.value = 'Stock reservado inválido.'
    return
  }
  if (stockReservado > stockReal) {
    stockError.value = 'Stock reservado no puede ser mayor a stock real.'
    return
  }

  stockSaving.value = true
  try {
    const existingRes = await runtimeApi.get('stock-balance', stockBalanceId)
    const existing = existingRes?.data || {}
    const createdAt = readField(existing, 'CreatedAt') || readField(existing, 'Createdat') || new Date().toISOString()
    const payload = {
      resourceinstanceid: resourceInstanceId,
      locationid: locationId,
      stockreal: stockReal,
      stockreservado: stockReservado,
      stockdisponible: stockReal - stockReservado,
      createdat: createdAt,
      updatedat: new Date().toISOString()
    }
    await runtimeApi.update('stock-balance', stockBalanceId, payload)
    stockAdjustDialog.value = false
    await loadData()
  } catch (err) {
    const payload = err?.response?.data
    stockError.value = payload?.message || payload?.error || (typeof payload === 'string' ? payload : 'No se pudo guardar el ajuste de stock.')
  } finally {
    stockSaving.value = false
  }
}

async function saveStockCreate() {
  stockError.value = ''
  const resourceInstanceId = toNumber(stockCreateForm.value.resourceInstanceId)
  const locationId = toNumber(location.value.id)
  const stockReal = toNumber(stockCreateForm.value.stockReal)
  const stockReservado = toNumber(stockCreateForm.value.stockReservado)

  if (!resourceInstanceId || !locationId) {
    stockError.value = 'Selecciona instancia y depósito.'
    return
  }
  if (stockReal == null || stockReal < 0) {
    stockError.value = 'Stock real inválido.'
    return
  }
  if (stockReservado == null || stockReservado < 0) {
    stockError.value = 'Stock reservado inválido.'
    return
  }
  if (stockReservado > stockReal) {
    stockError.value = 'Stock reservado no puede ser mayor a stock real.'
    return
  }

  stockSaving.value = true
  try {
    const now = new Date().toISOString()
    await runtimeApi.create('stock-balance', {
      resourceinstanceid: resourceInstanceId,
      locationid: locationId,
      stockreal: stockReal,
      stockreservado: stockReservado,
      stockdisponible: stockReal - stockReservado,
      createdat: now,
      updatedat: now
    })
    stockCreateDialog.value = false
    await loadData()
  } catch (err) {
    const payload = err?.response?.data
    stockError.value = payload?.message || payload?.error || (typeof payload === 'string' ? payload : 'No se pudo crear el ítem de stock.')
  } finally {
    stockSaving.value = false
  }
}

async function confirmDeleteStock() {
  stockError.value = ''
  const stockBalanceId = toNumber(stockDeleteTarget.value.stockBalanceId)
  if (!stockBalanceId) {
    stockError.value = 'No se pudo resolver el ítem a eliminar.'
    return
  }

  stockSaving.value = true
  try {
    await runtimeApi.remove('stock-balance', stockBalanceId)
    stockDeleteDialog.value = false
    stockDeleteTarget.value = {
      stockBalanceId: null,
      resourceCode: '',
      resourceName: '',
      instanceCode: '',
      estado: ''
    }
    await loadData()
  } catch (err) {
    const payload = err?.response?.data
    stockError.value = payload?.message || payload?.error || (typeof payload === 'string' ? payload : 'No se pudo eliminar el ítem de stock.')
  } finally {
    stockSaving.value = false
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

.stock-actions {
  display: flex;
  gap: 6px;
  justify-content: flex-end;
}

.stock-dialog__header {
  align-items: center;
}

.stock-dialog__icon--danger {
  background: color-mix(in srgb, #ef4444 14%, transparent);
}

.stock-dialog__body {
  display: grid;
  gap: 12px;
}

.stock-resource-card {
  border: 1px solid var(--sb-border-soft);
  border-radius: 12px;
  padding: 12px 14px;
  background: color-mix(in srgb, var(--sb-primary-soft, rgba(37,99,235,0.12)) 36%, transparent);
}

.stock-metric {
  border: 1px solid var(--sb-border-soft);
  border-radius: 12px;
  padding: 10px 12px;
  display: grid;
  gap: 2px;
}

.stock-metric span {
  font-size: 0.77rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--sb-text-soft, #64748b);
}

.stock-metric strong {
  font-size: 1rem;
}

.stock-metric--accent {
  background: color-mix(in srgb, var(--sb-primary-soft, rgba(37,99,235,0.12)) 45%, transparent);
}

.stock-result {
  border: 1px dashed color-mix(in srgb, var(--sb-primary) 35%, var(--sb-border-soft));
  border-radius: 12px;
  padding: 10px 12px;
  display: flex;
  gap: 8px;
  align-items: center;
  background: color-mix(in srgb, var(--sb-primary-soft, rgba(37,99,235,0.12)) 26%, transparent);
}

.stock-dialog__actions :deep(.v-btn) {
  min-width: 132px;
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
