<script setup lang="ts">
// Check-in wizard: one step per screen, bar progress, thumb-optimized (spec §9.1).
// Full-screen on mobile; a centered panel on desktop. Steps derive from definitions.
import { computed, reactive, ref } from 'vue'
import UiButton from '@/components/ui/UiButton.vue'
import type { CheckinStep } from '@/lib/checkinSteps'
import type { MetricValueInput } from '@/api/types'
import { isAnswered, toInput } from '@/lib/metricValues'
import { sleepHours } from '@/lib/dates'
import { METRIC_KEYS } from '@/lib/metricKeys'
import MetricField from '@/components/metric/MetricField.vue'

const props = defineProps<{
  steps: CheckinStep[]
  title: string
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
  <div class="wizard-backdrop">
    <div class="wizard">
      <header class="wizard-header">
        <button class="close" type="button" aria-label="Close" @click="emit('cancel')">×</button>
        <div class="progress">
          <div
            v-for="(s, i) in steps"
            :key="i"
            class="bar"
            :class="{ done: i < current, active: i === current }"
          />
        </div>
        <span class="title overline">{{ title }}</span>
        <span class="step-counter data">{{ String(current + 1).padStart(2, '0') }}/{{ String(steps.length).padStart(2, '0') }}</span>
      </header>

      <main class="wizard-body">
        <!-- The sleep pair: two time pickers + auto-computed total (spec §9.1) -->
        <template v-if="step.kind === 'sleep'">
          <div v-for="def in step.defs" :key="def.key" class="field-block">
            <MetricField :def="def" v-model="raw[def.key]" />
          </div>
          <p class="sleep-total data" :class="{ empty: sleepTotal === null }">
            {{ sleepTotal !== null ? `= ${sleepTotal} hours` : 'pick both times' }}
          </p>
        </template>

        <template v-else>
          <div v-for="def in step.defs" :key="def.key" class="field-block">
            <MetricField :def="def" v-model="raw[def.key]" />
            <div class="field-actions">
              <UiButton
                v-if="def.deferrableDays != null && !isAnswered(def, raw[def.key])"
                label="Later"
                variant="subtle"
                size="sm"
                @click="defer(def.key)"
              />
              <UiButton
                v-if="def.validation?.required === false && step.defs.length === 1"
                label="Skip"
                variant="subtle"
                size="sm"
                @click="skip(def.key)"
              />
            </div>
          </div>
        </template>
      </main>

      <footer class="wizard-footer">
        <UiButton v-if="current > 0" label="Back" variant="ghost" size="lg" @click="back" />
        <UiButton
          :label="isLast ? 'Done' : 'Continue'"
          :disabled="!canAdvance"
          :loading="submitting"
          size="lg"
          class="advance"
          @click="next"
        />
      </footer>
    </div>
  </div>
</template>

<style scoped>
.wizard-backdrop {
  position: fixed;
  inset: 0;
  z-index: 100;
  background: var(--bg);
  display: flex;
  justify-content: center;
}
.wizard {
  display: flex;
  flex-direction: column;
  width: 100%;
  background: var(--bg);
}

/* Desktop: a framed centered panel — no full-width stretched buttons */
@media (min-width: 900px) {
  .wizard-backdrop {
    align-items: center;
    background: color-mix(in srgb, var(--bg) 80%, transparent);
    backdrop-filter: blur(2px);
  }
  .wizard {
    max-width: 520px;
    max-height: min(720px, 90vh);
    border: 1px solid var(--border);
    border-radius: 10px;
    overflow: hidden;
    background: var(--surface);
  }
}

.wizard-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--border);
}
.close {
  border: none;
  background: none;
  font-size: 1.6rem;
  line-height: 1;
  color: var(--text-muted);
  cursor: pointer;
  padding: 0.25rem 0.5rem;
}
.close:hover {
  color: var(--text);
}
.progress {
  flex: 1;
  display: flex;
  gap: 5px;
}
.bar {
  flex: 1;
  height: 3px;
  border-radius: 1.5px;
  background: var(--surface-raised);
}
.bar.done {
  background: var(--accent);
  opacity: 0.45;
}
.bar.active {
  background: var(--accent);
}
.title {
  white-space: nowrap;
}
.step-counter {
  font-size: 0.7rem;
  color: var(--accent);
  white-space: nowrap;
}
.wizard-body {
  flex: 1;
  overflow-y: auto;
  padding: 1.75rem 1.25rem;
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
  font-size: 1.2rem;
  font-weight: 600;
  color: var(--accent);
  margin: 0;
}
.sleep-total.empty {
  color: var(--text-faint);
  font-weight: 400;
  font-size: 0.9rem;
}
.wizard-footer {
  display: flex;
  gap: 0.75rem;
  padding: 1rem 1.25rem calc(1rem + env(safe-area-inset-bottom));
  border-top: 1px solid var(--border);
}
.advance {
  flex: 1;
}
</style>
