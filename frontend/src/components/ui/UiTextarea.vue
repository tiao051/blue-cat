<script setup lang="ts">
// Autosizing textarea — optional, unlimited length (spec §5)
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
  border: 1px solid #373737;
  background: #8b8b8b;
  box-shadow: var(--bevel-in);
  color: #fff;
  text-shadow: 1px 1px 0 rgba(0, 0, 0, 0.5);
  font-family: var(--font-ui);
  font-size: 0.9rem;
  line-height: 1.6;
  resize: none;
}
.textarea::placeholder {
  color: #d5d5d5;
  text-shadow: none;
}
.textarea:focus {
  outline: none;
  background: #969696;
}
.textarea:disabled {
  opacity: 0.5;
}
</style>
