<script setup lang="ts">
// Time picker: dùng input time native — mobile mở wheel của OS, ít ma sát nhất (nguyên tắc 1).
// Default = giá trị lần trước do parent truyền vào (spec §5 "nhớ giá trị lần trước").
const model = defineModel<string | null>({ default: null })

function onInput(e: Event) {
  const v = (e.target as HTMLInputElement).value
  model.value = v.length > 0 ? v : null
}
</script>

<template>
  <input class="time-input" type="time" :value="model ?? ''" @input="onInput" />
</template>

<style scoped>
.time-input {
  width: 100%;
  font-size: 2rem;
  text-align: center;
  padding: 0.75rem;
  border: 1px solid var(--p-surface-300);
  border-radius: 10px;
  background: var(--p-surface-0);
  color: var(--p-text-color);
}
@media (prefers-color-scheme: dark) {
  .time-input {
    background: var(--p-surface-900);
    border-color: var(--p-surface-700);
  }
}
</style>
