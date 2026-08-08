<script setup lang="ts">
// v1: a single read-only year-goal line (R12). The full Week tier arrives in M3.
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
    /* secondary screen — not worth surfacing an error */
  }
})
</script>

<template>
  <div class="plan">
    <h1>Plan</h1>
    <div v-if="goal" class="year-goal">
      <span class="overline">year goal</span>
      <span class="goal-title">{{ goal.title }}</span>
    </div>
    <p class="muted data">week tier — M3</p>
  </div>
</template>

<style scoped>
.plan {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.plan h1 {
  margin: 0;
}
.year-goal {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  padding: 0.9rem 1rem;
  border: 1px solid var(--border);
  border-left: 3px solid var(--accent);
  border-radius: var(--radius);
  background: var(--surface);
}
.goal-title {
  font-size: 1.1rem;
  font-weight: 600;
}
.muted {
  color: var(--text-faint);
  font-size: 0.8rem;
}
</style>
