<script setup lang="ts">
// Việc vụn kiểu todo list: checkbox binary, thêm việc bằng dòng input cuối,
// bỏ việc bằng nút ✗ (desktop hiện khi hover — cùng ngôn ngữ HabitTickList).
import { ref } from 'vue'
import type { Task } from '@/api/types'

const props = withDefaults(
  defineProps<{
    tasks: Task[]
    /** cho phép thêm việc mới (ngày chưa qua) */
    addable?: boolean
    /** read-only toàn bộ (ngày đã đóng) */
    readonly?: boolean
    addPlaceholder?: string
  }>(),
  { addable: false, readonly: false, addPlaceholder: 'Thêm việc…' },
)

const emit = defineEmits<{
  add: [title: string]
  toggle: [id: string, done: boolean]
  drop: [id: string]
}>()

const draft = ref('')

function submit() {
  const title = draft.value.trim()
  if (title.length === 0) return
  emit('add', title)
  draft.value = ''
}
</script>

<template>
  <div class="task-list" :class="{ readonly }">
    <ul v-if="tasks.length > 0" class="rows">
      <li v-for="task in tasks" :key="task.id" class="task-row" :class="{ done: task.status === 'done' }">
        <button
          type="button"
          class="checkbox"
          :disabled="readonly"
          :aria-label="`${task.title}: ${task.status === 'done' ? 'xong' : 'chưa xong'}`"
          @click="emit('toggle', task.id, task.status !== 'done')"
        >
          <span class="box">
            <svg v-if="task.status === 'done'" viewBox="0 0 16 16" fill="none"
              stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M3 8.5l3.5 3.5L13 4.5" />
            </svg>
          </span>
        </button>
        <span class="task-title">{{ task.title }}</span>
        <button
          v-if="!readonly"
          type="button"
          class="drop-btn data"
          title="Bỏ việc này"
          @click="emit('drop', task.id)"
        >
          ✗
        </button>
      </li>
    </ul>

    <p v-else-if="!addable" class="empty data">không có việc nào</p>

    <div v-if="addable && !readonly" class="add-row">
      <span class="add-plus data">+</span>
      <input
        v-model="draft"
        class="add-input"
        type="text"
        :placeholder="addPlaceholder"
        @keyup.enter="submit"
        @blur="submit"
      />
    </div>
  </div>
</template>

<style scoped>
.task-list {
  border: 1px solid var(--border);
  border-radius: var(--radius);
  background: var(--surface);
  overflow: hidden;
}
.rows {
  list-style: none;
  margin: 0;
  padding: 0;
}
.task-row {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.15rem 0.5rem 0.15rem 0.25rem;
  transition: background 100ms;
}
.task-row + .task-row {
  border-top: 1px solid var(--border);
}
.task-row:hover {
  background: var(--surface-raised);
}

.checkbox {
  width: var(--tap);
  height: var(--tap);
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  background: none;
  cursor: pointer;
  padding: 0;
  flex-shrink: 0;
}
.checkbox:disabled {
  cursor: default;
}
.box {
  width: 20px;
  height: 20px;
  border: 1.5px solid var(--border-strong);
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--surface);
  transition: border-color 100ms, background 100ms, transform 100ms;
}
.box svg {
  width: 13px;
  height: 13px;
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

.task-title {
  flex: 1;
  min-width: 0;
  font-size: 0.95rem;
  transition: color 100ms;
}
/* quy ước todo: việc xong gạch nhẹ + dịu màu */
.done .task-title {
  color: var(--text-faint);
  text-decoration: line-through;
  text-decoration-color: var(--border-strong);
}

.drop-btn {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid transparent;
  border-radius: var(--radius);
  background: none;
  color: var(--text-faint);
  font-size: 0.8rem;
  cursor: pointer;
  transition: color 100ms, border-color 100ms, opacity 100ms;
}
.drop-btn:hover {
  color: var(--text);
  border-color: var(--border-strong);
}
@media (min-width: 900px) {
  .drop-btn {
    opacity: 0;
  }
  .task-row:hover .drop-btn,
  .drop-btn:focus-visible {
    opacity: 1;
  }
}

.empty {
  margin: 0;
  padding: 0.7rem 0.9rem;
  color: var(--text-faint);
  font-size: 0.8rem;
}

.add-row {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.15rem 0.5rem 0.15rem 0.25rem;
}
.rows + .add-row {
  border-top: 1px solid var(--border);
}
.add-plus {
  width: var(--tap);
  height: var(--tap);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-faint);
  font-size: 1.1rem;
}
.add-input {
  flex: 1;
  border: none;
  background: none;
  color: var(--text);
  font-family: var(--font-ui);
  font-size: 0.95rem;
  min-height: var(--tap);
}
.add-input::placeholder {
  color: var(--text-faint);
}
.add-input:focus {
  outline: none;
}
</style>
