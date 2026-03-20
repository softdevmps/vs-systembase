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
        <v-btn variant="tonal" color="primary" @click="goTo('/depositos/listado')">
          <v-icon start>mdi-format-list-bulleted-square</v-icon>
          Listado
        </v-btn>
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
      <v-col cols="12" md="5" lg="4">
        <v-select
          v-model="selectedRubroId"
          :items="rubroFilterItems"
          item-title="title"
          item-value="value"
          label="Filtrar mapa por rubro"
          variant="outlined"
          density="comfortable"
          :loading="loading"
          clearable
        >
          <template #item="{ props, item }">
            <v-list-item v-bind="props">
              <template #prepend>
                <span class="rubro-dot" :style="{ backgroundColor: item.raw.colorHex || '#64748b' }" />
              </template>
            </v-list-item>
          </template>
        </v-select>
      </v-col>
      <v-col cols="12" md="7" lg="8" class="d-flex align-center">
        <v-chip
          size="small"
          variant="tonal"
          :color="selectedRubroId ? 'primary' : 'grey'"
        >
          {{ selectedRubroLabel }}
        </v-chip>
      </v-col>
    </v-row>

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
                  {{ selectedLocation.rubroNombre || 'Sin rubro' }} · {{ selectedLocation.tipo }} · Disp: {{ formatNumber(selectedLocation.stockDisponible) }} · Pend: {{ selectedLocation.pendingMovements }}
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
            @click:row="(_, row) => openDepositoModal(row?.item?.locationId)"
          >
            <template #item.nombre="{ item }">
              <div class="table-name">{{ item.codigo }} · {{ item.nombre }}</div>
              <div class="table-sub">{{ item.rubroNombre || 'Sin rubro' }} · {{ item.tipo }}</div>
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

    <v-dialog v-model="depositoModalOpen" max-width="560">
      <v-card v-if="selectedLocation" class="deposito-modal">
        <v-card-title class="modal-header">
          <div class="modal-header-main">
            <div class="modal-icon">
              <v-icon color="primary" size="24">mdi-warehouse</v-icon>
            </div>
            <div>
              <div class="modal-eyebrow">Depósito</div>
              <div class="modal-code">{{ selectedLocation.codigo }}</div>
              <div class="modal-name">{{ selectedLocation.nombre }}</div>
            </div>
          </div>
          <v-btn icon variant="text" size="small" @click="depositoModalOpen = false">
            <v-icon>mdi-close</v-icon>
          </v-btn>
        </v-card-title>

        <v-card-text class="pt-0">
          <div class="modal-tags">
            <v-chip size="small" color="primary" variant="tonal">{{ selectedLocation.tipo }}</v-chip>
            <v-chip
              size="small"
              variant="tonal"
              :style="{ backgroundColor: withAlpha(selectedLocation.rubroColorHex || '#64748b', 0.14), color: selectedLocation.rubroColorHex || '#334155' }"
            >
              {{ selectedLocation.rubroNombre || 'Sin rubro' }}
            </v-chip>
            <v-chip size="small" :color="selectedLocation.coordinateMode === 'db' ? 'green' : 'orange'" variant="tonal">
              {{ selectedLocation.coordinateMode === 'db' ? 'Coordenadas reales' : 'Coordenadas sintéticas' }}
            </v-chip>
            <v-chip size="small" :color="selectedHealth.color" variant="tonal">
              {{ selectedHealth.label }}
            </v-chip>
          </div>
        </v-card-text>
        <v-divider />
        <v-card-text>
          <v-row dense>
            <v-col cols="6">
              <div class="modal-kpi">
                <div class="modal-kpi-head">
                  <v-icon size="16" color="teal">mdi-scale-balance</v-icon>
                  <span>Stock disponible</span>
                </div>
                <strong>{{ formatNumber(selectedLocation.stockDisponible) }}</strong>
              </div>
            </v-col>
            <v-col cols="6">
              <div class="modal-kpi">
                <div class="modal-kpi-head">
                  <v-icon size="16" color="primary">mdi-database</v-icon>
                  <span>Stock real</span>
                </div>
                <strong>{{ formatNumber(selectedLocation.stockReal) }}</strong>
              </div>
            </v-col>
            <v-col cols="6">
              <div class="modal-kpi">
                <div class="modal-kpi-head">
                  <v-icon size="16" color="orange">mdi-lock-outline</v-icon>
                  <span>Stock reservado</span>
                </div>
                <strong>{{ formatNumber(selectedLocation.stockReservado) }}</strong>
              </div>
            </v-col>
            <v-col cols="6">
              <div class="modal-kpi">
                <div class="modal-kpi-head">
                  <v-icon size="16" :color="selectedLocation.pendingMovements > 0 ? 'orange' : 'green'">mdi-timer-sand</v-icon>
                  <span>Pendientes</span>
                </div>
                <strong>{{ selectedLocation.pendingMovements }}</strong>
              </div>
            </v-col>
          </v-row>

          <div class="modal-coords mt-3">
            <div class="coord-item"><span>Lat</span><strong>{{ formatCoord(selectedLocation.lat) }}</strong></div>
            <div class="coord-item"><span>Lng</span><strong>{{ formatCoord(selectedLocation.lng) }}</strong></div>
          </div>
        </v-card-text>
        <v-divider />
        <v-card-actions class="modal-actions">
          <v-btn variant="text" @click="depositoModalOpen = false">Cerrar</v-btn>
          <v-btn variant="tonal" color="primary" :href="buildOsmLink(selectedLocation)" target="_blank" rel="noopener">
            Ver mapa real
          </v-btn>
          <v-btn color="primary" @click="openContextFromModal(selectedLocation.locationId)">
            Abrir contexto
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
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
const selectedRubroId = ref(null)
const depositoModalOpen = ref(false)

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

function formatCoord(value) {
  const num = toNumber(value)
  if (num == null) return '—'
  return num.toFixed(6)
}

function withAlpha(hex, alpha = 0.16) {
  const safe = String(hex || '').trim()
  const normalized = /^#([0-9a-f]{3}){1,2}$/i.test(safe) ? safe : '#64748b'
  const full = normalized.length === 4
    ? `#${normalized[1]}${normalized[1]}${normalized[2]}${normalized[2]}${normalized[3]}${normalized[3]}`
    : normalized
  const int = parseInt(full.slice(1), 16)
  const r = (int >> 16) & 255
  const g = (int >> 8) & 255
  const b = int & 255
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

function goTo(path) {
  if (!path) return
  router.push(path)
}

function openContext(locationId) {
  if (!locationId) return
  router.push(`/depositos/${locationId}`)
}

function openContextFromModal(locationId) {
  depositoModalOpen.value = false
  openContext(locationId)
}

function selectLocation(locationId) {
  selectedLocationId.value = locationId
}

function openDepositoModal(locationId) {
  if (!locationId) return
  selectedLocationId.value = locationId
  depositoModalOpen.value = true
}

function normalizeDeposito(row) {
  return {
    locationId: toNumber(readField(row, 'LocationId')),
    codigo: readField(row, 'Codigo') || '—',
    nombre: readField(row, 'Nombre') || 'Sin nombre',
    tipo: readField(row, 'Tipo') || 'n/a',
    rubroId: toNumber(readField(row, 'RubroId')),
    rubroCodigo: String(readField(row, 'RubroCodigo') || '').trim(),
    rubroNombre: String(readField(row, 'RubroNombre') || '').trim(),
    rubroColorHex: String(readField(row, 'RubroColorHex') || '').trim(),
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

const rubroFilterItems = computed(() => {
  const rubros = toArray(readField(response.value, 'Rubros'))
    .map(row => {
      const id = toNumber(readField(row, 'Id'))
      const codigo = String(readField(row, 'Codigo') || '').trim()
      const nombre = String(readField(row, 'Nombre') || '').trim()
      const colorHex = String(readField(row, 'ColorHex') || '#64748b').trim()
      const depositos = toNumber(readField(row, 'Depositos')) || 0
      return {
        value: id,
        title: `${nombre || codigo || `#${id}`} (${depositos})`,
        nombre: nombre || codigo || `#${id}`,
        colorHex
      }
    })
    .filter(item => item.value != null)
    .sort((a, b) => String(a.nombre).localeCompare(String(b.nombre), 'es'))

  return [{ value: null, title: 'Todos los rubros', nombre: 'Todos los rubros', colorHex: '#64748b' }, ...rubros]
})

const selectedRubroLabel = computed(() => {
  const current = rubroFilterItems.value.find(item => item.value === toNumber(selectedRubroId.value))
  return current?.title || 'Todos los rubros'
})

const depositos = computed(() => toArray(readField(response.value, 'Depositos'))
  .map(normalizeDeposito)
  .filter(item => item.locationId != null && item.lat != null && item.lng != null))

const selectedLocation = computed(() => {
  if (!depositos.value.length) return null
  const found = depositos.value.find(item => item.locationId === selectedLocationId.value)
  return found || depositos.value[0]
})

const selectedHealth = computed(() => {
  const item = selectedLocation.value
  if (!item) return { label: 'Sin datos', color: 'grey' }
  if ((item.stockDisponible || 0) <= 0) return { label: 'Stock agotado', color: 'red' }
  if ((item.pendingMovements || 0) > 0) return { label: 'Con pendientes', color: 'orange' }
  return { label: 'Operativo', color: 'green' }
})

function markerColor(item) {
  const rubroColor = String(item?.rubroColorHex || '').trim()
  if (/^#([0-9a-f]{3}){1,2}$/i.test(rubroColor)) return rubroColor
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
    marker.on('click', () => openDepositoModal(item.locationId))
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
    const { data } = await runtimeApi.getOpsDepositosMapa(toNumber(selectedRubroId.value))
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

watch(selectedRubroId, async () => {
  selectedLocationId.value = null
  await loadData()
})

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

.deposito-modal {
  border: 1px solid var(--sb-border-soft);
  border-radius: 16px;
  overflow: hidden;
}

.modal-header {
  padding: 16px 16px 10px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 10px;
}

.modal-header-main {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  min-width: 0;
}

.modal-icon {
  width: 40px;
  height: 40px;
  border-radius: 12px;
  background: var(--sb-primary-soft, rgba(37,99,235,0.1));
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.modal-eyebrow {
  font-size: 0.73rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--sb-text-soft, #64748b);
}

.modal-code {
  font-weight: 700;
  font-size: 1.12rem;
  line-height: 1.2;
  word-break: break-word;
}

.modal-name {
  margin-top: 2px;
  color: var(--sb-text-soft, #64748b);
  font-size: 0.92rem;
}

.modal-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.modal-kpi {
  border: 1px solid var(--sb-border-soft);
  border-radius: 10px;
  padding: 8px 10px;
  background: color-mix(in srgb, var(--sb-surface) 94%, transparent);
}

.modal-kpi-head {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 4px;
}

.modal-kpi span {
  font-size: 0.75rem;
  color: var(--sb-text-soft, #64748b);
}

.modal-kpi strong {
  font-size: 1.1rem;
  line-height: 1.2;
}

.modal-coords {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
}

.coord-item {
  border: 1px dashed var(--sb-border-soft);
  border-radius: 9px;
  padding: 6px 8px;
}

.coord-item span {
  display: block;
  font-size: 0.72rem;
  color: var(--sb-text-soft, #64748b);
}

.coord-item strong {
  font-size: 0.9rem;
}

.modal-actions {
  justify-content: flex-end;
  padding: 12px 16px 16px;
  gap: 8px;
  flex-wrap: wrap;
}

.selected-meta,
.table-sub {
  font-size: 0.82rem;
  color: var(--sb-text-soft, #64748b);
}

.table-name {
  font-weight: 600;
}

.rubro-dot {
  width: 10px;
  height: 10px;
  border-radius: 999px;
  display: inline-block;
  border: 1px solid rgba(15, 23, 42, 0.2);
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

  .modal-actions {
    justify-content: stretch;
  }

  .modal-actions :deep(.v-btn) {
    flex: 1 1 100%;
  }

  .modal-coords {
    grid-template-columns: 1fr;
  }
}
</style>
