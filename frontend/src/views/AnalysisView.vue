<template>
  <!-- Spec §9.5: mobile only shows a notice; the desktop color grid is M4 -->
  <div class="analysis">
    <h1>Analysis</h1>
    <p class="muted">The month-at-a-glance color grid is designed for desktop — coming in M4.</p>
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
