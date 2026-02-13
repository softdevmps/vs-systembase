<template>
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-col cols="12" sm="8" md="4">
        <v-card elevation="10">
          <v-card-title class="text-center text-h6">
            <v-icon class="mr-2">mdi-lock</v-icon>
            Iniciar sesión
          </v-card-title>

          <v-card-text>
            <v-form @submit.prevent="login">
              <v-text-field
                v-model="usuario"
                label="Usuario o Email"
                prepend-inner-icon="mdi-account"
                :rules="[v => !!v || 'Campo requerido']"
                required
              />

              <v-text-field
                v-model="password"
                label="Contraseña"
                type="password"
                prepend-inner-icon="mdi-lock"
                :rules="[v => !!v || 'Campo requerido']"
                required
              />

              <v-alert v-if="error" type="error" density="compact" class="mb-3">
                {{ error }}
              </v-alert>

              <v-btn block color="primary" size="large" type="submit" :loading="loading">
                Ingresar
              </v-btn>
            </v-form>
          </v-card-text>

          <v-card-actions class="justify-center">
            <v-btn variant="text" @click="$router.push('/register')">
              Crear cuenta
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { authService } from '../api/auth.service'

const usuario = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)
const router = useRouter()

async function login() {
  error.value = ''
  loading.value = true

  try {
    const res = await authService.login({
      usuario: usuario.value,
      password: password.value
    })

    const token = res.data?.token || res.data?.Token
    if (token) {
      localStorage.setItem('token', token)
    } else {
      throw new Error('Token no recibido')
    }
    router.push('/home')
  } catch {
    error.value = 'Usuario o contraseña incorrectos'
  } finally {
    loading.value = false
  }
}
</script>
