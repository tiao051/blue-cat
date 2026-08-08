/** Mọi "ngày" là chuỗi yyyy-MM-dd theo giờ local (spec §10). */

function toDateString(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

/**
 * Ngày logic đang theo dõi. Trước 4h sáng vẫn tính là hôm trước —
 * spec §7: ngày không đóng lúc nửa đêm vì thường ngủ sau đó
 * (check-in tối lúc 00:30 vẫn thuộc về "hôm nay" theo nghĩa sinh hoạt).
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

/** Tổng giờ ngủ từ hai mốc HH:mm, wrap qua nửa đêm: 23:30 → 07:00 = 7.5 (khớp backend). */
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

/** Mã tuần ISO 8601 (tuần bắt đầu thứ Hai) — vd "2026-W32", khớp backend. */
export function isoWeekCode(date: string): string {
  const d = new Date(`${date}T12:00:00`)
  // dời về thứ Năm của tuần ISO hiện tại
  const day = (d.getDay() + 6) % 7 // 0 = thứ Hai
  d.setDate(d.getDate() - day + 3)
  const isoYear = d.getFullYear()
  const jan4 = new Date(isoYear, 0, 4)
  const jan4Day = (jan4.getDay() + 6) % 7
  const week1Thu = new Date(jan4)
  week1Thu.setDate(jan4.getDate() - jan4Day + 3)
  const week = 1 + Math.round((d.getTime() - week1Thu.getTime()) / (7 * 24 * 3600 * 1000))
  return `${isoYear}-W${String(week).padStart(2, '0')}`
}

const WEEKDAYS = ['Chủ nhật', 'Thứ hai', 'Thứ ba', 'Thứ tư', 'Thứ năm', 'Thứ sáu', 'Thứ bảy']

export function formatDisplay(date: string): string {
  const d = new Date(`${date}T12:00:00`)
  return `${WEEKDAYS[d.getDay()]}, ${d.getDate()}/${d.getMonth() + 1}`
}
