import { describe, expect, it } from 'vitest'
import { directionsUrl, distanceMeters, formatDistance } from './geo'
import { matches } from './labels'
import { detectLang } from './languages'
import { splitSentences } from './sentences'
import { pickVoice } from './speech'

const voice = (lang: string, localService = true) => ({ lang, localService, name: lang }) as SpeechSynthesisVoice

describe('detectLang', () => {
  it('prefers the saved choice', () => expect(detectLang('ko', ['ja-JP'])).toBe('ko'))
  it('falls back to the first supported navigator language', () => expect(detectLang(null, ['fr-FR', 'zh-CN'])).toBe('zh'))
  it('defaults to vi (PRD)', () => expect(detectLang(null, ['fr-FR', 'de'])).toBe('vi'))
  it('ignores an invalid saved value', () => expect(detectLang('xx', ['vi-VN'])).toBe('vi'))
})

describe('pickVoice', () => {
  it('picks the exact BCP-47 match, preferring a local voice', () => {
    const remote = voice('ko-KR', false)
    const local = voice('ko-KR', true)
    expect(pickVoice([voice('en-US'), remote, local], 'ko-KR')).toBe(local)
  })
  it('falls back to the same language prefix', () => {
    const zhTw = voice('zh-TW')
    expect(pickVoice([voice('en-US'), zhTw], 'zh-CN')).toBe(zhTw)
  })
  it('handles underscore locale tags (Android)', () => {
    const ja = voice('ja_JP')
    expect(pickVoice([ja], 'ja-JP')).toBe(ja)
  })
  it('returns null when no voice exists for the language', () => {
    expect(pickVoice([voice('en-US')], 'vi-VN')).toBeNull()
  })
})

describe('splitSentences', () => {
  it('splits Latin punctuation', () => {
    expect(splitSentences('Một câu. Câu hai! Câu ba?')).toEqual(['Một câu.', 'Câu hai!', 'Câu ba?'])
  })
  it('splits CJK punctuation and keeps a trailing fragment', () => {
    expect(splitSentences('第一句。第二句！最后')).toEqual(['第一句。', '第二句！', '最后'])
  })
  it('returns [] for empty input', () => expect(splitSentences(null)).toEqual([]))
})

describe('geo', () => {
  it('computes Haversine distance (Chợ Bình Tây → Đình Bình Tiên ≈ 1 km)', () => {
    const d = distanceMeters({ lat: 10.749, lng: 106.651 }, { lat: 10.75, lng: 106.642 })
    expect(d).toBeGreaterThan(950)
    expect(d).toBeLessThan(1000)
  })
  it('formats metres and kilometres', () => {
    expect(formatDistance(847, 'vi')).toBe('850 m')
    expect(formatDistance(2600, 'vi')).toBe('2,6 km')
    expect(formatDistance(2600, 'en')).toBe('2.6 km')
  })
  it('builds a walking Google Maps URL', () => {
    expect(directionsUrl(10.7, 106.6)).toBe(
      'https://www.google.com/maps/dir/?api=1&destination=10.7,106.6&travelmode=walking',
    )
  })
})

describe('matches', () => {
  it('ignores case and Vietnamese diacritics', () => {
    expect(matches('Chợ Bình Tây', 'cho binh tay')).toBe(true)
    expect(matches('Đình Bình Tiên', 'dinh')).toBe(true)
    expect(matches('Công viên Bình Phú', 'phu lam')).toBe(false)
  })
})
