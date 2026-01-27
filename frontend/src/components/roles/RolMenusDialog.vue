<template>
  <v-dialog
    :model-value="modelValue"
    @update:model-value="cerrar"
    max-width="500"
  >
    <v-card>
      <v-card-title>
        Menús del Rol: {{ rol?.nombre }}
      </v-card-title>

      <v-card-text>
        <v-checkbox
          v-for="menu in menus"
          :key="menu.id"
          v-model="menu.asignado"
          :label="menu.titulo"
        />
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn text @click="cerrar">Cancelar</v-btn>
        <v-btn color="primary" @click="guardar">Guardar</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script>
import rolService from '../../api/rol.service';

export default {
  props: {
    modelValue: {
      type: Boolean,
      required: true
    },
    rol: {
      type: Object,
      required: true
    }
  },

  data() {
    return {
      menus: []
    };
  },

  watch: {
    modelValue(value) {
      if (value) {
        this.cargarMenus();
      }
    }
  },

  methods: {
    async cargarMenus() {
      const res = await rolService.getById(this.rol.id);
      this.menus = res.data.menus;
    },

    cerrar() {
      this.$emit('update:modelValue', false);
    },

    async guardar() {
      const menusIds = this.menus
        .filter(m => m.asignado)
        .map(m => m.id);

      await rolService.asignarMenus(this.rol.id, menusIds);
      this.cerrar();
    }
  }
};
</script>
