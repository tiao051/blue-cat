import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import MultiEnumField from '@/components/metric/MultiEnumField.vue'
import type { MetricDefinition } from '@/api/types'

const def: MetricDefinition = {
  key: 'attention_main',
  label: 'Where was your mind today?',
  type: 'multi_enum',
  phase: 'EVENING',
  order: 60,
  dayOffset: 0,
  maxSelect: 2,
  active: true,
  options: [
    { value: 'work', label: 'Work' },
    { value: 'learning', label: 'Learning & growth' },
    { value: 'phone', label: 'Phone & entertainment' },
    { value: 'social', label: 'Social & other people' },
    { value: 'empty', label: 'Empty' },
  ],
}

describe('MultiEnumField — hard maxSelect block (M1 checklist)', () => {
  it('allows selecting up to maxSelect', async () => {
    const wrapper = mount(MultiEnumField, {
      props: { def, modelValue: [] as string[], 'onUpdate:modelValue': (v: string[]) => wrapper.setProps({ modelValue: v }) },
    })

    const chips = wrapper.findAll('button.chip')
    await chips[0]!.trigger('click')
    await chips[1]!.trigger('click')
    expect(wrapper.props('modelValue')).toEqual(['work', 'learning'])
  })

  it('disables unselected chips at the limit and ignores their clicks', async () => {
    const wrapper = mount(MultiEnumField, {
      props: { def, modelValue: ['work', 'learning'] },
    })

    const chips = wrapper.findAll('button.chip')
    // third chip (phone) is unselected → disabled
    expect(chips[2]!.attributes('disabled')).toBeDefined()

    await chips[2]!.trigger('click')
    // no third selection is emitted
    const emitted = wrapper.emitted('update:modelValue')
    expect(emitted ?? []).toEqual([])
  })

  it('still allows deselecting while at the limit', async () => {
    const wrapper = mount(MultiEnumField, {
      props: { def, modelValue: ['work', 'learning'] },
    })

    const chips = wrapper.findAll('button.chip')
    await chips[0]!.trigger('click') // deselect 'work'
    expect(wrapper.emitted('update:modelValue')![0]![0]).toEqual(['learning'])
  })
})
