<script setup lang="ts">
// Minecraft stone button: gray slab, black outline, raised bevel, label with hard
// pixel shadow. Hover turns the label pale yellow (the classic MC selected color).
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
  border: 2px solid #1a1a1a;
  font-weight: 400;
  font-size: 0.85rem;
  cursor: pointer;
  user-select: none;
  text-shadow: var(--px-text-shadow);
  box-shadow: var(--bevel-out);
  transition: filter 60ms;
}
.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.btn:not(:disabled):hover {
  filter: brightness(1.12);
  color: #ffffa0; /* MC hover text */
}
.btn:not(:disabled):active {
  box-shadow: var(--bevel-in); /* pressed = bevel inverts */
}
.block {
  width: 100%;
}

.s-sm {
  min-height: 34px;
  padding: 0 0.75rem;
  font-size: 0.75rem;
}
.s-md {
  min-height: 40px;
  padding: 0 1rem;
}
.s-lg {
  min-height: var(--tap);
  padding: 0 1.25rem;
  font-size: 0.95rem;
}

/* primary: XP-green slab */
.v-primary {
  background: #578a2c;
  color: #ffffff;
}

/* ghost: the classic gray stone button */
.v-ghost {
  background: #6f6f6f;
  color: #ffffff;
}

/* subtle: flat text action, panel-colored */
.v-subtle {
  background: transparent;
  border-color: transparent;
  box-shadow: none;
  color: var(--text-muted);
  text-shadow: none;
}
.v-subtle:not(:disabled):hover {
  color: var(--text);
  background: var(--surface-raised);
  filter: none;
}
.v-subtle:not(:disabled):active {
  box-shadow: none;
}

.spinner {
  width: 12px;
  height: 12px;
  border: 3px solid currentColor;
  border-right-color: transparent;
  animation: spin 0.8s steps(8) infinite;
}
@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
