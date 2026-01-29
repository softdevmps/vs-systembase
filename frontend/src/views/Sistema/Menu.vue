<template>
  <v-container fluid>

    <!-- HEADER -->
    <v-row class="mb-6 align-center">
      <v-col>
        <div class="d-flex align-center">
          <v-icon class="mr-2" color="primary" size="28">
            mdi-view-list
          </v-icon>

          <div>
            <h2 class="mb-1">Menú</h2>
            <span class="grey--text text-body-2">
              Administración de menús del sistema
            </span>
          </div>
        </div>
      </v-col>

      <v-col cols="auto">
        <v-btn color="primary" @click="nuevoMenu">
          <v-icon left>mdi-plus</v-icon>
          Nuevo menú
        </v-btn>
      </v-col>
    </v-row>

    <!-- TABLA -->
    <MenuTable :menus="menus" @editar="editarMenu" />

    <!-- DIALOG -->
    <MenuForm v-if="showForm" :menu="menuSeleccionado" :menus="menus" @cerrar="cerrarForm" @guardado="recargar" />

  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import MenuService from '../../api/menu.service'

import MenuTable from '../../components/menu/MenuTable.vue'
import MenuForm from '../../components/menu/MenuForm.vue'

const menus = ref([])
const showForm = ref(false)
const menuSeleccionado = ref(null)

async function cargarMenus() {
  const { data } = await MenuService.getMenuTree()
  menus.value = data
}

function nuevoMenu() {
  menuSeleccionado.value = null
  showForm.value = true
}

function editarMenu(menu) {
  menuSeleccionado.value = menu
  showForm.value = true
}

function cerrarForm() {
  showForm.value = false
}

async function recargar() {
  showForm.value = false
  await cargarMenus()
}

onMounted(cargarMenus)
</script>

<style scoped>
h2 {
  font-weight: 600;
}
</style>
