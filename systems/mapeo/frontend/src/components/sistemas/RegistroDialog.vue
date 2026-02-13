<template>
  <v-dialog v-model="model" max-width="720px">
    <v-card class="dialog-card">
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2" color="primary">
          {{ isEdit ? 'mdi-pencil' : isDuplicate ? 'mdi-content-copy' : 'mdi-plus-box' }}
        </v-icon>
        <span class="text-h6 font-weight-medium">
          {{ dialogTitle }}
        </span>
      </v-card-title>

      <v-divider />

      <v-card-text>
        <v-form class="form">
          <template v-if="layout === 'tabs'">
            <v-tabs v-model="activeTab" density="compact">
              <v-tab v-for="(group, index) in fieldGroups" :key="group.name" :value="index">
                {{ group.name }}
              </v-tab>
            </v-tabs>
            <v-window v-model="activeTab">
              <v-window-item v-for="(group, index) in fieldGroups" :key="group.name" :value="index">
                <v-row v-for="field in group.fields" :key="field.columnName">
                  <v-col cols="12">
                    <component
                      :is="resolveComponent(field)"
                      v-model="form[field.columnName]"
                      :label="field.label || field.name || field.columnName"
                      :placeholder="field.placeholder || undefined"
                      :hint="field.helpText || undefined"
                      persistent-hint
                      :maxlength="field.maxLength || undefined"
                      :type="resolveInputType(field) === 'number' ? 'number' : resolveInputType(field) === 'date' ? 'date' : resolveInputType(field) === 'datetime' ? 'datetime-local' : undefined"
                      clearable
                      :density="density"
                      :rules="rulesFor(field)"
                    />
                  </v-col>
                </v-row>
              </v-window-item>
            </v-window>
          </template>

          <template v-else-if="layout === 'sections'">
            <v-card v-for="group in fieldGroups" :key="group.name" class="mb-3" elevation="0">
              <v-card-title class="text-subtitle-2">{{ group.name }}</v-card-title>
              <v-card-text>
                <v-row v-for="field in group.fields" :key="field.columnName">
                  <v-col cols="12">
                    <component
                      :is="resolveComponent(field)"
                      v-model="form[field.columnName]"
                      :label="field.label || field.name || field.columnName"
                      :placeholder="field.placeholder || undefined"
                      :hint="field.helpText || undefined"
                      persistent-hint
                      :maxlength="field.maxLength || undefined"
                      :type="resolveInputType(field) === 'number' ? 'number' : resolveInputType(field) === 'date' ? 'date' : resolveInputType(field) === 'datetime' ? 'datetime-local' : undefined"
                      clearable
                      :density="density"
                      :rules="rulesFor(field)"
                    />
                  </v-col>
                </v-row>
              </v-card-text>
            </v-card>
          </template>

          <template v-else>
            <v-row v-for="field in editableFields" :key="field.columnName">
              <v-col cols="12">
                <component
                  :is="resolveComponent(field)"
                  v-model="form[field.columnName]"
                  :label="field.label || field.name || field.columnName"
                  :placeholder="field.placeholder || undefined"
                  :hint="field.helpText || undefined"
                  persistent-hint
                  :maxlength="field.maxLength || undefined"
                  :type="resolveInputType(field) === 'number' ? 'number' : resolveInputType(field) === 'date' ? 'date' : resolveInputType(field) === 'datetime' ? 'datetime-local' : undefined"
                  clearable
                  :density="density"
                  :rules="rulesFor(field)"
                />
              </v-col>
            </v-row>
          </template>
        </v-form>
      </v-card-text>

      <v-divider />

      <v-card-actions class="pa-4">
        <v-spacer />
        <v-btn variant="text" :density="density" @click="cerrar">Cancelar</v-btn>
        <v-btn color="primary" :density="density" @click="guardar">Guardar</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script>
import runtimeApi from '../../api/runtime.service.js'

export default {
  props: {
    modelValue: Boolean,
    record: Object,
    fields: Array,
    layout: {
      type: String,
      default: 'single'
    },
    density: {
      type: String,
      default: 'comfortable'
    },
    messages: {
      type: Object,
      default: () => ({})
    },
    confirmSave: {
      type: Boolean,
      default: true
    },
    mode: {
      type: String,
      default: 'create'
    },
    apiRoute: {
      type: String,
      default: ''
    }
  },

  emits: ['update:modelValue', 'guardado'],

  data() {
    return {
      form: {},
      activeTab: 0
    }
  },

  computed: {
    model: {
      get() {
        return this.modelValue
      },
      set(v) {
        this.$emit('update:modelValue', v)
      }
    },

    isEdit() {
      return this.mode === 'edit'
    },

    isDuplicate() {
      return this.mode === 'duplicate'
    },

    dialogTitle() {
      if (this.isEdit) return 'Editar registro'
      if (this.isDuplicate) return 'Duplicar registro'
      return 'Nuevo registro'
    },

    pkField() {
      return this.fields?.find(f => f.isPrimaryKey) || this.fields?.find(f => String(f.columnName || f.name).toLowerCase() === 'id')
    },

    editableFields() {
      if (!this.fields) return []
      return this.fields.filter(f => {
        if (f.showInForm === false) return false
        if (f.isIdentity) return false
        if (this.isEdit && f.isPrimaryKey) return false
        return true
      })
    },

    fieldGroups() {
      const groups = {}
      for (const field of this.editableFields) {
        const section = field.section || 'General'
        if (!groups[section]) groups[section] = []
        groups[section].push(field)
      }
      return Object.entries(groups).map(([name, fields]) => ({ name, fields }))
    }
  },

  watch: {
    record: {
      immediate: true,
      handler(value) {
        if (value) {
          this.form = { ...value }
        } else {
          this.form = {}
        }
      }
    }
  },

  methods: {
    resolveInputType(field) {
      if (field.inputType) return field.inputType
      const data = String(field.dataType || '').toLowerCase()
      if (data.includes('date') && data.includes('time')) return 'datetime'
      if (data.includes('date')) return 'date'
      if (data.includes('int') || data.includes('decimal') || data.includes('numeric') || data.includes('float')) return 'number'
      if (data.includes('bit') || data.includes('bool')) return 'checkbox'
      return 'text'
    },

    resolveComponent(field) {
      const type = this.resolveInputType(field)
      if (type === 'textarea') return 'v-textarea'
      if (type === 'checkbox') return 'v-checkbox'
      if (type === 'switch') return 'v-switch'
      return 'v-text-field'
    },

    rulesFor(field) {
      const rules = []
      if (field.required) {
        rules.push(v => !!v || 'Campo requerido')
      }
      if (field.pattern) {
        rules.push(v => {
          if (!v) return true
          try {
            const regex = new RegExp(field.pattern)
            return regex.test(v) || 'Formato invalido'
          } catch {
            return true
          }
        })
      }
      if (field.min != null) {
        rules.push(v => (v == null || v === '' || Number(v) >= Number(field.min)) || `Minimo ${field.min}`)
      }
      if (field.max != null) {
        rules.push(v => (v == null || v === '' || Number(v) <= Number(field.max)) || `Maximo ${field.max}`)
      }
      return rules
    },

    cerrar() {
      this.model = false
    },

    async guardar() {
      if (this.confirmSave) {
        const ok = window.confirm(this.messages?.confirmSave || 'Guardar cambios?')
        if (!ok) return
      }

      const payload = { ...this.form }
      const pkName = this.pkField?.columnName || this.pkField?.name
      if (this.isDuplicate && pkName) {
        delete payload[pkName]
      }

      try {
        if (!this.apiRoute) throw new Error('Ruta de API no definida')

        if (this.isEdit && pkName) {
          await runtimeApi.update(this.apiRoute, this.record[pkName], payload)
        } else {
          await runtimeApi.create(this.apiRoute, payload)
        }

        this.$emit('guardado')
        this.cerrar()
      } catch (error) {
        const message = error?.response?.data?.message || error?.message || 'Error al guardar.'
        window.alert(message)
      }
    }
  }
}
</script>

<style scoped>
.dialog-card {
  border-radius: 16px;
}
</style>
