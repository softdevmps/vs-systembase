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

function prettyTitle(value) {
  if (!value) return ''
  return String(value)
    .replace(/[_-]+/g, ' ')
    .replace(/([a-z0-9])([A-ZÁÉÍÓÚÑ])/g, '$1 $2')
    .replace(/\s+/g, ' ')
    .trim()
}

const menuItems = computed(() => {
  const entities = (frontendConfig?.entities || []).filter(e => e?.showInMenu !== false)

  const items = [
    {
      id: 'home',
      titulo: 'Home',
      icono: 'mdi-home',
      ruta: '/home'
    },
    {
      id: 'workflow',
      titulo: 'Workflow IA',
      icono: 'mdi-source-branch',
      children: [
        { id: 'stage-template', titulo: '1. Template', icono: 'mdi-shape-plus', ruta: '/stage/template' },
        { id: 'stage-project', titulo: '2. Proyecto', icono: 'mdi-folder-cog', ruta: '/stage/project' },
        { id: 'stage-dataset', titulo: '3. Dataset', icono: 'mdi-database-cog', ruta: '/stage/dataset' },
        { id: 'stage-rag', titulo: '4. RAG Index', icono: 'mdi-vector-link', ruta: '/stage/rag' },
        { id: 'stage-train', titulo: '5. Train LoRA', icono: 'mdi-brain', ruta: '/stage/train' },
        { id: 'stage-eval', titulo: '6. Evaluar', icono: 'mdi-chart-line', ruta: '/stage/eval' },
        { id: 'stage-deploy', titulo: '7. Deploy', icono: 'mdi-rocket-launch', ruta: '/stage/deploy' },
        { id: 'stage-playground', titulo: '8. Playground', icono: 'mdi-flask-outline', ruta: '/stage/playground' }
      ]
    },
    {
      id: 'group-management',
      titulo: 'Gestión',
      icono: 'mdi-view-list',
      children: [
        { id: 'templates', titulo: 'Templates', icono: 'mdi-table', ruta: '/templates' },
        { id: 'projects', titulo: 'Proyectos', icono: 'mdi-table', ruta: '/projects' },
        { id: 'runs', titulo: 'Runs', icono: 'mdi-table', ruta: '/runs' }
      ]
    }
  ]

  entities.forEach(entity => {
    const baseName = String(entity?.name || '').toLowerCase()
    if (baseName === 'templates' || baseName === 'projects' || baseName === 'runs') return

    const menuItem = {
      id: entity.entityId ?? entity.id ?? entity.name,
      titulo: prettyTitle(entity.menuLabel || entity.displayName || entity.name),
      icono: entity.menuIcon || 'mdi-table',
      ruta: `/${entityRoute(entity)}`
    }
    items.push(menuItem)
  })

  return items
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
