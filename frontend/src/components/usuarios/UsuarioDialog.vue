<template>
  <v-dialog v-model="model" max-width="600px">
    <v-card>
      <v-card-title>
        {{ usuarioId ? 'Editar usuario' : 'Nuevo usuario' }}
      </v-card-title>

      <v-card-text>
        <v-form>
          <v-text-field v-model="form.username" label="Username" />
          <v-text-field v-model="form.email" label="Email" />
          <v-text-field v-model="form.nombre" label="Nombre" />
          <v-text-field v-model="form.apellido" label="Apellido" />

          <v-select v-model="form.rolId" :items="roles" item-title="nombre" item-value="id" label="Rol" />

          <v-text-field v-model="form.password" label="Password" type="password"
            :hint="usuarioId ? 'Dejar vacío para no cambiar' : ''" persistent-hint />
        </v-form>
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
import usuarioService from '../../api/usuario.service.js';

export default {
  props: {
    modelValue: Boolean,
    usuarioId: Number,
    roles: Array
  },

  emits: ['update:modelValue', 'guardado'],

  data() {
    return {
      form: {
        username: '',
        email: '',
        nombre: '',
        apellido: '',
        rolId: null,
        password: ''
      }
    };
  },

  computed: {
    model: {
      get() {
        return this.modelValue;
      },
      set(v) {
        this.$emit('update:modelValue', v);
      }
    }
  },

  watch: {
    usuarioId: {
      immediate: true,
      handler(id) {
        this.reset();
        if (id) this.cargarUsuario(id);
      }
    }
  },

  methods: {
    cargarUsuario(id) {
      usuarioService.obtenerPorId(id).then(res => {
        this.form = {
          username: res.data.username,
          email: res.data.email,
          nombre: res.data.nombre,
          apellido: res.data.apellido,
          rolId: Number(res.data.rolId),
          password: ''
        };
      });
    },

    guardar() {
      const data = { ...this.form };
      if (!data.password) delete data.password;

      const req = this.usuarioId
        ? usuarioService.editar(this.usuarioId, data)
        : usuarioService.crear(data);

      req.then(() => {
        this.$emit('guardado');
        this.cerrar();
      });
    },

    cerrar() {
      this.model = false;
    },

    reset() {
      this.form = {
        username: '',
        email: '',
        nombre: '',
        apellido: '',
        rolId: null,
        password: ''
      };
    }
  }
};
</script>
