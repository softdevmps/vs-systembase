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
      <v-col cols="auto" class="d-flex ga-2 flex-wrap justify-end">
        <v-chip color="primary" variant="tonal" size="small">Paso 2/6</v-chip>
        <v-btn variant="tonal" color="primary" @click="goTo('/kardex')">
          <v-icon start>mdi-notebook-outline</v-icon>
          Kardex
        </v-btn>
        <v-btn variant="tonal" color="success" @click="goTo('/despacho')">
          <v-icon start>mdi-arrow-right-circle-outline</v-icon>
          Siguiente: Despachar
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
              <v-col cols="12" md="4">
                <v-select
                  v-model="form.rubroId"
                  :items="rubroItems"
                  item-title="title"
                  item-value="value"
                  label="Rubro"
                  :loading="loadingCatalogs"
                  variant="outlined"
                  density="comfortable"
                >
                  <template #item="{ props, item }">
                    <v-list-item v-bind="props">
                      <template #prepend>
                        <span
                          class="rubro-dot"
                          :style="{ backgroundColor: item.raw.colorHex || '#64748b' }"
                        />
                      </template>
                    </v-list-item>
                  </template>
                </v-select>
              </v-col>
              <v-col cols="12" md="8">
                <v-select
                  v-model="form.resourceInstanceId"
                  :items="resourceItems"
                  item-title="title"
                  item-value="value"
                  label="Recurso (instancia)"
                  :loading="loadingCatalogs"
                  :menu-props="{ maxHeight: 420 }"
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
                    <div class="summary-row"><span>Rubro</span><strong>{{ selectedRubroLabel || '—' }}</strong></div>
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
                          <v-icon :color="checkRubro ? 'green' : 'red'">{{ checkRubro ? 'mdi-check-circle' : 'mdi-close-circle' }}</v-icon>
                        </template>
                        <v-list-item-title>Rubro seleccionado</v-list-item-title>
                      </v-list-item>
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

    <v-card class="recepcion-card mt-4">
      <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-2">
        <div class="d-flex align-center ga-2">
          <v-icon color="primary">mdi-history</v-icon>
          <span>Historial de Recepción</span>
        </div>
        <div class="d-flex align-center ga-2">
          <v-chip size="small" variant="tonal">Mostrando {{ filteredHistory.length }}</v-chip>
          <v-btn size="small" variant="text" color="primary" :loading="loadingCatalogs" @click="loadCatalogs">
            <v-icon start>mdi-refresh</v-icon>
            Actualizar
          </v-btn>
        </div>
      </v-card-title>
      <v-divider />
      <v-card-text class="pb-0">
        <v-row dense>
          <v-col cols="12" md="5">
            <v-text-field
              v-model="historySearch"
              variant="outlined"
              density="comfortable"
              label="Buscar referencia o depósito"
              clearable
              prepend-inner-icon="mdi-magnify"
            />
          </v-col>
          <v-col cols="12" md="3">
            <v-select
              v-model="historyStatusFilter"
              :items="historyStatusItems"
              item-title="title"
              item-value="value"
              variant="outlined"
              density="comfortable"
              label="Estado"
              clearable
            />
          </v-col>
          <v-col cols="6" md="2">
            <v-text-field
              v-model="historyDateFrom"
              variant="outlined"
              density="comfortable"
              label="Desde"
              type="date"
            />
          </v-col>
          <v-col cols="6" md="2">
            <v-text-field
              v-model="historyDateTo"
              variant="outlined"
              density="comfortable"
              label="Hasta"
              type="date"
            />
          </v-col>
        </v-row>
      </v-card-text>
      <v-divider class="mt-2" />
      <v-data-table
        class="history-table"
        :headers="historyHeaders"
        :items="filteredHistory"
        :loading="loadingCatalogs"
        :items-per-page="8"
        density="comfortable"
        no-data-text="No hay recepciones registradas."
      >
        <template #item.referenceNo="{ item }">
          <div class="history-reference">
            <strong>{{ item.referenceNo }}</strong>
            <div class="history-reference__sub">#{{ item.id }}</div>
          </div>
        </template>
        <template #item.status="{ item }">
          <v-chip size="x-small" :color="historyStatusColor(item.status)" variant="tonal">
            {{ formatStatus(item.status) || '—' }}
          </v-chip>
        </template>
        <template #item.quantity="{ item }">{{ formatNumber(item.quantity) }}</template>
        <template #item.operationAt="{ item }">{{ formatDate(item.operationAt) }}</template>
        <template #item.targetLabel="{ item }">
          <span class="history-route">{{ item.targetLabel }}</span>
        </template>
        <template #item.actions="{ item }">
          <div class="history-actions">
            <v-btn
              v-if="canConfirm(item)"
              size="x-small"
              variant="text"
              color="green"
              :loading="Boolean(rowActionLoading[item.id])"
              @click="updateMovementStatus(item, 'confirmado')"
            >
              Confirmar
            </v-btn>
            <v-btn
              v-if="canCancel(item)"
              size="x-small"
              variant="text"
              color="error"
              :loading="Boolean(rowActionLoading[item.id])"
              @click="updateMovementStatus(item, 'anulado')"
            >
              Anular
            </v-btn>
            <v-btn
              v-if="canRetry(item)"
              size="x-small"
              variant="text"
              color="orange"
              :loading="Boolean(rowActionLoading[item.id])"
              @click="retryMovement(item)"
            >
              Reintentar
            </v-btn>
            <v-btn size="x-small" variant="text" color="primary" @click="goTo(`/movement?focus=${item.id}`)">
              Abrir
            </v-btn>
          </div>
        </template>
      </v-data-table>
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
  { title: 'Recurso y cantidad', value: 1 },
  { title: 'Destino y reglas', value: 2 },
  { title: 'Confirmación', value: 3 }
]

const loadingCatalogs = ref(false)
const submitting = ref(false)
const error = ref('')
const successMessage = ref('')

const rubros = ref([])
const resources = ref([])
const resourceDefinitions = ref([])
const locations = ref([])
const movements = ref([])
const movementLines = ref([])
const rowActionLoading = ref({})
const historySearch = ref('')
const historyStatusFilter = ref(null)
const historyDateFrom = ref('')
const historyDateTo = ref('')

const form = ref(buildDefaultForm())

function buildDefaultForm() {
  return {
    rubroId: null,
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

function formatStatus(value) {
  const status = String(value || '').trim()
  if (!status) return ''
  return status.charAt(0).toUpperCase() + status.slice(1).toLowerCase()
}

function truncateText(value, max = 34) {
  const text = String(value || '').trim()
  if (!text || text.length <= max) return text
  return `${text.slice(0, Math.max(1, max - 1))}…`
}

function formatDate(value) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return String(value)
  return date.toLocaleString('es-AR')
}

function historyStatusColor(value) {
  const normalized = normalizeKey(value)
  if (normalized.includes('confirmado') || normalized.includes('ok')) return 'green'
  if (normalized.includes('borrador') || normalized.includes('pendiente')) return 'orange'
  if (normalized.includes('anulado') || normalized.includes('cancelado') || normalized.includes('error')) return 'red'
  return 'grey'
}

const resourceDefinitionMap = computed(() => {
  const map = {}
  resourceDefinitions.value.forEach(row => {
    const id = toNumber(readField(row, 'Id'))
    if (id == null) return
    const code = readField(row, 'Codigo')
    const name = readField(row, 'Nombre')
    map[id] = {
      code: String(code || '').trim(),
      name: String(name || '').trim(),
      display: String(name || code || `Def#${id}`).trim(),
      rubroId: toNumber(readField(row, 'RubroId')),
      rubroNombre: String(readField(row, 'Rubronombre') || '')
    }
  })
  return map
})

const rubroItems = computed(() => rubros.value
  .map(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = String(readField(row, 'Codigo') || '').trim()
    const name = String(readField(row, 'Nombre') || '').trim()
    const colorHex = String(readField(row, 'Colorhex') || '#64748b').trim()
    return {
      value: id,
      title: code && name ? `${name} (${code})` : (name || code || `#${id}`),
      code,
      name,
      colorHex
    }
  })
  .filter(item => item.value != null)
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

const resourceItems = computed(() => resources.value
  .map(row => {
    const id = toNumber(readField(row, 'Id'))
    const definitionId = toNumber(readField(row, 'Resourcedefinitionid') ?? readField(row, 'ResourceDefinitionId'))
    const definition = resourceDefinitionMap.value[definitionId || -1] || { code: '', name: '', display: `Def#${definitionId ?? '-'}` }
    const rubroId = toNumber(readField(row, 'Rubroid')) ?? toNumber(definition.rubroId)
    const definitionLabel = truncateText(definition.display || '', 34)
    const rubroLabel = truncateText(String(readField(row, 'Rubronombre') || definition.rubroNombre || ''), 20)
    const title = [definitionLabel, normalizeKey(rubroLabel) === 'general' ? '' : rubroLabel].filter(Boolean).join(' · ')
    return {
      value: id,
      title: title || (definitionLabel || `Recurso #${id}`),
      rubroId
    }
  })
  .filter(item => {
    const selectedRubroId = toNumber(form.value.rubroId)
    if (selectedRubroId == null) return true
    return item.rubroId === selectedRubroId
  })
  .filter(item => item.value != null)
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

const locationItems = computed(() => locations.value
  .map(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = readField(row, 'Codigo')
    const name = readField(row, 'Nombre')
    const rubroId = toNumber(readField(row, 'Rubroid'))
    const title = code && name ? `${code} · ${name}` : (name || code || `#${id}`)
    return { value: id, title, rubroId }
  })
  .filter(item => {
    const selectedRubroId = toNumber(form.value.rubroId)
    if (selectedRubroId == null) return true
    return item.rubroId === selectedRubroId
  })
  .filter(item => item.value != null)
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

const selectedRubroLabel = computed(() => rubroItems.value.find(item => item.value === toNumber(form.value.rubroId))?.title || '')
const selectedResourceLabel = computed(() => {
  const selected = resourceItems.value.find(item => item.value === toNumber(form.value.resourceInstanceId))
  if (!selected) return ''
  return selected.title || ''
})
const selectedLocationLabel = computed(() => locationItems.value.find(item => item.value === toNumber(form.value.targetLocationId))?.title || '')

const checkRubro = computed(() => toNumber(form.value.rubroId) != null)
const checkResource = computed(() => toNumber(form.value.resourceInstanceId) != null)
const checkQuantity = computed(() => (toNumber(form.value.quantity) || 0) > 0)
const checkLocation = computed(() => toNumber(form.value.targetLocationId) != null)
const isFormValid = computed(() => checkRubro.value && checkResource.value && checkQuantity.value && checkLocation.value)

const locationMap = computed(() => {
  const map = new Map()
  toArray(locations.value).forEach(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = readField(row, 'Codigo')
    const name = readField(row, 'Nombre')
    if (id == null) return
    map.set(id, code && name ? `${code} · ${name}` : (name || code || `#${id}`))
  })
  return map
})

const movementLinesByMovementId = computed(() => {
  const map = {}
  toArray(movementLines.value).forEach(row => {
    const movementId = toNumber(readField(row, 'Movementid') ?? readField(row, 'MovementId'))
    if (movementId == null) return
    if (!map[movementId]) map[movementId] = []
    map[movementId].push({
      resourceInstanceId: toNumber(readField(row, 'Resourceinstanceid') ?? readField(row, 'ResourceInstanceId')),
      quantity: toNumber(readField(row, 'Quantity')) || 0,
      unitCost: toNumber(readField(row, 'Unitcost') ?? readField(row, 'UnitCost')),
      serie: readField(row, 'Serie') || null,
      lote: readField(row, 'Lote') || null
    })
  })
  return map
})

const quantityByMovementId = computed(() => {
  const map = {}
  Object.entries(movementLinesByMovementId.value).forEach(([movementId, lines]) => {
    map[Number(movementId)] = (lines || []).reduce((acc, line) => acc + (toNumber(line.quantity) || 0), 0)
  })
  return map
})

const historyRows = computed(() => toArray(movements.value)
  .map(row => {
    const movementType = String(readField(row, 'Movementtype') || readField(row, 'MovementType') || '').trim().toLowerCase()
    const id = toNumber(readField(row, 'Id'))
    const targetLocationId = toNumber(readField(row, 'Targetlocationid') ?? readField(row, 'TargetLocationId'))
    return {
      id,
      movementType,
      status: String(readField(row, 'Status') || '').trim().toLowerCase(),
      referenceNo: readField(row, 'Referenceno') || readField(row, 'ReferenceNo') || `MOV-${id ?? '—'}`,
      targetLocationId,
      targetLabel: targetLocationId != null ? (locationMap.value.get(targetLocationId) || `#${targetLocationId}`) : '—',
      quantity: quantityByMovementId.value[id || -1] ?? (toNumber(readField(row, 'Totalquantity') ?? readField(row, 'TotalQuantity')) ?? 0),
      operationAt: readField(row, 'Operationat') || readField(row, 'OperationAt') || readField(row, 'Createdat') || readField(row, 'CreatedAt') || null,
      createdAt: readField(row, 'Createdat') || readField(row, 'CreatedAt') || null,
      createdBy: readField(row, 'Createdby') || readField(row, 'CreatedBy') || null,
      notes: readField(row, 'Notes') || null
    }
  })
  .filter(item => item.id != null && item.movementType === 'ingreso')
  .sort((a, b) => new Date(b.operationAt || 0).getTime() - new Date(a.operationAt || 0).getTime()))

const historyStatusItems = computed(() => {
  const set = new Set(historyRows.value.map(item => item.status).filter(Boolean))
  return Array.from(set)
    .sort((a, b) => a.localeCompare(b, 'es'))
    .map(value => ({ value, title: formatStatus(value) }))
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

const filteredHistory = computed(() => {
  const query = String(historySearch.value || '').trim().toLowerCase()
  const selectedStatus = String(historyStatusFilter.value || '').trim().toLowerCase()
  const from = toDateStart(historyDateFrom.value)
  const to = toDateEnd(historyDateTo.value)

  return historyRows.value
    .filter(item => {
      if (selectedStatus && String(item.status || '').trim().toLowerCase() !== selectedStatus) return false
      const itemDate = item.operationAt ? new Date(item.operationAt) : null
      if (from && itemDate && itemDate < from) return false
      if (to && itemDate && itemDate > to) return false
      if (!query) return true
      const haystack = [item.referenceNo, item.targetLabel, formatStatus(item.status)]
        .map(v => String(v || '').toLowerCase())
        .join(' ')
      return haystack.includes(query)
    })
    .slice(0, 80)
})

const historyHeaders = [
  { title: 'Referencia', key: 'referenceNo' },
  { title: 'Estado', key: 'status' },
  { title: 'Destino', key: 'targetLabel' },
  { title: 'Cant.', key: 'quantity', align: 'end' },
  { title: 'Fecha', key: 'operationAt' },
  { title: 'Acciones', key: 'actions', sortable: false, align: 'end' }
]

watch(() => form.value.rubroId, () => {
  const selectedResourceId = toNumber(form.value.resourceInstanceId)
  const selectedLocationId = toNumber(form.value.targetLocationId)
  if (selectedResourceId != null && !resourceItems.value.some(item => item.value === selectedResourceId)) {
    form.value.resourceInstanceId = null
  }
  if (selectedLocationId != null && !locationItems.value.some(item => item.value === selectedLocationId)) {
    form.value.targetLocationId = null
  }
})

function goTo(path) {
  if (!path) return
  router.push(path)
}

function validateStep(currentStep) {
  if (currentStep === 1) {
    if (!checkRubro.value) return 'Selecciona un rubro.'
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

function normalizeStatus(value) {
  return normalizeKey(value)
}

function canConfirm(item) {
  return normalizeStatus(item?.status) === 'borrador'
}

function canCancel(item) {
  return normalizeStatus(item?.status) === 'borrador'
}

function canRetry(item) {
  return normalizeStatus(item?.status) === 'anulado'
}

function buildMovementUpdatePayload(row, nextStatus) {
  const nowIso = new Date().toISOString()
  return {
    movementtype: 'ingreso',
    status: nextStatus,
    sourcelocationid: null,
    targetlocationid: row.targetLocationId,
    referenceno: row.referenceNo || null,
    notes: row.notes || null,
    operationat: row.operationAt || nowIso,
    createdby: row.createdBy || null,
    createdat: row.createdAt || nowIso
  }
}

async function updateMovementStatus(row, nextStatus) {
  const id = toNumber(row?.id)
  if (!id) return
  error.value = ''
  successMessage.value = ''
  rowActionLoading.value = { ...rowActionLoading.value, [id]: true }
  try {
    await runtimeApi.update('movement', id, buildMovementUpdatePayload(row, nextStatus))
    await loadCatalogs()
    successMessage.value = `Movimiento ${row.referenceNo || `#${id}`} actualizado a ${nextStatus}.`
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || (typeof payload === 'string' ? payload : 'No se pudo actualizar el movimiento.')
  } finally {
    const next = { ...rowActionLoading.value }
    delete next[id]
    rowActionLoading.value = next
  }
}

function buildRetryReference(referenceNo, id) {
  const base = (String(referenceNo || `MOV-${id || 'X'}`)).trim()
  const suffix = `-R${Date.now().toString().slice(-6)}`
  const maxBase = Math.max(1, 80 - suffix.length)
  return `${base.slice(0, maxBase)}${suffix}`
}

async function retryMovement(row) {
  const id = toNumber(row?.id)
  if (!id) return
  error.value = ''
  successMessage.value = ''
  rowActionLoading.value = { ...rowActionLoading.value, [id]: true }
  try {
    const nowIso = new Date().toISOString()
    const retryReference = buildRetryReference(row.referenceNo, id)
    await runtimeApi.create('movement', {
      movementtype: 'ingreso',
      status: 'borrador',
      sourcelocationid: null,
      targetlocationid: row.targetLocationId,
      referenceno: retryReference,
      notes: row.notes || `Reintento de ${row.referenceNo || `MOV-${id}`}`,
      operationat: nowIso,
      createdby: row.createdBy || null,
      createdat: nowIso
    })

    await loadCatalogs()
    const created = historyRows.value.find(item => item.referenceNo === retryReference)
    if (!created?.id) {
      throw new Error('No se pudo resolver el nuevo movimiento reintentado.')
    }
    const lines = movementLinesByMovementId.value[id] || []
    for (const line of lines) {
      await runtimeApi.create('movement-line', {
        movementid: created.id,
        resourceinstanceid: line.resourceInstanceId,
        quantity: line.quantity,
        unitcost: line.unitCost,
        serie: line.serie,
        lote: line.lote,
        createdat: nowIso
      })
    }
    await loadCatalogs()
    successMessage.value = `Reintento creado (${retryReference}) en borrador.`
  } catch (err) {
    const payload = err?.response?.data
    error.value = payload?.message || payload?.error || (typeof payload === 'string' ? payload : err?.message || 'No se pudo reintentar el movimiento.')
  } finally {
    const next = { ...rowActionLoading.value }
    delete next[id]
    rowActionLoading.value = next
  }
}

async function loadCatalogs() {
  loadingCatalogs.value = true
  error.value = ''

  try {
    const withTimeout = (promise, ms, label) =>
      Promise.race([
        promise,
        new Promise((_, reject) => setTimeout(() => reject(new Error(`Timeout en ${label}`)), ms))
      ])

    const [rubrosRes, resourcesRes, resourceDefsRes, locationsRes, movementsRes, movementLinesRes] = await Promise.allSettled([
      withTimeout(runtimeApi.list('rubro'), 10000, 'rubro'),
      withTimeout(runtimeApi.list('resource-instance'), 10000, 'resource-instance'),
      withTimeout(runtimeApi.list('resource-definition'), 10000, 'resource-definition'),
      withTimeout(runtimeApi.list('location'), 10000, 'location'),
      withTimeout(runtimeApi.list('movement'), 10000, 'movement'),
      withTimeout(runtimeApi.list('movement-line'), 10000, 'movement-line')
    ])

    const warnings = []
    const takeData = (result, label) => {
      if (result.status === 'fulfilled') return toArray(result.value?.data)
      warnings.push(label)
      return []
    }

    rubros.value = takeData(rubrosRes, 'rubros')
    resources.value = takeData(resourcesRes, 'instancias')
    resourceDefinitions.value = takeData(resourceDefsRes, 'tipos de recurso')
    locations.value = takeData(locationsRes, 'ubicaciones')
    movements.value = takeData(movementsRes, 'movimientos')
    movementLines.value = takeData(movementLinesRes, 'líneas de movimiento')

    if (toNumber(form.value.rubroId) == null && rubroItems.value.length === 1) {
      form.value.rubroId = rubroItems.value[0].value
    }

    if (warnings.length > 0) {
      error.value = `Carga parcial: no se pudieron obtener ${warnings.join(', ')}.`
    }
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
    rubroid: toNumber(form.value.rubroId),
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

.history-route {
  color: var(--sb-text-soft, #475569);
  font-size: 0.86rem;
}

.history-actions {
  display: flex;
  gap: 4px;
  justify-content: flex-end;
  flex-wrap: wrap;
}

.history-reference {
  display: grid;
  line-height: 1.2;
}

.history-reference__sub {
  font-size: 0.75rem;
  color: var(--sb-text-soft, #64748b);
}

.recepcion-view :deep(.history-table thead th) {
  color: var(--sb-text, #0f172a) !important;
  font-weight: 700 !important;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  font-size: 0.72rem;
  background: color-mix(in srgb, var(--sb-primary-soft, rgba(37,99,235,0.12)) 42%, transparent);
  border-bottom: 1px solid var(--sb-border-soft);
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

.rubro-dot {
  width: 10px;
  height: 10px;
  border-radius: 999px;
  display: inline-block;
  border: 1px solid rgba(15, 23, 42, 0.2);
}
</style>
