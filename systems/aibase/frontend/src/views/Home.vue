<template>
  <v-container fluid class="home-dashboard">
    <v-row>
      <v-col cols="12">
        <v-card class="card hero-card">
          <div class="hero-content">
            <div class="hero-text">
              <div class="hero-icon">
                <v-icon color="primary" size="26">mdi-brain</v-icon>
              </div>
              <div>
                <h1>AIBase</h1>
                <p>Proyecto independiente dentro de SystemBase para pipelines de IA.</p>
              </div>
            </div>
            <div class="hero-actions">
              <v-btn class="sb-btn primary" @click="goToAibase">Abrir AIBase</v-btn>
            </div>
          </div>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { useRouter } from 'vue-router'

const router = useRouter()

function goToAibase() {
  router.push('/aibase')
}
</script>

<style scoped>
.hero-card {
  padding: 20px 24px;
}

.hero-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.hero-text {
  display: flex;
  gap: 14px;
  align-items: center;
}

.hero-text h1 {
  margin: 0;
  font-size: 1.4rem;
}

.hero-text p {
  margin: 4px 0 0;
  color: var(--sb-text-soft, var(--sb-muted));
}

.hero-icon {
  width: 44px;
  height: 44px;
  border-radius: 14px;
  background: var(--sb-primary-soft);
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>
