import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import MultiEnumField from '@/components/metric/MultiEnumField.vue'
import type { MetricDefinition } from '@/api/types'

const def: MetricDefinition = {
  key: 'attention_main',
  label: 'Tâm trí chủ yếu ở đâu',
  type: 'multi_enum',
  phase: 'EVENING',
  order: 60,
  dayOffset: 0,
  maxSelect: 2,
  active: true,
  options: [
    { value: 'work', label: 'Công việc' },
    { value: 'learning', label: 'Học & phát triển' },
    { value: 'phone', label: 'Cày phone, giải trí' },
    { value: 'social', label: 'Xã hội, người khác' },
    { value: 'empty', label: 'Trống rỗng' },
  ],
}

describe('MultiEnumField — chặn cứng maxSelect (checklist M1)', () => {
  it('chọn được tới maxSelect', async () => {
    const wrapper = mount(MultiEnumField, {
      props: { def, modelValue: [] as string[], 'onUpdate:modelValue': (v: string[]) => wrapper.setProps({ modelValue: v }) },
    })

    const chips = wrapper.findAll('button.chip')
    await chips[0]!.trigger('click')
    await chips[1]!.trigger('click')
    expect(wrapper.props('modelValue')).toEqual(['work', 'learning'])
  })

  it('đủ maxSelect thì chip chưa chọn bị disable và click không ăn', async () => {
    const wrapper = mount(MultiEnumField, {
      props: { def, modelValue: ['work', 'learning'] },
    })

    const chips = wrapper.findAll('button.chip')
    // chip thứ 3 (phone) chưa chọn → disabled
    expect(chips[2]!.attributes('disabled')).toBeDefined()

    await chips[2]!.trigger('click')
    // không emit thêm lựa chọn thứ 3
    const emitted = wrapper.emitted('update:modelValue')
    expect(emitted ?? []).toEqual([])
  })

  it('bỏ chọn được khi đang ở limit', async () => {
    const wrapper = mount(MultiEnumField, {
      props: { def, modelValue: ['work', 'learning'] },
    })

    const chips = wrapper.findAll('button.chip')
    await chips[0]!.trigger('click') // bỏ 'work'
    expect(wrapper.emitted('update:modelValue')![0]![0]).toEqual(['learning'])
  })
})
