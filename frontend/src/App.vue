<script setup lang="ts">
// 4-item nav (spec §9): mobile = bottom tab bar, desktop ≥900px = left sidebar.
// Full-screen check-in views hide the nav (meta.fullscreen).
import { useRoute } from 'vue-router'
import { useSessionStore } from '@/stores/session'
import KeyGate from '@/components/KeyGate.vue'

const route = useRoute()
const session = useSessionStore()

// icons: 1.5px stroke, minimal geometry — no emoji
const tabs = [
  {
    to: '/',
    label: 'Today',
    d: 'M4 5h16v15H4z M4 9h16 M8 5V3 M16 5V3',
  },
  {
    to: '/plan',
    label: 'Plan',
    d: 'M4 6h10 M4 12h16 M4 18h13 M17 4l3 2-3 2',
  },
  {
    to: '/analysis',
    label: 'Analysis',
    d: 'M5 20V10 M12 20V4 M19 20v-7',
  },
  {
    to: '/settings',
    label: 'Settings',
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
/* content lives on one big inventory panel — like an opened chest over the world */
.content-inner {
  margin: 0.9rem auto calc(92px + env(safe-area-inset-bottom));
  padding: 1.25rem 1rem;
  max-width: 640px;
  background: var(--surface);
  border: 2px solid #1a1a1a;
  box-shadow: var(--bevel-out);
}
.fullscreen .content-inner {
  margin: 0;
  padding: 0;
  max-width: none;
  background: transparent;
  border: none;
  box-shadow: none;
}

/* ---- Mobile: the hotbar ---- */
.nav {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 50;
  display: flex;
  justify-content: center;
  background: rgba(12, 10, 8, 0.82);
  border-top: 2px solid #1a1a1a;
  padding: 6px 6px calc(6px + env(safe-area-inset-bottom));
}
.brand {
  display: none;
}
.nav-items {
  display: flex;
  gap: 6px;
  flex: 1;
  max-width: 420px;
}
/* each tab is a hotbar slot; the active one gets the thick white selection frame */
.nav-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  padding: 0.45rem 0 0.35rem;
  text-decoration: none;
  color: #cfcfcf;
  background: rgba(139, 139, 139, 0.25);
  border: 2px solid #4f4f4f;
  box-shadow: var(--bevel-in);
}
.nav-item.active {
  color: #fff;
  border-color: #ffffff;
  background: rgba(139, 139, 139, 0.4);
}
.nav-icon {
  width: 20px;
  height: 20px;
}
.nav-label {
  font-size: 0.6rem;
  font-family: var(--font-data);
  letter-spacing: 0.02em;
  text-shadow: var(--px-text-shadow);
}

/* ---- Desktop ≥900px: vertical hotbar on the left ---- */
@media (min-width: 900px) {
  .shell {
    display: grid;
    grid-template-columns: 210px 1fr;
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
    justify-content: flex-start;
    border-top: none;
    border-right: 2px solid #1a1a1a;
    background: rgba(12, 10, 8, 0.82);
    padding: 1.25rem 0.75rem;
    gap: 1.25rem;
  }
  .brand {
    display: block;
    margin: 0 0 0 0.4rem;
    color: var(--accent-bright);
    text-shadow: var(--px-text-shadow);
  }
  .nav-items {
    flex-direction: column;
    gap: 6px;
    flex: initial;
    max-width: none;
  }
  .nav-item {
    flex-direction: row;
    gap: 0.65rem;
    padding: 0.55rem 0.65rem;
    align-items: center;
  }
  .nav-item:hover {
    color: #ffffa0;
  }
  .nav-icon {
    width: 18px;
    height: 18px;
  }
  .nav-label {
    font-size: 0.78rem;
    font-family: var(--font-ui);
  }
  .content-inner {
    margin: 2rem;
    padding: 1.75rem 2rem;
    max-width: 720px;
  }
}
</style>
