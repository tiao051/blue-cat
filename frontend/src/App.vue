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
  <!-- square pixel particles drifting over the world, MC-style fireflies at dusk -->
  <div class="fireflies" aria-hidden="true">
    <span v-for="i in 9" :key="i" class="fly" />
  </div>

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
  position: relative;
  z-index: 1; /* content rides above the atmosphere layers */
}
.content {
  min-height: 100dvh;
}

/* fireflies: blocky particles with a bloom halo, drifting slowly */
.fireflies {
  position: fixed;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
}
.fly {
  position: absolute;
  width: 4px;
  height: 4px;
  background: #ffd97a;
  box-shadow: 0 0 10px 3px rgba(255, 200, 90, 0.55); /* the bloom */
  animation: drift 14s ease-in-out infinite alternate, flicker 3.4s steps(2) infinite;
  opacity: 0.8;
}
.fly:nth-child(1) { left: 8%;  top: 72%; animation-duration: 16s, 3.1s; }
.fly:nth-child(2) { left: 21%; top: 34%; animation-duration: 13s, 2.6s; animation-delay: -4s, 0.4s; }
.fly:nth-child(3) { left: 36%; top: 84%; animation-duration: 18s, 3.8s; animation-delay: -9s, 1.1s; }
.fly:nth-child(4) { left: 52%; top: 18%; animation-duration: 15s, 2.9s; animation-delay: -2s, 0.7s; background: #d9ff8a; box-shadow: 0 0 10px 3px rgba(180, 240, 100, 0.5); }
.fly:nth-child(5) { left: 64%; top: 62%; animation-duration: 12s, 3.3s; animation-delay: -7s, 1.6s; }
.fly:nth-child(6) { left: 78%; top: 40%; animation-duration: 17s, 2.4s; animation-delay: -11s, 0.2s; }
.fly:nth-child(7) { left: 88%; top: 78%; animation-duration: 14s, 3.6s; animation-delay: -5s, 1.9s; background: #d9ff8a; box-shadow: 0 0 10px 3px rgba(180, 240, 100, 0.5); }
.fly:nth-child(8) { left: 44%; top: 52%; animation-duration: 19s, 2.8s; animation-delay: -13s, 0.9s; }
.fly:nth-child(9) { left: 12%; top: 12%; animation-duration: 15s, 3.2s; animation-delay: -8s, 1.4s; }

@keyframes drift {
  0%   { transform: translate(0, 0); }
  50%  { transform: translate(22px, -30px); }
  100% { transform: translate(-14px, -56px); }
}
@keyframes flicker {
  0%, 100% { opacity: 0.85; }
  50%      { opacity: 0.25; }
}
/* content lives on one big inventory panel — like an opened chest over the world */
.content-inner {
  margin: 0.9rem auto calc(92px + env(safe-area-inset-bottom));
  padding: 1.25rem 1rem;
  max-width: 640px;
  /* subtle top-light on the panel + soft ambient shadow beneath it (shader AO) */
  background: linear-gradient(180deg, #cdcdcd 0%, var(--surface) 18%, #bfbfbf 100%);
  border: 2px solid #1a1a1a;
  box-shadow: var(--bevel-out), 0 18px 48px rgba(0, 0, 0, 0.55);
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
  box-shadow: var(--bevel-in), 0 0 12px rgba(255, 255, 180, 0.3); /* selected-slot glow */
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
    margin: 2rem auto; /* centered in the content column */
    padding: 1.75rem 2rem;
    max-width: 720px;
    width: calc(100% - 4rem);
  }
}
</style>
