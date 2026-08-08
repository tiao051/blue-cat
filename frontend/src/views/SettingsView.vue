<script setup lang="ts">
import UiButton from '@/components/ui/UiButton.vue'
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
    <UiButton label="Đổi secret key" variant="ghost" @click="session.reset()" />
  </div>
</template>

<style scoped>
.settings {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  align-items: flex-start;
}
.settings h1 {
  margin: 0;
}
.muted {
  color: var(--text-muted);
  margin: 0;
}
</style>
