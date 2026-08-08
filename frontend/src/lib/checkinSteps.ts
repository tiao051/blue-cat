import type { MetricDefinition } from '@/api/types'
import { METRIC_KEYS } from '@/lib/metricKeys'

/**
 * One wizard step. Steps are derived from definitions, NOT hardcoded —
 * inserting a new metric into the DB makes the form grow a step (spec §5, M1 checklist).
 */
export interface CheckinStep {
  /** 'sleep' = the sleep_start + sleep_end pair on one screen with the computed total (the only deliberate exception) */
  kind: 'sleep' | 'fields'
  defs: MetricDefinition[]
}

/**
 * Morning: the sleep pair becomes one step, every remaining metric gets its own step
 * (one question per screen — §9.1).
 */
export function deriveMorningSteps(defs: MetricDefinition[]): CheckinStep[] {
  const sorted = [...defs].sort((a, b) => a.order - b.order)
  const sleepKeys: string[] = [METRIC_KEYS.sleepStart, METRIC_KEYS.sleepEnd]
  const sleepDefs = sorted.filter((d) => sleepKeys.includes(d.key))
  const rest = sorted.filter((d) => !sleepKeys.includes(d.key))

  const steps: CheckinStep[] = []
  if (sleepDefs.length === 2) steps.push({ kind: 'sleep', defs: sleepDefs })
  else if (sleepDefs.length === 1) steps.push({ kind: 'fields', defs: sleepDefs })
  for (const def of rest) steps.push({ kind: 'fields', defs: [def] })
  return steps
}

/**
 * Evening: all scales grouped on one screen (3 workday / 5 day-off / 4 sick — falls out of
 * visibleWhen automatically, §9.3), then each non-scale metric gets its own step.
 */
export function deriveEveningSteps(defs: MetricDefinition[]): CheckinStep[] {
  const sorted = [...defs].sort((a, b) => a.order - b.order)
  const scales = sorted.filter((d) => d.type === 'scale')
  const rest = sorted.filter((d) => d.type !== 'scale')

  const steps: CheckinStep[] = []
  if (scales.length > 0) steps.push({ kind: 'fields', defs: scales })
  for (const def of rest) steps.push({ kind: 'fields', defs: [def] })
  return steps
}
