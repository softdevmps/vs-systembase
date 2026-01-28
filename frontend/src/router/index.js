import { createRouter, createWebHistory } from 'vue-router'

import Login from '../views/Login.vue'
import Register from '../views/Register.vue'
import Rol from '../views/Sistema/Roles.vue'
import Home from '../views/Home.vue'
import Usuarios from '../views/Sistema/Usuarios.vue'
import Menu from '../views/Sistema/Menu.vue'
import MainLayout from '../components/Layouts/MainLayout.vue'

const routes = [
  { path: '/', redirect: '/login' },

  { path: '/login', component: Login },
  { path: '/register', component: Register },

  {
    path: '/',
    component: MainLayout,
    children: [
      {
        path: 'home',
        component: Home
      },
      {
        path: 'menu',
        component: Menu
      },
      {
        path: 'roles',
        component: Rol
      },
      {
        path: 'usuarios',
        component: Usuarios
      }
            // acá después van usuarios, roles, etc
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
