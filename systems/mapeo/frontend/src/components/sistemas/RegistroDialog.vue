<template>
  <v-dialog v-model="model" max-width="600" scrollable>
    <v-card class="dialog-card sb-dialog">
      <v-card-title class="sb-dialog-title">
        <div class="sb-dialog-icon">
          <v-icon color="primary">
            {{ isEdit ? 'mdi-pencil' : isDuplicate ? 'mdi-content-copy' : 'mdi-plus-box' }}
          </v-icon>
        </div>
        <div>
          <div class="sb-dialog-title-text">
            {{ dialogTitle }}
          </div>
          <div class="sb-dialog-subtitle">
            Completa los campos requeridos para continuar.
          </div>
        </div>
      </v-card-title>

      <v-divider />

      <v-card-text class="sb-dialog-body sb-dialog-scroll">
        <v-form class="form sb-form">
          <template v-if="layout === 'tabs'">
            <v-tabs v-model="activeTab" density="compact">
              <v-tab v-for="(group, index) in fieldGroups" :key="group.name" :value="index">
                {{ group.name }}
              </v-tab>
            </v-tabs>
            <v-window v-model="activeTab">
              <v-window-item v-for="(group, index) in fieldGroups" :key="group.name" :value="index">
                <v-row class="sb-form-row sb-form-grid" dense>
                  <v-col
                    v-for="field in group.fields"
                    :key="field.columnName"
                    :cols="fieldCols(field).cols"
                    :sm="fieldCols(field).sm"
                    :md="fieldCols(field).md"
                  >
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
                      :density="modalDensity"
                      :variant="inputVariant"
                      :rules="rulesFor(field)"
                    />
                  </v-col>
                </v-row>
              </v-window-item>
            </v-window>
          </template>

          <template v-else-if="layout === 'sections'">
            <v-card v-for="group in fieldGroups" :key="group.name" class="mb-3 sb-form-section" elevation="0">
              <v-card-title class="text-subtitle-2">{{ group.name }}</v-card-title>
              <v-card-text>
                <v-row class="sb-form-row sb-form-grid" dense>
                  <v-col
                    v-for="field in group.fields"
                    :key="field.columnName"
                    :cols="fieldCols(field).cols"
                    :sm="fieldCols(field).sm"
                    :md="fieldCols(field).md"
                  >
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
                      :density="modalDensity"
                      :variant="inputVariant"
                      :rules="rulesFor(field)"
                    />
                  </v-col>
                </v-row>
              </v-card-text>
            </v-card>
          </template>

          <template v-else>
            <v-row class="sb-form-row sb-form-grid" dense>
              <v-col
                v-for="field in editableFields"
                :key="field.columnName"
                :cols="fieldCols(field).cols"
                :sm="fieldCols(field).sm"
                :md="fieldCols(field).md"
              >
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
                  :density="modalDensity"
                  :variant="inputVariant"
                  :rules="rulesFor(field)"
                />
              </v-col>
            </v-row>
          </template>
        </v-form>
      </v-card-text>

      <v-divider />

      <v-card-actions class="pa-4 sb-dialog-actions">
        <v-spacer />
        <v-btn class="sb-btn ghost" variant="text" :density="modalDensity" @click="cerrar">Cancelar</v-btn>
        <v-btn class="sb-btn primary" color="primary" :density="modalDensity" @click="guardar">Guardar</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <v-dialog v-model="confirmDialog" max-width="420">
    <v-card class="sb-dialog">
      <v-card-title class="sb-dialog-title">
        <div class="sb-dialog-icon">
          <v-icon color="primary">mdi-content-save</v-icon>
        </div>
        <div>
          <div class="sb-dialog-title-text">Confirmar guardado</div>
          <div class="sb-dialog-subtitle">Revisa que los datos sean correctos.</div>
        </div>
      </v-card-title>
      <v-divider />
      <v-card-text class="sb-dialog-body">
        {{ messages?.confirmSave || 'Guardar cambios?' }}
      </v-card-text>
      <v-card-actions class="sb-dialog-actions d-flex justify-end ga-2">
        <v-btn class="sb-btn ghost" variant="text" @click="confirmDialog = false">Cancelar</v-btn>
        <v-btn class="sb-btn primary" @click="confirmSaveAction">Aceptar</v-btn>
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
      activeTab: 0,
      confirmDialog: false,
      pendingSave: false
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
    },

    modalDensity() {
      if (this.density === 'comfortable') return 'compact'
      return this.density
    },

    inputVariant() {
      return 'outlined'
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

    fieldCols(field) {
      const type = this.resolveInputType(field)
      const name = String(field?.columnName || field?.name || '').toLowerCase()
      const smallField = [
        'id', 'incidenteid', 'tipohechoid', 'lat', 'lng', 'confidence',
        'format', 'durationsec', 'hash', 'estado', 'createdat', 'fecha', 'hora'
      ].some(token => name.includes(token))
      const longText = type === 'textarea'
        || (field?.maxLength && Number(field.maxLength) > 200)
        || name.includes('descripcion')
        || name.includes('observ')
        || name.includes('detalle')
        || name.includes('coment')
        || name.includes('nota')
        || name.includes('json')
        || name.includes('raw')
        || name.includes('text')
      if (longText) return { cols: 12, sm: 12, md: 12 }
      if (smallField || type === 'checkbox' || type === 'switch') return { cols: 12, sm: 6, md: 6 }
      return { cols: 12, sm: 6, md: 6 }
    },

    cerrar() {
      this.model = false
    },

    async guardar() {
      if (this.confirmSave && !this.pendingSave) {
        this.confirmDialog = true
        this.pendingSave = true
        return
      }
      this.pendingSave = false

      await this.persistSave()
    },

    async confirmSaveAction() {
      this.confirmDialog = false
      await this.persistSave()
    },

    async persistSave() {
      const payload = this.normalizePayload({ ...this.form })
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
        const message = this.extractErrorMessage(error)
        window.alert(message)
      }
    },

    normalizePayload(raw) {
      const payload = { ...raw }
      const fields = Array.isArray(this.fields) ? this.fields : []
      const byName = new Map(
        fields.map(f => [String(f.columnName || f.name || '').toLowerCase(), f])
      )

      Object.keys(payload).forEach((key) => {
        const field = byName.get(String(key).toLowerCase())
        const value = payload[key]
        payload[key] = this.normalizeFieldValue(field, value)
      })

      return payload
    },

    normalizeFieldValue(field, value) {
      if (value === '' || value === undefined) return null
      if (value === null) return null

      const type = field ? this.resolveInputType(field) : 'text'

      if (type === 'number') {
        if (typeof value === 'number') return Number.isFinite(value) ? value : null
        const normalized = String(value).trim().replace(',', '.')
        if (!normalized) return null
        const num = Number(normalized)
        if (Number.isFinite(num)) return num
        throw new Error(`Valor numerico invalido en "${field?.label || field?.name || field?.columnName || 'campo'}"`)
      }

      if (type === 'date' || type === 'datetime') {
        return this.normalizeDateValue(String(value))
      }

      return value
    },

    normalizeDateValue(input) {
      const value = String(input || '').trim()
      if (!value) return null

      // yyyy-MM-dd or yyyy-MM-ddTHH:mm[:ss] (already compatible)
      if (/^\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2})?)?$/.test(value)) {
        return value.replace(' ', 'T')
      }

      // dd/MM/yyyy[, ]HH:mm[:ss]
      const m = value.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})(?:[,\s]+(\d{1,2})(?::(\d{1,2}))?(?::(\d{1,2}))?)?$/)
      if (m) {
        const dd = m[1].padStart(2, '0')
        const mm = m[2].padStart(2, '0')
        const yyyy = m[3]
        const hh = (m[4] || '00').padStart(2, '0')
        const min = (m[5] || '00').padStart(2, '0')
        const ss = (m[6] || '00').padStart(2, '0')
        return `${yyyy}-${mm}-${dd}T${hh}:${min}:${ss}`
      }

      const parsed = new Date(value)
      if (!Number.isNaN(parsed.getTime())) {
        const yyyy = parsed.getFullYear()
        const mm = String(parsed.getMonth() + 1).padStart(2, '0')
        const dd = String(parsed.getDate()).padStart(2, '0')
        const hh = String(parsed.getHours()).padStart(2, '0')
        const min = String(parsed.getMinutes()).padStart(2, '0')
        const ss = String(parsed.getSeconds()).padStart(2, '0')
        return `${yyyy}-${mm}-${dd}T${hh}:${min}:${ss}`
      }

      return value
    },

    extractErrorMessage(error) {
      const responseData = error?.response?.data
      if (typeof responseData === 'string' && responseData.trim()) return responseData
      if (responseData?.message) return responseData.message
      if (responseData?.title && responseData?.errors) {
        const details = Object.values(responseData.errors)
          .flat()
          .filter(Boolean)
          .join(' | ')
        return details ? `${responseData.title}: ${details}` : responseData.title
      }
      if (responseData?.title) return responseData.title
      return error?.message || 'Error al guardar.'
    }
  }
}
</script>

<style scoped>
.dialog-card {
  border-radius: var(--sb-radius);
}
</style>
