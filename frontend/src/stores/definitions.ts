import { ref } from 'vue'
import { defineStore } from 'pinia'
import { execute } from '@/api/client'
import { HABITS_QUERY, METRIC_DEFINITIONS_QUERY } from '@/api/operations'
import type { DayType, Habit, MetricDefinition, Phase } from '@/api/types'

/**
 * Definitions + habits. Metric definitions LUÔN refetch khi mở check-in —
 * để "insert document mới → form tự mọc thêm ô" hoạt động chỉ với một lần mở lại màn (spec §5).
 */
export const useDefinitionsStore = defineStore('definitions', () => {
  const habits = ref<Habit[]>([])

  async function fetchDefinitions(phase?: Phase, dayType?: DayType): Promise<MetricDefinition[]> {
    const data = await execute<{ metricDefinitions: MetricDefinition[] }>(
      METRIC_DEFINITIONS_QUERY,
      { phase: phase ?? null, dayType: dayType ?? null },
    )
    return data.metricDefinitions
  }

  async function fetchHabits(force = false): Promise<Habit[]> {
    if (habits.value.length === 0 || force) {
      const data = await execute<{ habits: Habit[] }>(HABITS_QUERY)
      habits.value = data.habits
    }
    return habits.value
  }

  return { habits, fetchDefinitions, fetchHabits }
})
