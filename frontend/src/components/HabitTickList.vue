<script setup lang="ts">
// Habit list kiểu todo: checkbox tri-state đầu dòng (nguyên tắc 7 — ô trống = no_data,
// ✓ = done, ✗ = not_done). Click = toggle done; nút ✗ là action phụ, desktop hiện khi
// hover dòng (pattern Todoist), mobile luôn hiện. Giờ 0 ≠ no_data.
import { computed } from 'vue'
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

const qualityOptions = Array.from({ length: 10 }, (_, i) => ({
  label: String(i + 1),
  value: i + 1,
}))

const byKey = computed(() => new Map(props.entries.map((e) => [e.habitKey, e])))

function entryOf(key: string): HabitEntry {
  return byKey.value.get(key) ?? { habitKey: key, state: 'NO_DATA', hours: null, quality: null }
}

function apply(habit: Habit, state: HabitState, hoursOverride?: number | null) {
  const cur = entryOf(habit.key)
  // no_data không được kèm giờ (backend enforce); rời done thì quality rơi
  const hours =
    state === 'NO_DATA' ? null : hoursOverride !== undefined ? hoursOverride : (cur.hours ?? null)
  const quality = state === 'DONE' ? (cur.quality ?? null) : null
  emit('change', habit.key, state, hours, quality)
}

/** Click checkbox: toggle done ↔ trống (như todo list). */
function toggleDone(habit: Habit) {
  const cur = entryOf(habit.key)
  apply(habit, cur.state === 'DONE' ? 'NO_DATA' : 'DONE')
}

/** Nút ✗: toggle not_done ↔ trống. */
function toggleNotDone(habit: Habit) {
  const cur = entryOf(habit.key)
  apply(habit, cur.state === 'NOT_DONE' ? 'NO_DATA' : 'NOT_DONE')
}

function setHours(habit: Habit, hours: number | null) {
  const cur = entryOf(habit.key)
  const state: HabitState = cur.state === 'NO_DATA' ? 'DONE' : cur.state
  apply(habit, state, hours)
}

function setQuality(habit: Habit, quality: string | number | null) {
  const cur = entryOf(habit.key)
  emit('change', habit.key, cur.state, cur.hours ?? null, quality === null ? null : Number(quality))
}
</script>

<template>
  <ul class="habit-list">
    <li
      v-for="habit in habits"
      :key="habit.key"
      class="habit-row"
      :class="{ done: entryOf(habit.key).state === 'DONE', 'not-done': entryOf(habit.key).state === 'NOT_DONE' }"
    >
      <!-- Checkbox tri-state: trống / ✓ / ✗ -->
      <button
        type="button"
        class="checkbox"
        :disabled="disabled"
        :aria-label="`${habit.label}: ${entryOf(habit.key).state === 'DONE' ? 'done' : entryOf(habit.key).state === 'NOT_DONE' ? 'không làm' : 'chưa có dữ liệu'}`"
        @click="toggleDone(habit)"
      >
        <span class="box">
          <svg v-if="entryOf(habit.key).state === 'DONE'" viewBox="0 0 16 16" fill="none"
            stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M3 8.5l3.5 3.5L13 4.5" />
          </svg>
          <svg v-else-if="entryOf(habit.key).state === 'NOT_DONE'" viewBox="0 0 16 16" fill="none"
            stroke="currentColor" stroke-width="2" stroke-linecap="round">
            <path d="M4.5 4.5l7 7 M11.5 4.5l-7 7" />
          </svg>
        </span>
      </button>

      <span class="habit-label">{{ habit.label }}</span>

      <!-- Metadata inline (desktop) / xuống dòng (mobile) -->
      <div class="habit-meta">
        <template v-if="habit.measure === 'duration' && entryOf(habit.key).state !== 'NO_DATA'">
          <span class="meta-label">giờ</span>
          <UiStepper
            :model-value="entryOf(habit.key).hours ?? null"
            :step="0.5"
            :min="0"
            :max="24"
            :disabled="disabled"
            @update:model-value="(v) => setHours(habit, v)"
          />
        </template>
        <template v-if="habit.hasQuality && entryOf(habit.key).state === 'DONE'">
          <span class="meta-label" :title="habit.qualityLabel ?? 'Chất lượng'">chất lượng</span>
          <UiSelect
            :model-value="entryOf(habit.key).quality ?? null"
            :options="qualityOptions"
            placeholder="1–10"
            :disabled="disabled"
            @update:model-value="(v) => setQuality(habit, v)"
          />
        </template>
      </div>

      <!-- Action phụ: đánh dấu "không làm" — desktop hiện khi hover dòng -->
      <button
        type="button"
        class="mark-not-done data"
        :class="{ engaged: entryOf(habit.key).state === 'NOT_DONE' }"
        :disabled="disabled"
        :title="entryOf(habit.key).state === 'NOT_DONE' ? 'Bỏ đánh dấu không làm' : 'Đánh dấu không làm'"
        @click="toggleNotDone(habit)"
      >
        ✗
      </button>
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
  display: grid;
  grid-template-columns: auto 1fr auto;
  grid-template-areas:
    'check label x'
    '. meta meta';
  align-items: center;
  column-gap: 0.25rem;
  padding: 0.35rem 0.5rem 0.35rem 0.25rem;
  transition: background 100ms;
}
.habit-row + .habit-row {
  border-top: 1px solid var(--border);
}
.habit-row:hover {
  background: var(--surface-raised);
}

/* Checkbox: vùng chạm 44px, ô 22px bo góc kiểu Things */
.checkbox {
  grid-area: check;
  width: var(--tap);
  height: var(--tap);
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  background: none;
  cursor: pointer;
  padding: 0;
}
.checkbox:disabled {
  cursor: not-allowed;
  opacity: 0.45;
}
.box {
  width: 22px;
  height: 22px;
  border: 1.5px solid var(--border-strong);
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--surface);
  transition: border-color 100ms, background 100ms, transform 100ms;
}
.box svg {
  width: 14px;
  height: 14px;
}
.checkbox:not(:disabled):hover .box {
  border-color: var(--accent);
}
.checkbox:not(:disabled):active .box {
  transform: scale(0.9);
}
.done .box {
  background: var(--accent);
  border-color: var(--accent);
  color: var(--on-accent);
}
/* animation vẽ nét tick — micro-interaction kiểu Things */
.done .box svg path {
  stroke-dasharray: 20;
  stroke-dashoffset: 0;
  animation: draw-check 220ms ease-out;
}
@keyframes draw-check {
  from {
    stroke-dashoffset: 20;
  }
}
.not-done .box {
  color: var(--text-faint);
  border-color: var(--border-strong);
  background: var(--surface-raised);
}

.habit-label {
  grid-area: label;
  font-weight: 500;
  font-size: 0.95rem;
  min-width: 0;
  transition: color 100ms;
}
/* Quy ước todo: dòng đã xử lý dịu đi một bậc */
.done .habit-label {
  color: var(--text-muted);
}
.not-done .habit-label {
  color: var(--text-faint);
}

.habit-meta {
  grid-area: meta;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.habit-meta:empty {
  display: none;
}
.meta-label {
  font-size: 0.78rem;
  color: var(--text-muted);
}
.meta-label:not(:first-child) {
  margin-left: 0.5rem;
}

/* Nút ✗ — mobile luôn hiện */
.mark-not-done {
  grid-area: x;
  width: 34px;
  height: 34px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid transparent;
  border-radius: var(--radius);
  background: none;
  color: var(--text-faint);
  font-size: 0.85rem;
  cursor: pointer;
  transition: color 100ms, border-color 100ms, opacity 100ms;
}
.mark-not-done:not(:disabled):hover {
  color: var(--text);
  border-color: var(--border-strong);
}
.mark-not-done.engaged {
  color: var(--text-muted);
  border-color: var(--border-strong);
  background: var(--surface-raised);
}
.mark-not-done:disabled {
  cursor: not-allowed;
  opacity: 0.3;
}

/* Desktop: một dòng đơn — meta inline phải, ✗ chỉ hiện khi hover (pattern Todoist) */
@media (min-width: 900px) {
  .habit-row {
    grid-template-columns: auto 1fr auto auto;
    grid-template-areas: 'check label meta x';
    column-gap: 0.5rem;
    padding-right: 0.4rem;
  }
  .habit-meta {
    justify-content: flex-end;
    flex-wrap: nowrap;
  }
  .mark-not-done {
    opacity: 0;
  }
  .habit-row:hover .mark-not-done,
  .mark-not-done.engaged,
  .mark-not-done:focus-visible {
    opacity: 1;
  }
}
</style>
