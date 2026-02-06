<template>
  <v-dialog v-model="model" max-width="720px">
    <v-card class="dialog-card">
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2" color="primary">
          {{ record ? 'mdi-pencil' : 'mdi-plus-box' }}
        </v-icon>
        <span class="text-h6 font-weight-medium">
          {{ record ? 'Editar registro' : 'Nuevo registro' }}
        </span>
      </v-card-title>

      <v-divider />

      <v-card-text>
        <v-form class="form">
          <v-row v-for="field in editableFields" :key="field.columnName">
            <v-col cols="12">
              <v-select
                v-if="fkOptions?.[field.columnName]"
                v-model="form[field.columnName]"
                :items="fkOptions[field.columnName].options"
                item-title="title"
                item-value="value"
                :label="field.columnName"
                clearable
                no-data-text="Sin registros"
              />

              <v-text-field
                v-else-if="field.dataType === 'string'"
                v-model="form[field.columnName]"
                :label="field.columnName"
                :maxlength="field.maxLength || undefined"
              />

              <v-text-field
                v-else-if="field.dataType === 'int' || field.dataType === 'decimal'"
                v-model="form[field.columnName]"
                type="number"
                :label="field.columnName"
              />

              <v-text-field
                v-else-if="field.dataType === 'datetime'"
                v-model="form[field.columnName]"
                type="datetime-local"
                :label="field.columnName"
              />

              <v-switch
                v-else-if="field.dataType === 'bool'"
                v-model="form[field.columnName]"
                inset
                color="green"
                :label="field.columnName"
              />

              <v-text-field
                v-else
                v-model="form[field.columnName]"
                :label="field.columnName"
              />
            </v-col>
          </v-row>
        </v-form>
      </v-card-text>

      <v-divider />

      <v-card-actions class="pa-4">
        <v-spacer />
        <v-btn variant="text" @click="cerrar">Cancelar</v-btn>
        <v-btn color="primary" @click="guardar">Guardar</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script>
import datosService from '../../api/datos.service.js';

export default {
  props: {
    modelValue: Boolean,
    record: Object,
    fields: Array,
    fkOptions: Object,
    systemId: Number,
    entityId: Number
  },

  emits: ['update:modelValue', 'guardado'],

  data() {
    return {
      form: {}
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
    },

    editableFields() {
      if (!this.fields) return [];
      return this.fields.filter(f => {
        if (f.isIdentity) return false;
        if (this.record && f.isPrimaryKey) return false;
        return true;
      });
    },

    pkField() {
      return this.fields?.find(f => f.isPrimaryKey);
    }
  },

  watch: {
    record: {
      immediate: true,
      handler(value) {
        const next = {};
        this.editableFields.forEach(field => {
          next[field.columnName] = value ? value[field.columnName] ?? '' : '';
          if (field.dataType === 'bool') {
            next[field.columnName] = value ? !!value[field.columnName] : false;
          }
        });
        this.form = next;
      }
    },

    fields: {
      immediate: true,
      handler() {
        if (!this.record) {
          const next = {};
          this.editableFields.forEach(field => {
            next[field.columnName] = field.dataType === 'bool' ? false : '';
          });
          this.form = next;
        }
      }
    }
  },

  methods: {
    guardar() {
      const payload = { ...this.form };

      if (this.record && !this.pkField) {
        window.alert('Entidad sin PK, no se puede editar.');
        return;
      }

      const req = this.record
        ? datosService.editar(this.systemId, this.entityId, this.record[this.pkField.columnName], payload)
        : datosService.crear(this.systemId, this.entityId, payload);

      req.then(() => {
        this.$emit('guardado');
        this.cerrar();
      });
    },

    cerrar() {
      this.model = false;
    }
  }
};
</script>

<style scoped>
.dialog-card {
  border-radius: 14px;
}

.form :deep(.v-field) {
  margin-bottom: 4px;
}
</style>
