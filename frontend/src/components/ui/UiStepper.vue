<script setup lang="ts">
// Stepper số: − [ 4.5 ] + — số chạy mono, bước cấu hình được (spec §5)
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
    <button type="button" class="nudge" :disabled="disabled" aria-label="Giảm" @click="nudge(-1)">
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
    <button type="button" class="nudge" :disabled="disabled" aria-label="Tăng" @click="nudge(1)">
      +
    </button>
  </div>
</template>

<style scoped>
.stepper {
  display: inline-flex;
  align-items: stretch;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius);
  overflow: hidden;
  background: var(--surface);
}
.stepper.disabled {
  opacity: 0.45;
}
.nudge {
  width: var(--tap);
  min-height: var(--tap);
  border: none;
  background: transparent;
  color: var(--text-muted);
  font-size: 1.3rem;
  font-family: var(--font-data);
  cursor: pointer;
}
.nudge:not(:disabled):hover {
  color: var(--accent);
  background: var(--accent-dim);
}
.nudge:disabled {
  cursor: not-allowed;
}
.value {
  width: 4.5rem;
  border: none;
  border-left: 1px solid var(--border);
  border-right: 1px solid var(--border);
  background: transparent;
  color: var(--text);
  text-align: center;
  font-size: 1.05rem;
}
.value:focus {
  outline: none;
  background: var(--accent-dim);
}
</style>
