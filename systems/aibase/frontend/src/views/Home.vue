<template>
  <v-container fluid class="home-dashboard">
    <v-row class="mb-4">
      <v-col cols="12">
        <v-card class="card hero-card">
          <div class="hero-content">
            <div class="hero-text">
              <div class="hero-icon">
                <v-icon color="primary" size="26">mdi-chart-box-outline</v-icon>
              </div>
              <div>
                <h1>Panel general</h1>
                <p>Vista rápida de entidades y actividad reciente.</p>
              </div>
            </div>
            <div class="hero-actions">
              <v-btn
                v-if="primaryEntity"
                class="sb-btn ghost"
                variant="text"
                @click="goToEntity(primaryEntity)"
              >
                Ver {{ primaryEntity.label }}
              </v-btn>
              <v-btn
                v-if="secondaryEntity"
                class="sb-btn primary"
                @click="goToEntity(secondaryEntity)"
              >
                Ver {{ secondaryEntity.label }}
              </v-btn>
            </div>
          </div>
        </v-card>
      </v-col>
    </v-row>

    <v-row dense>
      <v-col v-for="card in statCards" :key="card.label" cols="12" sm="6" md="4" lg="2">
        <v-card class="card stat-card">
          <div class="stat-icon">
            <v-icon :color="card.color || 'primary'">{{ card.icon }}</v-icon>
          </div>
          <div>
            <div class="stat-label">{{ card.label }}</div>
            <div class="stat-value">{{ card.value }}</div>
          </div>
        </v-card>
      </v-col>
    </v-row>

    <v-alert
      v-if="error"
      class="mt-4"
      type="warning"
      variant="tonal"
      density="comfortable"
      border="start"
      icon="mdi-alert-circle-outline"
      :text="error"
    />

    <v-row class="mt-4" dense>
      <v-col cols="12" md="5">
        <v-card class="card">
          <v-card-title class="d-flex align-center">
            <v-icon class="mr-2" color="primary">mdi-table-large</v-icon>
            Registros por entidad
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loading" class="sb-skeleton" style="height: 160px;"></div>
            <div v-else-if="entitySummaries.length === 0" class="text-caption text-medium-emphasis">
              No hay entidades configuradas.
            </div>
            <div v-else class="entity-list">
              <div v-for="summary in entitySummaries" :key="summary.key" class="entity-item">
                <div class="entity-head">
                  <div class="entity-title">
                    <v-icon size="16" class="mr-1" :color="summary.ok ? 'primary' : 'warning'">
                      {{ summary.icon }}
                    </v-icon>
                    {{ summary.label }}
                  </div>
                  <strong>{{ summary.count }}</strong>
                </div>
                <v-progress-linear
                  :model-value="getEntityBarValue(summary)"
                  :color="summary.ok ? 'primary' : 'warning'"
                  height="7"
                  rounded
                />
              </div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="12" md="7">
        <v-card class="card">
          <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
              <v-icon class="mr-2" color="primary">mdi-clock-outline</v-icon>
              Actividad reciente
            </div>
            <span class="text-caption text-medium-emphasis">{{ lastUpdatedLabel }}</span>
          </v-card-title>
          <v-divider />
          <v-card-text>
            <div v-if="loading" class="sb-skeleton" style="height: 120px;"></div>
            <div v-else-if="recentActivity.length === 0" class="text-caption text-medium-emphasis">
              No hay actividad reciente.
            </div>
            <div v-else class="recent-list">
              <div v-for="item in recentActivity" :key="item.key" class="recent-item">
                <div class="recent-dot"></div>
                <div>
                  <div class="recent-title">{{ item.title }}</div>
                  <div class="recent-meta">
                    {{ item.entityLabel }} · {{ item.subtitle }}
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
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import runtimeApi from '../api/runtime.service'
import frontendConfig from '../config/frontend-config.json'
import { toKebab } from '../utils/slug'

const router = useRouter()
const loading = ref(true)
const error = ref('')
const entityStates = ref([])

function normalizeRecord(record) {
  if (!record || typeof record !== 'object') return record
  const copy = { ...record }
  const keyMap = new Map(Object.keys(copy).map(k => [k.toLowerCase(), k]))
  const ensure = name => {
    if (copy[name] !== undefined) return
    const matchKey = keyMap.get(String(name).toLowerCase())
    if (matchKey) copy[name] = copy[matchKey]
  }
  ;[
    'Id',
    'Name',
    'Title',
    'Slug',
    'Key',
    'Descripcion',
    'Description',
    'CreatedAt',
    'UpdatedAt',
    'UpdateAt',
    'FechaHora',
    'Status'
  ].forEach(ensure)
  return copy
}

function pick(obj, ...keys) {
  for (const key of keys) {
    const value = obj?.[key]
    if (value !== undefined && value !== null && String(value).trim() !== '') return value
  }
  return null
}

function parseDateValue(raw) {
  if (!raw) return null
  if (raw instanceof Date) {
    const time = raw.getTime()
    return Number.isNaN(time) ? null : time
  }
  if (typeof raw === 'number') {
    if (!Number.isFinite(raw)) return null
    return raw > 1e12 ? raw : raw * 1000
  }
  const str = String(raw).trim()
  if (!str) return null
  const direct = new Date(str)
  if (!Number.isNaN(direct.getTime())) return direct.getTime()

  const match = str.match(/(\d{1,2})[\/-](\d{1,2})[\/-](\d{2,4})(?:[\s,]+(\d{1,2})(?::(\d{2}))?)?/)
  if (!match) return null
  const day = Number(match[1])
  const month = Number(match[2])
  let year = Number(match[3])
  const hour = match[4] ? Number(match[4]) : 0
  const minute = match[5] ? Number(match[5]) : 0
  if (year < 100) year += 2000
  const parsed = new Date(year, month - 1, day, hour, minute)
  return Number.isNaN(parsed.getTime()) ? null : parsed.getTime()
}

const entityDefs = computed(() => (frontendConfig?.entities || [])
  .filter(e => e?.name)
  .map(entity => ({
    key: toKebab(entity?.routeSlug || entity?.name),
    route: `/${toKebab(entity?.routeSlug || entity?.name)}`,
    slug: toKebab(entity?.routeSlug || entity?.name),
    label: entity?.menuLabel || entity?.displayName || entity?.name,
    icon: entity?.menuIcon || 'mdi-table'
  })))

const primaryEntity = computed(() => entityDefs.value[0] || null)
const secondaryEntity = computed(() => entityDefs.value[1] || null)

function goToEntity(entity) {
  if (!entity?.route) return
  router.push(entity.route)
}

const loadedCount = computed(() => entityStates.value.filter(e => e.ok).length)
const loadErrors = computed(() => entityStates.value.filter(e => !e.ok).length)
const entitiesWithDataCount = computed(() => entityStates.value.filter(e => e.count > 0).length)
const totalRecords = computed(() => entityStates.value.reduce((acc, item) => acc + (item.count || 0), 0))

function getRecordTimestamp(item) {
  return parseDateValue(pick(
    item,
    'UpdatedAt',
    'UpdateAt',
    'updatedAt',
    'updateAt',
    'CreatedAt',
    'createdAt',
    'FechaHora',
    'fechaHora'
  ))
}

const last24hRecords = computed(() => {
  const limit = Date.now() - (24 * 60 * 60 * 1000)
  let total = 0
  for (const entity of entityStates.value) {
    for (const item of entity.items || []) {
      const ts = getRecordTimestamp(item)
      if (ts && ts >= limit) total++
    }
  }
  return total
})

const statCards = computed(() => ([
  { label: 'Entidades', value: entityDefs.value.length, icon: 'mdi-table-large' },
  { label: 'Cargadas', value: loadedCount.value, icon: 'mdi-database-check', color: 'green' },
  { label: 'Con datos', value: entitiesWithDataCount.value, icon: 'mdi-format-list-bulleted-square', color: 'teal' },
  { label: 'Registros', value: totalRecords.value, icon: 'mdi-counter' },
  { label: 'Últimas 24h', value: last24hRecords.value, icon: 'mdi-timer-outline', color: 'primary' },
  { label: 'Fallas carga', value: loadErrors.value, icon: 'mdi-alert-circle-outline', color: 'warning' }
]))

const maxEntityCount = computed(() => Math.max(...entityStates.value.map(e => e.count || 0), 1))
function getEntityBarValue(summary) {
  if (!summary?.count) return summary?.ok ? 8 : 4
  return Math.round((summary.count / maxEntityCount.value) * 100)
}

const entitySummaries = computed(() => [...entityStates.value]
  .sort((a, b) => (b.count || 0) - (a.count || 0))
  .slice(0, 8))

function buildRecentTitle(item) {
  const id = pick(item, 'Id', 'id')
  return String(
    pick(item, 'Name', 'name', 'Title', 'title', 'Slug', 'slug', 'Key', 'key', 'Descripcion', 'Description')
    || (id != null ? `Registro #${id}` : 'Registro')
  )
}

function formatDate(value) {
  if (!value) return 'Sin fecha'
  return new Date(value).toLocaleString('es-AR')
}

const recentActivity = computed(() => {
  const rows = []
  for (const entity of entityStates.value) {
    for (const item of entity.items || []) {
      const rowIndex = rows.length
      const ts = getRecordTimestamp(item)
      rows.push({
        key: `${entity.key}-${pick(item, 'Id', 'id', 'Key', 'key', 'Slug', 'slug') || rowIndex}`,
        entityLabel: entity.label,
        title: buildRecentTitle(item),
        subtitle: formatDate(ts),
        sortValue: ts || 0
      })
    }
  }
  return rows
    .sort((a, b) => b.sortValue - a.sortValue)
    .slice(0, 8)
})

const lastUpdatedLabel = computed(() => {
  const timestamps = recentActivity.value.map(item => item.sortValue).filter(Boolean)
  if (!timestamps.length) return '—'
  return new Date(Math.max(...timestamps)).toLocaleString('es-AR')
})

async function load() {
  loading.value = true
  error.value = ''

  const defs = entityDefs.value
  const results = await Promise.all(defs.map(async def => {
    try {
      const response = await runtimeApi.list(def.slug)
      const items = Array.isArray(response?.data) ? response.data.map(normalizeRecord) : []
      return { ...def, ok: true, count: items.length, items }
    } catch (err) {
      return { ...def, ok: false, count: 0, items: [], error: err?.message || 'Error' }
    }
  }))

  entityStates.value = results
  if (results.some(r => !r.ok)) {
    error.value = 'No se pudieron cargar algunas entidades.'
  }
  loading.value = false
}

onMounted(load)
</script>

<style scoped>
.home-dashboard {
  padding-bottom: 32px;
}

.hero-card {
  padding: 20px 24px;
}

.hero-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.hero-text {
  display: flex;
  gap: 14px;
  align-items: center;
}

.hero-text h1 {
  margin: 0;
  font-size: 1.4rem;
}

.hero-text p {
  margin: 4px 0 0;
  color: var(--sb-text-soft, var(--sb-muted));
}

.hero-icon {
  width: 44px;
  height: 44px;
  border-radius: 14px;
  background: var(--sb-primary-soft);
  display: flex;
  align-items: center;
  justify-content: center;
}

.hero-actions {
  display: flex;
  gap: 10px;
}

.stat-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
}

.stat-icon {
  width: 36px;
  height: 36px;
  border-radius: 12px;
  background: var(--sb-primary-soft);
  display: flex;
  align-items: center;
  justify-content: center;
}

.stat-label {
  font-size: 0.7rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--sb-text-soft, var(--sb-muted));
}

.stat-value {
  font-size: 1.2rem;
  font-weight: 600;
}

.progress-row {
  display: flex;
  justify-content: space-between;
  font-size: 0.85rem;
  color: var(--sb-text-soft, var(--sb-muted));
  margin-bottom: 6px;
}

.entity-list {
  display: grid;
  gap: 14px;
}

.entity-item {
  display: grid;
  gap: 6px;
}

.entity-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.entity-title {
  display: inline-flex;
  align-items: center;
  font-weight: 600;
}

.recent-list {
  display: grid;
  gap: 12px;
}

.recent-item {
  display: flex;
  gap: 10px;
  align-items: flex-start;
}

.recent-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--sb-primary);
  margin-top: 6px;
}

.recent-title {
  font-weight: 600;
}

.recent-meta {
  font-size: 0.8rem;
  color: var(--sb-text-soft, var(--sb-muted));
}

.split-stats {
  display: flex;
  gap: 24px;
  flex-wrap: wrap;
}

@media (max-width: 960px) {
  .hero-content {
    flex-direction: column;
    align-items: flex-start;
  }
  .hero-actions {
    width: 100%;
    justify-content: flex-start;
  }
}
</style>
