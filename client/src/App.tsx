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

// Leaflet and Admin screens are lazy-loaded to keep the guest initial bundle small (NFR-02, ADR-05).
const MapPage = lazy(() => import('./pages/MapPage'))
const AdminLoginPage = lazy(() => import('./pages/admin/AdminLoginPage'))
const AdminLayout = lazy(() => import('./pages/admin/AdminLayout'))
const AdminOverviewPage = lazy(() => import('./pages/admin/AdminOverviewPage'))
const AdminPlacesPage = lazy(() => import('./pages/admin/AdminPlacesPage'))
const AdminPlaceEditorPage = lazy(() => import('./pages/admin/AdminPlaceEditorPage'))
const AdminNarrationsPage = lazy(() => import('./pages/admin/AdminNarrationsPage'))
const AdminLanguagesPage = lazy(() => import('./pages/admin/AdminLanguagesPage'))
const AdminUsersPage = lazy(() => import('./pages/admin/AdminUsersPage'))
const AdminUsagePage = lazy(() => import('./pages/admin/AdminUsagePage'))

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Suspense fallback={<Loading />}>
          <Routes>
            <Route path="/" element={<Navigate to={`/${detectLang()}`} replace />} />
            <Route path="/q/:slug" element={<QrEntry />} />

            {/* Admin Dashboard — SCR-A1..A8 (ADR-05) */}
            <Route path="/admin/login" element={<AdminLoginPage />} />
            <Route path="/admin" element={<AdminLayout />}>
              <Route index element={<AdminOverviewPage />} />
              <Route path="places" element={<AdminPlacesPage />} />
              <Route path="places/:id" element={<AdminPlaceEditorPage />} />
              <Route path="narrations" element={<AdminNarrationsPage />} />
              <Route path="languages" element={<AdminLanguagesPage />} />
              <Route path="users" element={<AdminUsersPage />} />
              <Route path="usage" element={<AdminUsagePage />} />
              <Route path="*" element={<NotFoundPage />} />
            </Route>

            {/* Guest Web — SCR-01..08 */}
            <Route path="/:lang" element={<LangLayout />}>
              <Route index element={<HomePage />} />
              <Route path="explore" element={<ExplorePage />} />
              <Route path="food-culture" element={<FoodCulturePage />} />
              <Route path="map" element={<MapPage />} />
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
        </Suspense>
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
