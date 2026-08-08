<script setup lang="ts">
import UiButton from '@/components/ui/UiButton.vue'
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()
</script>

<template>
  <!-- The full Settings screen is v2 — v1 changes config by inserting DB documents (spec v3.2) -->
  <div class="settings">
    <h1>Settings</h1>
    <p class="muted">
      Adding or editing habits and tracked metrics: insert a document into MongoDB — the form
      updates itself, no deploy needed. A full UI for this is a v2 thing.
    </p>
    <UiButton label="Change secret key" variant="ghost" @click="session.reset()" />
  </div>
</template>

<style scoped>
.settings {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  align-items: flex-start;
}
.settings h1 {
  margin: 0;
}
.muted {
  color: var(--text-muted);
  margin: 0;
}
</style>
