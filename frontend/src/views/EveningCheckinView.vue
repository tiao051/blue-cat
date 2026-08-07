<script setup lang="ts">
// Check-in tối 3 bước (spec §9.3): thang gộp một màn theo visibleWhen(dayType), chip attention, note.
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { execute, executeMutation } from '@/api/client'
import { EVENING_CHECKIN_MUTATION, TODAY_QUERY } from '@/api/operations'
import type { MetricValueInput, TodayPayload } from '@/api/types'
import { deriveEveningSteps, type CheckinStep } from '@/lib/checkinSteps'
import { logicalToday } from '@/lib/dates'
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
    // dayType của hôm nay quyết định bộ thang (ngày nghỉ +2, ngày ốm +1 — spec v3.2)
    const todayData = await execute<{ today: TodayPayload }>(TODAY_QUERY, { date: today })
    const entry = todayData.today.entry

    const defs = await definitions.fetchDefinitions('EVENING', entry.dayType)
    steps.value = deriveEveningSteps(defs)

    // Sửa trong ngày: điền lại giá trị đã có (spec v3.2 — ngày chưa đóng sửa được)
    const init: Record<string, unknown> = {}
    for (const def of defs) {
      const raw = toRaw(def, entry.values.find((v) => v.key === def.key))
      if (raw !== null && raw !== undefined && !(Array.isArray(raw) && raw.length === 0))
        init[def.key] = raw
    }
    initial.value = init
  } catch (e) {
    error.value = (e as Error).message
  }
})

async function onFinish(values: MetricValueInput[], _deferredKeys: string[]) {
  submitting.value = true
  error.value = ''
  try {
    await executeMutation(EVENING_CHECKIN_MUTATION, { date: today, values })
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
    title="Check-in tối"
    :initial="initial"
    :submitting="submitting"
    @finish="onFinish"
    @cancel="router.push('/')"
  />
</template>
