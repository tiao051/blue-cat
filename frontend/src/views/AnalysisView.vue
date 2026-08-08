<template>
  <!-- Spec §9.5: mobile chỉ hiện thông báo; lưới màu desktop là M4 -->
  <div class="analysis">
    <h1>Phân tích</h1>
    <p class="muted">Lưới màu cả tháng — thiết kế cho máy tính, sẽ có ở M4.</p>
  </div>
</template>

<style scoped>
.analysis {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.analysis h1 {
  margin: 0;
}
.muted {
  color: var(--text-muted);
}
</style>
