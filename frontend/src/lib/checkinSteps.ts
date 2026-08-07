import type { MetricDefinition } from '@/api/types'
import { METRIC_KEYS } from '@/lib/metricKeys'

/**
 * Một bước của wizard check-in. Step derive từ definitions, KHÔNG hardcode —
 * insert biến mới vào DB là form tự mọc thêm bước (spec §5, checklist M1).
 */
export interface CheckinStep {
  /** 'sleep' = cặp sleep_start + sleep_end gộp màn kèm tổng giờ (ngoại lệ có chủ đích duy nhất) */
  kind: 'sleep' | 'fields'
  defs: MetricDefinition[]
}

/**
 * Sáng: cặp ngủ thành 1 bước, mỗi biến còn lại 1 bước riêng (một câu hỏi một màn — §9.1).
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
 * Tối: mọi thang gộp 1 màn (3 ngày làm / 5 ngày nghỉ / 4 ngày ốm — tự đúng theo visibleWhen §9.3),
 * sau đó mỗi biến không-phải-thang 1 bước.
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
