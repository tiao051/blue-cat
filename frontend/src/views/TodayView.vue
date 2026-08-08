<script setup lang="ts">
// Màn Hôm nay — M1: CTA check-in, bảng habit, field để-sau, đổi dayType.
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import UiButton from '@/components/ui/UiButton.vue'
import UiSelect from '@/components/ui/UiSelect.vue'
import UiMessage from '@/components/ui/UiMessage.vue'
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
  { label: 'ngày làm', value: 'WORKDAY' },
  { label: 'cuối tuần', value: 'WEEKEND' },
  { label: 'nghỉ phép', value: 'DAYOFF' },
  { label: 'ốm', value: 'SICK' },
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

const onDayTypeChange = (dayType: string | number | null) =>
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
      <div class="date-block">
        <p class="overline">{{ today }}</p>
        <h1 class="date-title">{{ formatDisplay(today) }}</h1>
      </div>
      <UiSelect
        v-if="entry"
        :model-value="entry.dayType"
        :options="dayTypeOptions"
        @update:model-value="onDayTypeChange"
      />
    </header>

    <UiMessage v-if="error" severity="error">{{ error }}</UiMessage>
    <p v-if="loading" class="muted data">đang tải…</p>

    <template v-if="entry && !loading">
      <UiButton
        v-if="needsMorning"
        label="Check-in sáng →"
        size="lg"
        block
        @click="router.push('/checkin/morning')"
      />

      <!-- Field để sau: ghi rõ ngày nó thuộc về (spec §9.2) -->
      <section v-if="deferred.length > 0" class="section">
        <h2 class="overline section-title">Còn thiếu</h2>
        <div
          v-for="field in deferred"
          :key="`${field.key}:${field.belongsToDate}`"
          class="deferred-row"
        >
          <p class="deferred-label">
            {{ field.label }}
            <span class="deferred-date data">{{ field.belongsToDate }}</span>
          </p>
          <div v-if="deferredDefs[field.key]" class="deferred-input">
            <MetricField
              :def="deferredDefs[field.key]!"
              :show-label="false"
              v-model="deferredRaw[field.key]"
            />
            <UiButton label="Lưu" size="sm" @click="submitDeferred(field)" />
          </div>
        </div>
      </section>

      <section class="section">
        <h2 class="overline section-title">Cuộc sống</h2>
        <HabitTickList
          :habits="definitions.habits"
          :entries="entry.habits"
          :disabled="entry.status !== 'OPEN'"
          @change="onHabitChange"
        />
      </section>

      <UiButton
        :label="hasEvening ? 'Sửa check-in tối' : 'Check-in tối →'"
        size="lg"
        block
        :variant="hasEvening ? 'ghost' : 'primary'"
        @click="router.push('/checkin/evening')"
      />
    </template>
  </div>
</template>

<style scoped>
.today {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}
.today-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1rem;
}
.date-block {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}
.date-title {
  margin: 0;
}
.overline {
  margin: 0;
}
.section {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}
.section-title {
  margin: 0;
}
.deferred-row {
  border: 1px dashed var(--border-strong);
  border-radius: var(--radius);
  padding: 0.75rem 0.9rem;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
  background: var(--surface);
}
.deferred-label {
  margin: 0;
  font-weight: 500;
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.5rem;
}
.deferred-date {
  font-size: 0.75rem;
  color: var(--text-faint);
}
.deferred-input {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.muted {
  color: var(--text-muted);
  font-size: 0.85rem;
}
</style>
