import { describe, expect, it } from 'vitest'
import { stepGeofence } from '../../lib/geo'

const zone = { key: 'a', lat: 10.75, lng: 106.65, radiusM: 100 }
const at = (dLat: number) => ({ lat: zone.lat + dLat, lng: zone.lng })

describe('stepGeofence', () => {
  it('reports entry once, then stays inside without re-triggering', () => {
    const first = stepGeofence(new Set(), at(0.0003), [zone]) // ~33 m
    expect(first.entered).toHaveLength(1)
    const again = stepGeofence(first.inside, at(0.0004), [zone])
    expect(again.entered).toHaveLength(0)
    expect(again.inside.has('a')).toBe(true)
  })

  it('ignores positions outside the radius', () => {
    expect(stepGeofence(new Set(), at(0.002), [zone]).inside.size).toBe(0) // ~222 m
  })

  it('applies exit hysteresis, then re-triggers after leaving', () => {
    const inside = new Set(['a'])
    expect(stepGeofence(inside, at(0.001), [zone]).inside.has('a')).toBe(true) // ~111 m: past radius but within 120%
    const out = stepGeofence(inside, at(0.002), [zone])
    expect(out.inside.size).toBe(0)
    expect(stepGeofence(out.inside, at(0.0003), [zone]).entered).toHaveLength(1)
  })
})
