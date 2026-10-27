import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { useLocalePath } from '../hooks/useLang'

export default function NotFoundPage() {
  const { t } = useTranslation()
  const to = useLocalePath()
  return (
    <div className="container">
      <div className="state">
        <h1 className="page-title">{t('notFound.title')}</h1>
        <Link to={to()} className="btn btn-primary">
          {t('notFound.back')}
        </Link>
      </div>
    </div>
  )
}
