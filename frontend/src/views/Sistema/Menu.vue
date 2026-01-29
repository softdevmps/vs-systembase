<template>
  <v-container fluid>
    <v-row align="center" class="mb-4">
      <v-col>
        <h1>📋 Administración de Menú</h1>
      </v-col>

      <v-col cols="auto">
        <v-btn color="primary" @click="nuevoMenu">
          <v-icon left>mdi-plus</v-icon>
          Nuevo menú
        </v-btn>
      </v-col>
    </v-row>

    <MenuTable :menus="menus" @editar="editarMenu" />

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
