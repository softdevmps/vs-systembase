<template>
  <tr>
    <td>
      <span :style="{ paddingLeft: `${nivel * 20}px` }">
        <v-icon small class="mr-1">{{ menu.icono }}</v-icon>
        {{ menu.titulo }}
      </span>
    </td>

    <td>{{ menu.ruta || '-' }}</td>
    <td>{{ menu.orden }}</td>

    <td>
      <v-btn icon size="small" @click="$emit('editar', menu)">
        <v-icon>mdi-pencil</v-icon>
      </v-btn>
    </td>
  </tr>

  <template v-for="child in menu.children" :key="child.id">
    <MenuRow :menu="child" :nivel="nivel + 1" @editar="$emit('editar', $event)" />
  </template>
</template>

<script setup>
defineProps({
  menu: Object,
  nivel: Number
})

defineEmits(['editar'])
</script>
