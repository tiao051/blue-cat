<script setup lang="ts">
// Time picker: native time input — mobile gets the OS wheel, lowest friction (principle 1).
// Default = last recorded value, passed in by the parent (spec §5 "remember last value").
const model = defineModel<string | null>({ default: null })

function onInput(e: Event) {
  const v = (e.target as HTMLInputElement).value
  model.value = v.length > 0 ? v : null
}
</script>

<template>
  <input class="time-input data" type="time" :value="model ?? ''" @input="onInput" />
</template>

<style scoped>
.time-input {
  width: 100%;
  font-size: 1.9rem;
  text-align: center;
  padding: 0.6rem;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius);
  background: var(--surface);
  color: var(--text);
}
.time-input:focus {
  border-color: var(--accent);
  outline: none;
}
/* webkit's calendar icon is dark and hard to see on a dark ground */
@media (prefers-color-scheme: dark) {
  .time-input::-webkit-calendar-picker-indicator {
    filter: invert(0.8);
  }
}
</style>
