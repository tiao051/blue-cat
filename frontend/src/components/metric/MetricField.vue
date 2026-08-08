<script setup lang="ts">
// The ONE component that renders a tracked metric — switches on def.type (spec §5).
// Raw values stay widget-shaped; conversion to MetricValueInput lives in lib/metricValues.
import type { MetricDefinition } from '@/api/types'
import ScaleField from './ScaleField.vue'
import NumberField from './NumberField.vue'
import TimeField from './TimeField.vue'
import EnumField from './EnumField.vue'
import MultiEnumField from './MultiEnumField.vue'
import TextField from './TextField.vue'

defineProps<{
  def: MetricDefinition
  showLabel?: boolean
  /** raw value: number | string | string[] | null depending on type */
  modelValue?: unknown
}>()

const emit = defineEmits<{ 'update:modelValue': [value: unknown] }>()

const update = (v: unknown) => emit('update:modelValue', v)
</script>

<template>
  <div class="metric-field">
    <label v-if="showLabel !== false" class="metric-label">{{ def.label }}</label>

    <ScaleField
      v-if="def.type === 'scale'"
      :def="def"
      :model-value="(modelValue as number | null) ?? null"
      @update:model-value="update"
    />
    <NumberField
      v-else-if="def.type === 'number'"
      :def="def"
      :model-value="(modelValue as number | null) ?? null"
      @update:model-value="update"
    />
    <TimeField
      v-else-if="def.type === 'time'"
      :model-value="(modelValue as string | null) ?? null"
      @update:model-value="update"
    />
    <EnumField
      v-else-if="def.type === 'enum'"
      :def="def"
      :model-value="(modelValue as string | null) ?? null"
      @update:model-value="update"
    />
    <MultiEnumField
      v-else-if="def.type === 'multi_enum'"
      :def="def"
      :model-value="(modelValue as string[]) ?? []"
      @update:model-value="update"
    />
    <TextField
      v-else-if="def.type === 'text'"
      :model-value="(modelValue as string | null) ?? null"
      @update:model-value="update"
    />
    <p v-else class="unknown">Type '{{ def.type }}' isn't supported yet</p>
  </div>
</template>

<style scoped>
.metric-field {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}
.metric-label {
  font-size: 1.05rem;
  font-weight: 600;
  letter-spacing: -0.005em;
}
.unknown {
  color: var(--danger);
  font-size: 0.9rem;
}
</style>
