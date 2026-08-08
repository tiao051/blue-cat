<script setup lang="ts">
// A strip of 10 cells, swipe or tap, big number above (spec §5 swipe-scale).
// Instrument: cells are gauge ticks, mono readout, darkening with level — never red/yellow/green.
import { computed, ref } from 'vue'
import type { MetricDefinition } from '@/api/types'

const props = defineProps<{ def: MetricDefinition }>()
const model = defineModel<number | null>({ default: null })

const min = computed(() => props.def.validation?.min ?? 1)
const max = computed(() => props.def.validation?.max ?? 10)
const cells = computed(() => {
  const list: number[] = []
  for (let v = min.value; v <= max.value; v++) list.push(v)
  return list
})

const track = ref<HTMLElement | null>(null)
const dragging = ref(false)

function valueFromPointer(e: PointerEvent): number {
  const el = track.value!
  const rect = el.getBoundingClientRect()
  const ratio = Math.min(Math.max((e.clientX - rect.left) / rect.width, 0), 0.999)
  return min.value + Math.floor(ratio * (max.value - min.value + 1))
}

function onPointerDown(e: PointerEvent) {
  dragging.value = true
  track.value?.setPointerCapture(e.pointerId)
  model.value = valueFromPointer(e)
}

function onPointerMove(e: PointerEvent) {
  if (dragging.value) model.value = valueFromPointer(e)
}

function onPointerUp() {
  dragging.value = false
}
</script>

<template>
  <div class="scale-field">
    <div class="scale-readout data" :class="{ empty: model === null }">
      <span class="value">{{ model ?? '–' }}</span>
      <span class="range">/ {{ max }}</span>
    </div>
    <div
      ref="track"
      class="scale-track"
      @pointerdown="onPointerDown"
      @pointermove="onPointerMove"
      @pointerup="onPointerUp"
      @pointercancel="onPointerUp"
    >
      <div
        v-for="v in cells"
        :key="v"
        class="scale-cell"
        :class="{ active: model !== null && v <= model, current: v === model }"
        :style="{ '--cell-strength': (v - min) / (max - min) }"
      />
    </div>
    <div class="scale-bounds data">
      <span>{{ min }}</span>
      <span>{{ max }}</span>
    </div>
  </div>
</template>

<style scoped>
.scale-field {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  touch-action: pan-y;
}
/* XP-orb readout: green level number with a hard pixel shadow, like the xp counter */
.scale-readout {
  display: flex;
  align-items: baseline;
  justify-content: center;
  gap: 0.4rem;
  min-height: 3.5rem;
}
.scale-readout .value {
  font-size: 3rem;
  font-weight: 700;
  line-height: 1;
  color: var(--accent-bright);
  /* hard pixel shadow + soft bloom, like the xp counter under shaders */
  text-shadow: 3px 3px 0 rgba(0, 0, 0, 0.55), 0 0 18px rgba(124, 220, 60, 0.55);
}
.scale-readout.empty .value {
  color: var(--text-faint);
  text-shadow: none;
}
.scale-readout .range {
  font-size: 0.95rem;
  color: var(--text-faint);
}
/* the XP bar: sunken black-framed track, lime segments */
.scale-track {
  display: flex;
  gap: 3px;
  height: 44px;
  cursor: pointer;
  user-select: none;
  touch-action: none;
  padding: 5px;
  border: 2px solid #1a1a1a;
  background: #4a4a4a;
  box-shadow: var(--bevel-in);
}
.scale-cell {
  flex: 1;
  background: #2f2f2f;
  transition: background 60ms;
}
.scale-cell.active {
  background: var(--accent-bright);
  box-shadow: inset 0 3px 0 rgba(255, 255, 255, 0.35), inset 0 -3px 0 rgba(0, 0, 0, 0.3),
    0 0 8px rgba(108, 198, 39, 0.55); /* bloom */
}
.scale-cell.current {
  outline: 2px solid #ffffa0;
  outline-offset: 0;
}
.scale-bounds {
  display: flex;
  justify-content: space-between;
  color: var(--text-faint);
  font-size: 0.72rem;
}
</style>
