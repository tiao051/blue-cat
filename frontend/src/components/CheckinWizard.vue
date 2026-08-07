<script setup lang="ts">
// Wizard full-screen: một bước một màn, thanh tiến trình vạch, tối ưu ngón cái (spec §9.1).
// Steps derive từ definitions (lib/checkinSteps) — không hardcode câu hỏi nào.
import { computed, reactive, ref } from 'vue'
import Button from 'primevue/button'
import type { CheckinStep } from '@/lib/checkinSteps'
import type { MetricValueInput } from '@/api/types'
import { isAnswered, toInput } from '@/lib/metricValues'
import { sleepHours } from '@/lib/dates'
import { METRIC_KEYS } from '@/lib/metricKeys'
import MetricField from '@/components/metric/MetricField.vue'

const props = defineProps<{
  steps: CheckinStep[]
  title: string
  /** giá trị khởi tạo (sửa lại trong ngày / default lần trước cho time) */
  initial?: Record<string, unknown>
  submitting?: boolean
}>()

const emit = defineEmits<{
  finish: [values: MetricValueInput[], deferredKeys: string[]]
  cancel: []
}>()

const current = ref(0)
const raw = reactive<Record<string, unknown>>({ ...(props.initial ?? {}) })
const deferred = reactive(new Set<string>())

const step = computed<CheckinStep>(
  () => props.steps[current.value] ?? { kind: 'fields', defs: [] },
)
const isLast = computed(() => current.value === props.steps.length - 1)

const sleepTotal = computed(() => {
  const s = raw[METRIC_KEYS.sleepStart]
  const e = raw[METRIC_KEYS.sleepEnd]
  if (typeof s !== 'string' || typeof e !== 'string') return null
  return sleepHours(s, e)
})

/** Bước hiện tại đi tiếp được chưa: field bắt buộc phải có giá trị hoặc đã "để sau". */
const canAdvance = computed(() =>
  step.value.defs.every((def) => {
    if (deferred.has(def.key)) return true
    if (def.validation?.required === false) return true
    return isAnswered(def, raw[def.key])
  }),
)

function defer(key: string) {
  deferred.add(key)
  delete raw[key]
  next()
}

function skip(key: string) {
  delete raw[key]
  next()
}

function next() {
  if (isLast.value) return finish()
  current.value++
}

function back() {
  if (current.value > 0) current.value--
}

function finish() {
  const values: MetricValueInput[] = []
  for (const s of props.steps) {
    for (const def of s.defs) {
      if (deferred.has(def.key)) continue
      const input = toInput(def, raw[def.key])
      if (input) values.push(input)
    }
  }
  emit('finish', values, [...deferred])
}
</script>

<template>
  <div class="wizard">
    <header class="wizard-header">
      <button class="close" type="button" aria-label="Đóng" @click="emit('cancel')">×</button>
      <div class="progress">
        <div
          v-for="(s, i) in steps"
          :key="i"
          class="bar"
          :class="{ done: i < current, active: i === current }"
        />
      </div>
      <span class="title">{{ title }}</span>
    </header>

    <main class="wizard-body">
      <!-- Cặp ngủ: hai time picker + tổng số giờ tính tự động (spec §9.1) -->
      <template v-if="step.kind === 'sleep'">
        <div v-for="def in step.defs" :key="def.key" class="field-block">
          <MetricField :def="def" v-model="raw[def.key]" />
        </div>
        <p class="sleep-total" :class="{ empty: sleepTotal === null }">
          {{ sleepTotal !== null ? `Ngủ ${sleepTotal} tiếng` : 'Chọn đủ hai mốc' }}
        </p>
      </template>

      <template v-else>
        <div v-for="def in step.defs" :key="def.key" class="field-block">
          <MetricField :def="def" v-model="raw[def.key]" />
          <div class="field-actions">
            <Button
              v-if="def.deferrableDays != null && !isAnswered(def, raw[def.key])"
              label="Để sau"
              severity="secondary"
              text
              @click="defer(def.key)"
            />
            <Button
              v-if="def.validation?.required === false && step.defs.length === 1"
              label="Bỏ qua"
              severity="secondary"
              text
              @click="skip(def.key)"
            />
          </div>
        </div>
      </template>
    </main>

    <footer class="wizard-footer">
      <Button v-if="current > 0" label="Quay lại" severity="secondary" outlined @click="back" />
      <Button
        :label="isLast ? 'Xong' : 'Tiếp tục'"
        :disabled="!canAdvance || submitting"
        :loading="submitting"
        class="advance"
        @click="next"
      />
    </footer>
  </div>
</template>

<style scoped>
.wizard {
  position: fixed;
  inset: 0;
  display: flex;
  flex-direction: column;
  background: var(--p-surface-0);
  z-index: 100;
}
.wizard-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
}
.close {
  border: none;
  background: none;
  font-size: 1.75rem;
  line-height: 1;
  color: var(--p-text-muted-color);
  cursor: pointer;
  padding: 0.25rem 0.5rem;
}
.progress {
  flex: 1;
  display: flex;
  gap: 6px;
}
.bar {
  flex: 1;
  height: 4px;
  border-radius: 2px;
  background: var(--p-surface-200);
}
.bar.done {
  background: var(--p-primary-color);
  opacity: 0.5;
}
.bar.active {
  background: var(--p-primary-color);
}
.title {
  font-size: 0.85rem;
  color: var(--p-text-muted-color);
  white-space: nowrap;
}
.wizard-body {
  flex: 1;
  overflow-y: auto;
  padding: 1.5rem 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 1.75rem;
}
.field-block {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.field-actions {
  display: flex;
  justify-content: flex-end;
}
.sleep-total {
  text-align: center;
  font-size: 1.15rem;
  font-weight: 600;
  color: var(--p-primary-color);
  margin: 0;
}
.sleep-total.empty {
  color: var(--p-text-muted-color);
  font-weight: 400;
}
.wizard-footer {
  display: flex;
  gap: 0.75rem;
  padding: 1rem 1.25rem calc(1rem + env(safe-area-inset-bottom));
}
.advance {
  flex: 1;
  min-height: 48px; /* ngón cái */
}
@media (prefers-color-scheme: dark) {
  .wizard {
    background: var(--p-surface-950);
  }
  .bar {
    background: var(--p-surface-800);
  }
}
</style>
