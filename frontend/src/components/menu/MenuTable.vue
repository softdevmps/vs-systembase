<template>
  <v-card>
    <v-table>
      <thead>
        <tr>
          <th>Menú</th>
          <th>Ruta</th>
          <th>Orden</th>
          <th>Acciones</th>
        </tr>
      </thead>

      <tbody>
        <template v-for="menu in menus" :key="menu.id">
          <MenuRow :menu="menu" :nivel="0" @editar="$emit('editar', $event)" />
        </template>
      </tbody>
    </v-table>
  </v-card>
</template>

<script setup>
import MenuRow from './MenuRow.vue'

defineProps({
  menus: Array
})

defineEmits(['editar'])
</script>
