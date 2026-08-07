<script setup lang="ts">
// M1: danh sách tick habit "xấu mà chạy" (spec §11 — lưới icon đẹp là chuyện M2).
// Đủ 3 trạng thái (nguyên tắc 7), giờ 0 nhập được và khác no_data, quality khi hợp lệ.
import { computed } from 'vue'
import SelectButton from 'primevue/selectbutton'
import InputNumber from 'primevue/inputnumber'
import Select from 'primevue/select'
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
  { label: '✓', value: 'DONE' },
  { label: '✗', value: 'NOT_DONE' },
  { label: '—', value: 'NO_DATA' },
]

const qualityOptions = Array.from({ length: 10 }, (_, i) => i + 1)

const byKey = computed(() => new Map(props.entries.map((e) => [e.habitKey, e])))

function entryOf(key: string): HabitEntry {
  return byKey.value.get(key) ?? { habitKey: key, state: 'NO_DATA', hours: null, quality: null }
}

function setState(habit: Habit, state: HabitState) {
  const cur = entryOf(habit.key)
  // no_data xoá giờ; đổi khỏi done xoá quality (spec §6)
  const hours = state === 'NO_DATA' ? null : (cur.hours ?? null)
  const quality = state === 'DONE' ? (cur.quality ?? null) : null
  emit('change', habit.key, state, hours, quality)
}

function setHours(habit: Habit, hours: number | null) {
  const cur = entryOf(habit.key)
  // nhập giờ khi đang no_data thì tự chuyển sang done (0 giờ → not_done hợp lý hơn nhưng
  // không đoán — người dùng chỉnh state bằng nút, đây chỉ là tiện tay)
  const state: HabitState = cur.state === 'NO_DATA' ? 'DONE' : cur.state
  emit('change', habit.key, state, hours, state === 'DONE' ? (cur.quality ?? null) : null)
}

function setQuality(habit: Habit, quality: number | null) {
  const cur = entryOf(habit.key)
  emit('change', habit.key, cur.state, cur.hours ?? null, quality)
}
</script>

<template>
  <ul class="habit-list">
    <li v-for="habit in habits" :key="habit.key" class="habit-row">
      <div class="habit-main">
        <span class="habit-label">{{ habit.label }}</span>
        <SelectButton
          :model-value="entryOf(habit.key).state"
          :options="stateOptions"
          option-label="label"
          option-value="value"
          :allow-empty="false"
          :disabled="disabled"
          @update:model-value="(v: HabitState) => setState(habit, v)"
        />
      </div>

      <div
        v-if="habit.measure === 'duration' && entryOf(habit.key).state !== 'NO_DATA'"
        class="habit-extra"
      >
        <label class="extra-label">Số giờ</label>
        <InputNumber
          :model-value="entryOf(habit.key).hours ?? null"
          show-buttons
          button-layout="horizontal"
          :step="0.5"
          :min="0"
          :max="24"
          :min-fraction-digits="0"
          :max-fraction-digits="1"
          :disabled="disabled"
          :input-style="{ width: '4.5rem', textAlign: 'center' }"
          @update:model-value="(v: number | null) => setHours(habit, v)"
        >
          <template #incrementicon>+</template>
          <template #decrementicon>−</template>
        </InputNumber>
      </div>

      <div v-if="habit.hasQuality && entryOf(habit.key).state === 'DONE'" class="habit-extra">
        <label class="extra-label">{{ habit.qualityLabel ?? 'Chất lượng' }}</label>
        <Select
          :model-value="entryOf(habit.key).quality ?? null"
          :options="qualityOptions"
          placeholder="1–10"
          :disabled="disabled"
          @update:model-value="(v: number | null) => setQuality(habit, v)"
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
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.habit-row {
  padding: 0.75rem;
  border: 1px solid var(--p-surface-200);
  border-radius: 10px;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}
.habit-main {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}
.habit-label {
  font-weight: 500;
  flex: 1;
}
.habit-extra {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}
.extra-label {
  font-size: 0.85rem;
  color: var(--p-text-muted-color);
}
@media (prefers-color-scheme: dark) {
  .habit-row {
    border-color: var(--p-surface-700);
  }
}
</style>
