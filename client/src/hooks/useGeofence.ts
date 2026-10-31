import { useEffect, useRef, useState } from 'react'
import { stepGeofence, type GeofenceZone, type LatLng } from '../lib/geo'

/**
 * Watches the device position while `enabled` and reports zones the user walks into.
 * Position stays in the browser (NFR-05). `entered` is the most recent zone entry; each entry fires once
 * until the user leaves the zone and comes back.
 */
export function useGeofence<Z extends GeofenceZone>(zones: readonly Z[], enabled: boolean) {
  const [position, setPosition] = useState<LatLng | null>(null)
  const [inside, setInside] = useState<ReadonlySet<string>>(new Set())
  const [entered, setEntered] = useState<Z | null>(null)
  const [denied, setDenied] = useState(false)
  const supported = typeof navigator !== 'undefined' && 'geolocation' in navigator
  const zonesRef = useRef(zones)
  const insideRef = useRef<ReadonlySet<string>>(new Set())

  useEffect(() => {
    zonesRef.current = zones
  }, [zones])

  useEffect(() => {
    if (!enabled || !supported) return
    const id = navigator.geolocation.watchPosition(
      (p) => {
        const pos = { lat: p.coords.latitude, lng: p.coords.longitude }
        const step = stepGeofence(insideRef.current, pos, zonesRef.current)
        insideRef.current = step.inside
        setPosition(pos)
        setInside(step.inside)
        if (step.entered.length > 0) setEntered(step.entered[step.entered.length - 1])
      },
      () => setDenied(true),
      { enableHighAccuracy: true, maximumAge: 5_000, timeout: 20_000 },
    )
    return () => {
      navigator.geolocation.clearWatch(id)
      insideRef.current = new Set()
      setInside(new Set())
      setEntered(null)
      setDenied(false)
    }
  }, [enabled, supported])

  const error = !supported ? 'unsupported' : denied ? 'denied' : null
  return { position, inside, entered, error, dismiss: () => setEntered(null) }
}
