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
  width: 100%;
  max-width: 340px;
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
  padding: 1.75rem;
  border: 1px solid var(--border);
  border-radius: 10px;
  background: var(--surface);
}
.gate-title {
  margin: 0;
  font-size: 1.25rem;
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
