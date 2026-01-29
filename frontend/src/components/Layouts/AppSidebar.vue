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

      <template v-for="item in menu" :key="item.id">

        <!-- ITEM SIMPLE -->
        <v-list-item v-if="!item.children || item.children.length === 0" :to="item.ruta" router link
          active-class="sidebar-active">
          <template #prepend>
            <v-icon>{{ item.icono }}</v-icon>
          </template>

          <v-list-item-title>
            {{ item.titulo }}
          </v-list-item-title>
        </v-list-item>

        <!-- ITEM PADRE -->
        <v-list-group v-else :value="isGroupOpen(item)">
          <template #activator="{ props }">
            <v-list-item v-bind="props">
              <template #prepend>
                <v-icon>{{ item.icono }}</v-icon>
              </template>

              <v-list-item-title>
                {{ item.titulo }}
              </v-list-item-title>
            </v-list-item>
          </template>

          <v-list-item v-for="child in item.children" :key="child.id" :to="child.ruta" router link
            active-class="sidebar-active" class="sidebar-child">
            <template #prepend>
              <v-icon size="18">{{ child.icono }}</v-icon>
            </template>

            <v-list-item-title>
              {{ child.titulo }}
            </v-list-item-title>
          </v-list-item>

        </v-list-group>

      </template>

    </v-list>
  </v-navigation-drawer>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import MenuService from '../../api/menu.service'

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
const menu = ref([])
const route = useRoute()

/* =========================
   METHODS
========================= */
function isGroupOpen(item) {
  return item.children?.some(child => child.ruta === route.path)
}

/* =========================
   LIFECYCLE
========================= */
onMounted(async () => {
  const response = await MenuService.getMenuTree()
  menu.value = response.data
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
.sidebar-child {
  padding-left: 32px;
}

/* ICONS */
.v-icon {
  opacity: 0.85;
}
</style>
