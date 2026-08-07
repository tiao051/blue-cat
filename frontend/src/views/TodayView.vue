<script setup lang="ts">
// Màn Hôm nay — M1: CTA check-in, habit tick list, field để-sau, đổi dayType.
// Lưới icon + task + khối Công việc là chuyện M2 (spec §9.2).
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Select from 'primevue/select'
import Message from 'primevue/message'
import { executeMutation, execute } from '@/api/client'
import {
  SET_DAY_TYPE_MUTATION,
  SET_HABIT_MUTATION,
  SET_METRIC_VALUE_MUTATION,
  TODAY_QUERY,
} from '@/api/operations'
import type {
  DailyEntry,
  DayType,
  DeferredField,
  HabitState,
  MetricDefinition,
  TodayPayload,
} from '@/api/types'
import { formatDisplay, logicalToday } from '@/lib/dates'
import { toInput } from '@/lib/metricValues'
import { useDefinitionsStore } from '@/stores/definitions'
import HabitTickList from '@/components/HabitTickList.vue'
import MetricField from '@/components/metric/MetricField.vue'

const router = useRouter()
const definitions = useDefinitionsStore()

const today = logicalToday()
const entry = ref<DailyEntry | null>(null)
const deferred = ref<DeferredField[]>([])
const deferredDefs = ref<Record<string, MetricDefinition>>({})
const deferredRaw = ref<Record<string, unknown>>({})
const error = ref('')
const loading = ref(true)

const dayTypeOptions: { label: string; value: DayType }[] = [
  { label: 'Ngày làm', value: 'WORKDAY' },
  { label: 'Cuối tuần', value: 'WEEKEND' },
  { label: 'Nghỉ phép', value: 'DAYOFF' },
  { label: 'Ốm', value: 'SICK' },
]

const needsMorning = computed(() => !!entry.value && !entry.value.morningCheckinAt)
const hasEvening = computed(() => !!entry.value?.eveningCheckinAt)

async function refresh() {
  loading.value = true
  error.value = ''
  try {
    const data = await execute<{ today: TodayPayload }>(TODAY_QUERY, { date: today })
    entry.value = data.today.entry
    deferred.value = data.today.deferred

    // Definitions cho field để-sau (render bằng MetricField như mọi field khác)
    if (deferred.value.length > 0) {
      const defs = await definitions.fetchDefinitions()
      deferredDefs.value = Object.fromEntries(defs.map((d) => [d.key, d]))
    }
    await definitions.fetchHabits()
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    loading.value = false
  }
}

async function run(fn: () => Promise<unknown>) {
  error.value = ''
  try {
    await fn()
    await refresh()
  } catch (e) {
    error.value = (e as Error).message
  }
}

const onHabitChange = (habitKey: string, state: HabitState, hours: number | null, quality: number | null) =>
  run(() =>
    executeMutation(SET_HABIT_MUTATION, { date: today, habitKey, state, hours, quality }),
  )

const onDayTypeChange = (dayType: DayType) =>
  run(() => executeMutation(SET_DAY_TYPE_MUTATION, { date: today, dayType }))

const submitDeferred = (field: DeferredField) => {
  const def = deferredDefs.value[field.key]
  if (!def) return
  const input = toInput(def, deferredRaw.value[field.key])
  if (!input) return
  return run(() =>
    executeMutation(SET_METRIC_VALUE_MUTATION, {
      date: field.belongsToDate,
      value: input,
      clientDate: today,
    }),
  )
}

onMounted(refresh)
</script>

<template>
  <div class="today">
    <header class="today-header">
      <div>
        <h1 class="date-title">{{ formatDisplay(today) }}</h1>
        <p class="date-sub">{{ today }}</p>
      </div>
      <Select
        v-if="entry"
        :model-value="entry.dayType"
        :options="dayTypeOptions"
        option-label="label"
        option-value="value"
        size="small"
        @update:model-value="onDayTypeChange"
      />
    </header>

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
    <p v-if="loading" class="muted">Đang tải…</p>

    <template v-if="entry && !loading">
      <!-- CTA check-in -->
      <Button
        v-if="needsMorning"
        label="☀️ Check-in sáng"
        size="large"
        fluid
        @click="router.push('/checkin/morning')"
      />

      <!-- Field để sau: dòng thường, ghi rõ ngày nó thuộc về (spec §9.2) -->
      <section v-if="deferred.length > 0" class="section">
        <h2 class="section-title">Còn thiếu</h2>
        <div v-for="field in deferred" :key="`${field.key}:${field.belongsToDate}`" class="deferred-row">
          <p class="deferred-label">
            {{ field.label }} <span class="muted">— thuộc về {{ field.belongsToDate }}</span>
          </p>
          <div v-if="deferredDefs[field.key]" class="deferred-input">
            <MetricField
              :def="deferredDefs[field.key]!"
              :show-label="false"
              v-model="deferredRaw[field.key]"
            />
            <Button label="Lưu" size="small" @click="submitDeferred(field)" />
          </div>
        </div>
      </section>

      <!-- Habit -->
      <section class="section">
        <h2 class="section-title">Cuộc sống</h2>
        <HabitTickList
          :habits="definitions.habits"
          :entries="entry.habits"
          :disabled="entry.status !== 'OPEN'"
          @change="onHabitChange"
        />
      </section>

      <Button
        :label="hasEvening ? '🌙 Sửa check-in tối' : '🌙 Check-in tối'"
        size="large"
        fluid
        :severity="hasEvening ? 'secondary' : 'primary'"
        :outlined="hasEvening"
        @click="router.push('/checkin/evening')"
      />
    </template>
  </div>
</template>

<style scoped>
.today {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  padding: 1rem;
  max-width: 640px;
  margin: 0 auto;
}
.today-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}
.date-title {
  margin: 0;
  font-size: 1.4rem;
}
.date-sub {
  margin: 0;
  font-size: 0.8rem;
  color: var(--p-text-muted-color);
}
.section {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}
.section-title {
  margin: 0;
  font-size: 1rem;
  color: var(--p-text-muted-color);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}
.deferred-row {
  border: 1px dashed var(--p-surface-300);
  border-radius: 10px;
  padding: 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.deferred-label {
  margin: 0;
  font-weight: 500;
}
.deferred-input {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.muted {
  color: var(--p-text-muted-color);
}
</style>
