<script setup lang="ts">
// Dải 10 ô, vuốt ngang hoặc chạm, số lớn phía trên (spec §5 swipe-scale).
// Instrument: ô là vạch đo, giá trị chạy mono, đậm dần theo mức — không đỏ/vàng/xanh.
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
.scale-readout {
  display: flex;
  align-items: baseline;
  justify-content: center;
  gap: 0.4rem;
  min-height: 3.5rem;
}
.scale-readout .value {
  font-size: 3.25rem;
  font-weight: 600;
  line-height: 1;
  color: var(--accent);
}
.scale-readout.empty .value {
  color: var(--text-faint);
}
.scale-readout .range {
  font-size: 1rem;
  color: var(--text-faint);
}
.scale-track {
  display: flex;
  gap: 3px;
  height: 56px;
  cursor: pointer;
  user-select: none;
  touch-action: none;
  padding: 4px;
  border: 1px solid var(--border);
  border-radius: var(--radius);
  background: var(--surface);
}
.scale-cell {
  flex: 1;
  border-radius: 2px;
  background: var(--surface-raised);
  transition: background 60ms;
}
.scale-cell.active {
  background: color-mix(
    in srgb,
    var(--accent) calc(30% + 70% * var(--cell-strength)),
    var(--surface-raised)
  );
}
.scale-cell.current {
  outline: 1.5px solid var(--accent);
  outline-offset: 1px;
}
.scale-bounds {
  display: flex;
  justify-content: space-between;
  color: var(--text-faint);
  font-size: 0.75rem;
}
</style>
