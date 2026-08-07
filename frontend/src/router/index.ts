import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'today', component: () => import('@/views/TodayView.vue') },
    {
      path: '/checkin/morning',
      name: 'morning-checkin',
      component: () => import('@/views/MorningCheckinView.vue'),
      meta: { fullscreen: true },
    },
    {
      path: '/checkin/evening',
      name: 'evening-checkin',
      component: () => import('@/views/EveningCheckinView.vue'),
      meta: { fullscreen: true },
    },
    { path: '/plan', name: 'plan', component: () => import('@/views/PlanView.vue') },
    { path: '/analysis', name: 'analysis', component: () => import('@/views/AnalysisView.vue') },
    { path: '/settings', name: 'settings', component: () => import('@/views/SettingsView.vue') },
  ],
})

export default router
