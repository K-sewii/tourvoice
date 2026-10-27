import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { useEffect, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { MapContainer, Marker, TileLayer, useMap } from 'react-leaflet'
import { getCatalog, placePath } from '../api/places'
import { CloseIcon, LocateIcon, NavigateIcon, PlayIcon } from '../components/Icons'
import { ErrorState, Loading } from '../components/States'
import { useAsync } from '../hooks/useAsync'
import { useDistanceFormatter, useGeolocation } from '../hooks/useGeolocation'
import { useLang, useLocalePath } from '../hooks/useLang'
import { directionsUrl } from '../lib/geo'
import { categoryLabel, foodTypeLabel, joinMeta } from '../lib/labels'

const CENTER: [number, number] = [10.748, 106.64]
const CATEGORIES = ['Heritage', 'Market', 'FoodStreet', 'Park'] as const

interface Pin {
  key: string
  slug: string
  kind: 'dest' | 'food'
  name: string
  label: string | null
  sub: string | null
  lat: number
  lng: number
  detailPath: string
  category: string | null
}

const makeIcon = (kind: Pin['kind'] | 'me', selected: boolean) =>
  L.divIcon({
    className: '',
    html: `<div class="pin pin-${kind}${selected ? ' selected' : ''}"></div>`,
    iconSize: kind === 'dest' ? [30, 30] : [22, 22],
    iconAnchor: kind === 'dest' ? [15, 15] : [11, 11],
  })

// Built once: a fresh L.divIcon per render makes every marker swap its DOM node on each render.
const ICONS = {
  dest: makeIcon('dest', false),
  destSelected: makeIcon('dest', true),
  food: makeIcon('food', false),
  foodSelected: makeIcon('food', true),
  me: makeIcon('me', false),
}
const icon = (kind: Pin['kind'] | 'me', selected = false) =>
  kind === 'me' ? ICONS.me : ICONS[`${kind}${selected ? 'Selected' : ''}`]

/** SCR-04 — Leaflet + OpenStreetMap (ADR-04), no API key. */
export default function MapPage() {
  const { t } = useTranslation()
  const lang = useLang()
  const to = useLocalePath()
  const [params, setParams] = useSearchParams()
  const filter = params.get('category') ?? ''
  const selectedSlug = params.get('focus')
  const geo = useGeolocation()
  const distance = useDistanceFormatter(lang)
  const { data, error, loading, reload } = useAsync(`catalog:${lang}`, (s) => getCatalog(lang, s))

  // /explore (shared cache) rather than /map/markers: the bottom sheet also needs address, price and hours.
  const pins = useMemo<Pin[]>(
    () =>
      (data ?? []).map((p) => {
        const isFood = p.kind === 'Food'
        return {
          key: `${p.kind}${p.id}`,
          slug: p.slug,
          kind: isFood ? 'food' : 'dest',
          name: p.name,
          label: isFood ? foodTypeLabel(t, p.foodType) : categoryLabel(t, p.category),
          sub: isFood ? joinMeta(p.priceRange, p.openingHours) : p.address,
          lat: p.latitude,
          lng: p.longitude,
          detailPath: placePath(p),
          category: isFood ? 'food' : p.category,
        }
      }),
    [data, t],
  )

  const visible = pins.filter((p) => !filter || p.category === filter || (filter === 'food' && p.kind === 'food'))
  const selected = pins.find((p) => p.slug === selectedSlug) ?? null

  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    setParams(next, { replace: true })
  }

  const chips = [
    { value: '', label: t('explore.all') },
    { value: 'food', label: t('explore.food') },
    ...CATEGORIES.map((c) => ({ value: c, label: categoryLabel(t, c)! })),
  ]

  return (
    <div className="map-page">
      <h1 className="visually-hidden">{t('map.title')}</h1>
      {loading && <Loading />}
      {!!error && <ErrorState error={error} onRetry={reload} />}
      {data && (
        <>
          <MapContainer center={CENTER} zoom={15} scrollWheelZoom zoomControl={false}>
            <TileLayer
              attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
              url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
            />
            {visible.map((p) => (
              <Marker
                key={p.key}
                position={[p.lat, p.lng]}
                icon={icon(p.kind, p.slug === selectedSlug)}
                title={p.name}
                alt={p.name}
                keyboard
                eventHandlers={{ click: () => setParam('focus', p.slug) }}
              />
            ))}
            {geo.position && (
              <Marker position={[geo.position.lat, geo.position.lng]} icon={icon('me')} title={t('map.yourLocation')} />
            )}
            <FlyTo target={selected ? [selected.lat, selected.lng] : null} />
            <FlyTo target={geo.position ? [geo.position.lat, geo.position.lng] : null} />
          </MapContainer>

          <div className="map-overlay">
            <div className="chips" role="group" aria-label={t('map.title')}>
              {chips.map((c) => (
                <button
                  key={c.value || 'all'}
                  type="button"
                  className="chip"
                  aria-pressed={filter === c.value}
                  onClick={() => setParam('category', c.value)}
                >
                  {c.label}
                </button>
              ))}
            </div>
            <button
              type="button"
              className="icon-btn map-locate"
              onClick={geo.locate}
              aria-label={t('map.locate')}
              title={t('map.locate')}
            >
              <LocateIcon />
            </button>
          </div>

          {selected && (
            <div className="sheet" role="region" aria-label={selected.name}>
              <div className="sheet-handle" />
              <button
                type="button"
                className="icon-btn sheet-close"
                onClick={() => setParam('focus', null)}
                aria-label={t('map.close')}
              >
                <CloseIcon />
              </button>
              <h2>{selected.name}</h2>
              <p className="meta">{joinMeta(selected.label, distance(selected.lat, selected.lng))}</p>
              {selected.sub && <p className="sub">{selected.sub}</p>}
              <div className="detail-actions">
                <Link to={to(selected.detailPath)} state={{ autoplay: true }} className="btn btn-primary">
                  <PlayIcon size={16} /> {t('common.listen')}
                </Link>
                <a
                  href={directionsUrl(selected.lat, selected.lng)}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="btn btn-outline"
                >
                  <NavigateIcon size={16} /> {t('common.directions')}
                </a>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}

function FlyTo({ target }: { target: [number, number] | null }) {
  const map = useMap()
  const lat = target?.[0]
  const lng = target?.[1]
  useEffect(() => {
    if (lat !== undefined && lng !== undefined) map.flyTo([lat, lng], Math.max(map.getZoom(), 16), { duration: 0.6 })
  }, [lat, lng, map])
  return null
}
