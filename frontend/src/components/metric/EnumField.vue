<script setup lang="ts">
// Chip chọn một (spec §5)
import SelectButton from 'primevue/selectbutton'
import type { MetricDefinition } from '@/api/types'

defineProps<{ def: MetricDefinition }>()
const model = defineModel<string | null>({ default: null })
</script>

<template>
  <SelectButton
    v-model="model"
    :options="def.options ?? []"
    option-label="label"
    option-value="value"
    :allow-empty="true"
  />
</template>
