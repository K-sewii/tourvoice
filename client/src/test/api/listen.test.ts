import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { setToken } from '../../api/http'
import { recordListen } from '../../api/listen'

describe('recordListen', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }))
  })
  afterEach(() => {
    setToken(null)
    vi.unstubAllGlobals()
    fetchMock.mockReset()
  })

  it('posts the listen to /listen-events (PRD API)', async () => {
    await recordListen({ placeSlug: 'cho-binh-tay', languageCode: 'ko', listenedSeconds: 12 })

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('http://localhost:5049/api/v1/listen-events')
    expect(init?.method).toBe('POST')
    expect(JSON.parse(init?.body as string)).toEqual({ placeSlug: 'cho-binh-tay', languageCode: 'ko', listenedSeconds: 12 })
    const headers = init?.headers as Record<string, string>
    expect(headers.Authorization).toBeUndefined()
    expect(headers['X-Device-Id']).toMatch(/^[0-9a-f-]{36}$/)
  })

  it('attaches the token when signed in so the listen shows in history', async () => {
    setToken('abc')
    await recordListen({ placeSlug: 'x', languageCode: 'vi', listenedSeconds: 10 })
    expect((fetchMock.mock.calls[0][1]?.headers as Record<string, string>).Authorization).toBe('Bearer abc')
  })

  it('never throws on server or network errors', async () => {
    fetchMock.mockResolvedValueOnce(new Response('{}', { status: 500 }))
    await expect(recordListen({ placeSlug: 'x', languageCode: 'vi', listenedSeconds: 10 })).resolves.toBeUndefined()
    fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'))
    await expect(recordListen({ placeSlug: 'x', languageCode: 'vi', listenedSeconds: 10 })).resolves.toBeUndefined()
  })
})
