<script setup lang="ts">
// Todo-style habit list: tri-state checkbox leading each row (principle 7 — empty = no_data,
// ✓ = done, ✗ = not_done). Click toggles done; the ✗ button is the secondary action, revealed
// on row hover on desktop (Todoist pattern), always visible on mobile. Hours 0 ≠ no_data.
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
  // no_data can't carry hours (backend enforces); leaving done drops quality
  const hours =
    state === 'NO_DATA' ? null : hoursOverride !== undefined ? hoursOverride : (cur.hours ?? null)
  const quality = state === 'DONE' ? (cur.quality ?? null) : null
  emit('change', habit.key, state, hours, quality)
}

/** Checkbox click: toggle done ↔ empty (like a todo list). */
function toggleDone(habit: Habit) {
  const cur = entryOf(habit.key)
  apply(habit, cur.state === 'DONE' ? 'NO_DATA' : 'DONE')
}

/** The ✗ button: toggle not_done ↔ empty. */
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
      <!-- Tri-state checkbox: empty / ✓ / ✗ -->
      <button
        type="button"
        class="checkbox"
        :disabled="disabled"
        :aria-label="`${habit.label}: ${entryOf(habit.key).state === 'DONE' ? 'done' : entryOf(habit.key).state === 'NOT_DONE' ? 'not done' : 'no data'}`"
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

      <!-- Metadata inline (desktop) / wrapped below (mobile) -->
      <div class="habit-meta">
        <template v-if="habit.measure === 'duration' && entryOf(habit.key).state !== 'NO_DATA'">
          <span class="meta-label">hours</span>
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
          <span class="meta-label" :title="habit.qualityLabel ?? 'Quality'">quality</span>
          <UiSelect
            :model-value="entryOf(habit.key).quality ?? null"
            :options="qualityOptions"
            placeholder="1–10"
            :disabled="disabled"
            @update:model-value="(v) => setQuality(habit, v)"
          />
        </template>
      </div>

      <!-- Secondary action: mark "not done" — revealed on row hover on desktop -->
      <button
        type="button"
        class="mark-not-done data"
        :class="{ engaged: entryOf(habit.key).state === 'NOT_DONE' }"
        :disabled="disabled"
        :title="entryOf(habit.key).state === 'NOT_DONE' ? 'Unmark not done' : 'Mark as not done'"
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

/* Checkbox: 44px hit area, 22px Things-style rounded box */
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
/* the checkbox is an item slot: sunken gray; done = a lime block placed in it */
.box {
  width: 24px;
  height: 24px;
  border: 1px solid #373737;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #8b8b8b;
  box-shadow: var(--bevel-in);
  transition: background 60ms;
}
.box svg {
  width: 14px;
  height: 14px;
}
.checkbox:not(:disabled):hover .box {
  background: #9d9d9d;
}
.checkbox:not(:disabled):active .box {
  background: #777;
}
.done .box {
  background: var(--accent-bright);
  color: #173300;
  box-shadow: var(--bevel-out);
}
/* check-drawing animation — Things-style micro-interaction */
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
  color: #e8e8e8;
  background: #5e5e5e;
}

.habit-label {
  grid-area: label;
  font-weight: 500;
  font-size: 0.95rem;
  min-width: 0;
  transition: color 100ms;
}
/* Todo convention: handled rows step back one level */
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

/* The ✗ button — always visible on mobile */
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

/* Desktop: single-line rows — meta inline right, ✗ only on hover (Todoist pattern) */
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
