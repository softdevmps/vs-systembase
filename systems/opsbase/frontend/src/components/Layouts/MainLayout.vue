<template>
  <v-app>
    <AppHeader @toggle-drawer="drawer = !drawer" />

    <AppSidebar
      :drawer="drawer"
      :menu="menuItems"
      @update:drawer="drawer = $event"
    />

    <v-main class="app-main">
      <div class="app-scroll">
        <router-view />
      </div>
    </v-main>
  </v-app>
</template>

<script setup>
import { computed, ref } from 'vue'
import AppHeader from './AppHeader.vue'
import AppSidebar from './AppSidebar.vue'
import frontendConfig from '../../config/frontend-config.json'
import { toKebab } from '../../utils/slug.js'

const drawer = ref(true)

function entityRoute(entity) {
  return toKebab(entity?.routeSlug || entity?.name || 'item')
}

function normalizeKey(value) {
  return String(value || '').trim().toLowerCase().replace(/[^a-z0-9]/g, '')
}

function prettyTitle(value) {
  if (!value) return ''
  return String(value)
    .replace(/[_-]+/g, ' ')
    .replace(/([a-z0-9])([A-ZÁÉÍÓÚÑ])/g, '$1 $2')
    .replace(/\s+/g, ' ')
    .trim()
}

const ENTITY_LABELS = {
  rubro: 'Rubros',
  attributedefinition: 'Definiciones de atributos',
  attributevalue: 'Valores de atributos',
  location: 'Ubicaciones',
  movement: 'Movimientos',
  movementline: 'Líneas de movimiento',
  operationaudit: 'Auditoría operativa',
  resourcedefinition: 'Definiciones de recursos',
  resourceinstance: 'Instancias de recursos',
  stockbalance: 'Saldos de stock'
}

const ENTITY_ICONS = {
  rubro: 'mdi-shape-outline',
  attributedefinition: 'mdi-tag-outline',
  attributevalue: 'mdi-form-textbox',
  location: 'mdi-map-marker-radius-outline',
  movement: 'mdi-swap-horizontal-bold',
  movementline: 'mdi-format-list-bulleted',
  operationaudit: 'mdi-timeline-alert-outline',
  resourcedefinition: 'mdi-shape-plus-outline',
  resourceinstance: 'mdi-cube-outline',
  stockbalance: 'mdi-scale-balance'
}

const ENTITY_ORDER = {
  rubro: 5,
  resourcedefinition: 10,
  attributedefinition: 20,
  attributevalue: 30,
  resourceinstance: 40,
  location: 50,
  movement: 60,
  movementline: 70,
  stockbalance: 80,
  operationaudit: 90
}

function entityTitle(entity) {
  const key = normalizeKey(entity?.routeSlug || entity?.name || entity?.menuLabel || '')
  return ENTITY_LABELS[key] || prettyTitle(entity?.menuLabel || entity?.displayName || entity?.name)
}

function entityIcon(entity) {
  const key = normalizeKey(entity?.routeSlug || entity?.name || entity?.menuLabel || '')
  return ENTITY_ICONS[key] || entity?.menuIcon || 'mdi-table'
}

function entityOrder(entity) {
  const key = normalizeKey(entity?.routeSlug || entity?.name || entity?.menuLabel || '')
  return ENTITY_ORDER[key] ?? 999
}

const menuItems = computed(() => {
  const entities = (frontendConfig?.entities || []).filter(e => e?.showInMenu !== false)
  const keys = new Set(entities.map(entity => normalizeKey(entity?.routeSlug || entity?.name || entity?.menuLabel || '')))
  const hasOpsCore = keys.has('movement') && keys.has('movementline') && keys.has('stockbalance')
  const entityByKey = new Map(
    entities.map(entity => [normalizeKey(entity?.routeSlug || entity?.name || entity?.menuLabel || ''), entity])
  )

  function entityMenuItem(key, overrides = {}) {
    const entity = entityByKey.get(normalizeKey(key))
    if (!entity) return null
    return {
      id: entity.entityId ?? entity.id ?? entity.name,
      titulo: overrides.titulo || entityTitle(entity),
      icono: overrides.icono || entityIcon(entity),
      ruta: `/${entityRoute(entity)}`
    }
  }

  if (!hasOpsCore) {
    const generic = entities
      .map(entity => ({
        id: entity.entityId ?? entity.id ?? entity.name,
        titulo: entityTitle(entity),
        icono: entityIcon(entity),
        orden: entityOrder(entity),
        ruta: `/${entityRoute(entity)}`
      }))
      .sort((a, b) => {
        if ((a.orden ?? 999) !== (b.orden ?? 999)) return (a.orden ?? 999) - (b.orden ?? 999)
        return String(a.titulo).localeCompare(String(b.titulo), 'es')
      })

    return [
      {
        id: 'inicio',
        titulo: 'Inicio',
        icono: 'mdi-home-outline',
        ruta: '/home'
      },
      ...generic
    ]
  }

  const operacionesChildren = [
    {
      id: 'ops-setup-inicial',
      titulo: 'Carga inicial',
      icono: 'mdi-clipboard-plus-outline',
      ruta: '/setup-inicial'
    },
    {
      id: 'ops-operaciones-panel',
      titulo: 'Panel de operaciones',
      icono: 'mdi-view-dashboard-outline',
      ruta: '/operaciones'
    },
    {
      id: 'ops-recepcion',
      titulo: 'Recepcionar',
      icono: 'mdi-truck-delivery-outline',
      ruta: '/recepcion'
    },
    {
      id: 'ops-despacho',
      titulo: 'Despachar / Trasladar',
      icono: 'mdi-truck-fast-outline',
      ruta: '/despacho'
    },
    {
      id: 'ops-ajustar',
      titulo: 'Ajustar stock',
      icono: 'mdi-tune-variant',
      ruta: '/operaciones?task=ajuste'
    }
  ]

  const depositosChildren = [
    {
      id: 'ops-depositos-red',
      titulo: 'Red logística',
      icono: 'mdi-map-marker-multiple-outline',
      ruta: '/depositos'
    },
    {
      id: 'ops-depositos-listado',
      titulo: 'Listado de depósitos',
      icono: 'mdi-format-list-bulleted-square',
      ruta: '/depositos/listado'
    },
    {
      id: 'ops-depositos-alta',
      titulo: 'Alta de depósito',
      icono: 'mdi-map-marker-plus-outline',
      ruta: '/depositos/nuevo'
    }
  ]

  const administracionChildren = [
    entityMenuItem('rubro', { titulo: 'Rubros', icono: 'mdi-shape-outline' }),
    entityMenuItem('resourcedefinition', { titulo: 'Recursos', icono: 'mdi-shape-plus-outline' }),
    entityMenuItem('resourceinstance', { titulo: 'Instancias', icono: 'mdi-cube-outline' }),
    entityMenuItem('location', { titulo: 'Ubicaciones base', icono: 'mdi-map-marker-radius-outline' }),
    entityMenuItem('movement', { titulo: 'Movimientos (ABM)', icono: 'mdi-swap-horizontal-bold' }),
    entityMenuItem('movementline', { titulo: 'Líneas (ABM)', icono: 'mdi-format-list-bulleted' }),
    entityMenuItem('stockbalance', { titulo: 'Saldos (ABM)', icono: 'mdi-archive-outline' }),
    entityMenuItem('operationaudit', { titulo: 'Auditoría (ABM)', icono: 'mdi-shield-check-outline' }),
    entityMenuItem('attributedefinition', { titulo: 'Def. atributos', icono: 'mdi-tag-outline' }),
    entityMenuItem('attributevalue', { titulo: 'Valores atributos', icono: 'mdi-form-textbox' })
  ].filter(Boolean)

  return [
    {
      id: 'ops-center-home',
      titulo: 'Centro operativo',
      icono: 'mdi-monitor-dashboard',
      ruta: '/home'
    },
    {
      id: 'ops-operaciones',
      titulo: 'Operaciones',
      icono: 'mdi-transit-transfer',
      children: operacionesChildren
    },
    {
      id: 'ops-pendientes',
      titulo: 'Pendientes',
      icono: 'mdi-alert-circle-outline',
      ruta: '/pendientes'
    },
    {
      id: 'ops-depositos',
      titulo: 'Depósitos',
      icono: 'mdi-warehouse',
      children: depositosChildren
    },
    {
      id: 'ops-movimientos',
      titulo: 'Movimientos',
      icono: 'mdi-notebook-outline',
      ruta: '/kardex'
    },
    {
      id: 'ops-trazabilidad',
      titulo: 'Trazabilidad',
      icono: 'mdi-timeline-clock-outline',
      ruta: '/trazabilidad'
    },
    {
      id: 'ops-admin',
      titulo: 'Administración',
      icono: 'mdi-cog-outline',
      children: administracionChildren
    }
  ].filter(group => !group.children || group.children.length > 0)
})
</script>

<style scoped>
.app-main {
  height: calc(100vh - var(--v-layout-top));
  overflow: hidden;
}

.app-scroll {
  height: 100%;
  overflow-y: auto;
  padding: 16px;
}
</style>
