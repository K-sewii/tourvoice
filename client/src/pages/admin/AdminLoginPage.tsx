import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import { ApiError } from '../../api/http'
import { useAuth } from '../../auth/AuthContext'
import { NoteIcon } from '../../components/Icons'

/** SCR-A1 — Đăng nhập quản trị (/admin/login). Chuyển về Tổng quan (/admin). */
export default function AdminLoginPage() {
  const { user, login, logout } = useAuth()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const returnUrl = params.get('returnUrl')

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  if (user && user.role === 'Admin') {
    const dest = returnUrl && returnUrl.startsWith('/admin') ? returnUrl : '/admin'
    return <Navigate to={dest} replace />
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    if (!email.trim() || !password) {
      setError('Vui lòng nhập đầy đủ email và mật khẩu.')
      return
    }
    setBusy(true)
    try {
      await login(email.trim(), password)
      const raw = localStorage.getItem('tv.user')
      const profile = raw ? (JSON.parse(raw) as { role?: string }) : null
      if (profile?.role !== 'Admin') {
        logout()
        setError('Tài khoản này không có quyền Quản trị viên (Admin).')
        return
      }
      const dest = returnUrl && returnUrl.startsWith('/admin') ? returnUrl : '/admin'
      navigate(dest, { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message || 'Đăng nhập thất bại.')
      } else {
        setError('Không thể kết nối máy chủ.')
      }
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="admin-login-page">
      <div className="auth-card">
        <div className="admin-brand" style={{ marginBottom: 20 }}>
          <span className="admin-brand-icon">
            <NoteIcon size={16} />
          </span>
          <strong>TourVoice</strong> <span>Quản trị</span>
        </div>

        <h1 style={{ fontSize: '1.45rem', marginBottom: 4 }}>Đăng nhập quản trị</h1>
        <p className="admin-muted" style={{ marginBottom: 20 }}>
          Bảng điều khiển quản trị du lịch Quận 6
        </p>

        <form onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label htmlFor="admin-email">Email</label>
            <input
              id="admin-email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="admin@tourvoice.local"
              required
            />
          </div>

          <div className="field">
            <label htmlFor="admin-password">Mật khẩu</label>
            <input
              id="admin-password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
            />
          </div>

          {error && (
            <div className="admin-alert admin-alert-danger" role="alert" style={{ marginBottom: 16 }}>
              {error}
            </div>
          )}

          <button type="submit" className="btn btn-primary btn-block" disabled={busy}>
            {busy ? 'Đang đăng nhập…' : 'Đăng nhập'}
          </button>
        </form>

        <div style={{ marginTop: 20, textAlign: 'center', fontSize: '0.88rem' }}>
          <Link to="/" className="admin-link">
            ← Về trang du khách TourVoice
          </Link>
        </div>
      </div>
    </div>
  )
}
