import { render, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n'
import MapPage from '../../pages/MapPage'

vi.mock('../../api/places', async (orig) => {
  const actual = await orig<typeof import('../../api/places')>()
  return {
    ...actual,
    getCatalog: vi.fn(async () => [
      {
        id: 1,
        slug: 'cho-binh-tay',
        name: 'Chợ Bình Tây',
        kind: 'Destination' as const,
        category: 'Market',
        foodType: null,
        coverImageUrl: null,
        latitude: 10.749,
        longitude: 106.651,
        address: '123 Bình Tây',
        priceRange: null,
        openingHours: null,
        parentSlug: null,
        parentName: null,
      },
    ]),
  }
})

describe('MapPage smoke', () => {
  it('renders without crashing', async () => {
    void i18n.changeLanguage('en')
    const { container } = render(
      <MemoryRouter initialEntries={['/en/map']}>
        <Routes>
          <Route path="/:lang/map" element={<MapPage />} />
        </Routes>
      </MemoryRouter>,
    )
    await waitFor(
      () => {
        expect(container.querySelector('.leaflet-container')).toBeTruthy()
      },
      { timeout: 3000 },
    )
    expect(container.querySelectorAll('.pin-dest')).toHaveLength(1)
  })
})
