import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { publishAdminPlace, translatePlace } from '../../api/admin'
import { ApiError } from '../../api/http'

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

describe('admin API & ApiError extensions', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
  })
  afterEach(() => {
    vi.unstubAllGlobals()
    fetchMock.mockReset()
  })

  it('extracts code and missing preconditions from 422 ProblemDetails (BR-08)', async () => {
    fetchMock.mockResolvedValueOnce(
      json(
        {
          status: 422,
          title: 'Địa điểm chưa đủ điều kiện để xuất bản.',
          code: 'publish_preconditions_failed',
          missing: ['vi_narration_reviewed', 'cover_image'],
        },
        422,
      ),
    )

    try {
      await publishAdminPlace(1)
      expect.fail('Expected publishAdminPlace to throw ApiError')
    } catch (err) {
      expect(err).toBeInstanceOf(ApiError)
      const apiErr = err as ApiError
      expect(apiErr.status).toBe(422)
      expect(apiErr.code).toBe('publish_preconditions_failed')
      expect(apiErr.missing).toEqual(['vi_narration_reviewed', 'cover_image'])
    }
  })

  it('calls POST /admin/places/{id}/translate and returns per-language results', async () => {
    fetchMock.mockResolvedValueOnce(
      json({
        results: [
          { languageCode: 'en', success: true, charactersUsed: 420, errorMessage: null, skipped: false },
          { languageCode: 'zh', success: true, charactersUsed: 0, errorMessage: null, skipped: true },
        ],
      }),
    )

    const res = await translatePlace(5)
    expect(res.results).toHaveLength(2)
    expect(res.results[0].charactersUsed).toBe(420)
    expect(res.results[1].skipped).toBe(true)
    expect(String(fetchMock.mock.calls[0][0])).toBe('http://localhost:5049/api/v1/admin/places/5/translate')
  })
})
