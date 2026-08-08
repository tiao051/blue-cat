import type { MetricDefinition, MetricValue, MetricValueInput } from '@/api/types'

/** Raw widget value → MetricValueInput with the right slot for its type (spec §10 typed slots). */
export function toInput(def: MetricDefinition, raw: unknown): MetricValueInput | null {
  if (raw === null || raw === undefined) return null

  switch (def.type) {
    case 'scale':
    case 'number':
      return typeof raw === 'number' ? { key: def.key, number: raw } : null
    case 'time':
      return typeof raw === 'string' && raw.length > 0 ? { key: def.key, time: raw } : null
    case 'enum':
      return typeof raw === 'string' && raw.length > 0 ? { key: def.key, options: [raw] } : null
    case 'multi_enum':
      return Array.isArray(raw) && raw.length > 0 ? { key: def.key, options: raw as string[] } : null
    case 'text':
      return typeof raw === 'string' && raw.trim().length > 0
        ? { key: def.key, text: raw.trim() }
        : null
    default:
      return null
  }
}

/** Server MetricValue → raw widget value (for same-day edits). */
export function toRaw(def: MetricDefinition, value: MetricValue | undefined): unknown {
  if (!value) return def.type === 'multi_enum' ? [] : null
  switch (def.type) {
    case 'scale':
    case 'number':
      return value.number ?? null
    case 'time':
      return value.time ?? null
    case 'enum':
      return value.options?.[0] ?? null
    case 'multi_enum':
      return value.options ?? []
    case 'text':
      return value.text ?? null
    default:
      return null
  }
}

/** Is a required field considered "answered" (enables the continue button). */
export function isAnswered(def: MetricDefinition, raw: unknown): boolean {
  return toInput(def, raw) !== null
}
