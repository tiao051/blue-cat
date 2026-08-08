// GraphQL documents viết tay (M1). Fragment chung cho DailyEntry để mutation nào cũng trả cùng shape.

const DAILY_ENTRY_FIELDS = `
  date
  status
  dayType
  values { key number text time options }
  habits { habitKey state hours quality }
  quickPlanned
  quickDone
  quickAddedLater
  ongoingTouched
  morningCheckinAt
  eveningCheckinAt
`

export const METRIC_DEFINITIONS_QUERY = `
  query MetricDefinitions($phase: Phase, $dayType: DayType) {
    metricDefinitions(phase: $phase, dayType: $dayType) {
      key label type phase order
      visibleWhen { field values }
      deferrableDays dayOffset polarity
      validation { min max step required }
      options { value label }
      maxSelect active
    }
  }
`

export const HABITS_QUERY = `
  query Habits {
    habits { key label shortLabel icon measure hasQuality qualityLabel active order }
  }
`

export const DAILY_ENTRY_QUERY = `
  query DailyEntry($date: String!, $clientDate: String!) {
    dailyEntry(date: $date, clientDate: $clientDate) { ${DAILY_ENTRY_FIELDS} }
  }
`

export const TODAY_QUERY = `
  query Today($date: String!) {
    today(date: $date) {
      entry { ${DAILY_ENTRY_FIELDS} }
      deferred { key label belongsToDate lastWritableDate }
    }
  }
`

const TASK_FIELDS = `id title category kind scope plannedDate status createdAt doneAt`

export const TASKS_QUERY = `
  query Tasks($from: String!, $to: String!) {
    tasks(from: $from, to: $to) { ${TASK_FIELDS} }
  }
`

export const ADD_TASK_MUTATION = `
  mutation AddTask($title: String!, $plannedDate: String!, $clientDate: String!) {
    addTask(title: $title, plannedDate: $plannedDate, clientDate: $clientDate) { ${TASK_FIELDS} }
  }
`

export const SET_TASK_DONE_MUTATION = `
  mutation SetTaskDone($id: String!, $done: Boolean!, $clientDate: String!) {
    setTaskDone(id: $id, done: $done, clientDate: $clientDate) { ${TASK_FIELDS} }
  }
`

export const DROP_TASK_MUTATION = `
  mutation DropTask($id: String!, $clientDate: String!) {
    dropTask(id: $id, clientDate: $clientDate) { ${TASK_FIELDS} }
  }
`

export const YEAR_GOAL_QUERY = `
  query YearGoal {
    yearGoal { title scope targetDate }
  }
`

export const MORNING_CHECKIN_MUTATION = `
  mutation MorningCheckin($date: String!, $values: [MetricValueInput!]!, $deferredKeys: [String!]!) {
    morningCheckin(date: $date, values: $values, deferredKeys: $deferredKeys) { ${DAILY_ENTRY_FIELDS} }
  }
`

export const EVENING_CHECKIN_MUTATION = `
  mutation EveningCheckin($date: String!, $values: [MetricValueInput!]!) {
    eveningCheckin(date: $date, values: $values) { ${DAILY_ENTRY_FIELDS} }
  }
`

export const SET_METRIC_VALUE_MUTATION = `
  mutation SetMetricValue($date: String!, $value: MetricValueInput!, $clientDate: String!) {
    setMetricValue(date: $date, value: $value, clientDate: $clientDate) { ${DAILY_ENTRY_FIELDS} }
  }
`

export const SET_HABIT_MUTATION = `
  mutation SetHabit($date: String!, $habitKey: String!, $state: HabitState!, $hours: Float, $quality: Int) {
    setHabit(date: $date, habitKey: $habitKey, state: $state, hours: $hours, quality: $quality) { ${DAILY_ENTRY_FIELDS} }
  }
`

export const SET_DAY_TYPE_MUTATION = `
  mutation SetDayType($date: String!, $dayType: DayType!) {
    setDayType(date: $date, dayType: $dayType) { ${DAILY_ENTRY_FIELDS} }
  }
`
