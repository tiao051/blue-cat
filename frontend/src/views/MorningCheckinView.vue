<script setup lang="ts">
// Check-in sáng 3 bước (spec §9.1) — step derive từ definitions, refetch mỗi lần mở (spec §5).
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { execute, executeMutation } from '@/api/client'
import { MORNING_CHECKIN_MUTATION, TODAY_QUERY } from '@/api/operations'
import type { MetricValueInput, TodayPayload } from '@/api/types'
import { deriveMorningSteps, type CheckinStep } from '@/lib/checkinSteps'
import { logicalToday, addDays } from '@/lib/dates'
import { toRaw } from '@/lib/metricValues'
import { useDefinitionsStore } from '@/stores/definitions'
import CheckinWizard from '@/components/CheckinWizard.vue'
import Message from 'primevue/message'

const router = useRouter()
const definitions = useDefinitionsStore()

const today = logicalToday()
const steps = ref<CheckinStep[] | null>(null)
const initial = ref<Record<string, unknown>>({})
const error = ref('')
const submitting = ref(false)

onMounted(async () => {
  try {
    const defs = await definitions.fetchDefinitions('MORNING')
    steps.value = deriveMorningSteps(defs)

    // Giá trị khởi tạo: đã nhập hôm nay (sửa trong ngày) — với time thì lấy
    // giá trị lần trước làm default (spec §5): giờ ngủ hôm qua đỡ phải chọn lại từ 00:00
    const [todayData, yesterdayData] = await Promise.all([
      execute<{ today: TodayPayload }>(TODAY_QUERY, { date: today }),
      execute<{ today: TodayPayload }>(TODAY_QUERY, { date: addDays(today, -1) }),
    ])
    const init: Record<string, unknown> = {}
    for (const def of defs) {
      const targetEntry = def.dayOffset === -1 ? yesterdayData.today.entry : todayData.today.entry
      const existing = targetEntry.values.find((v) => v.key === def.key)
      let raw = toRaw(def, existing)
      if (def.type === 'time' && (raw === null || raw === undefined)) {
        // nhớ lần trước: lấy từ hôm qua
        raw = toRaw(def, yesterdayData.today.entry.values.find((v) => v.key === def.key))
      }
      if (raw !== null && raw !== undefined) init[def.key] = raw
    }
    initial.value = init
  } catch (e) {
    error.value = (e as Error).message
  }
})

async function onFinish(values: MetricValueInput[], deferredKeys: string[]) {
  submitting.value = true
  error.value = ''
  try {
    await executeMutation(MORNING_CHECKIN_MUTATION, { date: today, values, deferredKeys })
    router.push('/')
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <Message v-if="error" severity="error" :closable="false" style="margin: 1rem">
    {{ error }}
  </Message>
  <CheckinWizard
    v-if="steps"
    :steps="steps"
    title="Check-in sáng"
    :initial="initial"
    :submitting="submitting"
    @finish="onFinish"
    @cancel="router.push('/')"
  />
</template>
