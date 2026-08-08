<script setup lang="ts">
// Number stepper: − [ 4.5 ] + — mono digits, configurable step (spec §5)
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    step?: number
    min?: number
    max?: number
    disabled?: boolean
  }>(),
  { step: 1, disabled: false },
)

const model = defineModel<number | null>({ default: null })

const decimals = computed(() => (Number.isInteger(props.step) ? 0 : 1))

function clamp(v: number): number {
  let out = v
  if (props.min !== undefined) out = Math.max(props.min, out)
  if (props.max !== undefined) out = Math.min(props.max, out)
  return Number(out.toFixed(2))
}

function nudge(dir: 1 | -1) {
  const base = model.value ?? (props.min ?? 0)
  const next = model.value === null ? base : base + dir * props.step
  model.value = clamp(next)
}

function onInput(e: Event) {
  const raw = (e.target as HTMLInputElement).value.replace(',', '.')
  if (raw === '') {
    model.value = null
    return
  }
  const n = Number(raw)
  if (!Number.isNaN(n)) model.value = clamp(n)
}

const display = computed(() =>
  model.value === null ? '' : model.value.toFixed(decimals.value === 0 && !Number.isInteger(model.value) ? 1 : decimals.value),
)
</script>

<template>
  <div class="stepper" :class="{ disabled }">
    <button type="button" class="nudge" :disabled="disabled" aria-label="Decrease" @click="nudge(-1)">
      −
    </button>
    <input
      class="value data"
      type="text"
      inputmode="decimal"
      :value="display"
      :disabled="disabled"
      placeholder="–"
      @change="onInput"
    />
    <button type="button" class="nudge" :disabled="disabled" aria-label="Increase" @click="nudge(1)">
      +
    </button>
  </div>
</template>

<style scoped>
/* MC style: two stone nudge buttons flanking a sunken slot */
.stepper {
  display: inline-flex;
  align-items: stretch;
  gap: 3px;
}
.stepper.disabled {
  opacity: 0.5;
}
.nudge {
  width: var(--tap);
  min-height: var(--tap);
  border: 2px solid #1a1a1a;
  background: #6f6f6f;
  color: #fff;
  text-shadow: var(--px-text-shadow);
  font-size: 1.2rem;
  font-family: var(--font-data);
  cursor: pointer;
  box-shadow: var(--bevel-out);
}
.nudge:not(:disabled):hover {
  filter: brightness(1.12);
  color: #ffffa0;
}
.nudge:not(:disabled):active {
  box-shadow: var(--bevel-in);
}
.nudge:disabled {
  cursor: not-allowed;
}
.value {
  width: 4.5rem;
  border: 1px solid #373737;
  background: #8b8b8b;
  box-shadow: var(--bevel-in);
  color: #fff;
  text-shadow: var(--px-text-shadow);
  text-align: center;
  font-size: 1rem;
}
.value::placeholder {
  color: #d0d0d0;
}
.value:focus {
  outline: none;
  background: #969696;
}
</style>
