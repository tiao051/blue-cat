<script setup lang="ts">
// Multi-choice chips with a HARD maxSelect block: at the limit, unselected chips disable —
// keeps attention's "mainly" meaningful (spec §5 + Appendix A, M1 checklist).
import { computed } from 'vue'
import type { MetricDefinition } from '@/api/types'

const props = defineProps<{ def: MetricDefinition }>()
const model = defineModel<string[]>({ default: () => [] })

const atLimit = computed(
  () => props.def.maxSelect != null && model.value.length >= props.def.maxSelect,
)

function toggle(value: string) {
  if (model.value.includes(value)) {
    model.value = model.value.filter((v) => v !== value)
  } else if (!atLimit.value) {
    model.value = [...model.value, value]
  }
}
</script>

<template>
  <div class="chips" role="group" :aria-label="def.label">
    <button
      v-for="opt in def.options ?? []"
      :key="opt.value"
      type="button"
      class="chip"
      :class="{ selected: model.includes(opt.value) }"
      :disabled="!model.includes(opt.value) && atLimit"
      :aria-pressed="model.includes(opt.value)"
      @click="toggle(opt.value)"
    >
      {{ opt.label }}
    </button>
    <p v-if="def.maxSelect" class="hint data">pick up to {{ def.maxSelect }}</p>
  </div>
</template>

<style scoped>
.chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}
.chip {
  padding: 0 0.9rem;
  min-height: var(--tap);
  border-radius: var(--radius);
  border: 1px solid var(--border-strong);
  background: var(--surface);
  color: var(--text);
  font-size: 0.9rem;
  cursor: pointer;
  transition: background 100ms, border-color 100ms;
}
.chip:not(:disabled):not(.selected):hover {
  border-color: var(--accent);
}
.chip.selected {
  background: var(--accent);
  border-color: var(--accent);
  color: var(--on-accent);
  font-weight: 500;
}
.chip:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}
.hint {
  width: 100%;
  margin: 0.25rem 0 0;
  font-size: 0.72rem;
  color: var(--text-faint);
}
</style>
