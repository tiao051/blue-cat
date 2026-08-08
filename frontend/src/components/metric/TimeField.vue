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
  font-size: 1.7rem;
  font-family: var(--font-data);
  text-align: center;
  padding: 0.6rem;
  border: 1px solid #373737;
  background: #8b8b8b;
  box-shadow: var(--bevel-in);
  color: #fff;
  text-shadow: 2px 2px 0 rgba(0, 0, 0, 0.5);
}
.time-input:focus {
  outline: none;
  background: #969696;
}
/* webkit's calendar icon is dark and hard to see on the sunken gray slot */
.time-input::-webkit-calendar-picker-indicator {
  filter: invert(0.9);
}
</style>
