<script setup lang="ts">
import Button from 'primevue/button'
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()
</script>

<template>
  <!-- Màn Cài đặt đầy đủ là v2 — v1 chỉnh config bằng insert DB (spec v3.2) -->
  <div class="settings">
    <h1>Cài đặt</h1>
    <p class="muted">
      Thêm/sửa habit và biến theo dõi: insert document vào MongoDB — form tự cập nhật, không cần
      deploy. UI đầy đủ là chuyện v2.
    </p>
    <Button label="Đổi secret key" severity="secondary" outlined @click="session.reset()" />
  </div>
</template>

<style scoped>
.settings {
  padding: 1rem;
  max-width: 640px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  align-items: flex-start;
}
.muted {
  color: var(--p-text-muted-color);
}
</style>
