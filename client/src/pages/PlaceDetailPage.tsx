import { useCallback, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Navigate, useLocation, useNavigate, useParams } from 'react-router-dom'
import { ApiError } from '../api/http'
import { recordListen } from '../api/listen'
import * as me from '../api/me'
import { getPlace, placePath } from '../api/places'
import type { PlaceDetail } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { CheckIcon, HeartIcon, NavigateIcon, PlayIcon } from '../components/Icons'
import { NarrationPlayer, type NarrationPlayerHandle } from '../components/NarrationPlayer'
import { PlaceCard } from '../components/PlaceCard'
import { Empty, ErrorState, Loading } from '../components/States'
import { useAsync } from '../hooks/useAsync'
import { useDistanceFormatter } from '../hooks/useGeolocation'
import { useLang, useLocalePath } from '../hooks/useLang'
import { directionsUrl } from '../lib/geo'
import { categoryLabel, foodTypeLabel, joinMeta } from '../lib/labels'
import { LANG_INFO, isLang } from '../lib/languages'

/**
 * SCR-02 destination detail (/:lang/places/:slug) and
 * SCR-03 food spot detail (/:lang/places/:slug/foods/:foodSlug).
 */
export default function PlaceDetailPage() {
  const { t } = useTranslation()
  const lang = useLang()
  const { slug = '', foodSlug } = useParams()
  const targetSlug = foodSlug ?? slug
  const to = useLocalePath()
  const { user } = useAuth()
  const { state, search } = useLocation() as { state: { autoplay?: boolean } | null; search: string }

  // The token changes isFavorite/isVisited in the response, so it is part of the key.
  const place = useAsync(`place:${targetSlug}:${lang}:${user?.email ?? ''}`, (s) => getPlace(targetSlug, lang, s))

  if (place.loading) return <Loading />
  if (place.error) {
    if (place.error instanceof ApiError && place.error.status === 404)
      return (
        <div className="container">
          <Empty>{t('place.notFound')}</Empty>
        </div>
      )
    return (
      <div className="container">
        <ErrorState error={place.error} onRetry={place.reload} />
      </div>
    )
  }
  if (!place.data) return null

  // Canonical URL: food spots live at /places/<parent>/foods/<food>. Fix a wrong parent, a food spot
  // opened as /places/<food> (e.g. its QR code), or a destination opened under /foods/.
  const canonical = placePath(place.data)
  const current = foodSlug ? `/places/${slug}/foods/${foodSlug}` : `/places/${slug}`
  if (canonical !== current) return <Navigate to={{ pathname: to(canonical), search }} state={state} replace />

  return (
    <PlaceView
      key={`${place.data.slug}:${place.data.languageCode}`}
      place={place.data}
      autoplay={!!state?.autoplay}
    />
  )
}

function PlaceView({ place, autoplay }: { place: PlaceDetail; autoplay: boolean }) {
  const { t } = useTranslation()
  const lang = useLang()
  const to = useLocalePath()
  const distance = useDistanceFormatter(lang)
  const isFood = place.kind === 'Food'
  const contentLang = isLang(place.languageCode) ? place.languageCode : 'vi'
  const chip = isFood ? foodTypeLabel(t, place.foodType) : categoryLabel(t, place.category)
  const away = distance(place.latitude, place.longitude)

  const onListened = useCallback(
    (seconds: number) =>
      void recordListen({ placeSlug: place.slug, languageCode: contentLang, listenedSeconds: seconds }),
    [contentLang, place.slug],
  )

  const player = useRef<NarrationPlayerHandle>(null)
  const listenNow = (e: React.MouseEvent) => {
    e.preventDefault()
    document.getElementById('listen')?.scrollIntoView({ behavior: 'smooth', block: 'center' })
    // Start (never toggle off) inside this click — the user gesture iOS requires for speech.
    player.current?.start()
  }

  return (
    <article>
      <div className="container" style={{ padding: 0 }}>
        <div className="detail-cover">
          {place.coverImageUrl && <img src={place.coverImageUrl} alt="" />}
          {chip && <span className="chip-cover">{chip}</span>}
        </div>
      </div>

      <div className="container">
        <div className="detail-layout">
          <div>
            {place.parentSlug && (
              <p className="breadcrumb">
                <Link to={to(`/places/${place.parentSlug}`)}>‹ {place.parentName ?? place.parentSlug}</Link>
              </p>
            )}
            <h1 className="detail-title">{place.name}</h1>
            {(place.address || away) && (
              <p className="detail-sub">{joinMeta(place.address, away && t('common.away', { distance: away }))}</p>
            )}

            <div className="detail-actions">
              <a href="#listen" className="btn btn-primary btn-lg" onClick={listenNow}>
                <PlayIcon size={18} /> {t('common.listenAudio')}
              </a>
              <a
                href={directionsUrl(place.latitude, place.longitude)}
                target="_blank"
                rel="noopener noreferrer"
                className="btn btn-outline btn-lg"
              >
                <NavigateIcon size={18} /> {t('common.directions')}
              </a>
            </div>

            <MyPlaceActions place={place} />

            {place.fallbackUsed && (
              <p className="notice" role="status">
                {t('place.fallback', {
                  requested: LANG_INFO[lang].label,
                  used: isLang(place.languageCode) ? LANG_INFO[place.languageCode].label : place.languageCode,
                })}
              </p>
            )}

            {(place.openingHours || place.priceRange) && (
              <dl className="facts">
                {place.openingHours && (
                  <div>
                    <dt>{t('place.hours')}</dt>
                    <dd>{place.openingHours}</dd>
                  </div>
                )}
                {place.priceRange && (
                  <div>
                    <dt>{t('place.price')}</dt>
                    <dd>{place.priceRange}</dd>
                  </div>
                )}
              </dl>
            )}

            <section className="section" aria-labelledby="about">
              <h2 id="about" className="section-head" style={{ fontSize: '1.3rem' }}>
                {t('place.about')}
              </h2>
              {place.narrationText ? (
                <p className="prose" lang={contentLang}>
                  {place.narrationText}
                </p>
              ) : (
                <p className="card-meta">{t('place.noNarration')}</p>
              )}
            </section>

            {!isFood && (
              <section className="section" aria-labelledby="foods">
                <div className="section-head">
                  <h2 id="foods" style={{ fontSize: '1.3rem' }}>
                    {t('place.foodSpots')}
                  </h2>
                </div>
                {place.foodPlaces.length > 0 ? (
                  <div className="grid">
                    {place.foodPlaces.map((f) => (
                      <PlaceCard
                        key={f.id}
                        to={to(`/places/${place.slug}/foods/${f.slug}`)}
                        title={f.name}
                        chip={foodTypeLabel(t, f.foodType)}
                        meta={joinMeta(f.priceRange, f.openingHours)}
                        coverImageUrl={f.coverImageUrl}
                      />
                    ))}
                  </div>
                ) : (
                  <p className="card-meta">{t('place.noFoodSpots')}</p>
                )}
              </section>
            )}

            <Link to={to(`/map?focus=${place.slug}`)} className="row-link">
              <span>{t('place.mapAndDirections')}</span>
              <span aria-hidden="true">›</span>
            </Link>
          </div>

          <aside className="detail-aside">
            <NarrationPlayer
              ref={player}
              text={place.narrationText}
              contentLang={contentLang}
              autoplay={autoplay}
              onListened={onListened}
            />
          </aside>
        </div>
      </div>
    </article>
  )
}

/**
 * Favorite + visited toggles (Customer only; guests are sent to sign in and brought back).
 * Initial state comes from the detail response (isFavorite/isVisited); after a successful
 * call the local state flips — no refetch, so nothing flickers.
 */
function MyPlaceActions({ place }: { place: PlaceDetail }) {
  const { t } = useTranslation()
  const { user } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const to = useLocalePath()
  const [busy, setBusy] = useState(false)
  const [favorite, setFavorite] = useState(!!place.isFavorite)
  const [visited, setVisited] = useState(!!place.isVisited)

  const requireLogin = () =>
    navigate(to(`/auth?returnUrl=${encodeURIComponent(location.pathname + location.search)}`))

  const run = async (action: () => Promise<void>, onDone: () => void) => {
    if (!user) return requireLogin()
    setBusy(true)
    try {
      await action()
      onDone()
    } catch (err) {
      // AuthProvider already signed out on 401; send the user to sign in and back here.
      if (err instanceof ApiError && err.status === 401) requireLogin()
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="secondary-actions">
      <button
        type="button"
        className="btn btn-ghost"
        aria-pressed={favorite}
        disabled={busy}
        onClick={() =>
          run(
            () => (favorite ? me.removeFavorite(place.id) : me.addFavorite(place.id)),
            () => setFavorite(!favorite),
          )
        }
      >
        <HeartIcon size={18} filled={favorite} /> {favorite ? t('place.unfavorite') : t('place.favorite')}
      </button>
      <button
        type="button"
        className="btn btn-ghost"
        aria-pressed={visited}
        disabled={busy || visited}
        onClick={() => run(() => me.markVisited(place.id), () => setVisited(true))}
      >
        <CheckIcon size={18} /> {visited ? t('place.visited') : t('place.markVisited')}
      </button>
    </div>
  )
}
