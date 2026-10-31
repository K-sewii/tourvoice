import { describe, expect, it } from 'vitest'
import { safeReturn } from '../../lib/safeReturn'

const ORIGIN = 'http://localhost:5173'

describe('safeReturn', () => {
  it('keeps an in-app path with query and hash', () => {
    expect(safeReturn('/vi/places/cho-binh-tay?src=qr#listen', ORIGIN)).toBe('/vi/places/cho-binh-tay?src=qr#listen')
  })

  it.each([
    ['protocol-relative', '//evil.example/x'],
    ['backslash trick', '/\\evil.example/x'],
    ['absolute URL', 'https://evil.example/x'],
    ['javascript URL', 'javascript:alert(1)'],
    ['relative path', 'vi/account'],
    ['empty', ''],
  ])('rejects %s', (_, value) => {
    expect(safeReturn(value, ORIGIN)).toBeNull()
  })

  it('rejects null', () => expect(safeReturn(null, ORIGIN)).toBeNull())
})
