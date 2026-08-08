<script setup lang="ts">
// Textarea autosize — optional, không giới hạn độ dài (spec §5)
import { ref, watch } from 'vue'

withDefaults(defineProps<{ placeholder?: string; disabled?: boolean }>(), { disabled: false })

const model = defineModel<string | null>({ default: null })
const el = ref<HTMLTextAreaElement | null>(null)

function resize() {
  const t = el.value
  if (!t) return
  t.style.height = 'auto'
  t.style.height = `${t.scrollHeight}px`
}

function onInput(e: Event) {
  const v = (e.target as HTMLTextAreaElement).value
  model.value = v.length > 0 ? v : null
  resize()
}

watch(model, () => requestAnimationFrame(resize))
</script>

<template>
  <textarea
    ref="el"
    class="textarea"
    rows="4"
    :placeholder="placeholder"
    :disabled="disabled"
    :value="model ?? ''"
    @input="onInput"
  />
</template>

<style scoped>
.textarea {
  width: 100%;
  min-height: 6rem;
  padding: 0.75rem;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius);
  background: var(--surface);
  color: var(--text);
  font-family: var(--font-ui);
  font-size: 0.95rem;
  line-height: 1.5;
  resize: none;
}
.textarea::placeholder {
  color: var(--text-faint);
}
.textarea:disabled {
  opacity: 0.45;
}
</style>
