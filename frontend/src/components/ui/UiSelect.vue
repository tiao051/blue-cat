<script setup lang="ts">
// Styled native select — mobile gets the OS picker, no homemade dropdown
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
  border: 2px solid #1a1a1a;
  background: #6f6f6f;
  color: #fff;
  text-shadow: var(--px-text-shadow);
  box-shadow: var(--bevel-out);
  font-size: 0.85rem;
  font-family: var(--font-ui);
  cursor: pointer;
}
.select:hover {
  filter: brightness(1.12);
}
.chevron {
  position: absolute;
  right: 0.6rem;
  top: 50%;
  transform: translateY(-50%);
  pointer-events: none;
  color: #efefef;
  font-size: 0.7rem;
  text-shadow: var(--px-text-shadow);
}
</style>
