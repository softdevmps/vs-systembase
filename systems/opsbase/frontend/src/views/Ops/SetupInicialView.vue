<template>
  <v-container fluid class="setup-view">
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="setup-head">
          <div class="setup-icon">
            <v-icon size="24" color="primary">mdi-clipboard-plus-outline</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Carga inicial guiada</h2>
            <div class="text-body-2 text-medium-emphasis">
              Configura datos base en orden: tipo de recurso, instancia, depósito y stock inicial.
            </div>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2">
        <v-btn variant="tonal" color="primary" :loading="loadingCatalogs" @click="loadCatalogs">
          <v-icon start>mdi-refresh</v-icon>
          Recargar
        </v-btn>
        <v-btn color="primary" @click="goTo('/operaciones')">
          <v-icon start>mdi-transit-transfer</v-icon>
          Ir a operaciones
        </v-btn>
      </v-col>
    </v-row>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4">{{ error }}</v-alert>
    <v-alert v-if="successMessage" type="success" variant="tonal" class="mb-4">{{ successMessage }}</v-alert>

    <v-row dense>
      <v-col cols="12" lg="8">
        <v-card class="setup-card">
          <v-stepper v-model="step" :items="stepItems" flat>
            <template #item.1>
              <v-card-text>
                <v-row dense>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model="resourceDefForm.Codigo"
                      label="Código"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="5">
                    <v-text-field
                      v-model="resourceDefForm.Nombre"
                      label="Nombre"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="3">
                    <v-select
                      v-model="resourceDefForm.TrackMode"
                      :items="trackModeItems"
                      label="Track mode"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-select
                      v-model="resourceDefForm.RubroId"
                      :items="rubroItems"
                      item-title="title"
                      item-value="value"
                      label="Rubro"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12">
                    <v-text-field
                      v-model="resourceDefForm.Descripcion"
                      label="Descripción"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                </v-row>
                <div class="d-flex justify-end mt-2">
                  <v-btn color="primary" :loading="saving.resourceDefinition" @click="saveResourceDefinition">
                    Guardar tipo y continuar
                  </v-btn>
                </div>
              </v-card-text>
            </template>

            <template #item.2>
              <v-card-text>
                <v-row dense>
                  <v-col cols="12" md="6">
                    <v-select
                      v-model="resourceInstanceForm.ResourceDefinitionId"
                      :items="resourceDefinitionItems"
                      item-title="title"
                      item-value="value"
                      label="Tipo de recurso"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="3">
                    <v-text-field
                      v-model="resourceInstanceForm.CodigoInterno"
                      label="Código interno"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="3">
                    <v-select
                      v-model="resourceInstanceForm.Estado"
                      :items="resourceStateItems"
                      label="Estado"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="6">
                    <v-text-field
                      v-model="resourceInstanceForm.Serie"
                      label="Serie (opcional)"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="6">
                    <v-text-field
                      v-model="resourceInstanceForm.Lote"
                      label="Lote (opcional)"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                </v-row>
                <div class="d-flex justify-end mt-2">
                  <v-btn color="primary" :loading="saving.resourceInstance" @click="saveResourceInstance">
                    Guardar instancia y continuar
                  </v-btn>
                </div>
              </v-card-text>
            </template>

            <template #item.3>
              <v-card-text>
                <v-row dense>
                  <v-col cols="12" md="3">
                    <v-text-field
                      v-model="locationForm.Codigo"
                      label="Código depósito"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="5">
                    <v-text-field
                      v-model="locationForm.Nombre"
                      label="Nombre depósito"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-select
                      v-model="locationForm.Tipo"
                      :items="locationTypeItems"
                      label="Tipo"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-select
                      v-model="locationForm.RubroId"
                      :items="rubroItems"
                      item-title="title"
                      item-value="value"
                      label="Rubro"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model.number="locationForm.Capacidad"
                      type="number"
                      min="0"
                      label="Capacidad"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model.number="locationForm.Latitud"
                      type="number"
                      step="0.000001"
                      label="Latitud (opcional)"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model.number="locationForm.Longitud"
                      type="number"
                      step="0.000001"
                      label="Longitud (opcional)"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                </v-row>
                <div class="d-flex justify-end mt-2">
                  <v-btn color="primary" :loading="saving.location" @click="saveLocation">
                    Guardar depósito y continuar
                  </v-btn>
                </div>
              </v-card-text>
            </template>

            <template #item.4>
              <v-card-text>
                <v-row dense>
                  <v-col cols="12" md="5">
                    <v-select
                      v-model="stockForm.ResourceInstanceId"
                      :items="resourceInstanceItems"
                      item-title="title"
                      item-value="value"
                      label="Instancia"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="5">
                    <v-select
                      v-model="stockForm.LocationId"
                      :items="locationItems"
                      item-title="title"
                      item-value="value"
                      label="Depósito"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="2">
                    <v-text-field
                      v-model.number="stockForm.Cantidad"
                      type="number"
                      min="0.001"
                      step="0.001"
                      label="Cantidad"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                </v-row>
                <div class="d-flex justify-end mt-2">
                  <v-btn color="primary" :loading="saving.stock" @click="saveStock">
                    Cargar stock inicial
                  </v-btn>
                </div>
              </v-card-text>
            </template>
          </v-stepper>

          <v-divider />
          <v-card-actions class="justify-space-between pa-4">
            <v-btn variant="text" :disabled="step <= 1" @click="step--">Anterior</v-btn>
            <v-btn
              variant="tonal"
              color="primary"
              :disabled="step >= 4"
              @click="step++"
            >
              Siguiente
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-col>

      <v-col cols="12" lg="4">
        <v-card class="status-card">
          <v-card-title>Estado de carga inicial</v-card-title>
          <v-card-text>
            <div class="status-row">
              <span>Rubros</span>
              <v-chip :color="rubros.length ? 'green' : 'grey'" size="small" variant="tonal">
                {{ rubros.length }}
              </v-chip>
            </div>
            <div class="status-row">
              <span>Tipos de recurso</span>
              <v-chip :color="resourceDefinitions.length ? 'green' : 'grey'" size="small" variant="tonal">
                {{ resourceDefinitions.length }}
              </v-chip>
            </div>
            <div class="status-row">
              <span>Instancias</span>
              <v-chip :color="resourceInstances.length ? 'green' : 'grey'" size="small" variant="tonal">
                {{ resourceInstances.length }}
              </v-chip>
            </div>
            <div class="status-row">
              <span>Depósitos</span>
              <v-chip :color="locations.length ? 'green' : 'grey'" size="small" variant="tonal">
                {{ locations.length }}
              </v-chip>
            </div>
            <div class="status-row">
              <span>Saldos de stock</span>
              <v-chip :color="stockBalances.length ? 'green' : 'grey'" size="small" variant="tonal">
                {{ stockBalances.length }}
              </v-chip>
            </div>
          </v-card-text>
          <v-divider />
          <v-card-actions class="pa-4 d-flex flex-wrap ga-2">
            <v-btn variant="tonal" color="primary" @click="goTo('/depositos')">Ver depósitos</v-btn>
            <v-btn variant="tonal" color="primary" @click="goTo('/kardex')">Ver kardex</v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>

    <v-row dense class="mt-4">
      <v-col cols="12">
        <v-card class="bulk-card">
          <v-card-title class="d-flex align-center justify-space-between flex-wrap ga-2">
            <div class="d-flex align-center ga-2">
              <v-icon color="primary">mdi-database-import-outline</v-icon>
              <span>Carga masiva</span>
            </div>
            <v-chip color="primary" variant="tonal" size="small">
              CSV
            </v-chip>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div class="bulk-guide mb-4">
              <div class="bulk-guide-step"><span>1</span>Elegí qué recurso querés cargar</div>
              <div class="bulk-guide-step"><span>2</span>Descargá plantilla o subí archivo</div>
              <div class="bulk-guide-step"><span>3</span>Revisá vista previa</div>
              <div class="bulk-guide-step"><span>4</span>Ejecutá importación</div>
            </div>

            <v-row dense>
              <v-col cols="12" lg="5">
                <v-card class="bulk-panel" variant="outlined">
                  <v-card-text>
                    <div class="bulk-panel-title">1. Configuración</div>
                    <v-select
                      v-model="bulkEntity"
                      :items="bulkEntityItems"
                      item-title="title"
                      item-value="value"
                      label="Qué recurso vas a cargar"
                      variant="outlined"
                      density="comfortable"
                    />

                    <v-alert type="info" variant="tonal" density="compact" class="mb-3">
                      {{ bulkSpec.description }}
                    </v-alert>

                    <div class="text-caption text-medium-emphasis mb-1">Columnas obligatorias</div>
                    <div class="d-flex flex-wrap ga-1 mb-3">
                      <v-chip
                        v-for="field in bulkSpec.required"
                        :key="`req-${field}`"
                        color="red"
                        size="x-small"
                        variant="tonal"
                      >
                        {{ prettyBulkField(field) }}
                      </v-chip>
                    </div>

                    <div class="text-caption text-medium-emphasis mb-1">Columnas opcionales</div>
                    <div class="d-flex flex-wrap ga-1">
                      <v-chip
                        v-for="field in bulkSpec.optional"
                        :key="`opt-${field}`"
                        color="blue"
                        size="x-small"
                        variant="tonal"
                      >
                        {{ prettyBulkField(field) }}
                      </v-chip>
                    </div>
                  </v-card-text>
                </v-card>
              </v-col>

              <v-col cols="12" lg="7">
                <v-card class="bulk-panel" variant="outlined">
                  <v-card-text>
                    <div class="bulk-panel-title">2. Archivo CSV</div>
                    <div class="d-flex ga-2 flex-wrap mb-2">
                      <v-btn variant="tonal" color="primary" @click="downloadBulkTemplate">
                        <v-icon start>mdi-download</v-icon>
                        Descargar plantilla
                      </v-btn>
                      <v-btn variant="text" color="primary" @click="loadBulkTemplate">
                        Usar ejemplo
                      </v-btn>
                      <v-btn variant="text" color="primary" @click="bulkInput = ''">
                        Limpiar
                      </v-btn>
                    </div>

                    <v-file-input
                      v-model="bulkFile"
                      label="Subir archivo CSV"
                      accept=".csv,text/csv"
                      prepend-icon="mdi-file-delimited-outline"
                      variant="outlined"
                      density="comfortable"
                      show-size
                      clearable
                      @update:modelValue="onBulkFileSelected"
                    />

                    <v-textarea
                      v-model="bulkInput"
                      label="O pegá acá el contenido del CSV"
                      placeholder="Ejemplo: Codigo;Nombre;Descripcion;TrackMode;IsActive"
                      rows="8"
                      auto-grow
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-card-text>
                </v-card>
              </v-col>
            </v-row>

            <v-card class="bulk-preview mt-3" variant="outlined">
              <v-card-text>
                <div class="d-flex align-center justify-space-between flex-wrap ga-2 mb-2">
                  <div>
                    <div class="bulk-panel-title mb-0">3. Vista previa y validación</div>
                    <div class="text-caption text-medium-emphasis">
                      Verificá que las columnas y filas estén correctas antes de importar.
                    </div>
                  </div>
                  <v-chip color="primary" variant="tonal" size="small">
                    {{ bulkParsedRows.length }} filas detectadas
                  </v-chip>
                </div>

                <v-alert
                  v-if="bulkMissingHeaders.length"
                  type="error"
                  variant="tonal"
                  density="compact"
                  class="mb-2"
                >
                  Faltan columnas obligatorias: {{ bulkMissingHeaders.map(prettyBulkField).join(', ') }}.
                </v-alert>

                <v-alert
                  v-else-if="!bulkParsedRows.length"
                  type="warning"
                  variant="tonal"
                  density="compact"
                  class="mb-2"
                >
                  No detectamos filas de datos. Cargá un CSV con cabecera y al menos una fila.
                </v-alert>

                <v-table v-else density="compact" class="bulk-preview-table mb-2">
                  <thead>
                    <tr>
                      <th v-for="header in bulkDetectedHeaders" :key="`header-${header}`">
                        {{ prettyBulkField(header) }}
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(row, rowIdx) in bulkPreviewRows" :key="`preview-${rowIdx}`">
                      <td v-for="header in bulkDetectedHeaders" :key="`cell-${rowIdx}-${header}`">
                        {{ row[header] }}
                      </td>
                    </tr>
                  </tbody>
                </v-table>

                <div v-if="bulkParsedRows.length > bulkPreviewRows.length" class="text-caption text-medium-emphasis mb-2">
                  Mostrando {{ bulkPreviewRows.length }} de {{ bulkParsedRows.length }} filas.
                </div>

                <div class="d-flex justify-end">
                  <v-btn color="primary" :loading="bulkRunning" :disabled="!canRunBulkImport" @click="executeBulkImport">
                    <v-icon start>mdi-play</v-icon>
                    Validar y ejecutar importación
                  </v-btn>
                </div>
              </v-card-text>
            </v-card>

            <v-progress-linear
              v-if="bulkRunning || bulkResult.total > 0"
              :model-value="bulkProgressPercent"
              color="primary"
              height="8"
              rounded
              class="mb-3"
            />

            <div v-if="bulkResult.total > 0" class="d-flex ga-2 flex-wrap mb-3">
              <v-chip color="primary" variant="tonal" size="small">Total: {{ bulkResult.total }}</v-chip>
              <v-chip color="green" variant="tonal" size="small">OK: {{ bulkResult.ok }}</v-chip>
              <v-chip color="red" variant="tonal" size="small">Error: {{ bulkResult.error }}</v-chip>
            </div>

            <v-alert
              v-if="bulkErrors.length"
              type="warning"
              variant="tonal"
              density="compact"
              class="mb-2"
            >
              Se detectaron filas con error.
            </v-alert>

            <v-list v-if="bulkErrors.length" density="compact" class="bulk-error-list">
              <v-list-item
                v-for="(msg, idx) in bulkErrors.slice(0, 12)"
                :key="`${idx}-${msg}`"
              >
                <v-list-item-title class="text-caption">{{ msg }}</v-list-item-title>
              </v-list-item>
            </v-list>
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

const step = ref(1)
const stepItems = [
  { title: 'Tipo de recurso', value: 1 },
  { title: 'Instancia', value: 2 },
  { title: 'Depósito', value: 3 },
  { title: 'Stock inicial', value: 4 }
]

const trackModeItems = ['none', 'serial', 'lote', 'serial_lote']
const resourceStateItems = ['activo', 'inactivo', 'bloqueado', 'cuarentena', 'reparacion', 'baja']
const locationTypeItems = ['deposito', 'sector', 'pasillo', 'estanteria', 'nivel', 'posicion']
const bulkEntityItems = [
  { value: 'resource-definition', title: 'Tipos de recurso' },
  { value: 'resource-instance', title: 'Instancias de recurso' }
]
const BULK_SPECS = {
  rubro: {
    description: 'Alta/actualización de rubros operativos para separar líneas de negocio.',
    required: ['Codigo', 'Nombre'],
    optional: ['Descripcion', 'ColorHex', 'IsActive'],
    labels: {
      codigo: 'Código',
      nombre: 'Nombre',
      descripcion: 'Descripción',
      colorhex: 'Color',
      isactive: 'Activo'
    }
  },
  'resource-definition': {
    description: 'Alta/actualización de catálogo de tipos de recurso. Cada tipo debe pertenecer a un rubro.',
    required: ['Codigo', 'Nombre', 'RubroCodigo'],
    optional: ['Descripcion', 'TrackMode', 'IsActive'],
    labels: {
      codigo: 'Código',
      nombre: 'Nombre',
      rubrocodigo: 'Código de rubro',
      descripcion: 'Descripción',
      trackmode: 'Modo de seguimiento',
      isactive: 'Activo'
    }
  },
  'resource-instance': {
    description: 'Alta/actualización de unidades operativas (cada activo físico).',
    required: ['ResourceDefinitionCodigo', 'CodigoInterno'],
    optional: ['Estado', 'Serie', 'Lote', 'IsActive'],
    labels: {
      resourcedefinitioncodigo: 'Código del tipo de recurso',
      codigointerno: 'Código interno',
      estado: 'Estado',
      serie: 'Serie',
      lote: 'Lote',
      isactive: 'Activo'
    }
  },
  location: {
    description: 'Alta/actualización de depósitos, sectores y posiciones.',
    required: ['Codigo', 'Nombre'],
    optional: ['RubroCodigo', 'Tipo', 'Capacidad', 'Latitud', 'Longitud', 'IsActive'],
    labels: {
      codigo: 'Código',
      nombre: 'Nombre',
      rubrocodigo: 'Código de rubro',
      tipo: 'Tipo',
      capacidad: 'Capacidad',
      latitud: 'Latitud',
      longitud: 'Longitud',
      isactive: 'Activo'
    }
  },
  'stock-balance': {
    description: 'Carga inicial o ajuste masivo de stock por recurso y depósito.',
    required: ['ResourceInstanceCodigo', 'LocationCodigo', 'StockReal'],
    optional: ['StockReservado', 'Mode'],
    labels: {
      resourceinstancecodigo: 'Código de instancia',
      locationcodigo: 'Código de depósito',
      stockreal: 'Stock real',
      stockreservado: 'Stock reservado',
      mode: 'Modo (set/add)'
    }
  },
  'ops-recepcion': {
    description: 'Crea recepciones en lote (ingreso a depósito), con confirmación opcional.',
    required: ['ResourceInstanceCodigo', 'TargetLocationCodigo', 'Quantity'],
    optional: ['RubroCodigo', 'UnitCost', 'Confirmar', 'ReferenceNo', 'Notes', 'OperationAt'],
    labels: {
      resourceinstancecodigo: 'Código de instancia',
      targetlocationcodigo: 'Código depósito destino',
      rubrocodigo: 'Código de rubro',
      quantity: 'Cantidad',
      unitcost: 'Costo unitario',
      confirmar: 'Confirmar',
      referenceno: 'Referencia',
      notes: 'Notas',
      operationat: 'Fecha/hora operación'
    }
  },
  'ops-despacho': {
    description: 'Crea despachos/traslados en lote, validando stock y ubicaciones.',
    required: ['MovementType', 'ResourceInstanceCodigo', 'SourceLocationCodigo', 'Quantity'],
    optional: ['RubroCodigo', 'TargetLocationCodigo', 'UnitCost', 'Confirmar', 'ReferenceNo', 'Notes', 'OperationAt'],
    labels: {
      movementtype: 'Tipo (egreso/transferencia)',
      resourceinstancecodigo: 'Código de instancia',
      sourcelocationcodigo: 'Código depósito origen',
      targetlocationcodigo: 'Código depósito destino',
      rubrocodigo: 'Código de rubro',
      quantity: 'Cantidad',
      unitcost: 'Costo unitario',
      confirmar: 'Confirmar',
      referenceno: 'Referencia',
      notes: 'Notas',
      operationat: 'Fecha/hora operación'
    }
  }
}

const loadingCatalogs = ref(false)
const error = ref('')
const successMessage = ref('')

const saving = ref({
  resourceDefinition: false,
  resourceInstance: false,
  location: false,
  stock: false
})

const rubros = ref([])
const resourceDefinitions = ref([])
const resourceInstances = ref([])
const locations = ref([])
const stockBalances = ref([])
const bulkEntity = ref('resource-definition')
const bulkFile = ref(null)
const bulkInput = ref('')
const bulkRunning = ref(false)
const bulkResult = ref({
  total: 0,
  current: 0,
  ok: 0,
  error: 0
})
const bulkErrors = ref([])

const resourceDefForm = ref(buildResourceDefinitionForm())
const resourceInstanceForm = ref(buildResourceInstanceForm())
const locationForm = ref(buildLocationForm())
const stockForm = ref(buildStockForm())

function buildResourceDefinitionForm() {
  return {
    Codigo: '',
    Nombre: '',
    RubroId: null,
    Descripcion: '',
    TrackMode: 'none',
    IsActive: true
  }
}

function buildResourceInstanceForm() {
  return {
    ResourceDefinitionId: null,
    CodigoInterno: '',
    Estado: 'activo',
    Serie: '',
    Lote: '',
    IsActive: true
  }
}

function buildLocationForm() {
  return {
    Codigo: '',
    Nombre: '',
    RubroId: null,
    Tipo: 'deposito',
    Capacidad: 1000,
    Latitud: null,
    Longitud: null,
    IsActive: true
  }
}

function buildStockForm() {
  return {
    ResourceInstanceId: null,
    LocationId: null,
    Cantidad: 1
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
  const key = Object.keys(item).find(k => normalizeKey(k) === normalizeKey(name))
  return key ? item[key] : undefined
}

function toNumber(value) {
  if (value == null || value === '') return null
  const num = Number(String(value).replace(',', '.'))
  return Number.isFinite(num) ? num : null
}

function toCode(value, fallback = '') {
  return String(value || fallback).trim()
}

function parseApiError(err, fallback) {
  const payload = err?.response?.data
  if (typeof payload === 'string' && payload.trim()) return payload
  return payload?.message || payload?.error || err?.message || fallback
}

function parseBool(value, fallback = true) {
  if (value == null || value === '') return fallback
  const normalized = normalizeKey(value)
  if (['1', 'true', 'si', 'sí', 'yes', 'activo'].includes(normalized)) return true
  if (['0', 'false', 'no', 'inactive', 'inactivo'].includes(normalized)) return false
  return fallback
}

function detectDelimiter(text) {
  const firstLine = String(text || '').split(/\r?\n/).find(line => line.trim())
  if (!firstLine) return ';'
  const semis = (firstLine.match(/;/g) || []).length
  const commas = (firstLine.match(/,/g) || []).length
  return semis >= commas ? ';' : ','
}

function splitCsvLine(line, delimiter) {
  const cells = []
  let current = ''
  let inQuotes = false

  for (let i = 0; i < line.length; i += 1) {
    const char = line[i]
    const next = line[i + 1]
    if (char === '"') {
      if (inQuotes && next === '"') {
        current += '"'
        i += 1
      } else {
        inQuotes = !inQuotes
      }
      continue
    }
    if (char === delimiter && !inQuotes) {
      cells.push(current.trim())
      current = ''
      continue
    }
    current += char
  }
  cells.push(current.trim())
  return cells
}

function parseCsv(text) {
  const lines = String(text || '')
    .split(/\r?\n/)
    .map(line => line.trim())
    .filter(Boolean)

  if (!lines.length) return []

  const delimiter = detectDelimiter(text)
  const headers = splitCsvLine(lines[0], delimiter)
    .map(value => value.replace(/^"|"$/g, '').trim())
    .filter(Boolean)

  if (!headers.length) return []

  return lines.slice(1).map((line, idx) => {
    const cols = splitCsvLine(line, delimiter).map(value => value.replace(/^"|"$/g, '').trim())
    const row = {}
    headers.forEach((header, hIdx) => {
      row[header] = cols[hIdx] ?? ''
    })
    row.__line = idx + 2
    return row
  }).filter(row => Object.keys(row).some(key => key !== '__line' && toCode(row[key]) !== ''))
}

function parseCsvHeaders(text) {
  const lines = String(text || '')
    .split(/\r?\n/)
    .map(line => line.trim())
    .filter(Boolean)

  if (!lines.length) return []

  const delimiter = detectDelimiter(text)
  return splitCsvLine(lines[0], delimiter)
    .map(value => value.replace(/^"|"$/g, '').trim())
    .filter(Boolean)
}

function hasHeader(headers, target) {
  return headers.some(header => normalizeKey(header) === normalizeKey(target))
}

function rowValue(row, ...keys) {
  for (const key of keys) {
    const match = Object.keys(row).find(k => normalizeKey(k) === normalizeKey(key))
    if (match) return toCode(row[match])
  }
  return ''
}

function requiredRowValue(row, ...keys) {
  const value = rowValue(row, ...keys)
  if (!value) throw new Error(`Falta campo requerido: ${keys[0]}.`)
  return value
}

function templateForEntity(entity) {
  if (entity === 'resource-definition') {
    return [
      'Codigo;Nombre;RubroCodigo;Descripcion;TrackMode;IsActive',
      'LAPTOP;Laptop;TECNOLOGIA;Computadora portátil;serial;1',
      'MOUSE;Mouse;TECNOLOGIA;Mouse óptico;none;1'
    ].join('\n')
  }
  if (entity === 'resource-instance') {
    return [
      'ResourceDefinitionCodigo;CodigoInterno;Estado;Serie;Lote;IsActive',
      'LAPTOP;LAP-001;activo;SN-LAP-001;;1',
      'LAPTOP;LAP-002;activo;SN-LAP-002;;1'
    ].join('\n')
  }
  if (entity === 'location') {
    return [
      'Codigo;Nombre;Tipo;Capacidad;Latitud;Longitud;IsActive',
      'DEP-CBA-CENTRAL;Depósito Central Córdoba;deposito;1000;-31.399656;-64.233627;1',
      'DEP-CBA-NORTE;Depósito Norte Córdoba;deposito;600;-31.381100;-64.203200;1'
    ].join('\n')
  }
  if (entity === 'stock-balance') {
    return [
      'ResourceInstanceCodigo;LocationCodigo;StockReal;StockReservado;Mode',
      'LAP-001;DEP-CBA-CENTRAL;10;0;set',
      'LAP-002;DEP-CBA-NORTE;4;1;set'
    ].join('\n')
  }
  if (entity === 'ops-recepcion') {
    return [
      'ResourceInstanceCodigo;TargetLocationCodigo;Quantity;UnitCost;Confirmar;ReferenceNo;Notes;OperationAt',
      'LAP-001;DEP-CBA-CENTRAL;5;120000;1;REC-001;Ingreso inicial;2026-03-18T10:00',
      'LAP-002;DEP-CBA-NORTE;2;98000;1;REC-002;Ingreso parcial;2026-03-18T10:10'
    ].join('\n')
  }
  if (entity === 'ops-despacho') {
    return [
      'MovementType;ResourceInstanceCodigo;SourceLocationCodigo;TargetLocationCodigo;Quantity;UnitCost;Confirmar;ReferenceNo;Notes;OperationAt',
      'egreso;LAP-001;DEP-CBA-CENTRAL;;1;120000;1;DESP-001;Salida operativa;2026-03-18T11:00',
      'transferencia;LAP-002;DEP-CBA-NORTE;DEP-CBA-CENTRAL;1;98000;1;TRF-001;Reubicación interna;2026-03-18T11:15'
    ].join('\n')
  }
  return templateForEntity('resource-definition')
}

function loadBulkTemplate() {
  bulkInput.value = templateForEntity(bulkEntity.value)
}

function downloadBulkTemplate() {
  const content = templateForEntity(bulkEntity.value)
  const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `opsbase-${bulkEntity.value}-plantilla.csv`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

async function onBulkFileSelected(value) {
  const file = Array.isArray(value) ? value[0] : value
  if (!file) return
  try {
    const text = await file.text()
    bulkInput.value = text
  } catch (err) {
    error.value = parseApiError(err, 'No se pudo leer el archivo CSV.')
  }
}

const bulkSpec = computed(() => BULK_SPECS[bulkEntity.value] || BULK_SPECS['resource-definition'])
const bulkDetectedHeaders = computed(() => parseCsvHeaders(bulkInput.value))
const bulkParsedRows = computed(() => parseCsv(bulkInput.value))
const bulkPreviewRows = computed(() => bulkParsedRows.value.slice(0, 6))
const bulkMissingHeaders = computed(() => {
  const required = bulkSpec.value?.required || []
  return required.filter(field => !hasHeader(bulkDetectedHeaders.value, field))
})
const canRunBulkImport = computed(() =>
  !bulkRunning.value &&
  bulkParsedRows.value.length > 0 &&
  bulkMissingHeaders.value.length === 0)

function prettyBulkField(fieldName) {
  const key = normalizeKey(fieldName)
  return bulkSpec.value?.labels?.[key] || fieldName
}

const bulkProgressPercent = computed(() => {
  if (!bulkResult.value.total) return 0
  return Math.round((bulkResult.value.current / bulkResult.value.total) * 100)
})

function buildBulkContext() {
  const rubrosByCode = new Map()
  const rubrosById = new Map()
  const defsByCode = new Map()
  const defsById = new Map()
  const instancesByCode = new Map()
  const instancesById = new Map()
  const locationsByCode = new Map()
  const locationsById = new Map()
  const stockByPair = new Map()

  rubros.value.forEach(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = toCode(readField(row, 'Codigo')).toLowerCase()
    if (id != null) rubrosById.set(id, row)
    if (code) rubrosByCode.set(code, row)
  })
  resourceDefinitions.value.forEach(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = toCode(readField(row, 'Codigo')).toLowerCase()
    if (id != null) defsById.set(id, row)
    if (code) defsByCode.set(code, row)
  })
  resourceInstances.value.forEach(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = toCode(readField(row, 'CodigoInterno')).toLowerCase()
    if (id != null) instancesById.set(id, row)
    if (code) instancesByCode.set(code, row)
  })
  locations.value.forEach(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = toCode(readField(row, 'Codigo')).toLowerCase()
    if (id != null) locationsById.set(id, row)
    if (code) locationsByCode.set(code, row)
  })
  stockBalances.value.forEach(row => {
    const ri = toNumber(readField(row, 'ResourceInstanceId'))
    const loc = toNumber(readField(row, 'LocationId'))
    if (ri == null || loc == null) return
    stockByPair.set(`${ri}::${loc}`, row)
  })

  return {
    rubrosByCode,
    rubrosById,
    defsByCode,
    defsById,
    instancesByCode,
    instancesById,
    locationsByCode,
    locationsById,
    stockByPair
  }
}

function resolveRubroIdFromRow(row, ctx, required = false) {
  const token = rowValue(row, 'RubroCodigo', 'Rubro', 'RubroId')
  if (!token) {
    if (!required) return null
    throw new Error('Falta rubro. Usa RubroCodigo o RubroId.')
  }

  const numericId = toNumber(token)
  if (numericId != null && ctx.rubrosById.has(numericId)) return numericId

  const fromCode = ctx.rubrosByCode.get(token.toLowerCase())
  const id = toNumber(readField(fromCode, 'Id'))
  if (id == null) throw new Error(`No existe rubro con identificador ${token}.`)
  return id
}

function validateNoDuplicates(rows, keyBuilder) {
  const seen = new Map()
  const errors = []
  rows.forEach(row => {
    const key = keyBuilder(row)
    if (!key) return
    if (seen.has(key)) {
      errors.push(`Fila ${row.__line}: clave duplicada con fila ${seen.get(key)} (${key}).`)
    } else {
      seen.set(key, row.__line)
    }
  })
  return errors
}

async function upsertResourceDefinition(row, ctx) {
  const code = requiredRowValue(row, 'Codigo')
  const name = requiredRowValue(row, 'Nombre')
  const rubroId = resolveRubroIdFromRow(row, ctx, true)
  const nowIso = new Date().toISOString()
  const payload = {
    Codigo: code,
    Nombre: name,
    RubroId: rubroId,
    Descripcion: rowValue(row, 'Descripcion') || null,
    TrackMode: rowValue(row, 'TrackMode') || 'none',
    IsActive: parseBool(rowValue(row, 'IsActive'), true),
    CreatedAt: nowIso,
    UpdatedAt: null
  }

  const existing = ctx.defsByCode.get(code.toLowerCase())
  if (existing) {
    const id = toNumber(readField(existing, 'Id'))
    if (id == null) throw new Error('No se pudo resolver Id para actualizar ResourceDefinition.')
    await runtimeApi.update('resource-definition', id, { ...existing, ...payload })
    return
  }

  await runtimeApi.create('resource-definition', payload)
}

async function upsertRubro(row, ctx) {
  const code = requiredRowValue(row, 'Codigo')
  const name = requiredRowValue(row, 'Nombre')
  const nowIso = new Date().toISOString()
  const payload = {
    Codigo: code,
    Nombre: name,
    Descripcion: rowValue(row, 'Descripcion') || null,
    ColorHex: rowValue(row, 'ColorHex') || null,
    IsActive: parseBool(rowValue(row, 'IsActive'), true),
    CreatedAt: nowIso,
    UpdatedAt: null
  }

  const existing = ctx.rubrosByCode.get(code.toLowerCase())
  if (existing) {
    const id = toNumber(readField(existing, 'Id'))
    if (id == null) throw new Error('No se pudo resolver Id para actualizar Rubro.')
    await runtimeApi.update('rubro', id, { ...existing, ...payload })
    return
  }

  await runtimeApi.create('rubro', payload)
}

async function upsertResourceInstance(row, ctx) {
  const definitionCode = requiredRowValue(row, 'ResourceDefinitionCodigo', 'DefinitionCodigo')
  const internalCode = requiredRowValue(row, 'CodigoInterno', 'Codigo')
  const definition = ctx.defsByCode.get(definitionCode.toLowerCase())
  if (!definition) throw new Error(`No existe ResourceDefinition con código ${definitionCode}.`)
  const nowIso = new Date().toISOString()

  const payload = {
    ResourceDefinitionId: toNumber(readField(definition, 'Id')),
    CodigoInterno: internalCode,
    Estado: rowValue(row, 'Estado') || 'activo',
    Serie: rowValue(row, 'Serie') || null,
    Lote: rowValue(row, 'Lote') || null,
    IsActive: parseBool(rowValue(row, 'IsActive'), true),
    CreatedAt: nowIso,
    UpdatedAt: null
  }
  if (payload.ResourceDefinitionId == null) throw new Error(`ResourceDefinition ${definitionCode} sin Id válido.`)

  const existing = ctx.instancesByCode.get(internalCode.toLowerCase())
  if (existing) {
    const id = toNumber(readField(existing, 'Id'))
    if (id == null) throw new Error('No se pudo resolver Id para actualizar ResourceInstance.')
    await runtimeApi.update('resource-instance', id, { ...existing, ...payload })
    return
  }

  await runtimeApi.create('resource-instance', payload)
}

async function upsertLocation(row, ctx) {
  const code = requiredRowValue(row, 'Codigo')
  const name = requiredRowValue(row, 'Nombre')
  const rubroId = resolveRubroIdFromRow(row, ctx, false)
  const nowIso = new Date().toISOString()
  const payload = {
    Codigo: code,
    Nombre: name,
    RubroId: rubroId,
    Tipo: rowValue(row, 'Tipo') || 'deposito',
    Capacidad: toNumber(rowValue(row, 'Capacidad')) ?? 0,
    Latitud: toNumber(rowValue(row, 'Latitud')),
    Longitud: toNumber(rowValue(row, 'Longitud')),
    IsActive: parseBool(rowValue(row, 'IsActive'), true),
    CreatedAt: nowIso,
    UpdatedAt: null
  }

  const existing = ctx.locationsByCode.get(code.toLowerCase())
  if (existing) {
    const id = toNumber(readField(existing, 'Id'))
    if (id == null) throw new Error('No se pudo resolver Id para actualizar Location.')
    await runtimeApi.update('location', id, { ...existing, ...payload })
    return
  }

  await runtimeApi.create('location', payload)
}

async function upsertStockBalance(row, ctx) {
  const resourceCode = requiredRowValue(row, 'ResourceInstanceCodigo', 'CodigoInterno')
  const locationCode = requiredRowValue(row, 'LocationCodigo')
  const resource = ctx.instancesByCode.get(resourceCode.toLowerCase())
  const location = ctx.locationsByCode.get(locationCode.toLowerCase())
  if (!resource) throw new Error(`No existe ResourceInstance con código ${resourceCode}.`)
  if (!location) throw new Error(`No existe Location con código ${locationCode}.`)

  const resourceId = toNumber(readField(resource, 'Id'))
  const locationId = toNumber(readField(location, 'Id'))
  if (resourceId == null || locationId == null) throw new Error('No se pudieron resolver IDs para StockBalance.')

  const mode = normalizeKey(rowValue(row, 'Mode') || 'set')
  const stockRealInput = toNumber(rowValue(row, 'StockReal')) ?? 0
  const stockReservadoInput = toNumber(rowValue(row, 'StockReservado')) ?? 0
  const pairKey = `${resourceId}::${locationId}`
  const existing = ctx.stockByPair.get(pairKey)

  if (existing) {
    const id = toNumber(readField(existing, 'Id'))
    if (id == null) throw new Error('No se pudo resolver Id para actualizar StockBalance.')
    const currentReal = toNumber(readField(existing, 'StockReal')) ?? 0
    const currentReservado = toNumber(readField(existing, 'StockReservado')) ?? 0
    const nextReal = mode === 'add' ? currentReal + stockRealInput : stockRealInput
    const nextReservado = mode === 'add' ? currentReservado + stockReservadoInput : stockReservadoInput
    await runtimeApi.update('stock-balance', id, {
      ...existing,
      StockReal: nextReal,
      StockReservado: nextReservado,
      StockDisponible: nextReal - nextReservado
    })
    return
  }

  await runtimeApi.create('stock-balance', {
    ResourceInstanceId: resourceId,
    LocationId: locationId,
    StockReal: stockRealInput,
    StockReservado: stockReservadoInput,
    StockDisponible: stockRealInput - stockReservadoInput,
    CreatedAt: new Date().toISOString(),
    UpdatedAt: null
  })
}

function parseOperationAtFromRow(row) {
  const raw = rowValue(row, 'OperationAt', 'FechaHora', 'Fecha')
  if (!raw) return new Date().toISOString()
  const date = new Date(raw)
  if (Number.isNaN(date.getTime())) return new Date().toISOString()
  return date.toISOString()
}

function resolveResourceInstanceId(row, ctx, keys = ['ResourceInstanceCodigo', 'CodigoInterno', 'ResourceInstanceId']) {
  const token = requiredRowValue(row, ...keys)
  const numericId = toNumber(token)
  if (numericId != null && ctx.instancesById.has(numericId)) return numericId
  const fromCode = ctx.instancesByCode.get(token.toLowerCase())
  const id = toNumber(readField(fromCode, 'Id'))
  if (id == null) throw new Error(`No existe instancia con identificador ${token}.`)
  return id
}

function resolveLocationId(row, ctx, keys = ['LocationCodigo', 'LocationId']) {
  const token = requiredRowValue(row, ...keys)
  const numericId = toNumber(token)
  if (numericId != null && ctx.locationsById.has(numericId)) return numericId
  const fromCode = ctx.locationsByCode.get(token.toLowerCase())
  const id = toNumber(readField(fromCode, 'Id'))
  if (id == null) throw new Error(`No existe ubicación con identificador ${token}.`)
  return id
}

function resolveResourceRubroId(resourceInstanceId, ctx) {
  const resource = ctx.instancesById.get(resourceInstanceId)
  const rubroId = toNumber(readField(resource, 'Rubroid'))
  if (rubroId != null) return rubroId

  const definitionId = toNumber(readField(resource, 'Resourcedefinitionid') ?? readField(resource, 'ResourceDefinitionId'))
  const definition = ctx.defsById.get(definitionId)
  return toNumber(readField(definition, 'Rubroid'))
}

function resolveLocationRubroId(locationId, ctx) {
  const location = ctx.locationsById.get(locationId)
  return toNumber(readField(location, 'Rubroid'))
}

async function executeOpsRecepcionRow(row, ctx) {
  const resourceinstanceid = resolveResourceInstanceId(row, ctx)
  const targetlocationid = resolveLocationId(row, ctx, ['TargetLocationCodigo', 'LocationCodigo', 'TargetLocationId', 'LocationId'])
  const resourceRubroId = resolveResourceRubroId(resourceinstanceid, ctx)
  const targetRubroId = resolveLocationRubroId(targetlocationid, ctx)
  const explicitRubroId = resolveRubroIdFromRow(row, ctx, false)
  const rubroid = explicitRubroId ?? resourceRubroId

  if (rubroid == null) throw new Error('No se pudo determinar el rubro de la recepción.')
  if (resourceRubroId != null && rubroid !== resourceRubroId) throw new Error('El recurso pertenece a otro rubro.')
  if (targetRubroId != null && rubroid !== targetRubroId) throw new Error('El depósito destino pertenece a otro rubro.')

  const quantity = toNumber(requiredRowValue(row, 'Quantity', 'Cantidad'))
  if (quantity == null || quantity <= 0) throw new Error('Quantity debe ser mayor a 0.')

  const unitcost = toNumber(rowValue(row, 'UnitCost', 'Costo'))
  const referenceno = rowValue(row, 'ReferenceNo', 'Referencia') || null
  const notes = rowValue(row, 'Notes', 'Nota', 'Observacion') || null
  const confirmar = parseBool(rowValue(row, 'Confirmar', 'Confirm', 'AutoConfirm'), true)
  const operationat = parseOperationAtFromRow(row)

  await runtimeApi.createOpsRecepcion({
    rubroid,
    resourceinstanceid,
    targetlocationid,
    quantity,
    unitcost,
    referenceno,
    notes,
    operationat,
    confirmar
  })
}

function normalizeMovementType(value) {
  const key = normalizeKey(value)
  if (key === 'transferencia' || key === 'transfer') return 'transferencia'
  if (key === 'egreso' || key === 'outbound' || key === 'salida') return 'egreso'
  return ''
}

async function executeOpsDespachoRow(row, ctx) {
  const movementtype = normalizeMovementType(requiredRowValue(row, 'MovementType', 'TipoMovimiento', 'Tipo'))
  if (!movementtype) throw new Error('MovementType inválido. Usa egreso o transferencia.')

  const resourceinstanceid = resolveResourceInstanceId(row, ctx)
  const sourcelocationid = resolveLocationId(row, ctx, ['SourceLocationCodigo', 'LocationOrigenCodigo', 'SourceLocationId'])
  const sourceRubroId = resolveLocationRubroId(sourcelocationid, ctx)
  let targetlocationid = null
  let targetRubroId = null

  if (movementtype === 'transferencia') {
    targetlocationid = resolveLocationId(row, ctx, ['TargetLocationCodigo', 'LocationDestinoCodigo', 'TargetLocationId'])
    if (targetlocationid === sourcelocationid) {
      throw new Error('En transferencia, origen y destino deben ser distintos.')
    }
    targetRubroId = resolveLocationRubroId(targetlocationid, ctx)
  }

  const resourceRubroId = resolveResourceRubroId(resourceinstanceid, ctx)
  const explicitRubroId = resolveRubroIdFromRow(row, ctx, false)
  const rubroid = explicitRubroId ?? resourceRubroId
  if (rubroid == null) throw new Error('No se pudo determinar el rubro del despacho.')
  if (resourceRubroId != null && rubroid !== resourceRubroId) throw new Error('El recurso pertenece a otro rubro.')
  if (sourceRubroId != null && rubroid !== sourceRubroId) throw new Error('El depósito origen pertenece a otro rubro.')
  if (targetRubroId != null && rubroid !== targetRubroId) throw new Error('El depósito destino pertenece a otro rubro.')

  const quantity = toNumber(requiredRowValue(row, 'Quantity', 'Cantidad'))
  if (quantity == null || quantity <= 0) throw new Error('Quantity debe ser mayor a 0.')

  const unitcost = toNumber(rowValue(row, 'UnitCost', 'Costo'))
  const referenceno = rowValue(row, 'ReferenceNo', 'Referencia') || null
  const notes = rowValue(row, 'Notes', 'Nota', 'Observacion') || null
  const confirmar = parseBool(rowValue(row, 'Confirmar', 'Confirm', 'AutoConfirm'), true)
  const operationat = parseOperationAtFromRow(row)

  await runtimeApi.createOpsDespacho({
    rubroid,
    movementtype,
    resourceinstanceid,
    sourcelocationid,
    targetlocationid,
    quantity,
    unitcost,
    referenceno,
    notes,
    operationat,
    confirmar
  })
}

async function executeBulkImport() {
  error.value = ''
  successMessage.value = ''
  bulkErrors.value = []

  if (bulkMissingHeaders.value.length) {
    error.value = `Faltan columnas obligatorias: ${bulkMissingHeaders.value.map(prettyBulkField).join(', ')}.`
    return
  }

  const rows = bulkParsedRows.value
  if (!rows.length) {
    error.value = 'No se detectaron filas para importar.'
    return
  }

  let duplicateErrors = []
  if (bulkEntity.value === 'resource-definition') {
    duplicateErrors = validateNoDuplicates(rows, row => rowValue(row, 'Codigo').toLowerCase())
  } else if (bulkEntity.value === 'resource-instance') {
    duplicateErrors = validateNoDuplicates(rows, row => rowValue(row, 'CodigoInterno', 'Codigo').toLowerCase())
  } else if (bulkEntity.value === 'location') {
    duplicateErrors = validateNoDuplicates(rows, row => rowValue(row, 'Codigo').toLowerCase())
  } else if (bulkEntity.value === 'stock-balance') {
    duplicateErrors = validateNoDuplicates(rows, row => {
      const ri = rowValue(row, 'ResourceInstanceCodigo', 'CodigoInterno').toLowerCase()
      const loc = rowValue(row, 'LocationCodigo').toLowerCase()
      return ri && loc ? `${ri}::${loc}` : ''
    })
  } else if (bulkEntity.value === 'ops-recepcion' || bulkEntity.value === 'ops-despacho') {
    duplicateErrors = validateNoDuplicates(rows, row => rowValue(row, 'ReferenceNo', 'Referencia').toLowerCase())
  }

  if (duplicateErrors.length) {
    bulkErrors.value = duplicateErrors
    error.value = 'El CSV tiene claves duplicadas. Revísalo y vuelve a intentar.'
    return
  }

  bulkRunning.value = true
  bulkResult.value = { total: rows.length, current: 0, ok: 0, error: 0 }

  try {
    await loadCatalogs()
    if (error.value) throw new Error(error.value)
    const ctx = buildBulkContext()

    for (const row of rows) {
      try {
        if (bulkEntity.value === 'resource-definition') {
          await upsertResourceDefinition(row, ctx)
        } else if (bulkEntity.value === 'resource-instance') {
          await upsertResourceInstance(row, ctx)
        } else if (bulkEntity.value === 'location') {
          await upsertLocation(row, ctx)
        } else if (bulkEntity.value === 'stock-balance') {
          await upsertStockBalance(row, ctx)
        } else if (bulkEntity.value === 'ops-recepcion') {
          await executeOpsRecepcionRow(row, ctx)
        } else if (bulkEntity.value === 'ops-despacho') {
          await executeOpsDespachoRow(row, ctx)
        }
        bulkResult.value.ok += 1
      } catch (err) {
        bulkResult.value.error += 1
        const rowError = parseApiError(err, err?.message || 'Error inesperado.')
        bulkErrors.value.push(`Fila ${row.__line}: ${rowError}`)
      } finally {
        bulkResult.value.current += 1
      }
    }

    await loadCatalogs()
    if (bulkResult.value.error === 0) {
      successMessage.value = `Carga masiva completada. ${bulkResult.value.ok}/${bulkResult.value.total} filas procesadas.`
    } else {
      successMessage.value = `Carga masiva finalizada con errores: OK ${bulkResult.value.ok} / Error ${bulkResult.value.error}.`
    }
  } catch (err) {
    error.value = parseApiError(err, 'No se pudo ejecutar la carga masiva.')
  } finally {
    bulkRunning.value = false
  }
}

watch(bulkEntity, () => {
  bulkFile.value = null
  bulkErrors.value = []
  bulkResult.value = { total: 0, current: 0, ok: 0, error: 0 }
  loadBulkTemplate()
})

watch(bulkInput, () => {
  if (bulkRunning.value) return
  bulkErrors.value = []
  bulkResult.value = { total: 0, current: 0, ok: 0, error: 0 }
})

function goTo(path) {
  if (!path) return
  router.push(path)
}

const rubroItems = computed(() => rubros.value
  .map(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = toCode(readField(row, 'Codigo'))
    const name = toCode(readField(row, 'Nombre'))
    return {
      value: id,
      title: code && name ? `${name} (${code})` : (name || code || `#${id}`)
    }
  })
  .filter(item => item.value != null)
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

const resourceDefinitionItems = computed(() => resourceDefinitions.value
  .map(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = toCode(readField(row, 'Codigo'))
    const name = toCode(readField(row, 'Nombre'))
    return {
      value: id,
      title: code && name ? `${code} · ${name}` : (name || code || `#${id}`)
    }
  })
  .filter(item => item.value != null)
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

const resourceInstanceItems = computed(() => resourceInstances.value
  .map(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = toCode(readField(row, 'CodigoInterno'), `#${id}`)
    const state = toCode(readField(row, 'Estado'))
    return {
      value: id,
      title: state ? `${code} · ${state}` : code
    }
  })
  .filter(item => item.value != null)
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

const locationItems = computed(() => locations.value
  .map(row => {
    const id = toNumber(readField(row, 'Id'))
    const code = toCode(readField(row, 'Codigo'))
    const name = toCode(readField(row, 'Nombre'))
    return {
      value: id,
      title: code && name ? `${code} · ${name}` : (name || code || `#${id}`)
    }
  })
  .filter(item => item.value != null)
  .sort((a, b) => String(a.title).localeCompare(String(b.title), 'es')))

async function loadCatalogs() {
  loadingCatalogs.value = true
  error.value = ''
  try {
    const [rubrosRes, defsRes, instRes, locRes, stockRes] = await Promise.all([
      runtimeApi.list('rubro'),
      runtimeApi.list('resource-definition'),
      runtimeApi.list('resource-instance'),
      runtimeApi.list('location'),
      runtimeApi.list('stock-balance')
    ])
    rubros.value = toArray(rubrosRes?.data)
    resourceDefinitions.value = toArray(defsRes?.data)
    resourceInstances.value = toArray(instRes?.data)
    locations.value = toArray(locRes?.data)
    stockBalances.value = toArray(stockRes?.data)

    if (toNumber(resourceDefForm.value.RubroId) == null && rubroItems.value.length === 1) {
      resourceDefForm.value.RubroId = rubroItems.value[0].value
    }
    if (toNumber(locationForm.value.RubroId) == null && rubroItems.value.length === 1) {
      locationForm.value.RubroId = rubroItems.value[0].value
    }
  } catch (err) {
    error.value = parseApiError(err, 'No se pudo cargar catálogo inicial.')
  } finally {
    loadingCatalogs.value = false
  }
}

async function saveResourceDefinition() {
  error.value = ''
  successMessage.value = ''

  if (!toCode(resourceDefForm.value.Codigo) || !toCode(resourceDefForm.value.Nombre)) {
    error.value = 'Completa código y nombre del tipo de recurso.'
    return
  }
  if (toNumber(resourceDefForm.value.RubroId) == null) {
    error.value = 'Selecciona un rubro para el tipo de recurso.'
    return
  }

  saving.value.resourceDefinition = true
  try {
    const nowIso = new Date().toISOString()
    const payload = {
      Codigo: toCode(resourceDefForm.value.Codigo),
      Nombre: toCode(resourceDefForm.value.Nombre),
      RubroId: toNumber(resourceDefForm.value.RubroId),
      Descripcion: toCode(resourceDefForm.value.Descripcion) || null,
      TrackMode: toCode(resourceDefForm.value.TrackMode, 'none'),
      IsActive: true,
      CreatedAt: nowIso,
      UpdatedAt: null
    }
    await runtimeApi.create('resource-definition', payload)
    successMessage.value = 'Tipo de recurso creado.'
    await loadCatalogs()

    const created = resourceDefinitions.value
      .find(item => toCode(readField(item, 'Codigo')).toLowerCase() === payload.Codigo.toLowerCase())
    resourceInstanceForm.value.ResourceDefinitionId = toNumber(readField(created, 'Id'))
    resourceDefForm.value = buildResourceDefinitionForm()
    step.value = 2
  } catch (err) {
    error.value = parseApiError(err, 'No se pudo crear el tipo de recurso.')
  } finally {
    saving.value.resourceDefinition = false
  }
}

async function saveResourceInstance() {
  error.value = ''
  successMessage.value = ''

  if (toNumber(resourceInstanceForm.value.ResourceDefinitionId) == null) {
    error.value = 'Selecciona el tipo de recurso.'
    return
  }
  if (!toCode(resourceInstanceForm.value.CodigoInterno)) {
    error.value = 'Completa el código interno de la instancia.'
    return
  }

  saving.value.resourceInstance = true
  try {
    const nowIso = new Date().toISOString()
    const payload = {
      ResourceDefinitionId: toNumber(resourceInstanceForm.value.ResourceDefinitionId),
      CodigoInterno: toCode(resourceInstanceForm.value.CodigoInterno),
      Estado: toCode(resourceInstanceForm.value.Estado, 'activo'),
      Serie: toCode(resourceInstanceForm.value.Serie) || null,
      Lote: toCode(resourceInstanceForm.value.Lote) || null,
      IsActive: true,
      CreatedAt: nowIso,
      UpdatedAt: null
    }
    await runtimeApi.create('resource-instance', payload)
    successMessage.value = 'Instancia de recurso creada.'
    await loadCatalogs()

    const created = resourceInstances.value
      .find(item => toCode(readField(item, 'CodigoInterno')).toLowerCase() === payload.CodigoInterno.toLowerCase())
    stockForm.value.ResourceInstanceId = toNumber(readField(created, 'Id'))
    resourceInstanceForm.value = buildResourceInstanceForm()
    step.value = 3
  } catch (err) {
    error.value = parseApiError(err, 'No se pudo crear la instancia.')
  } finally {
    saving.value.resourceInstance = false
  }
}

async function saveLocation() {
  error.value = ''
  successMessage.value = ''

  if (!toCode(locationForm.value.Codigo) || !toCode(locationForm.value.Nombre)) {
    error.value = 'Completa código y nombre del depósito.'
    return
  }
  if (toNumber(locationForm.value.RubroId) == null) {
    error.value = 'Selecciona un rubro para el depósito.'
    return
  }

  saving.value.location = true
  try {
    const nowIso = new Date().toISOString()
    const payload = {
      Codigo: toCode(locationForm.value.Codigo),
      Nombre: toCode(locationForm.value.Nombre),
      RubroId: toNumber(locationForm.value.RubroId),
      Tipo: toCode(locationForm.value.Tipo, 'deposito'),
      Capacidad: toNumber(locationForm.value.Capacidad) ?? 0,
      Latitud: toNumber(locationForm.value.Latitud),
      Longitud: toNumber(locationForm.value.Longitud),
      IsActive: true,
      CreatedAt: nowIso,
      UpdatedAt: null
    }
    await runtimeApi.create('location', payload)
    successMessage.value = 'Depósito creado.'
    await loadCatalogs()

    const created = locations.value
      .find(item => toCode(readField(item, 'Codigo')).toLowerCase() === payload.Codigo.toLowerCase())
    stockForm.value.LocationId = toNumber(readField(created, 'Id'))
    locationForm.value = buildLocationForm()
    step.value = 4
  } catch (err) {
    error.value = parseApiError(err, 'No se pudo crear el depósito.')
  } finally {
    saving.value.location = false
  }
}

async function saveStock() {
  error.value = ''
  successMessage.value = ''

  const resourceInstanceId = toNumber(stockForm.value.ResourceInstanceId)
  const locationId = toNumber(stockForm.value.LocationId)
  const quantity = toNumber(stockForm.value.Cantidad)

  if (resourceInstanceId == null || locationId == null) {
    error.value = 'Selecciona instancia y depósito.'
    return
  }
  if (quantity == null || quantity <= 0) {
    error.value = 'La cantidad debe ser mayor a 0.'
    return
  }

  saving.value.stock = true
  try {
    const existing = stockBalances.value.find(row =>
      toNumber(readField(row, 'ResourceInstanceId')) === resourceInstanceId &&
      toNumber(readField(row, 'LocationId')) === locationId)

    if (existing) {
      const id = toNumber(readField(existing, 'Id'))
      const real = (toNumber(readField(existing, 'StockReal')) ?? 0) + quantity
      const reservado = toNumber(readField(existing, 'StockReservado')) ?? 0
      const disponible = real - reservado
      await runtimeApi.update('stock-balance', id, {
        ...existing,
        StockReal: real,
        StockDisponible: disponible
      })
      successMessage.value = 'Stock actualizado en depósito.'
    } else {
      await runtimeApi.create('stock-balance', {
        ResourceInstanceId: resourceInstanceId,
        LocationId: locationId,
        StockReal: quantity,
        StockReservado: 0,
        StockDisponible: quantity,
        CreatedAt: new Date().toISOString(),
        UpdatedAt: null
      })
      successMessage.value = 'Stock inicial cargado.'
    }

    stockForm.value = buildStockForm()
    await loadCatalogs()
  } catch (err) {
    error.value = parseApiError(err, 'No se pudo cargar stock inicial.')
  } finally {
    saving.value.stock = false
  }
}

onMounted(() => {
  loadCatalogs()
  loadBulkTemplate()
})
</script>

<style scoped>
.setup-head {
  display: flex;
  align-items: center;
  gap: 12px;
}

.setup-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--sb-primary-soft, rgba(37,99,235,0.1));
}

.setup-card,
.status-card,
.bulk-card {
  border: 1px solid var(--sb-border-soft);
  border-radius: 14px;
}

.bulk-guide {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 8px;
}

.bulk-guide-step {
  border: 1px solid var(--sb-border-soft);
  border-radius: 10px;
  padding: 8px 10px;
  font-size: 0.78rem;
  color: var(--sb-text-soft, #64748b);
  background: color-mix(in srgb, var(--sb-surface) 96%, transparent);
  display: flex;
  align-items: center;
  gap: 8px;
}

.bulk-guide-step > span {
  width: 18px;
  height: 18px;
  border-radius: 999px;
  background: var(--sb-primary-soft, rgba(37,99,235,0.1));
  color: var(--sb-primary, #2563eb);
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 0.72rem;
}

.bulk-panel {
  border-radius: 12px;
}

.bulk-panel-title {
  font-weight: 700;
  margin-bottom: 8px;
}

.bulk-preview {
  border-radius: 12px;
}

.bulk-preview-table {
  border: 1px solid var(--sb-border-soft);
  border-radius: 10px;
  overflow: hidden;
}

.setup-view :deep(.bulk-preview-table table) {
  background: var(--sb-surface, #ffffff);
}

.setup-view :deep(.bulk-preview-table th),
.setup-view :deep(.bulk-preview-table td),
.setup-view :deep(.bulk-preview-table .v-table__wrapper > table > thead > tr > th),
.setup-view :deep(.bulk-preview-table .v-table__wrapper > table > tbody > tr > td) {
  color: var(--sb-text, #0f172a) !important;
  opacity: 1 !important;
}

.setup-view :deep(.bulk-preview-table th) {
  font-weight: 700;
  font-size: 0.75rem;
  letter-spacing: 0.03em;
  background: color-mix(in srgb, var(--sb-primary-soft, rgba(37, 99, 235, 0.1)) 40%, var(--sb-surface, #ffffff));
}

.bulk-error-list {
  max-height: 220px;
  overflow: auto;
  border: 1px solid var(--sb-border-soft);
  border-radius: 10px;
}

.status-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px dashed color-mix(in srgb, var(--sb-border-soft) 75%, transparent);
}

.status-row:last-child {
  border-bottom: 0;
}

.setup-view :deep(.v-stepper-header) {
  border-bottom: 1px solid var(--sb-border-soft);
}

.setup-view :deep(.v-card-title),
.setup-view :deep(.v-label),
.setup-view :deep(.v-field__input),
.setup-view :deep(.v-stepper-item__title),
.setup-view :deep(.v-stepper-item__subtitle) {
  color: var(--sb-text, #0f172a) !important;
}

@media (max-width: 1100px) {
  .bulk-guide {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 620px) {
  .bulk-guide {
    grid-template-columns: 1fr;
  }
}
</style>
