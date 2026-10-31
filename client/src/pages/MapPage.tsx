import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { MapContainer, Marker, TileLayer, Circle, Polyline, useMap } from 'react-leaflet'
import { getCatalog, placePath } from '../api/places'
import { CloseIcon, LocateIcon, NavigateIcon, PlayIcon } from '../components/Icons'
import { ErrorState, Loading } from '../components/States'
import { useAsync } from '../hooks/useAsync'
import { useDistanceFormatter, useGeolocation } from '../hooks/useGeolocation'
import { useGeofence } from '../hooks/useGeofence'
import { useLang, useLocalePath } from '../hooks/useLang'
import { MapRadiusDefaultKm, MapRadiusMaxKm } from '../api/types'
import { categoryLabel, foodTypeLabel, joinMeta, localizePrice } from '../lib/labels'
import { distanceMeters, formatDistance, type LatLng } from '../lib/geo'

const CENTER: LatLng = { lat: 10.748, lng: 106.64 }
const RADIUS_MIN_M = 50
const RADIUS_STEP_M = 50

interface Route {
  slug: string
  path: [number, number][]
  meters: number
  seconds: number
}

/** Walking route from OSRM's public server (no API key); only the two endpoints leave the browser. */
async function fetchRoute(from: LatLng, to: LatLng, signal: AbortSignal): Promise<Omit<Route, 'slug'>> {
  const url = `https://router.project-osrm.org/route/v1/foot/${from.lng},${from.lat};${to.lng},${to.lat}?overview=full&geometries=geojson`
  const res = await fetch(url, { signal })
  if (!res.ok) throw new Error(`route ${res.status}`)
  const json = await res.json()
  const r = json.routes?.[0]
  if (!r) throw new Error('no route')
  return {
    path: r.geometry.coordinates.map(([lng, lat]: [number, number]) => [lat, lng]),
    meters: r.distance,
    seconds: r.duration,
  }
}

const CATEGORIES = ['Heritage', 'Market', 'FoodStreet', 'Park'] as const

interface Pin {
  key: string
  slug: string
  kind: 'dest' | 'food'
  name: string
  label: string | null
  sub: string | null
  address: string | null
  lat: number
  lng: number
  detailPath: string
  category: string | null
  radiusM: number
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

  // Radius is adjustable by the user and filtered client-side only (NFR-05).
  const [radiusM, setRadiusM] = useState(MapRadiusDefaultKm * 1000)

  // In-map directions: wanted slug -> route from the user's position, drawn as a line on the map.
  const [routeFor, setRouteFor] = useState<string | null>(null)
  const [route, setRoute] = useState<Route | null>(null)
  const [routeError, setRouteError] = useState(false)

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
          sub: isFood ? joinMeta(localizePrice(p.priceRange, lang), p.openingHours) : p.address,
          address: p.address,
          lat: p.latitude,
          lng: p.longitude,
          detailPath: placePath(p),
          category: isFood ? 'food' : p.category,
          radiusM: p.geofenceRadiusM ?? 100,
        }
      }),
    [data, lang, t],
  )

  const visible = useMemo(() => (filter ? pins.filter((p) => p.category === filter) : pins), [pins, filter])
  // Geofence: while on, watch the position and announce the place whose zone the user walks into.
  const [geofenceOn, setGeofenceOn] = useState(false)
  const fence = useGeofence(pins, geofenceOn)
  const enteredPin = fence.entered
  const selected = pins.find((p) => p.slug === selectedSlug) ?? null

  // Other places inside the blue circle around the selected place.
  const nearby = useMemo(() => {
    if (!selected) return []
    return visible
      .filter((p) => p.key !== selected.key)
      .map((p) => ({ pin: p, d: distanceMeters(selected, p) }))
      .filter((x): x is { pin: Pin; d: number } => x.d !== null && x.d <= radiusM)
      .sort((x, y) => x.d - y.d)
  }, [visible, selected, radiusM])

  const routeTarget = pins.find((p) => p.slug === routeFor) ?? null
  const origin = geo.position
  useEffect(() => {
    if (!routeTarget || !origin) return
    const ctrl = new AbortController()
    fetchRoute(origin, routeTarget, ctrl.signal)
      .then((r) => {
        setRoute({ slug: routeTarget.slug, ...r })
        setRouteError(false)
      })
      .catch((e) => {
        if (!ctrl.signal.aborted) setRouteError(true)
        void e
      })
    return () => ctrl.abort()
  }, [routeTarget, origin])

  const startRoute = (p: Pin) => {
    setRoute(null)
    setRouteError(false)
    setRouteFor(p.slug)
    if (!geo.position) geo.locate()
  }
  const clearRoute = () => {
    setRouteFor(null)
    setRoute(null)
    setRouteError(false)
  }
  const activeRoute = route && route.slug === selectedSlug ? route : null
  const routing = routeFor === selectedSlug && !activeRoute && !routeError
  const radiusLabel = radiusM < 1000 ? `${radiusM} m` : `${radiusM / 1000} km`

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
          <MapContainer center={geo.position ?? CENTER} zoom={15} scrollWheelZoom zoomControl={false}>
            <TileLayer
              attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
              url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
            />
            {selected && (
              <Circle
                center={[selected.lat, selected.lng]}
                radius={radiusM}
                pathOptions={{ color: '#2563eb', weight: 2, fillColor: '#2563eb', fillOpacity: 0.12 }}
              />
            )}
            {geofenceOn &&
              visible.map((p) => (
                <Circle
                  key={`fence-${p.key}`}
                  center={[p.lat, p.lng]}
                  radius={p.radiusM}
                  pathOptions={{
                    color: fence.inside.has(p.key) ? '#16a34a' : '#147d78',
                    weight: 1.5,
                    dashArray: '4 4',
                    fillOpacity: fence.inside.has(p.key) ? 0.25 : 0.06,
                  }}
                />
              ))}
            {fence.position && (
              <Marker position={[fence.position.lat, fence.position.lng]} icon={icon('me')} title={t('map.yourLocation')} />
            )}
            {activeRoute && (
              <Polyline positions={activeRoute.path} pathOptions={{ color: '#147d78', weight: 5, opacity: 0.9 }} />
            )}
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
            <FitRoute path={activeRoute?.path ?? null} />
            <FlyTo target={selected && !activeRoute ? [selected.lat, selected.lng] : null} />
            <FlyTo target={!selected && geo.position ? [geo.position.lat, geo.position.lng] : null} />
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
            <button
              type="button"
              className="chip geofence-toggle"
              aria-pressed={geofenceOn}
              onClick={() => setGeofenceOn((v) => !v)}
            >
              📍 {t('map.geofence', { defaultValue: 'Tự động báo khi đến gần' })}
            </button>
            {geofenceOn && fence.error && (
              <p className="geofence-note">
                {fence.error === 'denied'
                  ? t('map.route_denied', { defaultValue: 'Hãy cho phép truy cập vị trí.' })
                  : t('map.geofence_unsupported', { defaultValue: 'Thiết bị không hỗ trợ định vị.' })}
              </p>
            )}
          </div>

          {geofenceOn && enteredPin && (
            <div className="geofence-banner" role="alert">
              <strong>{t('map.geofence_enter', { defaultValue: 'Bạn đang ở gần' })} {enteredPin.name}</strong>
              <div className="detail-actions">
                <Link to={to(enteredPin.detailPath)} state={{ autoplay: true }} className="btn btn-primary">
                  <PlayIcon size={16} /> {t('common.listen')}
                </Link>
                <button
                  type="button"
                  className="btn btn-outline"
                  onClick={() => {
                    setParam('focus', enteredPin.slug)
                    fence.dismiss()
                  }}
                >
                  {t('map.geofence_show', { defaultValue: 'Xem trên bản đồ' })}
                </button>
                <button type="button" className="icon-btn" onClick={fence.dismiss} aria-label={t('map.close')}>
                  <CloseIcon />
                </button>
              </div>
            </div>
          )}

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
                <button
                  type="button"
                  className="btn btn-outline"
                  disabled={routing}
                  onClick={() => (activeRoute ? clearRoute() : startRoute(selected))}
                >
                  <NavigateIcon size={16} />{' '}
                  {activeRoute ? t('map.route_clear', { defaultValue: 'Tắt chỉ đường' }) : t('common.directions')}
                </button>
              </div>
              {routing && (
                <p className="sub">
                  {origin
                    ? t('map.route_loading', { defaultValue: 'Đang tìm đường…' })
                    : t('map.route_locating', { defaultValue: 'Đang xác định vị trí của bạn…' })}
                </p>
              )}
              {routeFor === selectedSlug && !origin && geo.status === 'denied' && (
                <p className="sub">{t('map.route_denied', { defaultValue: 'Hãy cho phép truy cập vị trí để chỉ đường.' })}</p>
              )}
              {routeError && (
                <p className="sub">{t('map.route_error', { defaultValue: 'Không tìm được đường đi.' })}</p>
              )}
              {activeRoute && (
                <p className="sub">
                  🚶 {formatDistance(Math.round(activeRoute.meters), lang)} · {Math.max(1, Math.round(activeRoute.seconds / 60))}{' '}
                  {t('map.minutes', { defaultValue: 'phút' })}
                </p>
              )}

              <div className="radius-control">
                <label htmlFor="radius-slider">
                  {t('map.within')} {radiusLabel} · {nearby.length} {t('map.nearby_places').toLowerCase()}
                </label>
                <input
                  id="radius-slider"
                  type="range"
                  min={RADIUS_MIN_M}
                  max={MapRadiusMaxKm * 1000}
                  step={RADIUS_STEP_M}
                  value={radiusM}
                  onChange={(e) => setRadiusM(Number(e.target.value))}
                />
                {nearby.length > 0 && (
                  <ul className="nearby-list">
                    {nearby.map(({ pin, d }) => (
                      <li key={pin.key}>
                        <button type="button" onClick={() => setParam('focus', pin.slug)}>
                          <span>{pin.name}</span>
                          <span>{formatDistance(Math.round(d), lang)}</span>
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}

function FitRoute({ path }: { path: [number, number][] | null }) {
  const map = useMap()
  useEffect(() => {
    if (path && path.length > 1) map.fitBounds(L.latLngBounds(path), { padding: [60, 60], maxZoom: 17 })
  }, [path, map])
  return null
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
