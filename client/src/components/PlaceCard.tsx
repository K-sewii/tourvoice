import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { PlayIcon } from './Icons'

interface Props {
  to: string
  title: string
  chip?: string | null
  meta?: string | null
  coverImageUrl?: string | null
  /** Show the "♪ Audio · 5" badge (destinations). */
  audioBadge?: boolean
}

export function PlaceCard({ to, title, chip, meta, coverImageUrl, audioBadge }: Props) {
  const { t } = useTranslation()
  return (
    <article className="card">
      <div className="card-cover">
        {coverImageUrl && <img src={coverImageUrl} alt="" loading="lazy" />}
        {chip && <span className="chip-cover">{chip}</span>}
        {audioBadge && <span className="badge-audio">♪ {t('common.audioBadge')}</span>}
      </div>
      <div className="card-body">
        <h3 className="card-title">
          <Link to={to}>{title}</Link>
        </h3>
        {meta && <p className="card-meta">{meta}</p>}
        <div className="card-foot">
          <Link to={to} className="link-more" tabIndex={-1} aria-hidden="true">
            {t('common.explore')} ›
          </Link>
          <Link to={to} state={{ autoplay: true }} className="btn btn-soft" aria-label={`${t('common.listen')}: ${title}`}>
            <PlayIcon size={16} /> {t('common.listen')}
          </Link>
        </div>
      </div>
    </article>
  )
}
