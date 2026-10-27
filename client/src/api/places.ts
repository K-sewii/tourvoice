import { request } from './http'
import type { ExploreItem, MapMarker, PlaceDetail, PlaceSummary } from './types'

const q = (lang: string) => `lang=${encodeURIComponent(lang)}`

export const getDestinations = (lang: string, signal?: AbortSignal) =>
  request<PlaceSummary[]>('GET', `/places?${q(lang)}`, undefined, signal)

export const getPlace = (slug: string, lang: string, signal?: AbortSignal) =>
  request<PlaceDetail>('GET', `/places/${encodeURIComponent(slug)}?${q(lang)}`, undefined, signal)

export const getMapMarkers = (lang: string, signal?: AbortSignal) =>
  request<MapMarker[]>('GET', `/map/markers?${q(lang)}`, undefined, signal)

/** In-app path of a place: food spots live under their destination. */
export const placePath = (p: { slug: string; parentSlug: string | null }) =>
  p.parentSlug ? `/places/${p.parentSlug}/foods/${p.slug}` : `/places/${p.slug}`

/**
 * Every destination and food spot (GET /explore, one request). Explore, Food & Culture filter it
 * client-side so search reacts per keystroke without a round trip.
 */
export const fetchCatalog = (lang: string, signal?: AbortSignal) =>
  request<ExploreItem[]>('GET', `/explore?${q(lang)}`, undefined, signal)

const CATALOG_TTL_MS = 60_000
const catalogCache = new Map<string, { at: number; promise: Promise<ExploreItem[]> }>()

/**
 * Cached `fetchCatalog` shared by Explore and Food & Culture so switching pages doesn't refetch.
 * Not tied to the caller's AbortSignal: one page leaving must not cancel the request another
 * page is waiting on.
 */
export function getCatalog(lang: string, signal?: AbortSignal): Promise<ExploreItem[]> {
  const hit = catalogCache.get(lang)
  if (hit && Date.now() - hit.at < CATALOG_TTL_MS) return withAbort(hit.promise, signal)
  const promise = fetchCatalog(lang)
  catalogCache.set(lang, { at: Date.now(), promise })
  promise.catch(() => catalogCache.delete(lang)) // never cache a failure
  return withAbort(promise, signal)
}

export function clearCatalogCache() {
  catalogCache.clear()
}

function withAbort<T>(promise: Promise<T>, signal?: AbortSignal): Promise<T> {
  if (!signal) return promise
  return new Promise((resolve, reject) => {
    const onAbort = () => reject(new DOMException('Aborted', 'AbortError'))
    if (signal.aborted) return onAbort()
    signal.addEventListener('abort', onAbort, { once: true })
    promise.then(resolve, reject).finally(() => signal.removeEventListener('abort', onAbort))
  })
}
