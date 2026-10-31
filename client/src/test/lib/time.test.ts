import { describe, expect, it } from 'vitest'
import { formatVnDateTime, formatVnTime } from '../../lib/time'

describe('formatVnTime', () => {
  it('converts a UTC instant to Vietnam time', () => {
    expect(formatVnTime('2026-10-27T07:32:05Z')).toBe('14:32:05')
  })

  it('treats a value without an offset as UTC (MySQL dates)', () => {
    expect(formatVnTime('2026-10-27T07:32:05')).toBe('14:32:05')
  })

  it('returns null for missing or invalid values', () => {
    expect(formatVnTime('')).toBeNull()
    expect(formatVnTime('not-a-date')).toBeNull()
  })
})

describe('formatVnDateTime', () => {
  it('uses Vietnam time when the UTC instant is on the previous day', () => {
    expect(formatVnDateTime('2026-10-27T18:30:00Z')).toContain('28/10')
  })

  it('returns the raw value when it cannot be parsed', () => {
    expect(formatVnDateTime('??')).toBe('??')
  })
})
