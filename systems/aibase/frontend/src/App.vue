<template>
  <div class="app-shell" :style="themeVars">
    <router-view />
  </div>
</template>

<script setup>
import { computed, ref, watchEffect, provide } from 'vue'
import frontendConfig from './config/frontend-config.json'
import { vuetify } from './plugins/vuetify'

const system = frontendConfig?.system || {}
const theme = frontendConfig?.theme || {}
const darkTheme = frontendConfig?.themeDark || {}

const storedMode = typeof localStorage !== 'undefined' ? localStorage.getItem('sb-theme') : null
const colorMode = ref(storedMode === 'dark' ? 'dark' : 'light')
const isDark = computed(() => colorMode.value === 'dark')

const lightDefaults = {
  primarySoft: 'rgba(29,78,216,0.12)',
  background: '#f8fafc',
  surface: '#ffffff',
  muted: '#64748b',
  border: 'rgba(15,23,42,0.12)',
  borderSoft: 'rgba(15,23,42,0.08)',
  radius: 16,
  shadow: '0 12px 30px rgba(15, 23, 42, 0.12)',
  fontBody: "Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
  fontDisplay: "'Space Grotesk', Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
  gradient: 'linear-gradient(135deg, rgba(29,78,216,0.16), rgba(14,165,233,0.08) 45%, rgba(248,250,252,0.95))',
  patternOpacity: 0.06,
  headerBg: 'rgba(255,255,255,0.9)',
  text: '#0f172a',
  textSoft: '#334155'
}

const darkDefaults = {
  primarySoft: 'rgba(59,130,246,0.22)',
  background: '#040b1b',
  surface: '#0f172a',
  muted: '#94a3b8',
  border: 'rgba(148,163,184,0.26)',
  borderSoft: 'rgba(148,163,184,0.18)',
  radius: 16,
  shadow: '0 16px 40px rgba(2, 6, 23, 0.45)',
  fontBody: "Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
  fontDisplay: "'Space Grotesk', Manrope, system-ui, -apple-system, 'Segoe UI', sans-serif",
  gradient: 'linear-gradient(135deg, rgba(15,23,42,0.96), rgba(2,6,23,0.96) 55%, rgba(8,47,73,0.72))',
  patternOpacity: 0.11,
  headerBg: 'rgba(2,6,23,0.86)',
  text: '#e2e8f0',
  textSoft: '#94a3b8'
}

const themeVars = computed(() => {
  const fallback = isDark.value ? darkDefaults : lightDefaults
  const activeTheme = isDark.value ? darkTheme : theme
  const brand = activeTheme?.brand || (isDark.value ? darkTheme?.brand : null) || theme?.brand || {}
  return {
    '--sb-primary': brand.primary || system.primaryColor || '#1d4ed8',
    '--sb-secondary': brand.secondary || system.secondaryColor || '#0ea5e9',
    '--sb-accent': brand.accent || '#f97316',
    '--sb-primary-soft': activeTheme.primarySoft || fallback.primarySoft,
    '--sb-bg': activeTheme.background || fallback.background,
    '--sb-surface': activeTheme.surface || fallback.surface,
    '--sb-muted': activeTheme.muted || fallback.muted,
    '--sb-border': activeTheme.border || fallback.border,
    '--sb-border-soft': activeTheme.borderSoft || fallback.borderSoft,
    '--sb-radius': `${activeTheme.radius ?? fallback.radius}px`,
    '--sb-shadow': activeTheme.shadow || fallback.shadow,
    '--sb-font': activeTheme.fontBody || theme.fontBody || system.fontFamily || fallback.fontBody,
    '--sb-font-display': activeTheme.fontDisplay || theme.fontDisplay || fallback.fontDisplay,
    '--sb-gradient': activeTheme.gradient || fallback.gradient,
    '--sb-pattern-opacity': activeTheme.patternOpacity ?? fallback.patternOpacity,
    '--sb-header-bg': activeTheme.headerBg || fallback.headerBg,
    '--sb-text': activeTheme.text || fallback.text,
    '--sb-text-soft': activeTheme.textSoft || fallback.textSoft
  }
})

watchEffect(() => {
  if (typeof document === 'undefined') return
  const root = document.documentElement
  const vars = themeVars.value || {}
  Object.entries(vars).forEach(([key, value]) => {
    root.style.setProperty(key, String(value))
  })
  root.classList.toggle('sb-dark', isDark.value)
  if (vuetify?.theme?.global?.name) {
    vuetify.theme.global.name.value = isDark.value ? 'systembaseDark' : 'systembase'
  }
})

function toggleTheme() {
  colorMode.value = isDark.value ? 'light' : 'dark'
  if (typeof localStorage !== 'undefined') {
    localStorage.setItem('sb-theme', colorMode.value)
  }
}

provide('colorMode', {
  isDark,
  toggleTheme
})
</script>

<style>
.app-shell {
  min-height: 100vh;
  font-family: var(--sb-font);
  background-color: var(--sb-bg);
  color: var(--sb-text, #0f172a);
  background-image:
    var(--sb-gradient),
    radial-gradient(circle at top right, rgba(15, 23, 42, calc(var(--sb-pattern-opacity) + 0.02)) 0, transparent 55%),
    radial-gradient(circle at 20% 20%, rgba(29, 78, 216, var(--sb-pattern-opacity)) 0, transparent 55%);
  background-attachment: fixed;
}
</style>
