<script setup lang="ts">
// Stepper — bước cấu hình được, 0.5 cho giờ (spec §5)
import { computed } from 'vue'
import InputNumber from 'primevue/inputnumber'
import type { MetricDefinition } from '@/api/types'

const props = defineProps<{ def: MetricDefinition }>()
const model = defineModel<number | null>({ default: null })

const step = computed(() => props.def.validation?.step ?? 1)
const fractionDigits = computed(() => (Number.isInteger(step.value) ? 0 : 1))
</script>

<template>
  <InputNumber
    v-model="model"
    show-buttons
    button-layout="horizontal"
    :step="step"
    :min="def.validation?.min ?? undefined"
    :max="def.validation?.max ?? undefined"
    :min-fraction-digits="0"
    :max-fraction-digits="fractionDigits"
    fluid
    :input-style="{ textAlign: 'center', fontSize: '1.5rem' }"
  >
    <template #incrementicon>+</template>
    <template #decrementicon>−</template>
  </InputNumber>
</template>
