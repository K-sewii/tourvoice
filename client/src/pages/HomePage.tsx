import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { getDestinations } from '../api/places'
import { DiamondIcon, NavigateIcon, NoteIcon } from '../components/Icons'
import { PlaceCard } from '../components/PlaceCard'
import { CardSkeletons, ErrorState } from '../components/States'
import { useAsync } from '../hooks/useAsync'
import { useDistanceFormatter } from '../hooks/useGeolocation'
import { useLang, useLocalePath } from '../hooks/useLang'
import { categoryLabel, joinMeta } from '../lib/labels'

/** SCR-01 */
export default function HomePage() {
  const { t } = useTranslation()
  const lang = useLang()
  const to = useLocalePath()
  const distance = useDistanceFormatter(lang)
  const { data, error, loading, reload } = useAsync(`dest:${lang}`, (s) => getDestinations(lang, s))

  const heroCover = data?.find((d) => d.slug === 'cho-binh-tay')?.coverImageUrl ?? data?.find((d) => d.coverImageUrl)?.coverImageUrl

  return (
    <div className="container">
      <section className="hero">
        <div>
          <p className="eyebrow">{t('home.eyebrow')}</p>
          <h1>
            {t('home.title1')}
            <br />
            {t('home.title2')}
          </h1>
          <p className="lead">{t('home.lead')}</p>
          <div className="hero-actions">
            <Link to={to('/explore')} className="btn btn-primary btn-lg">
              {t('home.ctaExplore')}
            </Link>
            <Link to={to('/map')} className="btn btn-outline btn-lg">
              {t('home.ctaMap')}
            </Link>
          </div>
          <p className="hero-note">♪ {t('home.voiceNote')}</p>
        </div>
        <div className="hero-media">
          {heroCover ? <img src={heroCover} alt={t('home.heroAlt')} /> : <span>{t('home.heroAlt')}</span>}
        </div>
      </section>

      <section className="section" aria-labelledby="featured">
        <div className="section-head">
          <h2 id="featured">{t('home.featured')}</h2>
          <Link to={to('/explore')} className="link-more">
            {t('home.seeAll')} ›
          </Link>
        </div>
        {loading && <CardSkeletons />}
        {!!error && <ErrorState error={error} onRetry={reload} />}
        {data && (
          <div className="grid">
            {data.slice(0, 3).map((p) => (
              <PlaceCard
                key={p.id}
                to={to(`/places/${p.slug}`)}
                title={p.name}
                chip={categoryLabel(t, p.category)}
                meta={joinMeta(p.address?.split(',')[0], distance(p.latitude, p.longitude))}
                coverImageUrl={p.coverImageUrl}
                audioBadge
              />
            ))}
          </div>
        )}
      </section>

      <section className="section" aria-labelledby="why">
        <div className="section-head">
          <h2 id="why">{t('home.why')}</h2>
        </div>
        <div className="features">
          <Feature icon={<DiamondIcon />} title={t('home.why1Title')} text={t('home.why1Text')} />
          <Feature icon={<NoteIcon />} title={t('home.why2Title')} text={t('home.why2Text')} />
          <Feature icon={<NavigateIcon />} title={t('home.why3Title')} text={t('home.why3Text')} />
        </div>
      </section>
    </div>
  )
}

function Feature({ icon, title, text }: { icon: React.ReactNode; title: string; text: string }) {
  return (
    <div className="feature">
      <span className="feature-icon">{icon}</span>
      <div>
        <h3>{title}</h3>
        <p>{text}</p>
      </div>
    </div>
  )
}
