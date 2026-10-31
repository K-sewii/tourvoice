import { useEffect, useState } from 'react'
import { render, waitFor } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { MapContainer, TileLayer, useMap } from 'react-leaflet'

function MapProbe() {
  useMap() // throws if rendered outside <MapContainer>
  return null
}

function LateMap() {
  const [ready, setReady] = useState(false)
  useEffect(() => {
    const t = setTimeout(() => setReady(true), 50)
    return () => clearTimeout(t)
  }, [])
  if (!ready) return <div>waiting</div>
  return (
    <MapContainer center={{ lat: 10.748, lng: 106.64 }} zoom={15}>
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <MapProbe />
    </MapContainer>
  )
}

describe('MapContainer late mount', () => {
  it('provides context when mounted after state update', async () => {
    const { container } = render(<LateMap />)
    await waitFor(() => expect(container.querySelector('.leaflet-container')).toBeTruthy(), { timeout: 3000 })
  })
})
