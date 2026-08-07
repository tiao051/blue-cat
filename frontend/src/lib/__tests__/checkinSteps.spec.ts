import { describe, expect, it } from 'vitest'
import { deriveEveningSteps, deriveMorningSteps } from '@/lib/checkinSteps'
import type { MetricDefinition } from '@/api/types'

function def(partial: Partial<MetricDefinition> & { key: string }): MetricDefinition {
  return {
    label: partial.key,
    type: 'scale',
    phase: 'MORNING',
    order: 0,
    dayOffset: 0,
    active: true,
    ...partial,
  }
}

const morningSeed = [
  def({ key: 'sleep_start', type: 'time', order: 10 }),
  def({ key: 'sleep_end', type: 'time', order: 20 }),
  def({ key: 'screen_time', type: 'number', order: 30, deferrableDays: 1, dayOffset: -1 }),
  def({ key: 'mood_morning', type: 'scale', order: 40 }),
]

describe('deriveMorningSteps — step mọc từ definitions, không hardcode', () => {
  it('seed chuẩn ra 3 bước: cặp ngủ + screen_time + mood (spec §9.1)', () => {
    const steps = deriveMorningSteps(morningSeed)
    expect(steps).toHaveLength(3)
    expect(steps[0]!.kind).toBe('sleep')
    expect(steps[0]!.defs.map((d) => d.key)).toEqual(['sleep_start', 'sleep_end'])
    expect(steps[1]!.defs[0]!.key).toBe('screen_time')
    expect(steps[2]!.defs[0]!.key).toBe('mood_morning')
  })

  it('insert biến mới → form tự mọc thêm bước (checklist M1 cuối)', () => {
    const steps = deriveMorningSteps([
      ...morningSeed,
      def({ key: 'energy_morning', type: 'scale', order: 45 }),
    ])
    expect(steps).toHaveLength(4)
    expect(steps[3]!.defs[0]!.key).toBe('energy_morning')
  })
})

describe('deriveEveningSteps — thang gộp một màn theo visibleWhen (spec §9.3)', () => {
  const evening = (keys: string[]) => [
    ...keys.map((k, i) => def({ key: k, type: 'scale', phase: 'EVENING', order: (i + 1) * 10 })),
    def({ key: 'attention_main', type: 'multi_enum', phase: 'EVENING', order: 60, maxSelect: 2 }),
    def({
      key: 'note',
      type: 'text',
      phase: 'EVENING',
      order: 70,
      validation: { required: false },
    }),
  ]

  it('ngày làm: 3 thang một màn + chip + note = 3 bước', () => {
    const steps = deriveEveningSteps(evening(['productivity', 'mood_evening', 'physical']))
    expect(steps).toHaveLength(3)
    expect(steps[0]!.defs).toHaveLength(3)
    expect(steps[1]!.defs[0]!.key).toBe('attention_main')
    expect(steps[2]!.defs[0]!.key).toBe('note')
  })

  it('ngày nghỉ: 5 thang một màn (server đã lọc visibleWhen)', () => {
    const steps = deriveEveningSteps(
      evening(['productivity', 'mood_evening', 'physical', 'recovery', 'time_meaningful']),
    )
    expect(steps[0]!.defs).toHaveLength(5)
  })
})
