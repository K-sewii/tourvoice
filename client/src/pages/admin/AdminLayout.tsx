import { useState } from 'react'
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'
import { CloseIcon, MenuIcon, NoteIcon } from '../../components/Icons'

const NAV_ITEMS = [
  { to: '/admin', label: 'Tổng quan', end: true },
  { to: '/admin/places', label: 'Địa điểm', end: false },
  { to: '/admin/narrations', label: 'Thuyết minh', end: false },
  { to: '/admin/languages', label: 'Ngôn ngữ', end: false },
  { to: '/admin/users', label: 'Người dùng', end: false },
  { to: '/admin/usage', label: 'Hạn mức', end: false },
]

export default function AdminLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [drawerOpen, setDrawerOpen] = useState(false)

  // Redirect to /admin/login if not signed in as Admin
  if (!user || user.role !== 'Admin') {
    const returnUrl = encodeURIComponent(location.pathname + location.search)
    return (
      <div className="admin-redirect-guard">
        <div className="auth-card">
          <h2>Yêu cầu quyền Quản trị (Admin)</h2>
          <p className="admin-muted" style={{ marginTop: 8, marginBottom: 20 }}>
            {user
              ? `Tài khoản ${user.email} có vai trò ${user.role}, không có quyền truy cập khu vực quản trị.`
              : 'Vui lòng đăng nhập bằng tài khoản Quản trị viên để tiếp tục.'}
          </p>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
            <Link
              to={`/admin/login?returnUrl=${returnUrl}`}
              className="btn btn-primary"
              onClick={() => {
                if (user) logout()
              }}
            >
              Đăng nhập Quản trị
            </Link>
            <Link to="/" className="btn btn-outline">
              Về trang du khách
            </Link>
          </div>
        </div>
      </div>
    )
  }

  const handleSignOut = () => {
    logout()
    navigate('/admin/login', { replace: true })
  }

  return (
    <div className="admin-shell">
      {/* Mobile top bar */}
      <header className="admin-mobile-bar">
        <button
          type="button"
          className="btn btn-ghost"
          aria-label="Mở menu quản trị"
          onClick={() => setDrawerOpen(true)}
        >
          <MenuIcon />
        </button>
        <Link to="/admin" className="admin-brand">
          <span className="admin-brand-icon">
            <NoteIcon size={16} />
          </span>
          <strong>TourVoice</strong> <span>Quản trị</span>
        </Link>
      </header>

      {drawerOpen && <div className="admin-backdrop" onClick={() => setDrawerOpen(false)} />}

      <aside className={`admin-sidebar ${drawerOpen ? 'open' : ''}`} aria-label="Điều hướng quản trị">
        <div className="admin-sidebar-top">
          <Link to="/admin" className="admin-brand" onClick={() => setDrawerOpen(false)}>
            <span className="admin-brand-icon">
              <NoteIcon size={16} />
            </span>
            <strong>TourVoice</strong> <span>Quản trị</span>
          </Link>
          <button
            type="button"
            className="btn btn-ghost admin-drawer-close"
            aria-label="Đóng menu"
            onClick={() => setDrawerOpen(false)}
          >
            <CloseIcon />
          </button>
        </div>

        <nav className="admin-nav">
          {NAV_ITEMS.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              onClick={() => setDrawerOpen(false)}
              className={({ isActive }) => `admin-nav-link ${isActive ? 'active' : ''}`}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="admin-sidebar-footer">
          <div className="admin-user-meta" title={user.email}>
            <div className="admin-user-name">{user.fullName}</div>
            <div className="admin-user-email">{user.email}</div>
          </div>
          <button type="button" className="admin-signout-btn" onClick={handleSignOut}>
            Đăng xuất
          </button>
        </div>
      </aside>

      <main className="admin-main" id="admin-main" tabIndex={-1}>
        <Outlet />
      </main>
    </div>
  )
}
