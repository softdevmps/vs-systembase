<template>
  <v-container fluid class="deposito-edit-view">
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="head-wrap">
          <div class="head-icon">
            <v-icon size="24" color="primary">mdi-map-marker-edit-outline</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Editar depósito</h2>
            <div class="text-body-2 text-medium-emphasis">
              Ajusta datos y geolocalización del depósito desde un mapa real.
            </div>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2">
        <v-btn variant="tonal" color="primary" @click="goTo(`/depositos/${locationId}`)">
          <v-icon start>mdi-arrow-left</v-icon>
          Volver al contexto
        </v-btn>
        <v-btn variant="text" color="primary" :loading="loading" @click="loadData">
          <v-icon start>mdi-refresh</v-icon>
          Recargar
        </v-btn>
      </v-col>
    </v-row>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">{{ error }}</v-alert>
    <v-alert v-if="success" type="success" variant="tonal" class="mb-4">{{ success }}</v-alert>

    <v-row dense>
      <v-col cols="12" lg="5">
        <v-card class="panel-card">
          <v-card-title>Datos del depósito</v-card-title>
          <v-divider />
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="6" lg="12">
                <v-text-field v-model="form.codigo" label="Código" variant="outlined" density="comfortable" />
              </v-col>
              <v-col cols="12" md="6" lg="12">
                <v-text-field v-model="form.nombre" label="Nombre" variant="outlined" density="comfortable" />
              </v-col>
              <v-col cols="12" md="6" lg="12">
                <v-select
                  v-model="form.tipo"
                  :items="tipoItems"
                  label="Tipo"
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="6" lg="12">
                <v-select
                  v-model="form.parentLocationId"
                  :items="parentItems"
                  item-title="title"
                  item-value="value"
                  label="Padre (opcional)"
                  clearable
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="6" lg="12">
                <v-text-field
                  v-model.number="form.capacidad"
                  label="Capacidad"
                  type="number"
                  min="0"
                  step="0.01"
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="6" lg="12">
                <v-switch v-model="form.isActive" label="Activo" color="primary" hide-details />
              </v-col>

              <v-col cols="12" md="6" lg="12">
                <v-text-field
                  v-model.number="form.lat"
                  label="Latitud"
                  type="number"
                  step="0.000001"
                  min="-90"
                  max="90"
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="6" lg="12">
                <v-text-field
                  v-model.number="form.lng"
                  label="Longitud"
                  type="number"
                  step="0.000001"
                  min="-180"
                  max="180"
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>

              <v-col cols="12">
                <div class="d-flex ga-2 flex-wrap">
                  <v-btn color="primary" :loading="saving" @click="submitUpdate">
                    <v-icon start>mdi-content-save-outline</v-icon>
                    Guardar cambios
                  </v-btn>
                  <v-btn color="error" variant="tonal" :loading="deleting" @click="submitDelete">
                    <v-icon start>mdi-delete-outline</v-icon>
                    Eliminar depósito
                  </v-btn>
                </div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" lg="7">
        <v-card class="panel-card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-crosshairs-gps</v-icon>
              Geolocalización
            </div>
            <v-chip size="small" variant="tonal">Click en mapa o arrastra el pin azul</v-chip>
          </v-card-title>
          <v-divider />

          <v-card-text>
            <div class="map-stage">
              <div ref="mapContainerRef" class="leaflet-map" />
            </div>

            <div class="map-help mt-3">
              Íconos verdes: depósitos existentes. Punto azul: ubicación actual del depósito.
            </div>

            <div class="d-flex ga-2 mt-3">
              <v-btn variant="tonal" color="primary" @click="setCordobaCenter">
                Usar Córdoba centro
              </v-btn>
              <v-btn v-if="osmLink" variant="text" color="primary" :href="osmLink" target="_blank" rel="noopener">
                Ver punto en OpenStreetMap
              </v-btn>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import runtimeApi from '../../api/runtime.service'
import { createWarehouseMarkerIcon } from '../../utils/leaflet-markers'

const route = useRoute()
const router = useRouter()

const loading = ref(false)
const saving = ref(false)
const deleting = ref(false)
const error = ref('')
const success = ref('')

const parentLocations = ref([])
const mapResponse = ref({})

const mapContainerRef = ref(null)
let mapInstance = null
let existingLayer = null
let draftMarker = null

const form = ref(buildDefaultForm())

function buildDefaultForm() {
  return {
    codigo: '',
    nombre: '',
    tipo: 'deposito',
    parentLocationId: null,
    capacidad: null,
    isActive: true,
    lat: -31.416667,
    lng: -64.183333
  }
}

const tipoItems = ['deposito', 'almacen', 'hub', 'sucursal', 'nodo', 'posicion']
const locationId = computed(() => Number(route.params.locationId || 0))

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

function goTo(path) {
  if (!path) return
  router.push(path)
}

const parentItems = computed(() => parentLocations.value
  .map(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = readField(row, 'Codigo')
    const name = readField(row, 'Nombre')
    const title = code && name ? `${code} · ${name}` : (name || code || `#${id}`)
    return { value: id, title }
  })
  .filter(item => item.value != null && item.value !== locationId.value)
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

const existingMarkers = computed(() => toArray(readField(mapResponse.value, 'Depositos'))
  .map(row => ({
    locationId: toNumber(readField(row, 'LocationId')),
    lat: toNumber(readField(row, 'Lat')),
    lng: toNumber(readField(row, 'Lng'))
  }))
  .filter(item => item.locationId != null && item.lat != null && item.lng != null && item.locationId !== locationId.value))

const osmLink = computed(() => {
  const lat = toNumber(form.value.lat)
  const lng = toNumber(form.value.lng)
  if (lat == null || lng == null) return ''
  return `https://www.openstreetmap.org/?mlat=${lat.toFixed(6)}&mlon=${lng.toFixed(6)}#map=17/${lat.toFixed(6)}/${lng.toFixed(6)}`
})

function ensureMap() {
  if (mapInstance || !mapContainerRef.value) return

  mapInstance = L.map(mapContainerRef.value, {
    zoomControl: true,
    attributionControl: true
  }).setView([form.value.lat, form.value.lng], 8)

  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; OpenStreetMap contributors'
  }).addTo(mapInstance)

  existingLayer = L.layerGroup().addTo(mapInstance)

  mapInstance.on('click', event => {
    form.value.lat = Number(event.latlng.lat.toFixed(6))
    form.value.lng = Number(event.latlng.lng.toFixed(6))
  })
}

function updateExistingMarkers() {
  if (!existingLayer) return
  existingLayer.clearLayers()

  existingMarkers.value.forEach(item => {
    L.marker([item.lat, item.lng], {
      icon: createWarehouseMarkerIcon({
        color: '#16a34a',
        size: 18
      })
    }).addTo(existingLayer)
  })
}

function createDraftIcon() {
  return L.divIcon({
    className: 'draft-pin-wrapper',
    html: '<div class="draft-pin"></div>',
    iconSize: [16, 16],
    iconAnchor: [8, 8]
  })
}

function updateDraftMarker() {
  if (!mapInstance) return

  const lat = toNumber(form.value.lat)
  const lng = toNumber(form.value.lng)
  if (lat == null || lng == null) return

  if (!draftMarker) {
    draftMarker = L.marker([lat, lng], {
      draggable: true,
      icon: createDraftIcon()
    }).addTo(mapInstance)

    draftMarker.on('dragend', event => {
      const pos = event.target.getLatLng()
      form.value.lat = Number(pos.lat.toFixed(6))
      form.value.lng = Number(pos.lng.toFixed(6))
    })
  } else {
    draftMarker.setLatLng([lat, lng])
  }
}

function fitToExisting() {
  if (!mapInstance || !existingMarkers.value.length) return
  const bounds = L.latLngBounds(existingMarkers.value.map(item => [item.lat, item.lng]))
  if (bounds.isValid()) {
    mapInstance.fitBounds(bounds.pad(0.2), { animate: false })
  }
}

function setCordobaCenter() {
  form.value.lat = -31.416667
  form.value.lng = -64.183333
}

function validate() {
  if (!locationId.value) return 'Depósito inválido.'
  if (!String(form.value.codigo || '').trim()) return 'Código es requerido.'
  if (!String(form.value.nombre || '').trim()) return 'Nombre es requerido.'
  const lat = toNumber(form.value.lat)
  const lng = toNumber(form.value.lng)
  if (lat == null || lat < -90 || lat > 90) return 'Latitud inválida.'
  if (lng == null || lng < -180 || lng > 180) return 'Longitud inválida.'
  return ''
}

async function submitUpdate() {
  error.value = ''
  success.value = ''

  const msg = validate()
  if (msg) {
    error.value = msg
    return
  }

  const payload = {
    codigo: String(form.value.codigo || '').trim(),
    nombre: String(form.value.nombre || '').trim(),
    tipo: String(form.value.tipo || 'deposito').trim(),
    parentLocationId: toNumber(form.value.parentLocationId),
    capacidad: toNumber(form.value.capacidad),
    isActive: Boolean(form.value.isActive),
    lat: Number(form.value.lat),
    lng: Number(form.value.lng)
  }

  saving.value = true
  try {
    await runtimeApi.updateOpsDeposito(locationId.value, payload)
    success.value = `Depósito ${payload.codigo} actualizado.`
    await loadData()
  } catch (err) {
    const payloadErr = err?.response?.data
    error.value = payloadErr?.message || payloadErr?.error || (typeof payloadErr === 'string' ? payloadErr : 'No se pudo actualizar el depósito.')
  } finally {
    saving.value = false
  }
}

async function submitDelete() {
  error.value = ''
  success.value = ''

  if (!locationId.value) {
    error.value = 'Depósito inválido.'
    return
  }

  const code = String(form.value.codigo || `#${locationId.value}`).trim()
  const confirmed = window.confirm(
    `¿Eliminar el depósito ${code}?\n\nSe desactivará y dejará de aparecer en la red logística.`
  )
  if (!confirmed) return

  deleting.value = true
  try {
    await runtimeApi.deleteOpsDeposito(locationId.value)
    success.value = `Depósito ${code} eliminado.`
    setTimeout(() => goTo('/depositos'), 400)
  } catch (err) {
    const payloadErr = err?.response?.data
    error.value = payloadErr?.message || payloadErr?.error || (typeof payloadErr === 'string' ? payloadErr : 'No se pudo eliminar el depósito.')
  } finally {
    deleting.value = false
  }
}

function applyLocationToForm(location) {
  form.value.codigo = readField(location, 'Codigo') || ''
  form.value.nombre = readField(location, 'Nombre') || ''
  form.value.tipo = readField(location, 'Tipo') || 'deposito'
  form.value.parentLocationId = toNumber(readField(location, 'ParentLocationId'))
  form.value.capacidad = toNumber(readField(location, 'Capacidad'))
  form.value.isActive = Boolean(readField(location, 'IsActive'))
  form.value.lat = toNumber(readField(location, 'Lat')) ?? -31.416667
  form.value.lng = toNumber(readField(location, 'Lng')) ?? -64.183333
}

async function loadData() {
  loading.value = true
  error.value = ''

  try {
    const [locationsRes, mapRes, contextRes] = await Promise.all([
      runtimeApi.list('location'),
      runtimeApi.getOpsDepositosMapa(),
      runtimeApi.getOpsDepositoContext(locationId.value, 1)
    ])

    parentLocations.value = toArray(locationsRes?.data)
    mapResponse.value = mapRes?.data || {}

    const location = readField(contextRes?.data, 'Location') || {}
    applyLocationToForm(location)

    await nextTick()
    ensureMap()
    updateExistingMarkers()
    fitToExisting()
    updateDraftMarker()
    mapInstance?.invalidateSize()
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || 'No se pudo cargar datos de edición de depósito.'
  } finally {
    loading.value = false
  }
}

watch(existingMarkers, () => {
  updateExistingMarkers()
}, { deep: true })

watch(() => [form.value.lat, form.value.lng], () => {
  updateDraftMarker()
})

watch(() => route.params.locationId, () => {
  loadData()
})

onMounted(async () => {
  await nextTick()
  ensureMap()
  updateDraftMarker()
  await loadData()
})

onBeforeUnmount(() => {
  if (mapInstance) {
    mapInstance.remove()
    mapInstance = null
  }
  existingLayer = null
  draftMarker = null
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

.panel-card {
  border: 1px solid var(--sb-border-soft);
  border-radius: 14px;
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

.map-help {
  font-size: 0.82rem;
  color: var(--sb-text-soft, #64748b);
}

.deposito-edit-view :deep(.draft-pin-wrapper) {
  background: transparent;
  border: none;
}

.deposito-edit-view :deep(.draft-pin) {
  width: 14px;
  height: 14px;
  border-radius: 999px;
  background: #2563eb;
  border: 2px solid #ffffff;
  box-shadow: 0 0 0 2px rgba(37,99,235,0.2);
}

.deposito-edit-view :deep(.v-data-table th),
.deposito-edit-view :deep(.v-data-table td),
.deposito-edit-view :deep(.v-data-table .v-data-table__th),
.deposito-edit-view :deep(.v-data-table .v-data-table__td),
.deposito-edit-view :deep(.v-card-title),
.deposito-edit-view :deep(.v-label),
.deposito-edit-view :deep(.v-field__input) {
  color: var(--sb-text, #0f172a) !important;
}

@media (max-width: 960px) {
  .map-stage {
    height: 300px;
  }
}
</style>
