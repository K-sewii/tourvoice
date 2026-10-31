import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { clearCatalogCache, getCatalog, placePath } from '../../api/places'

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

const items = [
  { id: 1, slug: 'cho-binh-tay', kind: 'Destination', parentSlug: null },
  { id: 10, slug: 'mau-pho-bo', kind: 'Food', parentSlug: 'cho-binh-tay' },
]

describe('placePath', () => {
  it('nests food spots under their destination', () => {
    expect(placePath({ slug: 'cho-binh-tay', parentSlug: null })).toBe('/places/cho-binh-tay')
    expect(placePath({ slug: 'mau-pho-bo', parentSlug: 'cho-binh-tay' })).toBe('/places/cho-binh-tay/foods/mau-pho-bo')
  })
})

describe('getCatalog', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    clearCatalogCache()
    vi.stubGlobal('fetch', fetchMock)
    fetchMock.mockImplementation(async () => json(items))
  })
  afterEach(() => {
    vi.unstubAllGlobals()
    fetchMock.mockReset()
  })

  it('loads destinations and food spots from /explore in one request', async () => {
    const catalog = await getCatalog('ko')
    expect(catalog).toHaveLength(2)
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(String(fetchMock.mock.calls[0][0])).toBe('http://localhost:5049/api/v1/explore?lang=ko')
  })

  it('serves repeat calls for the same language from cache', async () => {
    await getCatalog('vi')
    await getCatalog('vi')
    expect(fetchMock).toHaveBeenCalledTimes(1)
    await getCatalog('en')
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('does not cache a failure', async () => {
    fetchMock.mockResolvedValueOnce(json({}, 500))
    await expect(getCatalog('vi')).rejects.toThrow()
    await expect(getCatalog('vi')).resolves.toHaveLength(2)
  })

  it('rejects for an aborted caller without cancelling the shared request', async () => {
    const ac = new AbortController()
    const first = getCatalog('vi', ac.signal)
    ac.abort()
    await expect(first).rejects.toMatchObject({ name: 'AbortError' })
    await expect(getCatalog('vi')).resolves.toHaveLength(2)
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})
