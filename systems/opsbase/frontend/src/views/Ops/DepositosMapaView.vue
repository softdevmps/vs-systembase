<template>
  <v-container fluid class="depositos-view">
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="head-wrap">
          <div class="head-icon">
            <v-icon size="24" color="primary">mdi-map-marker-multiple-outline</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Red logística</h2>
            <div class="text-body-2 text-medium-emphasis">
              Mapa operativo de depósitos con stock, pendientes y acceso al contexto de cada nodo.
            </div>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2">
        <v-btn variant="tonal" color="primary" @click="goTo('/home')">
          <v-icon start>mdi-monitor-dashboard</v-icon>
          Centro operativo
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
      <v-col cols="12" sm="6" md="2" v-for="card in kpiCards" :key="card.label">
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
      <v-col cols="12" lg="8">
        <v-card class="map-card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-map-outline</v-icon>
              Mapa de depósitos
            </div>
            <v-chip size="small" :color="usesSynthetic ? 'orange' : 'green'" variant="tonal">
              {{ usesSynthetic ? 'Con coordenadas sintéticas' : 'Con coordenadas reales' }}
            </v-chip>
          </v-card-title>
          <v-divider />

          <v-card-text>
            <div class="map-stage">
              <div ref="mapContainerRef" class="leaflet-map" />
            </div>

            <v-alert v-if="usesSynthetic" type="warning" variant="tonal" class="mt-3" density="comfortable">
              Algunos depósitos aún no tienen geolocalización real en DB. Mostramos una coordenada de respaldo para mantener visible la red.
            </v-alert>

            <div v-if="selectedLocation" class="selected-box mt-3">
              <div>
                <div class="selected-title">{{ selectedLocation.codigo }} · {{ selectedLocation.nombre }}</div>
                <div class="selected-meta">
                  {{ selectedLocation.tipo }} · Disp: {{ formatNumber(selectedLocation.stockDisponible) }} · Pend: {{ selectedLocation.pendingMovements }}
                </div>
              </div>
              <div class="d-flex ga-2">
                <v-btn size="small" color="primary" variant="tonal" @click="openContext(selectedLocation.locationId)">
                  Abrir contexto
                </v-btn>
                <v-btn
                  size="small"
                  variant="text"
                  color="primary"
                  :href="buildOsmLink(selectedLocation)"
                  target="_blank"
                  rel="noopener"
                >
                  Ver mapa real
                </v-btn>
              </div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" lg="4">
        <v-card class="list-card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-warehouse</v-icon>
              Depósitos
            </div>
            <v-chip size="small" variant="tonal">{{ depositos.length }}</v-chip>
          </v-card-title>
          <v-divider />

          <v-data-table
            class="depositos-table"
            :headers="tableHeaders"
            :items="depositos"
            :loading="loading"
            :items-per-page="8"
            density="comfortable"
            no-data-text="No hay depósitos para mostrar."
            @click:row="(_, row) => selectLocation(row?.item?.locationId)"
          >
            <template #item.nombre="{ item }">
              <div class="table-name">{{ item.codigo }} · {{ item.nombre }}</div>
              <div class="table-sub">{{ item.tipo }}</div>
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
              <v-btn size="x-small" variant="text" color="primary" @click.stop="openContext(item.locationId)">
                Abrir
              </v-btn>
            </template>
          </v-data-table>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import runtimeApi from '../../api/runtime.service'
import { createWarehouseMarkerIcon } from '../../utils/leaflet-markers'

const router = useRouter()

const loading = ref(false)
const error = ref('')

const response = ref({})
const selectedLocationId = ref(null)

const mapContainerRef = ref(null)
let mapInstance = null
let markersLayer = null
let hasAutoFit = false

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

function selectLocation(locationId) {
  selectedLocationId.value = locationId
}

function normalizeDeposito(row) {
  return {
    locationId: toNumber(readField(row, 'LocationId')),
    codigo: readField(row, 'Codigo') || '—',
    nombre: readField(row, 'Nombre') || 'Sin nombre',
    tipo: readField(row, 'Tipo') || 'n/a',
    stockDisponible: toNumber(readField(row, 'StockDisponible')) || 0,
    stockReal: toNumber(readField(row, 'StockReal')) || 0,
    stockReservado: toNumber(readField(row, 'StockReservado')) || 0,
    pendingMovements: toNumber(readField(row, 'PendingMovements')) || 0,
    confirmedToday: toNumber(readField(row, 'ConfirmedToday')) || 0,
    lat: toNumber(readField(row, 'Lat')),
    lng: toNumber(readField(row, 'Lng')),
    coordinateMode: (readField(row, 'CoordinateMode') || 'db').toString().toLowerCase()
  }
}

const kpis = computed(() => {
  const source = readField(response.value, 'Kpis') || {}
  return {
    totalDepositos: toNumber(readField(source, 'TotalDepositos')) || 0,
    activos: toNumber(readField(source, 'TotalActivos')) || 0,
    coordsReales: toNumber(readField(source, 'ConCoordenadasReales')) || 0,
    stockCritico: toNumber(readField(source, 'StockCritico')) || 0,
    pendientes: toNumber(readField(source, 'Pendientes')) || 0,
    stockDisponibleTotal: toNumber(readField(source, 'StockDisponibleTotal')) || 0
  }
})

const usesSynthetic = computed(() => Boolean(readField(response.value, 'UsesSyntheticCoordinates')))

const depositos = computed(() => toArray(readField(response.value, 'Depositos'))
  .map(normalizeDeposito)
  .filter(item => item.locationId != null && item.lat != null && item.lng != null))

const selectedLocation = computed(() => {
  if (!depositos.value.length) return null
  const found = depositos.value.find(item => item.locationId === selectedLocationId.value)
  return found || depositos.value[0]
})

function markerColor(item) {
  if ((item.stockDisponible || 0) <= 0) return '#dc2626'
  if ((item.pendingMovements || 0) > 0) return '#f59e0b'
  return '#16a34a'
}

function buildOsmLink(item) {
  const lat = toNumber(item?.lat)
  const lng = toNumber(item?.lng)
  if (lat == null || lng == null) return '#'
  return `https://www.openstreetmap.org/?mlat=${lat.toFixed(6)}&mlon=${lng.toFixed(6)}#map=17/${lat.toFixed(6)}/${lng.toFixed(6)}`
}

const kpiCards = computed(() => [
  { label: 'Depósitos', value: kpis.value.totalDepositos, icon: 'mdi-warehouse' },
  { label: 'Activos', value: kpis.value.activos, icon: 'mdi-check-circle-outline', color: 'green' },
  { label: 'Coords reales', value: kpis.value.coordsReales, icon: 'mdi-map-marker-check-outline', color: 'teal' },
  { label: 'Pendientes', value: kpis.value.pendientes, icon: 'mdi-timer-sand', color: 'orange' },
  { label: 'Stock crítico', value: kpis.value.stockCritico, icon: 'mdi-alert-outline', color: kpis.value.stockCritico > 0 ? 'red' : 'green' },
  { label: 'Stock disponible', value: formatNumber(kpis.value.stockDisponibleTotal), icon: 'mdi-scale-balance', color: 'primary' }
])

const tableHeaders = [
  { title: 'Depósito', key: 'nombre', sortable: false },
  { title: 'Stock disp', key: 'stockDisponible', align: 'end' },
  { title: 'Pend.', key: 'pendingMovements', align: 'center' },
  { title: 'Acción', key: 'actions', sortable: false, align: 'end' }
]

function ensureMap() {
  if (mapInstance || !mapContainerRef.value) return

  mapInstance = L.map(mapContainerRef.value, {
    zoomControl: true,
    attributionControl: true
  }).setView([-31.416667, -64.183333], 6)

  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; OpenStreetMap contributors'
  }).addTo(mapInstance)

  markersLayer = L.layerGroup().addTo(mapInstance)
}

function refreshMarkers() {
  if (!mapInstance || !markersLayer) return

  markersLayer.clearLayers()

  depositos.value.forEach(item => {
    const isSelected = item.locationId === selectedLocationId.value
    const marker = L.marker([item.lat, item.lng], {
      icon: createWarehouseMarkerIcon({
        color: markerColor(item),
        size: isSelected ? 28 : 24,
        selected: isSelected
      }),
      zIndexOffset: isSelected ? 600 : 0
    })

    marker.bindTooltip(`${item.codigo} · ${item.nombre}`)
    marker.on('click', () => selectLocation(item.locationId))
    marker.addTo(markersLayer)
  })
}

function fitToData(force = false) {
  if (!mapInstance || !depositos.value.length) return
  if (!force && hasAutoFit) return

  const bounds = L.latLngBounds(depositos.value.map(item => [item.lat, item.lng]))
  if (bounds.isValid()) {
    mapInstance.fitBounds(bounds.pad(0.2), { animate: false })
    hasAutoFit = true
  }
}

function focusSelected() {
  if (!mapInstance || !selectedLocation.value) return
  mapInstance.setView([selectedLocation.value.lat, selectedLocation.value.lng], Math.max(mapInstance.getZoom(), 11))
}

async function loadData() {
  loading.value = true
  error.value = ''

  try {
    const { data } = await runtimeApi.getOpsDepositosMapa()
    response.value = data || {}

    const first = depositos.value[0]
    if (selectedLocationId.value == null && first?.locationId != null) {
      selectedLocationId.value = first.locationId
    }

    await nextTick()
    ensureMap()
    refreshMarkers()
    fitToData(true)
    mapInstance?.invalidateSize()
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || (typeof payload === 'string' ? payload : 'No se pudo cargar el mapa de depósitos.')
  } finally {
    loading.value = false
  }
}

watch(depositos, async () => {
  await nextTick()
  ensureMap()
  refreshMarkers()
  fitToData()
}, { deep: true })

watch(selectedLocationId, () => {
  refreshMarkers()
  focusSelected()
})

onMounted(async () => {
  await nextTick()
  ensureMap()
  await loadData()
})

onBeforeUnmount(() => {
  if (mapInstance) {
    mapInstance.remove()
    mapInstance = null
  }
  markersLayer = null
})
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
.map-card,
.list-card {
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

.map-stage {
  border: 1px dashed var(--sb-border-soft);
  border-radius: 12px;
  height: 420px;
  overflow: hidden;
}

.leaflet-map {
  width: 100%;
  height: 100%;
}

.selected-box {
  border: 1px solid var(--sb-border-soft);
  border-radius: 12px;
  padding: 10px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.selected-title {
  font-weight: 600;
}

.selected-meta,
.table-sub {
  font-size: 0.82rem;
  color: var(--sb-text-soft, #64748b);
}

.table-name {
  font-weight: 600;
}

.depositos-view :deep(.v-data-table th),
.depositos-view :deep(.v-data-table td),
.depositos-view :deep(.v-data-table .v-data-table__th),
.depositos-view :deep(.v-data-table .v-data-table__td),
.depositos-view :deep(.v-card-title),
.depositos-view :deep(.v-label),
.depositos-view :deep(.v-field__input) {
  color: var(--sb-text, #0f172a) !important;
}

@media (max-width: 960px) {
  .map-stage {
    height: 320px;
  }
}
</style>
