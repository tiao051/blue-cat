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
  <!-- the world behind the panel: square moon, blocky clouds, a grass horizon -->
  <div class="world" aria-hidden="true">
    <div class="moon"></div>
    <div class="cloud c1"></div>
    <div class="cloud c2"></div>
    <div class="cloud c3"></div>
    <div class="horizon"></div>
  </div>

  <!-- pixel bees buzzing around the night garden -->
  <div class="bees" aria-hidden="true">
    <div v-for="i in 4" :key="i" class="bee" :class="`b${i}`">
      <div class="bee-bob">
        <svg viewBox="0 0 14 11" shape-rendering="crispEdges" xmlns="http://www.w3.org/2000/svg">
          <!-- wing, fluttering -->
          <g class="wing">
            <rect x="5" y="0" width="5" height="2" fill="#e6eef5" opacity="0.75" />
            <rect x="6" y="2" width="3" height="1" fill="#e6eef5" opacity="0.6" />
          </g>
          <!-- striped body -->
          <rect x="2" y="3" width="10" height="6" fill="#e8c33c" />
          <rect x="2" y="3" width="10" height="1" fill="#f2d465" />
          <rect x="2" y="8" width="10" height="1" fill="#c9a52e" />
          <rect x="4" y="3" width="2" height="6" fill="#6e4f23" />
          <rect x="8" y="3" width="2" height="6" fill="#6e4f23" />
          <!-- eye + antenna -->
          <rect x="10" y="4" width="2" height="2" fill="#1d1d21" />
          <rect x="12" y="2" width="1" height="1" fill="#1d1d21" />
          <!-- stinger -->
          <rect x="1" y="5" width="1" height="2" fill="#b8b8b8" />
          <!-- little legs -->
          <rect x="4" y="9" width="1" height="1" fill="#1d1d21" />
          <rect x="7" y="9" width="1" height="1" fill="#1d1d21" />
        </svg>
      </div>
    </div>
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

/* ---- the world ---- */
.world {
  position: fixed;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
}

/* a square moon with blocky craters and a soft moonlight bloom */
.moon {
  position: absolute;
  top: 7%;
  right: 11%;
  width: 52px;
  height: 52px;
  background: #e9ecd8;
  box-shadow:
    inset -12px -10px 0 #ccd2b4,
    inset 8px 10px 0 #f5f7e8,
    0 0 46px 12px rgba(210, 225, 255, 0.28);
}
.moon::after {
  content: '';
  position: absolute;
  width: 8px;
  height: 8px;
  background: #bcc2a4;
  top: 12px;
  left: 14px;
  box-shadow: 18px 14px 0 #bcc2a4, 6px 26px 0 4px #c5cbab;
}

/* flat blocky clouds drifting across the night sky */
.cloud {
  position: absolute;
  width: 84px;
  height: 12px;
  background: #dfe6f2;
  opacity: 0.13;
  box-shadow: 24px -12px 0 #dfe6f2, -24px 0 0 #dfe6f2, 48px 0 0 #dfe6f2, 12px 12px 0 #dfe6f2;
  animation: cloud-drift linear infinite;
}
.c1 { top: 13%; animation-duration: 150s; }
.c2 { top: 24%; animation-duration: 210s; animation-delay: -80s; transform: scale(1.5); }
.c3 { top: 5%;  animation-duration: 260s; animation-delay: -160s; transform: scale(0.8); opacity: 0.09; }
@keyframes cloud-drift {
  from { left: -160px; }
  to   { left: 110vw; }
}

/* the grass-topped dirt horizon at the bottom of the world */
.horizon {
  position: absolute;
  left: 0;
  right: 0;
  bottom: 0;
  height: 112px;
  background-image:
    url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='8'%3E%3Crect width='16' height='5' fill='%23477a2d'/%3E%3Crect x='0' y='0' width='4' height='3' fill='%23548c35'/%3E%3Crect x='8' y='0' width='5' height='2' fill='%23548c35'/%3E%3Crect x='2' y='5' width='3' height='2' fill='%23477a2d'/%3E%3Crect x='9' y='5' width='4' height='3' fill='%23477a2d'/%3E%3C/svg%3E"),
    url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='8' height='8'%3E%3Crect width='8' height='8' fill='%2326201a'/%3E%3Crect x='1' y='0' width='2' height='2' fill='%232e2620'/%3E%3Crect x='5' y='1' width='2' height='2' fill='%23201a15'/%3E%3Crect x='0' y='4' width='2' height='2' fill='%23231d17'/%3E%3Crect x='3' y='3' width='2' height='2' fill='%232b231c'/%3E%3Crect x='6' y='5' width='2' height='2' fill='%23312822'/%3E%3Crect x='2' y='6' width='2' height='2' fill='%231e1813'/%3E%3C/svg%3E");
  background-repeat: repeat-x, repeat;
  background-size: 64px 32px, 48px 48px;
  background-position: top, 0 24px;
  image-rendering: pixelated;
  box-shadow: 0 -18px 30px rgba(0, 0, 0, 0.35);
}

/* pixel bees: gentle roaming + a quick hover bob + fluttering wings */
.bees {
  position: fixed;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
}
.bee {
  position: absolute;
  width: 30px;
  height: 24px;
  animation: bee-roam ease-in-out infinite alternate;
  opacity: 0.9;
}
.bee svg {
  width: 100%;
  height: 100%;
  image-rendering: pixelated;
}
/* two of them fly the other way */
.b2 svg,
.b4 svg {
  transform: scaleX(-1);
}
.b1 { left: 12%; top: 34%; animation-duration: 21s; }
.b2 { left: 74%; top: 22%; animation-duration: 26s; animation-delay: -9s; width: 24px; height: 19px; }
.b3 { left: 40%; top: 64%; animation-duration: 24s; animation-delay: -15s; width: 26px; height: 21px; opacity: 0.75; }
.b4 { left: 86%; top: 56%; animation-duration: 19s; animation-delay: -5s; }

.bee-bob {
  width: 100%;
  height: 100%;
  animation: bee-bob 1.05s ease-in-out infinite alternate;
}
.wing {
  transform-origin: 7px 3px;
  animation: bee-flap 0.18s steps(2) infinite;
}

@keyframes bee-roam {
  0%   { transform: translate(0, 0); }
  30%  { transform: translate(46px, -18px); }
  60%  { transform: translate(12px, 22px); }
  100% { transform: translate(-38px, -8px); }
}
@keyframes bee-bob {
  from { transform: translateY(0); }
  to   { transform: translateY(-5px); }
}
@keyframes bee-flap {
  from { transform: scaleY(1); }
  to   { transform: scaleY(0.35); }
}
/* content lives on one big inventory panel — like an opened chest over the world */
.content-inner {
  position: relative;
  margin: 0.9rem auto calc(92px + env(safe-area-inset-bottom));
  padding: 1.6rem 1rem 1.25rem;
  max-width: 640px;
  /* subtle top-light on the panel + soft ambient shadow beneath it (shader AO) */
  background: linear-gradient(180deg, #cdcdcd 0%, var(--surface) 18%, #bfbfbf 100%);
  border: 2px solid #1a1a1a;
  box-shadow: var(--bevel-out), 0 18px 48px rgba(0, 0, 0, 0.55);
}
/* grass growing along the panel's top edge — the panel is a block seen from the side */
.content-inner::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 8px;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='8'%3E%3Crect width='16' height='6' fill='%23548c35'/%3E%3Crect x='0' y='0' width='4' height='4' fill='%2363a03f'/%3E%3Crect x='9' y='0' width='4' height='3' fill='%2363a03f'/%3E%3Crect x='3' y='6' width='3' height='2' fill='%23548c35'/%3E%3Crect x='11' y='6' width='3' height='2' fill='%23548c35'/%3E%3C/svg%3E");
  background-size: 32px 8px;
  image-rendering: pixelated;
  pointer-events: none;
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
  counter-reset: slot;
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
  position: relative;
  counter-increment: slot;
}
/* tiny slot number, like hotbar keybinds */
.nav-item::before {
  content: counter(slot);
  position: absolute;
  top: 1px;
  left: 4px;
  font-family: var(--font-data);
  font-size: 8px;
  color: rgba(255, 255, 255, 0.45);
  text-shadow: 1px 1px 0 rgba(0, 0, 0, 0.8);
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
