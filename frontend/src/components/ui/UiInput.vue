<script setup lang="ts">
withDefaults(
  defineProps<{
    type?: 'text' | 'password'
    placeholder?: string
    disabled?: boolean
    mono?: boolean
  }>(),
  { type: 'text', disabled: false, mono: false },
)

const model = defineModel<string>({ default: '' })
const emit = defineEmits<{ enter: [] }>()

function onInput(e: Event) {
  model.value = (e.target as HTMLInputElement).value
}
</script>

<template>
  <input
    class="input"
    :class="{ data: mono }"
    :type="type"
    :placeholder="placeholder"
    :disabled="disabled"
    :value="model"
    autocomplete="off"
    @input="onInput"
    @keyup.enter="emit('enter')"
  />
</template>

<style scoped>
.input {
  width: 100%;
  min-height: var(--tap);
  padding: 0 0.75rem;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius);
  background: var(--surface);
  color: var(--text);
  font-family: var(--font-ui);
  font-size: 0.95rem;
}
.input::placeholder {
  color: var(--text-faint);
}
.input:disabled {
  opacity: 0.45;
}
</style>
