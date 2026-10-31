export interface LatLng {
  lat: number
  lng: number
}

/** Great-circle distance in metres (Haversine). Runs only in the browser; position is never sent to the server. */
export function distanceMeters(a: LatLng, b: LatLng): number {
  const R = 6_371_000
  const rad = (d: number) => (d * Math.PI) / 180
  const dLat = rad(b.lat - a.lat)
  const dLng = rad(b.lng - a.lng)
  const h = Math.sin(dLat / 2) ** 2 + Math.cos(rad(a.lat)) * Math.cos(rad(b.lat)) * Math.sin(dLng / 2) ** 2
  return 2 * R * Math.asin(Math.sqrt(h))
}

export function formatDistance(meters: number, locale: string): string {
  if (meters < 1000) return `${Math.round(meters / 10) * 10} m`
  return `${(meters / 1000).toLocaleString(locale, { maximumFractionDigits: 1 })} km`
}

/** Link tìm đường OpenStreetMap (không cần API key) */
export const directionsUrl = (lat: number, lng: number) =>
  `https://www.openstreetmap.org/#map=18/${lat}/${lng}`

/** Link tìm đường OpenStreetMap từ điểm này tới đích */
export const routeUrl = (from: {lat: number; lng: number}, to: {lat: number; lng: number}) =>
  `https://www.openstreetmap.org/route?from=/${from.lat}/${from.lng}/to/${to.lat}/${to.lng}`

export interface GeofenceZone extends LatLng {
  key: string
  radiusM: number
}

/** Leaving needs this much margin past the radius, so GPS jitter at the edge doesn't re-trigger. */
const EXIT_MARGIN = 1.2

/**
 * One geofence step: which zones the position is inside, and which of those it just entered.
 * A zone already in `prev` stays inside until the position is `EXIT_MARGIN` x radius away.
 */
export function stepGeofence<Z extends GeofenceZone>(prev: ReadonlySet<string>, pos: LatLng, zones: readonly Z[]) {
  const inside = new Set<string>()
  const entered: Z[] = []
  for (const z of zones) {
    const d = distanceMeters(pos, z)
    const was = prev.has(z.key)
    if (d <= z.radiusM || (was && d <= z.radiusM * EXIT_MARGIN)) {
      inside.add(z.key)
      if (!was) entered.push(z)
    }
  }
  return { inside, entered }
}
