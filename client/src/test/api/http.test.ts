import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, REQUEST_TIMEOUT_MS, request } from '../../api/http'

const json = (body: unknown) =>
  new Response(JSON.stringify(body), { status: 200, headers: { 'Content-Type': 'application/json' } })

describe('request', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
  })
  afterEach(() => {
    vi.unstubAllGlobals()
    fetchMock.mockReset()
    vi.useRealTimers()
  })

  it('returns the parsed body', async () => {
    fetchMock.mockResolvedValueOnce(json({ id: 3 }))

    await expect(request<{ id: number }>('GET', '/x')).resolves.toEqual({ id: 3 })
  })

  it('turns a hanging request into a timeout ApiError', async () => {
    vi.useFakeTimers()
    fetchMock.mockImplementationOnce(
      (_url, init) =>
        new Promise((_resolve, reject) => {
          init?.signal?.addEventListener('abort', () => reject(new DOMException('Aborted', 'AbortError')))
        }),
    )

    const expectation = expect(request('GET', '/slow')).rejects.toMatchObject({ status: 0, message: 'timeout' })
    await vi.advanceTimersByTimeAsync(REQUEST_TIMEOUT_MS)

    await expectation
  })

  it('propagates the caller abort instead of reporting a network error', async () => {
    const controller = new AbortController()
    fetchMock.mockImplementationOnce(
      (_url, init) =>
        new Promise((_resolve, reject) => {
          init?.signal?.addEventListener('abort', () => reject(new DOMException('Aborted', 'AbortError')))
        }),
    )

    const expectation = expect(request('GET', '/x', undefined, controller.signal)).rejects.toBeInstanceOf(DOMException)
    controller.abort()

    await expectation
  })

  it('rejects invalid JSON with a readable ApiError', async () => {
    fetchMock.mockResolvedValueOnce(new Response('<html>', { status: 200 }))

    try {
      await request('GET', '/x')
      expect.fail('Expected request to throw')
    } catch (err) {
      expect(err).toBeInstanceOf(ApiError)
      expect((err as ApiError).message).toBe('invalid_response')
    }
  })
})
