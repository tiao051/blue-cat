<script setup lang="ts">
// Màn nhập secret key lần đầu — nhập một lần, lưu localStorage (spec §10)
import { ref } from 'vue'
import Button from 'primevue/button'
import Password from 'primevue/password'
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()
const input = ref('')

function save() {
  if (input.value.trim().length > 0) session.save(input.value)
}
</script>

<template>
  <div class="gate">
    <div class="gate-card">
      <h1>Daily Tracker</h1>
      <p class="muted">Nhập secret key để bắt đầu — chỉ cần một lần trên thiết bị này.</p>
      <Password v-model="input" :feedback="false" toggle-mask fluid @keyup.enter="save" />
      <Button label="Vào app" fluid :disabled="input.trim().length === 0" @click="save" />
    </div>
  </div>
</template>

<style scoped>
.gate {
  min-height: 100dvh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.5rem;
}
.gate-card {
  width: 100%;
  max-width: 360px;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.gate-card h1 {
  margin: 0;
  text-align: center;
}
.muted {
  color: var(--p-text-muted-color);
  text-align: center;
  margin: 0;
}
</style>
