<script setup lang="ts">
// Chip chọn một (spec §5)
import { computed } from 'vue'
import UiSegmented from '@/components/ui/UiSegmented.vue'
import type { MetricDefinition } from '@/api/types'

const props = defineProps<{ def: MetricDefinition }>()
const model = defineModel<string | null>({ default: null })

const options = computed(() =>
  (props.def.options ?? []).map((o) => ({ label: o.label, value: o.value })),
)
</script>

<template>
  <UiSegmented v-model="model" :options="options" />
</template>
