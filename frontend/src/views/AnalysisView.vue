<template>
  <!-- Spec §9.5: mobile chỉ hiện thông báo; lưới màu desktop là M4 -->
  <div class="analysis">
    <h1>Phân tích</h1>
    <p class="muted">Màn này thiết kế cho máy tính — lưới màu cả tháng sẽ có ở M4.</p>
  </div>
</template>

<style scoped>
.analysis {
  padding: 1rem;
  max-width: 640px;
  margin: 0 auto;
}
.muted {
  color: var(--p-text-muted-color);
}
</style>
