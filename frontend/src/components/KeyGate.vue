<script setup lang="ts">
// First-run secret key gate — entered once, kept in localStorage (spec §10)
import { ref } from 'vue'
import UiButton from '@/components/ui/UiButton.vue'
import UiInput from '@/components/ui/UiInput.vue'
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()
const input = ref('')

function save() {
  if (input.value.trim().length > 0) session.save(input.value)
}
</script>

<template>
  <div class="gate">
    <div class="gate-card">
      <p class="overline">daily tracker</p>
      <span class="splash" aria-hidden="true">Track every day!</span>
      <h1 class="gate-title">Enter your secret key</h1>
      <p class="muted">Only needed once on this device.</p>
      <UiInput v-model="input" type="password" mono placeholder="secret key" @enter="save" />
      <UiButton label="Unlock" size="lg" block :disabled="input.trim().length === 0" @click="save" />
    </div>
  </div>
</template>

<style scoped>
.gate {
  min-height: 100dvh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.5rem;
}
.gate-card {
  position: relative;
  width: 100%;
  max-width: 360px;
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
  padding: 1.75rem;
  border: 2px solid #1a1a1a;
  box-shadow: var(--bevel-out);
  background: var(--surface);
}
/* the pulsing yellow splash text from the title screen */
.splash {
  position: absolute;
  top: 0.6rem;
  right: -1.5rem;
  transform: rotate(-14deg);
  font-family: var(--font-data);
  font-size: 0.8rem;
  font-weight: 700;
  color: #ffff55;
  text-shadow: 2px 2px 0 #3f3f10;
  animation: splash-pulse 0.5s ease-in-out infinite alternate;
  pointer-events: none;
}
@keyframes splash-pulse {
  from { transform: rotate(-14deg) scale(1); }
  to   { transform: rotate(-14deg) scale(1.07); }
}
.gate-title {
  margin: 0;
  font-size: 1.15rem;
  font-weight: 700;
}
.overline {
  margin: 0;
  color: var(--accent);
}
.muted {
  margin: 0;
  color: var(--text-muted);
  font-size: 0.88rem;
}
</style>
