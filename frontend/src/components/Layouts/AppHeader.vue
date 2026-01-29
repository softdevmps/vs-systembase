<template>
  <v-app-bar color="primary" dark elevation="1" class="app-header">
    <!-- TOGGLE DRAWER -->
    <v-btn icon @click="$emit('toggle-drawer')">
      <v-icon>mdi-menu</v-icon>
    </v-btn>

    <!-- TITLE -->
    <v-app-bar-title class="d-flex align-center">
      <v-icon class="mr-2" size="22">
        mdi-view-dashboard
      </v-icon>
      <span class="app-title">
        SystemBase
      </span>
    </v-app-bar-title>

    <v-spacer />

    <!-- LOGOUT -->
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
import { useRouter } from 'vue-router'

const router = useRouter()

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
