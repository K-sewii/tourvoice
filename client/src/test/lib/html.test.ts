import { describe, expect, it } from 'vitest'
import { escapeHtml } from '../../lib/html'

describe('escapeHtml', () => {
  it('escapes the five HTML metacharacters', () => {
    expect(escapeHtml('<a href="x">Tom & Jerry\'s</a>')).toBe(
      '&lt;a href=&quot;x&quot;&gt;Tom &amp; Jerry&#39;s&lt;/a&gt;',
    )
  })

  it('leaves plain Vietnamese text untouched', () => {
    expect(escapeHtml('Chợ Bình Tây')).toBe('Chợ Bình Tây')
  })
})
