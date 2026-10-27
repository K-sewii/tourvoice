import { useTranslation } from 'react-i18next'
import { ApiError } from '../api/http'

export function Loading({ label }: { label?: string }) {
  const { t } = useTranslation()
  return (
    <div className="state" role="status" aria-live="polite">
      <div className="spinner" />
      <span>{label ?? t('common.loading')}</span>
    </div>
  )
}

export function CardSkeletons({ count = 3 }: { count?: number }) {
  const { t } = useTranslation()
  return (
    <div className="grid" role="status" aria-label={t('common.loading')}>
      {Array.from({ length: count }, (_, i) => (
        <div key={i} className="skeleton" style={{ height: 300 }} />
      ))}
    </div>
  )
}

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const { t } = useTranslation()
  const detail = error instanceof ApiError && error.status !== 0 ? ` (${error.status})` : ''
  return (
    <div className="state" role="alert">
      <p>
        {t('common.error')}
        {detail}
      </p>
      {onRetry && (
        <button type="button" className="btn btn-outline" onClick={onRetry}>
          {t('common.retry')}
        </button>
      )}
    </div>
  )
}

export function Empty({ children }: { children: React.ReactNode }) {
  return <div className="state">{children}</div>
}
