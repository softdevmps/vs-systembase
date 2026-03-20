<template>
  <v-container fluid class="operaciones-view">
    <v-row class="mb-4 align-center">
      <v-col>
        <div class="head-wrap">
          <div class="head-icon">
            <v-icon size="24" color="primary">mdi-transit-transfer</v-icon>
          </div>
          <div>
            <h2 class="mb-1">Operaciones</h2>
            <div class="text-body-2 text-medium-emphasis">
              Ejecuta tareas operativas en flujo guiado: operar, corregir pendientes y validar resultados.
            </div>
          </div>
        </div>
      </v-col>
      <v-col cols="auto" class="d-flex ga-2 flex-wrap justify-end">
        <v-btn variant="tonal" color="primary" @click="goTo('/setup-inicial')">
          <v-icon start>mdi-clipboard-plus-outline</v-icon>
          Carga inicial
        </v-btn>
        <v-btn variant="tonal" color="primary" @click="goTo('/home')">
          <v-icon start>mdi-monitor-dashboard</v-icon>
          Centro operativo
        </v-btn>
        <v-btn variant="tonal" color="warning" @click="goTo('/pendientes')">
          <v-icon start>mdi-alert-circle-outline</v-icon>
          Ver pendientes
        </v-btn>
      </v-col>
    </v-row>

    <v-row dense class="mb-4">
      <v-col cols="12" md="4">
        <v-card :class="['task-card', isTaskActive('setup') && 'task-card--active']">
          <div class="task-head">
            <v-icon color="indigo">mdi-clipboard-plus-outline</v-icon>
            <span>Carga inicial</span>
          </div>
          <p>Alta guiada del maestro mínimo para empezar: recurso, instancia, depósito y stock inicial.</p>
          <v-btn color="primary" variant="tonal" @click="goTo('/setup-inicial')">
            Iniciar configuración
          </v-btn>
        </v-card>
      </v-col>

      <v-col cols="12" md="4">
        <v-card :class="['task-card', isTaskActive('recepcion') && 'task-card--active']">
          <div class="task-head">
            <v-icon color="green">mdi-truck-delivery-outline</v-icon>
            <span>Recepcionar</span>
          </div>
          <p>Ingreso de activos al depósito destino con validación y confirmación operativa.</p>
          <v-btn color="primary" @click="goTo('/recepcion')">
            Iniciar recepción
          </v-btn>
        </v-card>
      </v-col>

      <v-col cols="12" md="4">
        <v-card :class="['task-card', isTaskActive('despacho') && 'task-card--active']">
          <div class="task-head">
            <v-icon color="blue">mdi-truck-fast-outline</v-icon>
            <span>Despachar / Trasladar</span>
          </div>
          <p>Egreso o transferencia entre depósitos desde un flujo único con reglas de operación.</p>
          <div class="d-flex ga-2 flex-wrap">
            <v-btn color="primary" @click="goTo('/despacho?tipo=egreso')">
              Despachar
            </v-btn>
            <v-btn variant="tonal" color="primary" @click="goTo('/despacho?tipo=transferencia')">
              Trasladar
            </v-btn>
          </div>
        </v-card>
      </v-col>

      <v-col cols="12" md="4">
        <v-card :class="['task-card', isTaskActive('ajuste') && 'task-card--active']">
          <div class="task-head">
            <v-icon color="orange">mdi-tune-variant</v-icon>
            <span>Ajustar stock</span>
          </div>
          <p>Ajustes excepcionales y correcciones manuales con trazabilidad en movimientos.</p>
          <v-btn color="primary" variant="tonal" @click="goTo(movementRoute)">
            Ir a movimientos
          </v-btn>
        </v-card>
      </v-col>
    </v-row>

    <v-card class="flow-card">
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2" color="primary">mdi-map-marker-path</v-icon>
        Flujo de trabajo recomendado
      </v-card-title>
      <v-divider />
      <v-card-text>
        <div class="flow-grid">
          <button class="flow-step" @click="goTo('/setup-inicial')">
            <span class="step-index">1</span>
            <div>
              <strong>Carga inicial</strong>
              <p>Configurar maestro mínimo para operar.</p>
            </div>
          </button>
          <button class="flow-step" @click="goTo('/home')">
            <span class="step-index">2</span>
            <div>
              <strong>Centro Operativo</strong>
              <p>Ver qué tarea ejecutar primero según prioridad.</p>
            </div>
          </button>
          <button class="flow-step" @click="goTo('/operaciones')">
            <span class="step-index">3</span>
            <div>
              <strong>Operar</strong>
              <p>Recepcionar, despachar o trasladar.</p>
            </div>
          </button>
          <button class="flow-step" @click="goTo('/pendientes')">
            <span class="step-index">4</span>
            <div>
              <strong>Corregir pendientes</strong>
              <p>Resolver borradores, errores e inconsistencias.</p>
            </div>
          </button>
          <button class="flow-step" @click="goTo('/depositos')">
            <span class="step-index">5</span>
            <div>
              <strong>Validar por depósito</strong>
              <p>Comprobar impacto operativo en la red logística.</p>
            </div>
          </button>
          <button class="flow-step" @click="goTo('/kardex')">
            <span class="step-index">6</span>
            <div>
              <strong>Control de movimientos</strong>
              <p>Revisar kardex y resultados del turno.</p>
            </div>
          </button>
          <button class="flow-step" @click="goTo('/trazabilidad')">
            <span class="step-index">7</span>
            <div>
              <strong>Trazabilidad</strong>
              <p>Auditar la historia completa ante incidentes.</p>
            </div>
          </button>
        </div>
      </v-card-text>
    </v-card>
  </v-container>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import frontendConfig from '../../config/frontend-config.json'
import { toKebab } from '../../utils/slug'

const router = useRouter()
const route = useRoute()

function normalizeKey(value) {
  return String(value || '').trim().toLowerCase().replace(/[^a-z0-9]/g, '')
}

function goTo(path) {
  if (!path) return
  router.push(path)
}

const movementRoute = computed(() => {
  const entities = frontendConfig?.entities || []
  const movement = entities.find(entity => normalizeKey(entity?.routeSlug || entity?.name) === 'movement')
  return `/${toKebab(movement?.routeSlug || movement?.name || 'movement')}`
})

function isTaskActive(task) {
  const queryTask = normalizeKey(route.query?.task)
  const path = normalizeKey(route.path)
  const target = normalizeKey(task)

  if (queryTask) return queryTask === target
  if (target === 'ajuste' && path.endsWith('operacionesajuste')) return true
  if (target === 'panel' && path.endsWith('operaciones')) return true
  return false
}
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

.task-card,
.flow-card {
  border: 1px solid var(--sb-border-soft);
  border-radius: 14px;
  padding: 14px;
}

.task-card {
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.task-card--active {
  border-color: var(--sb-primary, #2563eb);
  box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.12);
}

.task-head {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 700;
}

.task-card p {
  margin: 0;
  color: var(--sb-text-soft, #64748b);
  font-size: 0.9rem;
  min-height: 50px;
}

.flow-grid {
  display: grid;
  gap: 10px;
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

.operaciones-view :deep(.v-card-title),
.operaciones-view :deep(.v-label),
.operaciones-view :deep(.v-field__input) {
  color: var(--sb-text, #0f172a) !important;
}
</style>
