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
import { ref } from 'vue'
import AppHeader from './AppHeader.vue'
import AppSidebar from './AppSidebar.vue'

const drawer = ref(true)

const menuItems = [
  {
    id: 'home',
    titulo: 'Home',
    icono: 'mdi-home',
    ruta: '/home'
  },
  {
    id: 'aibase',
    titulo: 'AIBase',
    icono: 'mdi-brain',
    ruta: '/aibase'
  }
]
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
