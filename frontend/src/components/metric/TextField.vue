<script setup lang="ts">
// Optional textarea, unlimited length (spec §5)
import UiTextarea from '@/components/ui/UiTextarea.vue'

const model = defineModel<string | null>({ default: null })
</script>

<template>
  <UiTextarea v-model="model" placeholder="Write if you feel like it…" />
</template>
