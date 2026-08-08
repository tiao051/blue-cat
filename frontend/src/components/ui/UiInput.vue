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
  border: 1px solid #373737;
  background: #8b8b8b;
  box-shadow: var(--bevel-in);
  color: #fff;
  text-shadow: 1px 1px 0 rgba(0, 0, 0, 0.5);
  font-family: var(--font-ui);
  font-size: 0.9rem;
}
.input::placeholder {
  color: #d5d5d5;
  text-shadow: none;
}
.input:focus {
  outline: none;
  background: #969696;
}
.input:disabled {
  opacity: 0.5;
}
</style>
