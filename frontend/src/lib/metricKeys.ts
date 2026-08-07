/**
 * Hằng số cho những key mà code gọi đích danh (spec §10 — type safety).
 * Mọi key khác đi qua form renderer động, không xuất hiện trong code.
 */
export const METRIC_KEYS = {
  /** Cặp duy nhất được gộp màn có chủ đích: hai mốc ngủ + tổng giờ tính tự động */
  sleepStart: 'sleep_start',
  sleepEnd: 'sleep_end',
} as const
