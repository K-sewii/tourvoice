import { lazy, Suspense, useEffect, useLayoutEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { BrowserRouter, Navigate, Outlet, Route, Routes, useLocation, useParams } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth/AuthContext'
import { Header } from './components/Header'
import { Loading } from './components/States'
import { usePresence } from './hooks/usePresence'
import { markQrEntry } from './lib/deviceId'
import { detectLang, isLang, saveLang } from './lib/languages'
import AccountPage from './pages/AccountPage'
import AuthPage from './pages/AuthPage'
import ExplorePage from './pages/ExplorePage'
import FoodCulturePage from './pages/FoodCulturePage'
import HomePage from './pages/HomePage'
import NotFoundPage from './pages/NotFoundPage'
import PlaceDetailPage from './pages/PlaceDetailPage'

// Leaflet is the heaviest dependency; keep it out of the initial bundle (NFR-02).
const MapPage = lazy(() => import('./pages/MapPage'))

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Navigate to={`/${detectLang()}`} replace />} />
          <Route path="/q/:slug" element={<QrEntry />} />
          <Route path="/:lang" element={<LangLayout />}>
            <Route index element={<HomePage />} />
            <Route path="explore" element={<ExplorePage />} />
            <Route path="food-culture" element={<FoodCulturePage />} />
            <Route
              path="map"
              element={
                <Suspense fallback={<Loading />}>
                  <MapPage />
                </Suspense>
              }
            />
            <Route path="places/:slug" element={<PlaceDetailPage />} />
            <Route path="places/:slug/foods/:foodSlug" element={<PlaceDetailPage />} />
            <Route path="auth" element={<AuthPage />} />
            <Route
              path="account"
              element={
                <RequireAuth>
                  <AccountPage />
                </RequireAuth>
              }
            />
            <Route path="*" element={<NotFoundPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}

/** Validates `:lang`, syncs i18next + <html lang>, and renders the shell. */
function LangLayout() {
  // A layout route still sees its children's params, so we know which place page is open.
  const { lang, slug, foodSlug } = useParams()
  const { pathname, search, hash } = useLocation()
  const { i18n } = useTranslation()
  const valid = isLang(lang)

  // Live-visitor heartbeat on every guest page (SD-04), tagged with the place when on one.
  usePresence(valid ? (foodSlug ?? slug) : undefined, valid ? lang : 'vi')

  useLayoutEffect(() => {
    if (!valid) return
    saveLang(lang)
    if (i18n.language !== lang) void i18n.changeLanguage(lang)
    document.documentElement.lang = lang
  }, [i18n, lang, valid])

  // "/foo" (no language) → "/<detected>/foo"
  if (!valid) return <Navigate to={`/${detectLang()}${pathname}${search}${hash}`} replace />

  return (
    <>
      <Header />
      <main id="main" tabIndex={-1}>
        <Outlet />
      </main>
      {!pathname.endsWith('/map') && (
        <footer className="footer">
          <div className="container">TourVoice · Quận 6 · TP. Hồ Chí Minh</div>
        </footer>
      )}
    </>
  )
}

/** SCR-09 — QR codes encode /q/{slug} without a language (BR-18). */
function QrEntry() {
  const { slug = '' } = useParams()
  useEffect(() => markQrEntry(), [])
  return <Navigate to={`/${detectLang()}/places/${encodeURIComponent(slug)}?src=qr`} replace />
}

function RequireAuth({ children }: { children: React.ReactNode }) {
  const { user } = useAuth()
  const { pathname, search } = useLocation()
  const { lang } = useParams()
  if (!user) return <Navigate to={`/${lang}/auth?returnUrl=${encodeURIComponent(pathname + search)}`} replace />
  return children
}
