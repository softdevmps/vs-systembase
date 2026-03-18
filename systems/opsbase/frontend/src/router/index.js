import { createRouter, createWebHistory } from 'vue-router'

import Login from '../views/Login.vue'
import Register from '../views/Register.vue'
import Home from '../views/Home.vue'
import KardexView from '../views/Ops/KardexView.vue'
import RecepcionView from '../views/Ops/RecepcionView.vue'
import DespachoView from '../views/Ops/DespachoView.vue'
import DepositosMapaView from '../views/Ops/DepositosMapaView.vue'
import DepositoContextView from '../views/Ops/DepositoContextView.vue'
import DepositoCreateView from '../views/Ops/DepositoCreateView.vue'
import DepositoEditView from '../views/Ops/DepositoEditView.vue'
import TrazabilidadView from '../views/Ops/TrazabilidadView.vue'
import SistemaRuntime from '../views/Sistema/SistemaRuntime.vue'
import MainLayout from '../components/Layouts/MainLayout.vue'

const routes = [
  { path: '/', redirect: '/login' },
  { path: '/login', component: Login },
  { path: '/register', component: Register },
  {
    path: '/',
    component: MainLayout,
    children: [
      { path: 'home', component: Home },
      { path: 'kardex', component: KardexView },
      { path: 'depositos', component: DepositosMapaView },
      { path: 'depositos/nuevo', component: DepositoCreateView },
      { path: 'depositos/:locationId/editar', component: DepositoEditView },
      { path: 'depositos/:locationId', component: DepositoContextView },
      { path: 'trazabilidad', component: TrazabilidadView },
      { path: 'recepcion', component: RecepcionView },
      { path: 'despacho', component: DespachoView },
      { path: ':entity', component: SistemaRuntime },
      { path: ':autoPath(.*)*', redirect: '/home' }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token')
  const publicRoutes = ['/login', '/register']

  if (!token && !publicRoutes.includes(to.path)) {
    next('/login')
  } else {
    next()
  }
})

export default router
