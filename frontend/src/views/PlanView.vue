<script setup lang="ts">
// v1: một dòng mục tiêu năm read-only (R12). Tầng Tuần đầy đủ là M3.
import { onMounted, ref } from 'vue'
import { execute } from '@/api/client'
import { YEAR_GOAL_QUERY } from '@/api/operations'
import type { Goal } from '@/api/types'

const goal = ref<Goal | null>(null)

onMounted(async () => {
  try {
    const data = await execute<{ yearGoal: Goal | null }>(YEAR_GOAL_QUERY)
    goal.value = data.yearGoal
  } catch {
    /* chưa có key hoặc backend chưa chạy — màn này không quan trọng tới mức báo lỗi */
  }
})
</script>

<template>
  <div class="plan">
    <h1>Kế hoạch</h1>
    <p v-if="goal" class="year-goal">🎯 {{ goal.title }}</p>
    <p class="muted">Chỉ tiêu tuần + backlog sẽ có ở M3.</p>
  </div>
</template>

<style scoped>
.plan {
  padding: 1rem;
  max-width: 640px;
  margin: 0 auto;
}
.year-goal {
  font-size: 1.15rem;
  font-weight: 600;
  padding: 0.75rem 1rem;
  border: 1px solid var(--p-surface-200);
  border-radius: 10px;
}
.muted {
  color: var(--p-text-muted-color);
}
</style>
