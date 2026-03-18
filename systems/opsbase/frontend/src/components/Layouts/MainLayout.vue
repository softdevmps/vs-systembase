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

  const catalogChildren = [
    entityMenuItem('resourcedefinition', { icono: 'mdi-shape-plus-outline' }),
    entityMenuItem('resourceinstance', { icono: 'mdi-cube-outline' }),
    entityMenuItem('attributedefinition', { icono: 'mdi-tag-outline' }),
    entityMenuItem('attributevalue', { icono: 'mdi-form-textbox' })
  ].filter(Boolean)

  const ubicacionesChildren = [
    {
      id: 'ops-depositos',
      titulo: 'Red logística',
      icono: 'mdi-map-marker-multiple-outline',
      ruta: '/depositos'
    },
    {
      id: 'ops-deposito-create',
      titulo: 'Alta de depósito',
      icono: 'mdi-map-marker-plus-outline',
      ruta: '/depositos/nuevo'
    },
    entityMenuItem('location', { titulo: 'Ubicaciones base', icono: 'mdi-map-marker-radius-outline' })
  ].filter(Boolean)

  const operacionesChildren = [
    {
      id: 'ops-kardex',
      titulo: 'Kardex',
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
      id: 'ops-recepcion',
      titulo: 'Recepción guiada',
      icono: 'mdi-truck-delivery-outline',
      ruta: '/recepcion'
    },
    {
      id: 'ops-despacho',
      titulo: 'Despacho guiado',
      icono: 'mdi-truck-fast-outline',
      ruta: '/despacho'
    },
    entityMenuItem('movement', { titulo: 'Movimientos', icono: 'mdi-swap-horizontal-bold' }),
    entityMenuItem('movementline', { titulo: 'Líneas de movimiento', icono: 'mdi-format-list-bulleted' })
  ].filter(Boolean)

  const stockChildren = [
    entityMenuItem('stockbalance', { titulo: 'Saldos de stock', icono: 'mdi-archive-outline' })
  ].filter(Boolean)

  const controlChildren = [
    entityMenuItem('operationaudit', { titulo: 'Auditoría operativa', icono: 'mdi-shield-check-outline' })
  ].filter(Boolean)

  return [
    {
      id: 'ops-center',
      titulo: 'Centro operativo',
      icono: 'mdi-monitor-dashboard',
      ruta: '/home'
    },
    {
      id: 'catalogo',
      titulo: 'Catálogo',
      icono: 'mdi-shape-outline',
      children: catalogChildren
    },
    {
      id: 'ubicaciones',
      titulo: 'Ubicaciones',
      icono: 'mdi-map-marker-outline',
      children: ubicacionesChildren
    },
    {
      id: 'operaciones',
      titulo: 'Operaciones',
      icono: 'mdi-transit-transfer',
      children: operacionesChildren
    },
    {
      id: 'stock',
      titulo: 'Stock',
      icono: 'mdi-archive-outline',
      children: stockChildren
    },
    {
      id: 'control',
      titulo: 'Control',
      icono: 'mdi-shield-check-outline',
      children: controlChildren
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
