<template>
  <v-dialog
    :model-value="modelValue"
    @update:model-value="cerrar"
    max-width="400"
  >
    <v-card>
      <v-card-title>
        {{ rol ? 'Editar Rol' : 'Nuevo Rol' }}
      </v-card-title>

      <v-card-text>
        <v-text-field
          label="Nombre"
          v-model="form.nombre"
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
      default: null
    }
  },

  data() {
    return {
      form: {
        nombre: ''
      }
    };
  },

  watch: {
    rol: {
      immediate: true,
      handler(rol) {
        this.form.nombre = rol ? rol.nombre : '';
      }
    }
  },

  methods: {
    cerrar() {
      this.$emit('update:modelValue', false);
    },

    async guardar() {
      if (this.rol) {
        await rolService.editar(this.rol.id, this.form);
      } else {
        await rolService.crear({
          nombre: this.form.nombre,
          activo: true
        });
      }

      this.cerrar();
      this.$emit('guardado');
    }
  }
};
</script>
