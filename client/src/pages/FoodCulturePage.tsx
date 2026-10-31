import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { getCatalog, placePath } from '../api/places'
import type { ExploreItem } from '../api/types'
import { PlaceCard } from '../components/PlaceCard'
import { CardSkeletons, Empty, ErrorState } from '../components/States'
import { useAsync } from '../hooks/useAsync'
import { useLang, useLocalePath } from '../hooks/useLang'
import { foodTypeLabel, joinMeta, localizePrice } from '../lib/labels'

/** SCR-08 — food spots grouped by the destination that hosts them. */
export default function FoodCulturePage() {
  const { t } = useTranslation()
  const lang = useLang()
  const to = useLocalePath()
  const [type, setType] = useState('')
  const { data, error, loading, reload } = useAsync(`catalog:${lang}`, (s) => getCatalog(lang, s))

  const foods = useMemo(() => (data ?? []).filter((p) => p.kind === 'Food' && p.parentSlug), [data])
  const types = useMemo(
    () => [...new Set(foods.map((f) => f.foodType).filter((x): x is string => !!x))],
    [foods],
  )

  const groups = useMemo(() => {
    const byParent = new Map<string, { name: string; slug: string; foods: ExploreItem[] }>()
    for (const f of foods) {
      if (type && f.foodType !== type) continue
      const slug = f.parentSlug!
      const g = byParent.get(slug) ?? { name: f.parentName ?? slug, slug, foods: [] }
      g.foods.push(f)
      byParent.set(slug, g)
    }
    return [...byParent.values()]
  }, [foods, type])

  return (
    <div className="container">
      <h1 className="page-title">{t('foodCulture.title')}</h1>
      <p className="page-lead">{t('foodCulture.lead')}</p>

      {types.length > 1 && (
        <div className="chips" role="group" aria-label={t('foodCulture.title')}>
          <button type="button" className="chip" aria-pressed={type === ''} onClick={() => setType('')}>
            {t('explore.all')}
          </button>
          {types.map((ft) => (
            <button key={ft} type="button" className="chip" aria-pressed={type === ft} onClick={() => setType(ft)}>
              {foodTypeLabel(t, ft)}
            </button>
          ))}
        </div>
      )}

      {loading && <CardSkeletons />}
      {!!error && <ErrorState error={error} onRetry={reload} />}
      {data && groups.length === 0 && <Empty>{t('foodCulture.empty')}</Empty>}

      {groups.map((g) => (
        <section key={g.slug} className="section" aria-labelledby={`g-${g.slug}`}>
          <div className="section-head">
            <h2 id={`g-${g.slug}`}>{t('foodCulture.at', { place: g.name })}</h2>
            <Link to={to(`/places/${g.slug}`)} className="link-more">
              {t('common.explore')} ›
            </Link>
          </div>
          <div className="grid">
            {g.foods.map((f) => (
              <PlaceCard
                key={f.id}
                to={to(placePath(f))}
                title={f.name}
                chip={foodTypeLabel(t, f.foodType)}
                meta={joinMeta(localizePrice(f.priceRange, lang), f.openingHours)}
                coverImageUrl={f.coverImageUrl}
              />
            ))}
          </div>
        </section>
      ))}
    </div>
  )
}
