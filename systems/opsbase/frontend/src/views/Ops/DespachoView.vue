<template>
  <v-container fluid class="despacho-view">
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="despacho-head">
          <div class="despacho-icon">
            <v-icon color="primary" size="24">mdi-truck-fast-outline</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Despacho guiado</h2>
            <div class="text-body-2 text-medium-emphasis">
              Egreso o transferencia en un flujo simple con validación y confirmación opcional.
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

    <v-card class="despacho-card">
      <v-stepper v-model="step" :items="stepItems" flat>
        <template #item.1>
          <v-card-text>
            <v-row dense>
              <v-col cols="12" md="4">
                <v-select
                  v-model="form.movementType"
                  :items="movementTypeItems"
                  item-title="title"
                  item-value="value"
                  label="Tipo"
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
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
              <v-col cols="12" md="4">
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
              <v-col cols="12" md="6">
                <v-select
                  v-model="form.sourceLocationId"
                  :items="locationItems"
                  item-title="title"
                  item-value="value"
                  label="Ubicación origen"
                  :loading="loadingCatalogs"
                  variant="outlined"
                  density="comfortable"
                />
              </v-col>
              <v-col cols="12" md="6" v-if="requiresTargetLocation">
                <v-select
                  v-model="form.targetLocationId"
                  :items="targetLocationOptions"
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
              <v-col cols="12" md="8">
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
                    <div class="summary-row"><span>Tipo</span><strong>{{ movementTypeLabel }}</strong></div>
                    <div class="summary-row"><span>Recurso</span><strong>{{ selectedResourceLabel || '—' }}</strong></div>
                    <div class="summary-row"><span>Cantidad</span><strong>{{ formatNumber(form.quantity) }}</strong></div>
                    <div class="summary-row"><span>Costo unitario</span><strong>{{ form.unitCost == null ? '—' : formatMoney(form.unitCost) }}</strong></div>
                    <div class="summary-row"><span>Origen</span><strong>{{ selectedSourceLabel || '—' }}</strong></div>
                    <div class="summary-row"><span>Destino</span><strong>{{ requiresTargetLocation ? (selectedTargetLabel || '—') : 'No aplica' }}</strong></div>
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
                          <v-icon :color="checkSource ? 'green' : 'red'">{{ checkSource ? 'mdi-check-circle' : 'mdi-close-circle' }}</v-icon>
                        </template>
                        <v-list-item-title>Ubicación origen seleccionada</v-list-item-title>
                      </v-list-item>
                      <v-list-item>
                        <template #prepend>
                          <v-icon :color="checkTarget ? 'green' : 'red'">{{ checkTarget ? 'mdi-check-circle' : 'mdi-close-circle' }}</v-icon>
                        </template>
                        <v-list-item-title>Ubicación destino válida</v-list-item-title>
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
          <v-btn v-else color="primary" :loading="submitting" @click="submitDespacho">Crear despacho</v-btn>
        </div>
      </v-card-actions>
    </v-card>
  </v-container>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import runtimeApi from '../../api/runtime.service'

const router = useRouter()

const step = ref(1)
const stepItems = [
  { title: 'Tipo, recurso y cantidad', value: 1 },
  { title: 'Ubicaciones y reglas', value: 2 },
  { title: 'Confirmación', value: 3 }
]

const movementTypeItems = [
  { value: 'egreso', title: 'Egreso' },
  { value: 'transferencia', title: 'Transferencia' }
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
    movementType: 'egreso',
    resourceInstanceId: null,
    sourceLocationId: null,
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

const requiresTargetLocation = computed(() => form.value.movementType === 'transferencia')

watch(requiresTargetLocation, enabled => {
  if (!enabled) form.value.targetLocationId = null
})

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

const targetLocationOptions = computed(() => {
  const source = toNumber(form.value.sourceLocationId)
  return locationItems.value.filter(item => item.value !== source)
})

const movementTypeLabel = computed(() => movementTypeItems.find(item => item.value === form.value.movementType)?.title || 'Egreso')
const selectedResourceLabel = computed(() => resourceItems.value.find(item => item.value === toNumber(form.value.resourceInstanceId))?.title || '')
const selectedSourceLabel = computed(() => locationItems.value.find(item => item.value === toNumber(form.value.sourceLocationId))?.title || '')
const selectedTargetLabel = computed(() => locationItems.value.find(item => item.value === toNumber(form.value.targetLocationId))?.title || '')

const checkResource = computed(() => toNumber(form.value.resourceInstanceId) != null)
const checkQuantity = computed(() => (toNumber(form.value.quantity) || 0) > 0)
const checkSource = computed(() => toNumber(form.value.sourceLocationId) != null)
const checkTarget = computed(() => {
  if (!requiresTargetLocation.value) return true
  const source = toNumber(form.value.sourceLocationId)
  const target = toNumber(form.value.targetLocationId)
  return target != null && source != null && target !== source
})

const isFormValid = computed(() => checkResource.value && checkQuantity.value && checkSource.value && checkTarget.value)

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
    if (!checkSource.value) return 'Selecciona ubicación origen.'
    if (!checkTarget.value) return 'Selecciona ubicación destino distinta para transferencia.'
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
    error.value = payload?.message || payload?.error || 'No se pudo cargar catálogo de despacho.'
  } finally {
    loadingCatalogs.value = false
  }
}

async function submitDespacho() {
  error.value = ''
  successMessage.value = ''

  if (!isFormValid.value) {
    error.value = 'Completa los datos requeridos antes de crear el despacho.'
    return
  }

  const payload = {
    movementtype: form.value.movementType,
    resourceinstanceid: toNumber(form.value.resourceInstanceId),
    sourcelocationid: toNumber(form.value.sourceLocationId),
    targetlocationid: requiresTargetLocation.value ? toNumber(form.value.targetLocationId) : null,
    quantity: Number(form.value.quantity),
    unitcost: toNumber(form.value.unitCost),
    referenceno: (form.value.referenceNo || '').trim() || null,
    notes: (form.value.notes || '').trim() || null,
    operationat: parseOperationAt(form.value.operationAt),
    confirmar: Boolean(form.value.confirmNow)
  }

  submitting.value = true
  try {
    const { data } = await runtimeApi.createOpsDespacho(payload)
    const movementId = data?.Movementid ?? data?.movementid
    const status = data?.Status ?? data?.status
    const referenceNo = data?.Referenceno ?? data?.referenceno

    successMessage.value = `Despacho creado (${referenceNo || 'sin referencia'}) en estado ${status || 'borrador'}.`
    form.value = buildDefaultForm()
    step.value = 1

    if (movementId != null) {
      setTimeout(() => {
        goTo(`/movement?focus=${movementId}`)
      }, 900)
    }
  } catch (err) {
    const payloadErr = err?.response?.data
    error.value = payloadErr?.message || payloadErr?.error || (typeof payloadErr === 'string' ? payloadErr : 'No se pudo crear el despacho.')
  } finally {
    submitting.value = false
  }
}

onMounted(loadCatalogs)
</script>

<style scoped>
.despacho-head {
  display: flex;
  align-items: center;
  gap: 12px;
}

.despacho-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--sb-primary-soft, rgba(37,99,235,0.1));
}

.despacho-card {
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

.despacho-view :deep(.v-stepper-header) {
  border-bottom: 1px solid var(--sb-border-soft);
}

.despacho-view :deep(.v-list-item-title),
.despacho-view :deep(.v-label),
.despacho-view :deep(.v-field__input),
.despacho-view :deep(.v-card-title),
.despacho-view :deep(.v-stepper-item__title),
.despacho-view :deep(.v-stepper-item__subtitle) {
  color: var(--sb-text, #0f172a) !important;
}
</style>
