<script setup lang="ts">
// Stepper — configurable step, 0.5 for hours (spec §5)
import { computed } from 'vue'
import UiStepper from '@/components/ui/UiStepper.vue'
import type { MetricDefinition } from '@/api/types'

const props = defineProps<{ def: MetricDefinition }>()
const model = defineModel<number | null>({ default: null })

const step = computed(() => props.def.validation?.step ?? 1)
</script>

<template>
  <div class="number-field">
    <UiStepper
      v-model="model"
      :step="step"
      :min="def.validation?.min ?? undefined"
      :max="def.validation?.max ?? undefined"
    />
  </div>
</template>

<style scoped>
.number-field {
  display: flex;
  justify-content: center;
}
</style>
