import { createRouter, createWebHistory } from 'vue-router'

import Login from '../views/Login.vue'
import Register from '../views/Register.vue'
import Home from '../views/Home.vue'
import SistemaRuntime from '../views/Sistema/SistemaRuntime.vue'
import StageTemplate from '../views/stages/StageTemplate.vue'
import StageProject from '../views/stages/StageProject.vue'
import StageDataset from '../views/stages/StageDataset.vue'
import StageRag from '../views/stages/StageRag.vue'
import StageTrain from '../views/stages/StageTrain.vue'
import StageEval from '../views/stages/StageEval.vue'
import StageDeploy from '../views/stages/StageDeploy.vue'
import StagePlayground from '../views/stages/StagePlayground.vue'
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
      { path: 'workflow', redirect: '/home' },
      { path: 'stage/template', component: StageTemplate },
      { path: 'stage/project', component: StageProject },
      { path: 'stage/dataset', component: StageDataset },
      { path: 'stage/rag', component: StageRag },
      { path: 'stage/train', component: StageTrain },
      { path: 'stage/eval', component: StageEval },
      { path: 'stage/deploy', component: StageDeploy },
      { path: 'stage/playground', component: StagePlayground },
      {
        path: 'pipeline',
        redirect: to => ({ path: '/stage/dataset', query: to.query })
      },
      {
        path: 'playground',
        redirect: to => ({ path: '/stage/playground', query: to.query })
      },
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
