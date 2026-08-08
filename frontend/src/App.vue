<script setup lang="ts">
// Nav 4 mục (spec §9): mobile = bottom tab bar, desktop ≥900px = sidebar trái.
// Màn check-in full-screen tự che nav (meta.fullscreen).
import { useRoute } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import KeyGate from '@/components/KeyGate.vue'

const route = useRoute()
const session = useSessionStore()

// icon: stroke 1.5px, hình học tối giản — không emoji
const tabs = [
  {
    to: '/',
    label: 'Hôm nay',
    d: 'M4 5h16v15H4z M4 9h16 M8 5V3 M16 5V3',
  },
  {
    to: '/plan',
    label: 'Kế hoạch',
    d: 'M4 6h10 M4 12h16 M4 18h13 M17 4l3 2-3 2',
  },
  {
    to: '/analysis',
    label: 'Phân tích',
    d: 'M5 20V10 M12 20V4 M19 20v-7',
  },
  {
    to: '/settings',
    label: 'Cài đặt',
    d: 'M4 8h10 M17 8h3 M14 5v6 M4 16h3 M10 16h10 M7 13v6',
  },
]
</script>

<template>
  <KeyGate v-if="!session.hasKey" />
  <template v-else>
    <div class="shell" :class="{ fullscreen: route.meta.fullscreen }">
      <nav v-if="!route.meta.fullscreen" class="nav">
        <p class="brand overline">tracker</p>
        <div class="nav-items">
          <RouterLink
            v-for="tab in tabs"
            :key="tab.to"
            :to="tab.to"
            class="nav-item"
            active-class="active"
          >
            <svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor"
              stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <path :d="tab.d" />
            </svg>
            <span class="nav-label">{{ tab.label }}</span>
          </RouterLink>
        </div>
      </nav>

      <main class="content">
        <div class="content-inner">
          <RouterView />
        </div>
      </main>
    </div>
  </template>
</template>

<style scoped>
.shell {
  min-height: 100dvh;
}
.content {
  min-height: 100dvh;
}
.content-inner {
  padding: 1.25rem 1rem calc(84px + env(safe-area-inset-bottom));
  max-width: 640px;
  margin: 0 auto;
}
.fullscreen .content-inner {
  padding: 0;
  max-width: none;
}

/* ---- Mobile: bottom tab bar ---- */
.nav {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 50;
  display: flex;
  border-top: 1px solid var(--border);
  background: var(--surface);
  padding-bottom: env(safe-area-inset-bottom);
}
.brand {
  display: none;
}
.nav-items {
  display: flex;
  flex: 1;
}
.nav-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 3px;
  padding: 0.55rem 0 0.5rem;
  text-decoration: none;
  color: var(--text-muted);
  position: relative;
}
.nav-item.active {
  color: var(--accent);
}
/* kim chỉ: vạch amber phía trên tab active (mobile) */
.nav-item::before {
  content: '';
  position: absolute;
  top: -1px;
  left: 25%;
  right: 25%;
  height: 2px;
  background: transparent;
  transition: background 120ms;
}
.nav-item.active::before {
  background: var(--accent);
}
.nav-icon {
  width: 21px;
  height: 21px;
}
.nav-label {
  font-size: 0.66rem;
  font-family: var(--font-data);
  letter-spacing: 0.02em;
}

/* ---- Desktop ≥900px: sidebar trái ---- */
@media (min-width: 900px) {
  .shell {
    display: grid;
    grid-template-columns: 200px 1fr;
  }
  .shell.fullscreen {
    display: block;
  }
  .nav {
    position: sticky;
    top: 0;
    bottom: auto;
    height: 100dvh;
    flex-direction: column;
    border-top: none;
    border-right: 1px solid var(--border);
    background: var(--surface);
    padding: 1.25rem 0.75rem;
    gap: 1.5rem;
  }
  .brand {
    display: block;
    margin: 0 0 0 0.65rem;
    color: var(--accent);
  }
  .nav-items {
    flex-direction: column;
    gap: 0.25rem;
    flex: initial;
  }
  .nav-item {
    flex-direction: row;
    gap: 0.65rem;
    padding: 0.55rem 0.65rem;
    border-radius: var(--radius);
    align-items: center;
  }
  .nav-item:hover {
    color: var(--text);
    background: var(--surface-raised);
  }
  .nav-item.active {
    color: var(--accent);
    background: var(--accent-dim);
  }
  /* kim chỉ desktop: vạch dọc bên trái item active */
  .nav-item::before {
    top: 20%;
    bottom: 20%;
    left: -0.75rem;
    right: auto;
    width: 2px;
    height: auto;
  }
  .nav-icon {
    width: 19px;
    height: 19px;
  }
  .nav-label {
    font-size: 0.85rem;
    font-family: var(--font-ui);
    font-weight: 500;
  }
  .content-inner {
    padding: 2.5rem 2rem 3rem;
    max-width: 720px;
    margin: 0;
  }
}
</style>
