<script setup lang="ts">
withDefaults(
  defineProps<{
    label?: string
    variant?: 'primary' | 'ghost' | 'subtle'
    size?: 'md' | 'lg' | 'sm'
    disabled?: boolean
    loading?: boolean
    block?: boolean
  }>(),
  { variant: 'primary', size: 'md', disabled: false, loading: false, block: false },
)

const emit = defineEmits<{ click: [] }>()
</script>

<template>
  <button
    type="button"
    class="btn"
    :class="[`v-${variant}`, `s-${size}`, { block }]"
    :disabled="disabled || loading"
    @click="emit('click')"
  >
    <span v-if="loading" class="spinner" aria-hidden="true" />
    <slot>{{ label }}</slot>
  </button>
</template>

<style scoped>
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  border-radius: var(--radius);
  border: 1px solid transparent;
  font-weight: 500;
  font-size: 0.9rem;
  cursor: pointer;
  transition: background 100ms, border-color 100ms;
  user-select: none;
}
.btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
.block {
  width: 100%;
}

.s-sm {
  min-height: 32px;
  padding: 0 0.75rem;
  font-size: 0.8rem;
}
.s-md {
  min-height: 40px;
  padding: 0 1rem;
}
.s-lg {
  min-height: var(--tap);
  padding: 0 1.25rem;
  font-size: 1rem;
}

.v-primary {
  background: var(--accent);
  color: var(--on-accent);
}
.v-primary:not(:disabled):hover {
  background: var(--accent-strong);
}

.v-ghost {
  background: transparent;
  border-color: var(--border-strong);
  color: var(--text);
}
.v-ghost:not(:disabled):hover {
  border-color: var(--accent);
  color: var(--accent);
}

.v-subtle {
  background: transparent;
  color: var(--text-muted);
}
.v-subtle:not(:disabled):hover {
  color: var(--text);
  background: var(--surface-raised);
}

.spinner {
  width: 14px;
  height: 14px;
  border: 2px solid currentColor;
  border-right-color: transparent;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}
@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
