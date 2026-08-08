<script setup lang="ts">
// Segmented control — state encoded in form, not in color noise
export interface SegmentOption {
  label: string
  value: string
  title?: string
}

withDefaults(
  defineProps<{
    options: SegmentOption[]
    disabled?: boolean
    mono?: boolean
    size?: 'sm' | 'md'
  }>(),
  { disabled: false, mono: false, size: 'md' },
)

const model = defineModel<string | null>({ default: null })
</script>

<template>
  <div class="segmented" :class="[`s-${size}`]" role="radiogroup">
    <button
      v-for="opt in options"
      :key="opt.value"
      type="button"
      class="segment"
      :class="{ active: model === opt.value, mono }"
      :disabled="disabled"
      :title="opt.title"
      role="radio"
      :aria-checked="model === opt.value"
      @click="model = opt.value"
    >
      {{ opt.label }}
    </button>
  </div>
</template>

<style scoped>
.segmented {
  display: inline-flex;
  border: 2px solid #1a1a1a;
  overflow: hidden;
  background: #8b8b8b;
  box-shadow: var(--bevel-in);
}
.segment {
  border: none;
  background: transparent;
  color: #efefef;
  text-shadow: var(--px-text-shadow);
  cursor: pointer;
  font-size: 0.85rem;
  transition: background 60ms, color 60ms;
}
.segment + .segment {
  border-left: 1px solid #373737;
}
.segment.mono {
  font-family: var(--font-data);
}
.s-md .segment {
  min-height: 40px;
  min-width: 44px;
  padding: 0 0.9rem;
}
.s-sm .segment {
  min-height: 32px;
  min-width: 38px;
  padding: 0 0.6rem;
  font-size: 0.8rem;
}
.segment:not(:disabled):hover {
  color: #ffffa0;
}
.segment.active {
  background: var(--accent-bright);
  color: #173300;
  text-shadow: none;
  box-shadow: var(--bevel-out);
}
.segment:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
