<script setup lang="ts">
// Chip chọn nhiều, CHẶN CỨNG maxSelect: đủ số thì chip chưa chọn bị disable —
// giữ chữ "chủ yếu" của attention (spec §5 + Phụ lục A, checklist M1).
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
    <p v-if="def.maxSelect" class="hint">Chọn tối đa {{ def.maxSelect }}</p>
  </div>
</template>

<style scoped>
.chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}
.chip {
  padding: 0.6rem 1rem;
  border-radius: 999px;
  border: 1px solid var(--p-surface-300);
  background: var(--p-surface-0);
  color: var(--p-text-color);
  font-size: 0.95rem;
  cursor: pointer;
  min-height: 44px; /* vùng chạm mobile */
}
.chip.selected {
  background: var(--p-primary-color);
  border-color: var(--p-primary-color);
  color: var(--p-primary-contrast-color);
}
.chip:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}
.hint {
  width: 100%;
  margin: 0.25rem 0 0;
  font-size: 0.8rem;
  color: var(--p-text-muted-color);
}
@media (prefers-color-scheme: dark) {
  .chip {
    background: var(--p-surface-900);
    border-color: var(--p-surface-700);
  }
  .chip.selected {
    background: var(--p-primary-color);
    border-color: var(--p-primary-color);
  }
}
</style>
