/** Every "date" is a yyyy-MM-dd string in local time (spec §10). */

function toDateString(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

/**
 * The logical day being tracked. Before 4am it still counts as the previous day —
 * spec §7: days don't close at midnight because you're usually asleep after it
 * (an evening check-in at 00:30 still belongs to "today" in the lived sense).
 */
export function logicalToday(now: Date = new Date()): string {
  const d = new Date(now)
  if (d.getHours() < 4) d.setDate(d.getDate() - 1)
  return toDateString(d)
}

export function addDays(date: string, days: number): string {
  const d = new Date(`${date}T12:00:00`)
  d.setDate(d.getDate() + days)
  return toDateString(d)
}

/** Sleep hours between two HH:mm marks, wrapping past midnight: 23:30 → 07:00 = 7.5 (matches backend). */
export function sleepHours(start: string, end: string): number | null {
  const parse = (t: string): number | null => {
    const m = /^(\d{2}):(\d{2})$/.exec(t)
    if (!m) return null
    return Number(m[1]) * 60 + Number(m[2])
  }
  const s = parse(start)
  const e = parse(end)
  if (s === null || e === null) return null
  const minutes = (e - s + 24 * 60) % (24 * 60)
  return Math.round((minutes / 60) * 100) / 100
}

/** ISO 8601 week code (Monday-first) — e.g. "2026-W32", matches backend. */
export function isoWeekCode(date: string): string {
  const d = new Date(`${date}T12:00:00`)
  // shift to the Thursday of the current ISO week
  const day = (d.getDay() + 6) % 7 // 0 = Monday
  d.setDate(d.getDate() - day + 3)
  const isoYear = d.getFullYear()
  const jan4 = new Date(isoYear, 0, 4)
  const jan4Day = (jan4.getDay() + 6) % 7
  const week1Thu = new Date(jan4)
  week1Thu.setDate(jan4.getDate() - jan4Day + 3)
  const week = 1 + Math.round((d.getTime() - week1Thu.getTime()) / (7 * 24 * 3600 * 1000))
  return `${isoYear}-W${String(week).padStart(2, '0')}`
}

const WEEKDAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

export function formatDisplay(date: string): string {
  const d = new Date(`${date}T12:00:00`)
  return `${WEEKDAYS[d.getDay()]}, ${MONTHS[d.getMonth()]} ${d.getDate()}`
}
