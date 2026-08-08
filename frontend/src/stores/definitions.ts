import { ref } from 'vue'
import { defineStore } from 'pinia'
import { execute } from '@/api/client'
import { HABITS_QUERY, METRIC_DEFINITIONS_QUERY } from '@/api/operations'
import type { DayType, Habit, MetricDefinition, Phase } from '@/api/types'

/**
 * Definitions + habits. Metric definitions are ALWAYS refetched when a check-in opens —
 * so "insert a new document → the form grows a field" works with a single screen reopen (spec §5).
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
