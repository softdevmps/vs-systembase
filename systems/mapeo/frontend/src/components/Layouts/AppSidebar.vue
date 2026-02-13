<template>
  <v-navigation-drawer
    app
    :model-value="drawer"
    @update:model-value="emit('update:drawer', $event)"
    width="260"
    class="sidebar"
  >
    <div class="sidebar-header">
      <v-icon color="primary" size="28">mdi-view-dashboard</v-icon>
      <span class="sidebar-title">{{ appTitle }}</span>
    </div>

    <v-divider />

    <v-list nav density="compact" class="sidebar-list">
      <SidebarItem
        v-for="item in menuItems"
        :key="item.id"
        :item="item"
        :depth="0"
        :active-path="route.path"
      />
    </v-list>
  </v-navigation-drawer>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import SidebarItem from './SidebarItem.vue'
import frontendConfig from '../../config/frontend-config.json'

const props = defineProps({
  drawer: {
    type: Boolean,
    required: true
  },
  menu: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['update:drawer'])
const route = useRoute()

const appTitle = computed(() => frontendConfig?.system?.appTitle || 'Sistema')

const menuItems = computed(() => props.menu || [])
</script>

<style scoped>
.sidebar {
  border-right: 1px solid rgba(0, 0, 0, 0.08);
}

.sidebar-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 16px;
}

.sidebar-title {
  font-weight: 600;
  font-size: 1.1rem;
}

.sidebar-list {
  padding-top: 8px;
}

.sidebar-active {
  background-color: rgba(25, 118, 210, 0.12);
  color: #1976d2;
}

.v-icon {
  opacity: 0.85;
}
</style>
