<script setup lang="ts">
// Textarea optional, không giới hạn độ dài (spec §5)
import UiTextarea from '@/components/ui/UiTextarea.vue'

const model = defineModel<string | null>({ default: null })
</script>

<template>
  <UiTextarea v-model="model" placeholder="Muốn ghi thì ghi…" />
</template>
