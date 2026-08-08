/**
 * Constants for the keys the code references by name (spec §10 — type safety).
 * Every other key flows through the dynamic form renderer and never appears in code.
 */
export const METRIC_KEYS = {
  /** The one deliberately paired screen: two sleep marks + auto-computed total */
  sleepStart: 'sleep_start',
  sleepEnd: 'sleep_end',
} as const
