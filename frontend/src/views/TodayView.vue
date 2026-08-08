<script setup lang="ts">
// The Today screen — three time zones: Yesterday (look back) · Today (do) · Tomorrow (plan ahead).
// R3: plan in the morning · tick through the day · review at night + plan for tomorrow.
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
  { label: 'workday', value: 'WORKDAY' },
  { label: 'weekend', value: 'WEEKEND' },
  { label: 'day off', value: 'DAYOFF' },
  { label: 'sick', value: 'SICK' },
]

const needsMorning = computed(() => !!entry.value && !entry.value.morningCheckinAt)
const hasEvening = computed(() => !!entry.value?.eveningCheckinAt)

// v1 shows personal tasks only — company work gets its own block in M2 (R26)
const personal = (list: Task[]) => list.filter((t) => t.category === 'personal')
const yesterdayTasks = computed(() => personal(tasks.value).filter((t) => t.plannedDate === yesterday))
const todayTasks = computed(() => personal(tasks.value).filter((t) => t.plannedDate === today))
const tomorrowTasks = computed(() => personal(tasks.value).filter((t) => t.plannedDate === tomorrow))

const quickRatio = computed(() => {
  const e = entry.value
  if (!e || e.quickPlanned == null) return null
  return `${e.quickDone}/${e.quickPlanned}`
})

/**
 * Fetches everything. `silent` skips the loading flag so the template never
 * unmounts — mutations must NEVER flash the whole screen (principle 3 vibes).
 */
async function refresh(silent = false) {
  if (!silent) loading.value = true
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
    if (!silent) loading.value = false
  }
}

/** Runs a mutation and refreshes silently in the background — no flicker. */
async function run(fn: () => Promise<unknown>) {
  error.value = ''
  try {
    await fn()
    await refresh(true)
  } catch (e) {
    error.value = (e as Error).message
  }
}

// Habit ticks and dayType changes return the updated entry — apply it directly,
// zero refetch, zero flicker
const onHabitChange = async (habitKey: string, state: HabitState, hours: number | null, quality: number | null) => {
  error.value = ''
  try {
    const data = await executeMutation<{ setHabit: DailyEntry }>(SET_HABIT_MUTATION, {
      date: today, habitKey, state, hours, quality,
    })
    entry.value = data.setHabit
  } catch (e) {
    error.value = (e as Error).message
  }
}

const onDayTypeChange = async (dayType: string | number | null) => {
  error.value = ''
  try {
    const data = await executeMutation<{ setDayType: DailyEntry }>(SET_DAY_TYPE_MUTATION, {
      date: today, dayType,
    })
    entry.value = data.setDayType
  } catch (e) {
    error.value = (e as Error).message
  }
}

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

onMounted(() => refresh())
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
    <p v-if="loading" class="muted data blink-cursor">loading</p>

    <template v-if="entry && !loading">
      <!-- YESTERDAY — look back, never editable -->
      <YesterdayRecap
        :date="yesterday"
        :entry="yesterdayEntry"
        :tasks="yesterdayTasks"
        :habits="definitions.habits"
      />

      <button v-if="needsMorning" type="button" class="cta cta-primary" @click="router.push('/checkin/morning')">
        <span class="cta-tag data">morning</span>
        <span class="cta-label">Morning check-in</span>
        <span class="cta-arrow" aria-hidden="true">→</span>
      </button>

      <!-- Deferred fields: labeled with their owning date (spec §9.2) -->
      <section v-if="deferred.length > 0" class="section">
        <h2 class="overline section-title rule-title">Still missing</h2>
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
            <UiButton label="Save" size="sm" @click="submitDeferred(field)" />
          </div>
        </div>
      </section>

      <!-- TODAY — tasks + habits -->
      <section class="section">
        <h2 class="overline section-title rule-title">
          Today's tasks
          <span v-if="quickRatio" class="ratio data">{{ quickRatio }}</span>
        </h2>
        <TaskList
          :tasks="todayTasks"
          addable
          :readonly="entry.status !== 'OPEN'"
          add-placeholder="Add a task for today…"
          @add="addTask(today)($event)"
          @toggle="toggleTask"
          @drop="dropTask"
        />
      </section>

      <section class="section">
        <h2 class="overline section-title rule-title">Habits</h2>
        <HabitTickList
          :habits="definitions.habits"
          :entries="entry.habits"
          :disabled="entry.status !== 'OPEN'"
          @change="onHabitChange"
        />
      </section>

      <!-- TOMORROW — plan ahead (R3: plan for tomorrow at night) -->
      <section class="section">
        <h2 class="overline section-title rule-title">Tomorrow <span class="ratio data">{{ tomorrow }}</span></h2>
        <TaskList
          :tasks="tomorrowTasks"
          addable
          add-placeholder="Plan something for tomorrow…"
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
        <span class="cta-tag data">evening</span>
        <span class="cta-label">{{ hasEvening ? 'Edit evening check-in' : 'Evening check-in' }}</span>
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
  gap: 0.5rem 1rem;
  flex-wrap: wrap; /* narrow screens: the dayType select drops below instead of squeezing the title */
}
.date-block {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  min-width: 0;
}
.date-title {
  margin: 0;
  font-size: clamp(1rem, 4.2vw, 1.4rem); /* pixel font is wide — scale with the viewport */
  font-weight: 700;
  white-space: nowrap;
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

/* Check-in CTA: a proper Minecraft button — beveled slab, pixel shadow, yellow hover text */
.cta {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-height: 52px;
  padding: 0 1rem;
  border: 2px solid #1a1a1a;
  box-shadow: var(--bevel-out);
  font-family: var(--font-ui);
  font-size: 0.95rem;
  color: #fff;
  text-shadow: var(--px-text-shadow);
  cursor: pointer;
  transition: filter 60ms;
  position: relative;
  overflow: hidden;
}
/* enchantment glint: a purple-white shimmer sweeping across, like an enchanted item */
.cta-primary::after {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(
    115deg,
    transparent 30%,
    rgba(216, 180, 255, 0.22) 44%,
    rgba(255, 255, 255, 0.3) 50%,
    rgba(216, 180, 255, 0.22) 56%,
    transparent 70%
  );
  transform: translateX(-130%);
  animation: glint 4.5s ease-in-out infinite;
  pointer-events: none;
}
@keyframes glint {
  0% { transform: translateX(-130%); }
  40%, 100% { transform: translateX(130%); }
}
.cta:hover {
  filter: brightness(1.12);
  color: #ffffa0;
}
.cta:active {
  box-shadow: var(--bevel-in);
}
.cta-tag {
  font-size: 0.62rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  padding: 0.15rem 0.45rem;
  background: rgba(0, 0, 0, 0.3);
}
.cta-label {
  flex: 1;
  text-align: left;
}
.cta-arrow {
  font-family: var(--font-data);
  transition: transform 140ms steps(3);
}
.cta:hover .cta-arrow {
  transform: translateX(4px);
}
.cta-primary {
  background: #578a2c;
}
.cta-ghost {
  background: #6f6f6f;
}

.deferred-row {
  border: 2px dashed #555;
  padding: 0.75rem 0.9rem;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
  background: var(--surface-raised);
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
