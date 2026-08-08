<script setup lang="ts">
// Bảng đo habit: mỗi hàng một kênh, trạng thái là segmented ✓/✗/—,
// giá trị chạy mono. Đủ 3 trạng thái (nguyên tắc 7), giờ 0 ≠ no_data.
import { computed } from 'vue'
import UiSegmented from '@/components/ui/UiSegmented.vue'
import UiStepper from '@/components/ui/UiStepper.vue'
import UiSelect from '@/components/ui/UiSelect.vue'
import type { Habit, HabitEntry, HabitState } from '@/api/types'

const props = defineProps<{
  habits: Habit[]
  entries: HabitEntry[]
  disabled?: boolean
}>()

const emit = defineEmits<{
  change: [habitKey: string, state: HabitState, hours: number | null, quality: number | null]
}>()

const stateOptions = [
  { label: '✓', value: 'DONE', title: 'Done' },
  { label: '✗', value: 'NOT_DONE', title: 'Không làm' },
  { label: '—', value: 'NO_DATA', title: 'Không có dữ liệu' },
]

const qualityOptions = Array.from({ length: 10 }, (_, i) => ({
  label: String(i + 1),
  value: i + 1,
}))

const byKey = computed(() => new Map(props.entries.map((e) => [e.habitKey, e])))

function entryOf(key: string): HabitEntry {
  return byKey.value.get(key) ?? { habitKey: key, state: 'NO_DATA', hours: null, quality: null }
}

function setState(habit: Habit, state: HabitState) {
  const cur = entryOf(habit.key)
  const hours = state === 'NO_DATA' ? null : (cur.hours ?? null)
  const quality = state === 'DONE' ? (cur.quality ?? null) : null
  emit('change', habit.key, state, hours, quality)
}

function setHours(habit: Habit, hours: number | null) {
  const cur = entryOf(habit.key)
  const state: HabitState = cur.state === 'NO_DATA' ? 'DONE' : cur.state
  emit('change', habit.key, state, hours, state === 'DONE' ? (cur.quality ?? null) : null)
}

function setQuality(habit: Habit, quality: string | number | null) {
  const cur = entryOf(habit.key)
  emit('change', habit.key, cur.state, cur.hours ?? null, quality === null ? null : Number(quality))
}
</script>

<template>
  <ul class="habit-list">
    <li v-for="habit in habits" :key="habit.key" class="habit-row">
      <div class="habit-main">
        <div class="habit-id">
          <span class="habit-label">{{ habit.label }}</span>
          <span v-if="habit.measure === 'duration'" class="habit-unit data">giờ</span>
        </div>
        <UiSegmented
          :model-value="entryOf(habit.key).state"
          :options="stateOptions"
          mono
          size="sm"
          :disabled="disabled"
          @update:model-value="(v) => setState(habit, v as HabitState)"
        />
      </div>

      <div
        v-if="habit.measure === 'duration' && entryOf(habit.key).state !== 'NO_DATA'"
        class="habit-extra"
      >
        <span class="extra-label">Số giờ</span>
        <UiStepper
          :model-value="entryOf(habit.key).hours ?? null"
          :step="0.5"
          :min="0"
          :max="24"
          :disabled="disabled"
          @update:model-value="(v) => setHours(habit, v)"
        />
      </div>

      <div v-if="habit.hasQuality && entryOf(habit.key).state === 'DONE'" class="habit-extra">
        <span class="extra-label">{{ habit.qualityLabel ?? 'Chất lượng' }}</span>
        <UiSelect
          :model-value="entryOf(habit.key).quality ?? null"
          :options="qualityOptions"
          placeholder="1–10"
          :disabled="disabled"
          @update:model-value="(v) => setQuality(habit, v)"
        />
      </div>
    </li>
  </ul>
</template>

<style scoped>
.habit-list {
  list-style: none;
  margin: 0;
  padding: 0;
  border: 1px solid var(--border);
  border-radius: var(--radius);
  background: var(--surface);
  overflow: hidden;
}
.habit-row {
  padding: 0.7rem 0.9rem;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}
.habit-row + .habit-row {
  border-top: 1px solid var(--border);
}
.habit-main {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}
.habit-id {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  flex: 1;
  min-width: 0;
}
.habit-label {
  font-weight: 500;
  font-size: 0.95rem;
}
.habit-unit {
  font-size: 0.7rem;
  color: var(--text-faint);
}
.habit-extra {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding-left: 0.1rem;
}
.extra-label {
  font-size: 0.83rem;
  color: var(--text-muted);
}
</style>
