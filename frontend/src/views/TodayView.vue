<script setup lang="ts">
// Màn Hôm nay — ba vùng thời gian: Hôm qua (nhìn lại) · Hôm nay (làm) · Ngày mai (plan trước).
// R3: sáng lên plan · trong ngày tick dần · tối review + lên plan cho mai.
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import UiButton from '@/components/ui/UiButton.vue'
import UiSelect from '@/components/ui/UiSelect.vue'
import UiMessage from '@/components/ui/UiMessage.vue'
import { executeMutation, execute } from '@/api/client'
import {
  ADD_TASK_MUTATION,
  DAILY_ENTRY_QUERY,
  DROP_TASK_MUTATION,
  SET_DAY_TYPE_MUTATION,
  SET_HABIT_MUTATION,
  SET_METRIC_VALUE_MUTATION,
  SET_TASK_DONE_MUTATION,
  TASKS_QUERY,
  TODAY_QUERY,
} from '@/api/operations'
import type {
  DailyEntry,
  DayType,
  DeferredField,
  HabitState,
  MetricDefinition,
  Task,
  TodayPayload,
} from '@/api/types'
import { addDays, formatDisplay, isoWeekCode, logicalToday } from '@/lib/dates'
import { toInput } from '@/lib/metricValues'
import { useDefinitionsStore } from '@/stores/definitions'
import HabitTickList from '@/components/HabitTickList.vue'
import TaskList from '@/components/TaskList.vue'
import YesterdayRecap from '@/components/YesterdayRecap.vue'
import MetricField from '@/components/metric/MetricField.vue'

const router = useRouter()
const definitions = useDefinitionsStore()

const today = logicalToday()
const yesterday = addDays(today, -1)
const tomorrow = addDays(today, 1)

const entry = ref<DailyEntry | null>(null)
const yesterdayEntry = ref<DailyEntry | null>(null)
const deferred = ref<DeferredField[]>([])
const tasks = ref<Task[]>([])
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

// v1 chỉ hiện việc cá nhân — việc công ty tách riêng ở M2 (R26)
const personal = (list: Task[]) => list.filter((t) => t.category === 'personal')
const yesterdayTasks = computed(() => personal(tasks.value).filter((t) => t.plannedDate === yesterday))
const todayTasks = computed(() => personal(tasks.value).filter((t) => t.plannedDate === today))
const tomorrowTasks = computed(() => personal(tasks.value).filter((t) => t.plannedDate === tomorrow))

const quickRatio = computed(() => {
  const e = entry.value
  if (!e || e.quickPlanned == null) return null
  return `${e.quickDone}/${e.quickPlanned}`
})

async function refresh() {
  loading.value = true
  error.value = ''
  try {
    const [todayData, yData, taskData] = await Promise.all([
      execute<{ today: TodayPayload }>(TODAY_QUERY, { date: today }),
      execute<{ dailyEntry: DailyEntry }>(DAILY_ENTRY_QUERY, { date: yesterday, clientDate: today }),
      execute<{ tasks: Task[] }>(TASKS_QUERY, { from: yesterday, to: tomorrow }),
    ])
    entry.value = todayData.today.entry
    deferred.value = todayData.today.deferred
    yesterdayEntry.value = yData.dailyEntry
    tasks.value = taskData.tasks

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

const addTask = (plannedDate: string) => (title: string) =>
  run(() => executeMutation(ADD_TASK_MUTATION, { title, plannedDate, clientDate: today }))

const toggleTask = (id: string, done: boolean) =>
  run(() => executeMutation(SET_TASK_DONE_MUTATION, { id, done, clientDate: today }))

const dropTask = (id: string) =>
  run(() => executeMutation(DROP_TASK_MUTATION, { id, clientDate: today }))

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
        <p class="overline">{{ today }} · {{ isoWeekCode(today).split('-')[1] }}</p>
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
    <p v-if="loading" class="muted data blink-cursor">đang tải</p>

    <template v-if="entry && !loading">
      <!-- HÔM QUA — nhìn lại, không sửa được -->
      <YesterdayRecap
        :date="yesterday"
        :entry="yesterdayEntry"
        :tasks="yesterdayTasks"
        :habits="definitions.habits"
      />

      <button v-if="needsMorning" type="button" class="cta cta-primary" @click="router.push('/checkin/morning')">
        <span class="cta-tag data">sáng</span>
        <span class="cta-label">Check-in sáng</span>
        <span class="cta-arrow" aria-hidden="true">→</span>
      </button>

      <!-- Field để sau: ghi rõ ngày nó thuộc về (spec §9.2) -->
      <section v-if="deferred.length > 0" class="section">
        <h2 class="overline section-title rule-title">Còn thiếu</h2>
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

      <!-- HÔM NAY — việc + thói quen -->
      <section class="section">
        <h2 class="overline section-title rule-title">
          Việc hôm nay
          <span v-if="quickRatio" class="ratio data">{{ quickRatio }}</span>
        </h2>
        <TaskList
          :tasks="todayTasks"
          addable
          :readonly="entry.status !== 'OPEN'"
          add-placeholder="Thêm việc cho hôm nay…"
          @add="addTask(today)($event)"
          @toggle="toggleTask"
          @drop="dropTask"
        />
      </section>

      <section class="section">
        <h2 class="overline section-title rule-title">Thói quen</h2>
        <HabitTickList
          :habits="definitions.habits"
          :entries="entry.habits"
          :disabled="entry.status !== 'OPEN'"
          @change="onHabitChange"
        />
      </section>

      <!-- NGÀY MAI — plan trước (R3: tối lên plan cho mai) -->
      <section class="section">
        <h2 class="overline section-title rule-title">Ngày mai <span class="ratio data">{{ tomorrow }}</span></h2>
        <TaskList
          :tasks="tomorrowTasks"
          addable
          add-placeholder="Plan việc cho ngày mai…"
          @add="addTask(tomorrow)($event)"
          @toggle="toggleTask"
          @drop="dropTask"
        />
      </section>

      <button
        type="button"
        class="cta"
        :class="hasEvening ? 'cta-ghost' : 'cta-primary'"
        @click="router.push('/checkin/evening')"
      >
        <span class="cta-tag data">tối</span>
        <span class="cta-label">{{ hasEvening ? 'Sửa check-in tối' : 'Check-in tối' }}</span>
        <span class="cta-arrow" aria-hidden="true">→</span>
      </button>
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
  gap: 0.2rem;
}
.date-title {
  margin: 0;
  font-size: 1.55rem;
  letter-spacing: -0.015em;
}
.overline {
  margin: 0;
  color: var(--accent);
}
.section {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}
.section-title {
  margin: 0;
}
.ratio {
  font-size: 0.68rem;
  color: var(--text-muted);
  text-transform: none;
}

/* CTA check-in: panel row — tag mono, mũi tên trượt khi hover */
.cta {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-height: 52px;
  padding: 0 1rem;
  border-radius: var(--radius);
  border: 1px solid transparent;
  font-family: var(--font-ui);
  font-size: 0.98rem;
  font-weight: 500;
  cursor: pointer;
  transition: background 120ms, border-color 120ms;
}
.cta-tag {
  font-size: 0.68rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  padding: 0.15rem 0.45rem;
  border-radius: 3px;
}
.cta-label {
  flex: 1;
  text-align: left;
}
.cta-arrow {
  font-family: var(--font-data);
  transition: transform 140ms ease-out;
}
.cta:hover .cta-arrow {
  transform: translateX(4px);
}
.cta-primary {
  background: var(--accent);
  color: var(--on-accent);
}
.cta-primary:hover {
  background: var(--accent-strong);
}
.cta-primary .cta-tag {
  background: color-mix(in srgb, var(--on-accent) 18%, transparent);
}
.cta-ghost {
  background: var(--surface);
  border-color: var(--border-strong);
  color: var(--text);
}
.cta-ghost:hover {
  border-color: var(--accent);
}
.cta-ghost .cta-tag {
  background: var(--surface-raised);
  color: var(--text-muted);
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
