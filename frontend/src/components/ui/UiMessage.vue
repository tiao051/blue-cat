<script setup lang="ts">
withDefaults(defineProps<{ severity?: 'error' | 'info' }>(), { severity: 'error' })
</script>

<template>
  <div class="message" :class="`sev-${severity}`" role="alert">
    <slot />
  </div>
</template>

<style scoped>
.message {
  padding: 0.6rem 0.9rem;
  border-radius: var(--radius);
  border: 1px solid;
  font-size: 0.88rem;
  line-height: 1.45;
}
.sev-error {
  border-color: var(--danger);
  background: var(--danger-dim);
  color: var(--danger);
}
.sev-info {
  border-color: var(--border-strong);
  background: var(--surface-raised);
  color: var(--text-muted);
}
</style>
