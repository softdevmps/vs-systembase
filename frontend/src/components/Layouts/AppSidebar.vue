<template>
  <v-navigation-drawer app :model-value="drawer" @update:model-value="emit('update:drawer', $event)" width="260"
    class="sidebar">
    <!-- HEADER -->
    <div class="sidebar-header">
      <v-icon color="primary" size="28">mdi-view-dashboard</v-icon>
      <span class="sidebar-title">SystemBase</span>
    </div>

    <v-divider />

    <!-- MENU -->
    <v-list nav density="compact" class="sidebar-list">
      <SidebarItem
        v-for="item in menu"
        :key="item.id"
        :item="item"
        :depth="0"
        :active-path="route.path"
      />
    </v-list>
  </v-navigation-drawer>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useMenuStore } from '../../store/menu.store.js'
import SidebarItem from './SidebarItem.vue'

/* =========================
   PROPS & EMITS
========================= */
const props = defineProps({
  drawer: {
    type: Boolean,
    required: true
  }
})

const emit = defineEmits(['update:drawer'])

/* =========================
   STATE
========================= */
const route = useRoute()
const { state, cargarMenuTree } = useMenuStore()

/* =========================
   COMPUTED
========================= */
const menu = computed(() => state.tree)

/* =========================
   LIFECYCLE
========================= */
onMounted(() => {
  if (!state.tree.length) {
    cargarMenuTree()
  }
})
</script>

<style scoped>
/* DRAWER */
.sidebar {
  border-right: 1px solid rgba(0, 0, 0, 0.08);
}

/* HEADER */
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

/* LIST */
.sidebar-list {
  padding-top: 8px;
}

/* ACTIVE ITEM */
.sidebar-active {
  background-color: rgba(25, 118, 210, 0.12);
  color: #1976d2;
}

/* CHILD */
/* ICONS */
.v-icon {
  opacity: 0.85;
}
</style>
