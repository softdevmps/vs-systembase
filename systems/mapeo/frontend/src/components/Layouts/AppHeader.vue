<template>
  <v-app-bar color="primary" dark elevation="1" class="app-header">
    <v-btn icon @click="$emit('toggle-drawer')">
      <v-icon>mdi-menu</v-icon>
    </v-btn>

    <v-app-bar-title class="d-flex align-center">
      <v-icon class="mr-2" size="22">mdi-view-dashboard</v-icon>
      <span class="app-title">{{ appTitle }}</span>
    </v-app-bar-title>

    <v-spacer />

    <v-tooltip text="Cerrar sesión">
      <template #activator="{ props }">
        <v-btn v-bind="props" icon @click="logout">
          <v-icon>mdi-logout</v-icon>
        </v-btn>
      </template>
    </v-tooltip>
  </v-app-bar>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import frontendConfig from '../../config/frontend-config.json'

const router = useRouter()

const appTitle = computed(() => frontendConfig?.system?.appTitle || 'Sistema')

function logout() {
  localStorage.removeItem('token')
  router.push('/login')
}
</script>

<style scoped>
.app-header {
  border-bottom: 1px solid rgba(255, 255, 255, 0.15);
}

.app-title {
  font-weight: 600;
  letter-spacing: 0.3px;
}
</style>
