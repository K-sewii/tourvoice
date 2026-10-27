import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import { getCatalog, placePath } from '../api/places'
import { PlaceCard } from '../components/PlaceCard'
import { CardSkeletons, Empty, ErrorState } from '../components/States'
import { useAsync } from '../hooks/useAsync'
import { useDistanceFormatter } from '../hooks/useGeolocation'
import { useLang, useLocalePath } from '../hooks/useLang'
import { categoryLabel, foodTypeLabel, joinMeta, matches } from '../lib/labels'

const CATEGORIES = ['Heritage', 'Market', 'FoodStreet', 'Park'] as const

/** SCR-07 — /:lang/explore?category=&q= */
export default function ExplorePage() {
  const { t } = useTranslation()
  const lang = useLang()
  const to = useLocalePath()
  const distance = useDistanceFormatter(lang)
  const [params, setParams] = useSearchParams()
  const category = params.get('category') ?? ''
  const q = params.get('q') ?? ''
  const { data, error, loading, reload } = useAsync(`catalog:${lang}`, (s) => getCatalog(lang, s))

  const setParam = (key: string, value: string) => {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    setParams(next, { replace: true })
  }

  const items = useMemo(
    () =>
      (data ?? [])
        .filter((p) =>
          !category ? true : category === 'food' ? p.kind === 'Food' : p.kind === 'Destination' && p.category === category,
        )
        .filter((p) => !q || matches(`${p.name} ${p.slug} ${p.parentName ?? ''}`, q))
        .map((p) => {
          const isFood = p.kind === 'Food'
          return {
            key: `${p.kind}${p.id}`,
            to: to(placePath(p)),
            title: p.name,
            chip: isFood ? foodTypeLabel(t, p.foodType) : categoryLabel(t, p.category),
            meta: isFood
              ? joinMeta(p.priceRange, p.openingHours, distance(p.latitude, p.longitude))
              : joinMeta(p.address?.split(',')[0], p.openingHours, distance(p.latitude, p.longitude)),
            cover: p.coverImageUrl,
            audio: !isFood,
          }
        }),
    [category, data, distance, q, t, to],
  )

  const chips = [
    { value: '', label: t('explore.all') },
    { value: 'food', label: t('explore.food') },
    ...CATEGORIES.map((c) => ({ value: c, label: categoryLabel(t, c)! })),
  ]

  return (
    <div className="container">
      <h1 className="page-title">{t('explore.title')}</h1>

      <div className="search">
        <label className="visually-hidden" htmlFor="explore-q">
          {t('explore.searchLabel')}
        </label>
        <input
          id="explore-q"
          type="search"
          placeholder={t('explore.search')}
          value={q}
          onChange={(e) => setParam('q', e.target.value)}
          autoComplete="off"
        />
      </div>

      <div className="chips" role="group" aria-label={t('explore.title')}>
        {chips.map((c) => (
          <button
            key={c.value || 'all'}
            type="button"
            className="chip"
            aria-pressed={category === c.value}
            onClick={() => setParam('category', c.value)}
          >
            {c.label}
          </button>
        ))}
      </div>

      {loading && <CardSkeletons count={4} />}
      {!!error && <ErrorState error={error} onRetry={reload} />}
      {data && (
        <>
          <p className="result-count" aria-live="polite">
            {t('explore.results', { count: items.length })}
          </p>
          {items.length === 0 ? (
            <Empty>{t('explore.noResults')}</Empty>
          ) : (
            <div className="grid">
              {items.map((i) => (
                <PlaceCard
                  key={i.key}
                  to={i.to}
                  title={i.title}
                  chip={i.chip}
                  meta={i.meta}
                  coverImageUrl={i.cover}
                  audioBadge={i.audio}
                />
              ))}
            </div>
          )}
        </>
      )}
    </div>
  )
}
