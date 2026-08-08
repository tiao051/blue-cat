<script setup lang="ts">
// Segmented control — trạng thái encode bằng hình khối, không màu mè
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
  border: 1px solid var(--border-strong);
  border-radius: var(--radius);
  overflow: hidden;
  background: var(--surface);
}
.segment {
  border: none;
  background: transparent;
  color: var(--text-muted);
  cursor: pointer;
  font-size: 0.9rem;
  transition: background 100ms, color 100ms;
}
.segment + .segment {
  border-left: 1px solid var(--border);
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
  color: var(--text);
}
.segment.active {
  background: var(--accent);
  color: var(--on-accent);
}
.segment:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
</style>
