<script setup lang="ts">
// Nav 4 mục: Hôm nay · Kế hoạch · Phân tích · Cài đặt (spec §9).
// Màn check-in full-screen tự che tab bar (meta.fullscreen).
import { useRoute } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import KeyGate from '@/components/KeyGate.vue'

const route = useRoute()
const session = useSessionStore()

const tabs = [
  { to: '/', label: 'Hôm nay', icon: '📅' },
  { to: '/plan', label: 'Kế hoạch', icon: '🗂️' },
  { to: '/analysis', label: 'Phân tích', icon: '📊' },
  { to: '/settings', label: 'Cài đặt', icon: '⚙️' },
]
</script>

<template>
  <KeyGate v-if="!session.hasKey" />
  <template v-else>
    <main class="app-main" :class="{ fullscreen: route.meta.fullscreen }">
      <RouterView />
    </main>
    <nav v-if="!route.meta.fullscreen" class="tab-bar">
      <RouterLink
        v-for="tab in tabs"
        :key="tab.to"
        :to="tab.to"
        class="tab"
        active-class="active"
      >
        <span class="tab-icon">{{ tab.icon }}</span>
        <span class="tab-label">{{ tab.label }}</span>
      </RouterLink>
    </nav>
  </template>
</template>

<style scoped>
.app-main {
  min-height: 100dvh;
  padding-bottom: 72px;
}
.app-main.fullscreen {
  padding-bottom: 0;
}
.tab-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  display: flex;
  border-top: 1px solid var(--p-surface-200);
  background: var(--p-surface-0);
  padding-bottom: env(safe-area-inset-bottom);
}
.tab {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  padding: 0.5rem 0;
  text-decoration: none;
  color: var(--p-text-muted-color);
  font-size: 0.7rem;
}
.tab.active {
  color: var(--p-primary-color);
}
.tab-icon {
  font-size: 1.25rem;
}
@media (prefers-color-scheme: dark) {
  .tab-bar {
    background: var(--p-surface-950);
    border-color: var(--p-surface-800);
  }
}
</style>
