import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import * as me from '../api/me'
import { placePath } from '../api/places'
import { useAuth } from '../auth/AuthContext'
import { ErrorState, Loading } from '../components/States'
import { useAsync } from '../hooks/useAsync'
import { useLang, useLocalePath } from '../hooks/useLang'
import { LANG_INFO, isLang } from '../lib/languages'

/** SCR-06 — favorites, visited progress (BR-10, computed by the API) and listening history. */
export default function AccountPage() {
  const { t } = useTranslation()
  const lang = useLang()
  const to = useLocalePath()
  const navigate = useNavigate()
  const { user, logout } = useAuth()

  const data = useAsync(user ? `account:${user.email}:${lang}` : null, async () => {
    const [favorites, visits, history] = await Promise.all([
      me.getFavorites(lang),
      me.getVisits(lang),
      me.getHistory(lang),
    ])
    return { favorites, visits, history }
  })

  const signOut = () => {
    logout()
    navigate(to(), { replace: true })
  }

  const fmtDate = (iso: string) =>
    new Date(iso).toLocaleString(lang, { dateStyle: 'medium', timeStyle: 'short' })

  const visited = data.data?.visits.visitedDestinations ?? 0
  const total = data.data?.visits.publishedDestinations ?? 0

  return (
    <div className="container">
      <h1 className="page-title">{t('account.title')}</h1>

      <div className="profile">
        <span className="avatar">{user?.fullName?.trim()?.[0]?.toUpperCase() ?? '?'}</span>
        <div className="grow">
          <strong>{user?.fullName}</strong>
          <div className="card-meta">{user?.email}</div>
        </div>
        <button type="button" className="btn btn-outline" onClick={signOut}>
          {t('account.signOut')}
        </button>
      </div>

      {data.loading && <Loading />}
      {!!data.error && <ErrorState error={data.error} onRetry={data.reload} />}
      {data.data && (
        <div className="account-grid">
          <section aria-labelledby="visited">
            <h2 id="visited">{t('account.visited')}</h2>
            <p>{t('account.progress', { count: visited, total })}</p>
            <div
              className="progress-bar"
              role="progressbar"
              aria-valuemin={0}
              aria-valuemax={total}
              aria-valuenow={visited}
              aria-label={t('account.visited')}
            >
              <span style={{ width: `${total ? (visited / total) * 100 : 0}%` }} />
            </div>
            {data.data.visits.items.length === 0 ? (
              <p className="card-meta">{t('account.noVisits')}</p>
            ) : (
              <ul className="list" style={{ marginTop: 12 }}>
                {data.data.visits.items.map((v) => (
                  <li key={v.place.placeId}>
                    <Link to={to(placePath(v.place))}>
                      <span>{v.place.name}</span>
                      <span className="sub">{fmtDate(v.visitedAt)}</span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section aria-labelledby="favorites">
            <h2 id="favorites">{t('account.favorites')}</h2>
            {data.data.favorites.length === 0 ? (
              <p className="card-meta">{t('account.noFavorites')}</p>
            ) : (
              <ul className="list">
                {data.data.favorites.map((f) => (
                  <li key={f.place.placeId}>
                    <Link to={to(placePath(f.place))}>
                      <span>{f.place.name}</span>
                      <span className="sub">{fmtDate(f.createdAt)}</span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section aria-labelledby="history">
            <h2 id="history">{t('account.history')}</h2>
            {data.data.history.length === 0 ? (
              <p className="card-meta">{t('account.noHistory')}</p>
            ) : (
              <ul className="list">
                {data.data.history.map((h, i) => {
                  const langLabel = isLang(h.languageCode) ? LANG_INFO[h.languageCode].label : h.languageCode
                  return (
                    <li key={`${h.place.placeId}-${h.playedAt}-${i}`}>
                      <Link to={to(placePath(h.place))}>
                        <span>{h.place.name}</span>
                        <span className="sub">
                          {langLabel} · {t('account.seconds', { count: h.listenedSeconds })} · {fmtDate(h.playedAt)}
                        </span>
                      </Link>
                    </li>
                  )
                })}
              </ul>
            )}
          </section>
        </div>
      )}
    </div>
  )
}
