<script setup lang="ts">
// Time picker: input time native — mobile mở wheel của OS, ít ma sát nhất (nguyên tắc 1).
// Default = giá trị lần trước do parent truyền vào (spec §5 "nhớ giá trị lần trước").
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
/* icon lịch của webkit tối màu khó thấy trên nền tối */
@media (prefers-color-scheme: dark) {
  .time-input::-webkit-calendar-picker-indicator {
    filter: invert(0.8);
  }
}
</style>
