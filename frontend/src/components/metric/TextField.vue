<script setup lang="ts">
// Textarea optional, không giới hạn độ dài (spec §5)
import Textarea from 'primevue/textarea'

const model = defineModel<string | null>({ default: null })
</script>

<template>
  <Textarea
    :model-value="model ?? ''"
    auto-resize
    rows="4"
    fluid
    placeholder="Muốn ghi thì ghi…"
    @update:model-value="(v: string) => (model = v.length > 0 ? v : null)"
  />
</template>
