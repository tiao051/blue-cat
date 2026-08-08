<script setup lang="ts">
// Native select styled — mobile mở picker OS, không dropdown tự chế
export interface SelectOption {
  label: string
  value: string | number
}

withDefaults(
  defineProps<{
    options: SelectOption[]
    placeholder?: string
    disabled?: boolean
  }>(),
  { disabled: false },
)

const model = defineModel<string | number | null>({ default: null })

function onChange(e: Event) {
  const v = (e.target as HTMLSelectElement).value
  model.value = v === '' ? null : v
}
</script>

<template>
  <span class="select-wrap" :class="{ disabled }">
    <select class="select data" :disabled="disabled" :value="model ?? ''" @change="onChange">
      <option v-if="placeholder" value="" disabled>{{ placeholder }}</option>
      <option v-for="opt in options" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
    </select>
    <span class="chevron" aria-hidden="true">▾</span>
  </span>
</template>

<style scoped>
.select-wrap {
  position: relative;
  display: inline-flex;
}
.select-wrap.disabled {
  opacity: 0.45;
}
.select {
  appearance: none;
  min-height: 40px;
  padding: 0 1.9rem 0 0.75rem;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius);
  background: var(--surface);
  color: var(--text);
  font-size: 0.9rem;
  cursor: pointer;
}
.chevron {
  position: absolute;
  right: 0.6rem;
  top: 50%;
  transform: translateY(-50%);
  pointer-events: none;
  color: var(--text-muted);
  font-size: 0.7rem;
}
</style>
