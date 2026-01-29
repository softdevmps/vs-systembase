<template>
  <v-dialog v-model="open" max-width="500">
    <v-card>
      <v-card-title>
        {{ menu ? 'Editar menú' : 'Nuevo menú' }}
      </v-card-title>

      <v-card-text>
        <v-text-field v-model="form.titulo" label="Título" />
        <v-text-field v-model="form.icono" label="Icono (mdi-*)" />
        <v-text-field v-model="form.ruta" label="Ruta" />
        <v-text-field v-model.number="form.orden" label="Orden" type="number" />

        <v-select v-model="form.padreId" :items="padres" item-title="titulo" item-value="id" label="Menú padre"
          clearable />
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn text @click="cerrar">Cancelar</v-btn>
        <v-btn color="primary" @click="guardar">Guardar</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import MenuService from '../../api/menu.service'

const props = defineProps({
  menu: Object,
  menus: Array
})

const emit = defineEmits(['cerrar', 'guardado'])

const open = ref(true)

const form = ref({
  titulo: '',
  icono: '',
  ruta: '',
  orden: 1,
  padreId: null,
  rolesIds: [1]
})

watch(
  () => props.menu,
  (m) => {
    if (m) {
      Object.assign(form.value, m)
    }
  },
  { immediate: true }
)

const padres = computed(() =>
  props.menus.map(m => ({ id: m.id, titulo: m.titulo }))
)

function cerrar() {
  open.value = false
  emit('cerrar')
}

async function guardar() {
  if (props.menu) {
    await MenuService.editar(props.menu.id, form.value)
  } else {
    await MenuService.crear(form.value)
  }
  emit('guardado')
}
</script>
