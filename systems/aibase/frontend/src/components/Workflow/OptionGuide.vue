<template>
  <v-expansion-panels variant="accordion" class="option-guide" elevation="0">
    <v-expansion-panel>
      <v-expansion-panel-title>
        <div class="d-flex align-center ga-2">
          <v-icon color="primary" size="18">mdi-help-circle-outline</v-icon>
          <span>{{ title }}</span>
        </div>
      </v-expansion-panel-title>
      <v-expansion-panel-text>
        <div class="guide-grid">
          <div v-for="item in items" :key="item.label" class="guide-item">
            <div class="guide-label">{{ item.label }}</div>
            <div class="guide-text">{{ item.description }}</div>
          </div>
        </div>
      </v-expansion-panel-text>
    </v-expansion-panel>
  </v-expansion-panels>
</template>

<script setup>
defineProps({
  title: {
    type: String,
    default: 'Qué significa cada opción'
  },
  items: {
    type: Array,
    default: () => []
  }
})
</script>

<style scoped>
.option-guide :deep(.v-expansion-panel-title) {
  min-height: 48px;
}

.guide-grid {
  display: grid;
  gap: 10px;
}

.guide-item {
  border: 1px solid rgba(120, 140, 170, 0.2);
  border-radius: 10px;
  padding: 10px;
  background: rgba(37, 99, 235, 0.03);
}

.guide-label {
  font-weight: 700;
  font-size: 0.9rem;
  color: var(--sb-primary, #2563eb);
}

.guide-text {
  margin-top: 4px;
  font-size: 0.84rem;
  color: var(--sb-text-soft, var(--sb-muted));
}
</style>
