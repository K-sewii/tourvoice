import { useMemo, useSyncExternalStore } from 'react'
import { distanceMeters, formatDistance, type LatLng } from '../lib/geo'

type GeoStatus = 'idle' | 'locating' | 'ready' | 'denied' | 'unsupported'
interface GeoState {
  status: GeoStatus
  position: LatLng | null
}

// Module-level store so every page shares one position fix (NFR-05: never sent to the server).
let state: GeoState = { status: 'idle', position: null }
const listeners = new Set<() => void>()
const set = (next: GeoState) => {
  state = next
  listeners.forEach((l) => l())
}

function locate() {
  if (!('geolocation' in navigator)) return set({ status: 'unsupported', position: null })
  if (state.status === 'locating') return
  set({ ...state, status: 'locating' })
  navigator.geolocation.getCurrentPosition(
    (p) => set({ status: 'ready', position: { lat: p.coords.latitude, lng: p.coords.longitude } }),
    () => set({ status: 'denied', position: null }),
    { enableHighAccuracy: false, maximumAge: 60_000, timeout: 10_000 },
  )
}

let probed = false
/** If permission was already granted, fetch silently — never prompt without a tap. */
function probe() {
  if (probed) return
  probed = true
  navigator.permissions
    ?.query({ name: 'geolocation' })
    .then((s) => {
      if (s.state === 'granted') locate()
    })
    .catch(() => {})
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  probe()
  return () => listeners.delete(listener)
}

/** Returns a formatter "lat,lng → '850 m'" once the user's position is known, else null. */
export function useDistanceFormatter(locale: string) {
  const { position } = useGeolocation()
  return useMemo(
    () =>
      position
        ? (lat: number, lng: number): string | null => formatDistance(distanceMeters(position, { lat, lng }), locale)
        : (): string | null => null,
    [position, locale],
  )
}

export function useGeolocation() {
  const snapshot = useSyncExternalStore(subscribe, () => state, () => state)
  return { ...snapshot, locate }
}
