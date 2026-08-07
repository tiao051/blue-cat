<script setup lang="ts">
// Dải 10 ô màu, vuốt ngang hoặc chạm, số lớn phía trên (spec §5 swipe-scale).
// Tự viết — không có widget PrimeVue tương đương. Ô ~24px là điểm treo ở spec §12,
// nên hỗ trợ cả drag để chọn chính xác.
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
    <div class="scale-value" :class="{ empty: model === null }">
      {{ model ?? '–' }}
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
    <div class="scale-bounds">
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
.scale-value {
  font-size: 3.5rem;
  font-weight: 700;
  text-align: center;
  line-height: 1;
  min-height: 3.5rem;
  color: var(--p-primary-color);
}
.scale-value.empty {
  color: var(--p-text-muted-color);
}
.scale-track {
  display: flex;
  gap: 4px;
  height: 56px;
  cursor: pointer;
  user-select: none;
  touch-action: none;
}
.scale-cell {
  flex: 1;
  border-radius: 6px;
  background: var(--p-surface-200);
  transition: background 80ms;
}
.scale-cell.active {
  background: color-mix(
    in srgb,
    var(--p-primary-color) calc(35% + 65% * var(--cell-strength)),
    var(--p-surface-100)
  );
}
.scale-cell.current {
  outline: 2px solid var(--p-primary-color);
  outline-offset: 1px;
}
.scale-bounds {
  display: flex;
  justify-content: space-between;
  color: var(--p-text-muted-color);
  font-size: 0.8rem;
}
@media (prefers-color-scheme: dark) {
  .scale-cell {
    background: var(--p-surface-800);
  }
}
</style>
