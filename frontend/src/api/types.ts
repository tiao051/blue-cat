// Hand-written types for M1 (codegen later — per plan). Mirrors the backend SDL.

export type Phase = 'MORNING' | 'EVENING' | 'ANYTIME'
export type DayType = 'WORKDAY' | 'WEEKEND' | 'DAYOFF' | 'SICK'
export type DayStatus = 'OPEN' | 'CLOSED' | 'PARTIAL' | 'MISSED'
export type HabitState = 'DONE' | 'NOT_DONE' | 'NO_DATA'
export type MetricType = 'scale' | 'number' | 'time' | 'enum' | 'multi_enum' | 'text'

export interface VisibleWhen {
  field: string
  values: string[]
}

export interface Validation {
  min?: number | null
  max?: number | null
  step?: number | null
  required?: boolean | null
}

export interface MetricOption {
  value: string
  label: string
}

export interface MetricDefinition {
  key: string
  label: string
  type: MetricType
  phase: Phase
  order: number
  visibleWhen?: VisibleWhen | null
  deferrableDays?: number | null
  dayOffset: number
  polarity?: string | null
  validation?: Validation | null
  options?: MetricOption[] | null
  maxSelect?: number | null
  active: boolean
}

export interface Habit {
  key: string
  label: string
  shortLabel: string
  icon: string
  measure: 'binary' | 'duration'
  hasQuality: boolean
  qualityLabel?: string | null
  active: boolean
  order: number
}

export interface MetricValue {
  key: string
  number?: number | null
  text?: string | null
  time?: string | null
  options?: string[] | null
}

export interface HabitEntry {
  habitKey: string
  state: HabitState
  hours?: number | null
  quality?: number | null
}

export interface DailyEntry {
  date: string
  status: DayStatus
  dayType: DayType
  values: MetricValue[]
  habits: HabitEntry[]
  quickPlanned?: number | null
  quickDone: number
  quickAddedLater: number
  ongoingTouched: number
  morningCheckinAt?: string | null
  eveningCheckinAt?: string | null
}

export interface DeferredField {
  key: string
  label: string
  belongsToDate: string
  lastWritableDate: string
}

export interface TodayPayload {
  entry: DailyEntry
  deferred: DeferredField[]
}

export interface Task {
  id: string
  title: string
  category: 'personal' | 'work'
  kind: 'quick' | 'ongoing'
  scope: string
  plannedDate?: string | null
  status: 'todo' | 'in_progress' | 'done' | 'dropped'
  createdAt: string
  doneAt?: string | null
}

export interface Goal {
  title: string
  scope: string
  targetDate?: string | null
}

/** Mutation input — same shape as MetricValue with a required key. */
export type MetricValueInput = MetricValue
