<template>
  <v-container fluid class="recepcion-view">
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="recepcion-head">
          <div class="recepcion-icon">
            <v-icon color="primary" size="24">mdi-truck-delivery-outline</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Recepción guiada</h2>
            <div class="text-body-2 text-medium-emphasis">
              Carga rápida de ingreso con validaciones de negocio y confirmación opcional.
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
    <v-alert v-if="successMessage" type="success" variant="tonal" class="mb-4">{{ successMessage }}</v-alert>

    <v-card class="recepcion-card">
      <v-stepper v-model="step" :items="stepItems" flat>
        <template #item.1>
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="8">
                <v-select
                  v-model="form.resourceInstanceId"
                  :items="resourceItems"
                  item-title="title"
                  item-value="value"
                  label="Recurso"
                  :loading="loadingCatalogs"
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="form.quantity"
                  label="Cantidad"
                  type="number"
                  min="0.001"
                  step="0.001"
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="form.unitCost"
                  label="Costo unitario"
                  type="number"
                  min="0"
                  step="0.01"
                  variant="outlined"
                  density="comfortable"
                  clearable
                />
              </v-col>
              <v-col cols="12" md="8">
                <v-text-field
                  v-model="form.referenceNo"
                  label="Referencia"
                  hint="Opcional. Si no completas, se genera automáticamente."
                  persistent-hint
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
            </v-row>
          </v-card-text>
        </template>

        <template #item.2>
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="8">
                <v-select
                  v-model="form.targetLocationId"
                  :items="locationItems"
                  item-title="title"
                  item-value="value"
                  label="Ubicación destino"
                  :loading="loadingCatalogs"
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model="form.operationAt"
                  label="Fecha/hora operación"
                  type="datetime-local"
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12">
                <v-textarea
                  v-model="form.notes"
                  label="Notas"
                  rows="3"
                  auto-grow
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12">
                <v-switch
                  v-model="form.confirmNow"
                  label="Confirmar inmediatamente (aplica stock)"
                  color="primary"
                  hide-details
                />
              </v-col>
            </v-row>
          </v-card-text>
        </template>

        <template #item.3>
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="6">
                <v-card class="summary-block">
                  <v-card-title>Resumen</v-card-title>
                  <v-card-text>
                    <div class="summary-row"><span>Recurso</span><strong>{{ selectedResourceLabel || '—' }}</strong></div>
                    <div class="summary-row"><span>Cantidad</span><strong>{{ formatNumber(form.quantity) }}</strong></div>
                    <div class="summary-row"><span>Costo unitario</span><strong>{{ form.unitCost == null ? '—' : formatMoney(form.unitCost) }}</strong></div>
                    <div class="summary-row"><span>Ubicación destino</span><strong>{{ selectedLocationLabel || '—' }}</strong></div>
                    <div class="summary-row"><span>Referencia</span><strong>{{ form.referenceNo || 'Autogenerada' }}</strong></div>
                    <div class="summary-row"><span>Estado final</span><strong>{{ form.confirmNow ? 'confirmado' : 'borrador' }}</strong></div>
                  </v-card-text>
                </v-card>
              </v-col>
              <v-col cols="12" md="6">
                <v-card class="summary-block">
                  <v-card-title>Checks</v-card-title>
                  <v-card-text>
                    <v-list density="compact">
                      <v-list-item>
                        <template #prepend>
                          <v-icon :color="checkResource ? 'green' : 'red'">{{ checkResource ? 'mdi-check-circle' : 'mdi-close-circle' }}</v-icon>
                        </template>
                        <v-list-item-title>Recurso seleccionado</v-list-item-title>
                      </v-list-item>
                      <v-list-item>
                        <template #prepend>
                          <v-icon :color="checkQuantity ? 'green' : 'red'">{{ checkQuantity ? 'mdi-check-circle' : 'mdi-close-circle' }}</v-icon>
                        </template>
                        <v-list-item-title>Cantidad válida (&gt; 0)</v-list-item-title>
                      </v-list-item>
                      <v-list-item>
                        <template #prepend>
                          <v-icon :color="checkLocation ? 'green' : 'red'">{{ checkLocation ? 'mdi-check-circle' : 'mdi-close-circle' }}</v-icon>
                        </template>
                        <v-list-item-title>Ubicación destino seleccionada</v-list-item-title>
                      </v-list-item>
                    </v-list>
                  </v-card-text>
                </v-card>
              </v-col>
            </v-row>
          </v-card-text>
        </template>
      </v-stepper>

      <v-divider />
      <v-card-actions class="justify-space-between pa-4">
        <v-btn variant="text" :disabled="step <= 1" @click="step--">Anterior</v-btn>
        <div class="d-flex ga-2">
          <v-btn v-if="step < 3" color="primary" @click="nextStep">Siguiente</v-btn>
          <v-btn v-else color="primary" :loading="submitting" @click="submitRecepcion">Crear recepción</v-btn>
        </div>
      </v-card-actions>
    </v-card>
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import runtimeApi from '../../api/runtime.service'

const router = useRouter()

const step = ref(1)
const stepItems = [
  { title: 'Recurso y cantidad', value: 1 },
  { title: 'Destino y reglas', value: 2 },
  { title: 'Confirmación', value: 3 }
]

const loadingCatalogs = ref(false)
const submitting = ref(false)
const error = ref('')
const successMessage = ref('')

const resources = ref([])
const locations = ref([])

const form = ref(buildDefaultForm())

function buildDefaultForm() {
  return {
    resourceInstanceId: null,
    targetLocationId: null,
    quantity: 1,
    unitCost: null,
    referenceNo: '',
    notes: '',
    confirmNow: true,
    operationAt: toLocalDateTimeInput(new Date())
  }
}

function toArray(data) {
  return Array.isArray(data) ? data : (Array.isArray(data?.items) ? data.items : [])
}

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

function toLocalDateTimeInput(date) {
  const pad = n => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function parseOperationAt(value) {
  if (!value) return new Date().toISOString()
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return new Date().toISOString()
  return date.toISOString()
}

function toNumber(value) {
  if (value == null || value === '') return null
  const num = Number(String(value).replace(',', '.'))
  return Number.isFinite(num) ? num : null
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

const selectedResourceLabel = computed(() => resourceItems.value.find(item => item.value === toNumber(form.value.resourceInstanceId))?.title || '')
const selectedLocationLabel = computed(() => locationItems.value.find(item => item.value === toNumber(form.value.targetLocationId))?.title || '')

const checkResource = computed(() => toNumber(form.value.resourceInstanceId) != null)
const checkQuantity = computed(() => (toNumber(form.value.quantity) || 0) > 0)
const checkLocation = computed(() => toNumber(form.value.targetLocationId) != null)
const isFormValid = computed(() => checkResource.value && checkQuantity.value && checkLocation.value)

function goTo(path) {
  if (!path) return
  router.push(path)
}

function validateStep(currentStep) {
  if (currentStep === 1) {
    if (!checkResource.value) return 'Selecciona un recurso.'
    if (!checkQuantity.value) return 'La cantidad debe ser mayor a 0.'
  }
  if (currentStep === 2) {
    if (!checkLocation.value) return 'Selecciona ubicación destino.'
  }
  return ''
}

function nextStep() {
  error.value = ''
  const message = validateStep(step.value)
  if (message) {
    error.value = message
    return
  }
  if (step.value < 3) step.value += 1
}

async function loadCatalogs() {
  loadingCatalogs.value = true
  error.value = ''

  try {
    const [resourcesRes, locationsRes] = await Promise.all([
      runtimeApi.list('resource-instance'),
      runtimeApi.list('location')
    ])

    resources.value = toArray(resourcesRes?.data)
    locations.value = toArray(locationsRes?.data)
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || 'No se pudo cargar catálogo de recepción.'
  } finally {
    loadingCatalogs.value = false
  }
}

async function submitRecepcion() {
  error.value = ''
  successMessage.value = ''

  if (!isFormValid.value) {
    error.value = 'Completa los datos requeridos antes de crear la recepción.'
    return
  }

  const payload = {
    resourceinstanceid: toNumber(form.value.resourceInstanceId),
    targetlocationid: toNumber(form.value.targetLocationId),
    quantity: Number(form.value.quantity),
    unitcost: toNumber(form.value.unitCost),
    referenceno: (form.value.referenceNo || '').trim() || null,
    notes: (form.value.notes || '').trim() || null,
    operationat: parseOperationAt(form.value.operationAt),
    confirmar: Boolean(form.value.confirmNow)
  }

  submitting.value = true
  try {
    const { data } = await runtimeApi.createOpsRecepcion(payload)
    const movementId = data?.Movementid ?? data?.movementid
    const status = data?.Status ?? data?.status
    const referenceNo = data?.Referenceno ?? data?.referenceno

    successMessage.value = `Recepción creada (${referenceNo || 'sin referencia'}) en estado ${status || 'borrador'}.`
    form.value = buildDefaultForm()
    step.value = 1

    if (movementId != null) {
      setTimeout(() => {
        goTo(`/movement?focus=${movementId}`)
      }, 900)
    }
  } catch (err) {
    const payloadErr = err?.response?.data
    error.value = payloadErr?.message || payloadErr?.error || (typeof payloadErr === 'string' ? payloadErr : 'No se pudo crear la recepción.')
  } finally {
    submitting.value = false
  }
}

onMounted(loadCatalogs)
</script>

<style scoped>
.recepcion-head {
  display: flex;
  align-items: center;
  gap: 12px;
}

.recepcion-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--sb-primary-soft, rgba(37,99,235,0.1));
}

.recepcion-card {
  border: 1px solid var(--sb-border-soft);
  border-radius: 16px;
}

.summary-block {
  border: 1px solid var(--sb-border-soft);
  border-radius: 14px;
  height: 100%;
}

.summary-row {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  padding: 6px 0;
  border-bottom: 1px dashed color-mix(in srgb, var(--sb-border-soft) 70%, transparent);
}

.summary-row:last-child {
  border-bottom: 0;
}

.recepcion-view :deep(.v-stepper-header) {
  border-bottom: 1px solid var(--sb-border-soft);
}

.recepcion-view :deep(.v-list-item-title),
.recepcion-view :deep(.v-label),
.recepcion-view :deep(.v-field__input),
.recepcion-view :deep(.v-card-title),
.recepcion-view :deep(.v-stepper-item__title),
.recepcion-view :deep(.v-stepper-item__subtitle) {
  color: var(--sb-text, #0f172a) !important;
}
</style>
