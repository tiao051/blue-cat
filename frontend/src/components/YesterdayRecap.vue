<script setup lang="ts">
// Nhìn lại hôm qua — read-only: trạng thái sổ, việc đã/chưa làm, habit đã tick.
// Chỉ xem, không sửa (miss là miss — R18).
import { computed } from 'vue'
import TaskList from '@/components/TaskList.vue'
import type { DailyEntry, Habit, Task } from '@/api/types'

const props = defineProps<{
  date: string
  entry: DailyEntry | null
  tasks: Task[]
  habits: Habit[]
}>()

const statusLabel = computed(() => {
  switch (props.entry?.status) {
    case 'CLOSED':
      return { text: 'đủ sổ', tone: 'good' }
    case 'PARTIAL':
      return { text: 'thiếu một phần', tone: 'mid' }
    case 'OPEN':
      return { text: 'chưa đóng sổ', tone: 'mid' }
    default:
      return { text: 'trống', tone: 'faint' }
  }
})

/** "gym ✓ 8đ · đọc 1.5h" — chỉ habit có dữ liệu thật */
const habitSummary = computed(() => {
  if (!props.entry) return []
  const labels = new Map(props.habits.map((h) => [h.key, h.shortLabel]))
  return props.entry.habits
    .filter((h) => h.state !== 'NO_DATA')
    .map((h) => {
      const name = labels.get(h.habitKey) ?? h.habitKey
      if (h.state === 'NOT_DONE') return `${name} ✗`
      const parts = [name, '✓']
      if (h.hours != null) parts.push(`${h.hours}h`)
      if (h.quality != null) parts.push(`${h.quality}đ`)
      return parts.join(' ')
    })
})

const quickRatio = computed(() => {
  const e = props.entry
  if (!e || e.quickPlanned == null) return null
  return `${e.quickDone}/${e.quickPlanned}`
})

const hasContent = computed(
  () => props.tasks.length > 0 || habitSummary.value.length > 0 || props.entry?.status === 'CLOSED' || props.entry?.status === 'PARTIAL',
)
</script>

<template>
  <section class="recap">
    <h2 class="overline rule-title">
      Hôm qua
      <span class="status data" :class="`tone-${statusLabel.tone}`">{{ statusLabel.text }}</span>
      <span v-if="quickRatio" class="ratio data">{{ quickRatio }} việc</span>
    </h2>

    <template v-if="hasContent">
      <p v-if="habitSummary.length > 0" class="habit-line data">
        {{ habitSummary.join(' · ') }}
      </p>
      <TaskList v-if="tasks.length > 0" :tasks="tasks" readonly />
    </template>
    <p v-else class="empty data">không có gì được ghi lại</p>
  </section>
</template>

<style scoped>
.recap {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}
.rule-title {
  margin: 0;
}
/* chip trạng thái đứng trước hairline */
.rule-title .status {
  order: 0;
}
.status {
  font-size: 0.68rem;
  padding: 0.1rem 0.45rem;
  border-radius: 3px;
  border: 1px solid var(--border);
  text-transform: none;
  letter-spacing: 0.02em;
}
.tone-good {
  color: var(--accent);
  border-color: color-mix(in srgb, var(--accent) 45%, transparent);
  background: var(--accent-dim);
}
.tone-mid {
  color: var(--text-muted);
  background: var(--surface-raised);
}
.tone-faint {
  color: var(--text-faint);
}
.ratio {
  font-size: 0.68rem;
  color: var(--text-muted);
  text-transform: none;
}
.habit-line {
  margin: 0;
  font-size: 0.82rem;
  color: var(--text-muted);
  line-height: 1.7;
}
.empty {
  margin: 0;
  font-size: 0.8rem;
  color: var(--text-faint);
}
</style>
