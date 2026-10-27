import { useState, type FormEvent } from 'react'
import { createAdminUser, getAdminUsers, lockAdminUser, unlockAdminUser } from '../../api/admin'
import type { AdminUser } from '../../api/types'
import { CloseIcon } from '../../components/Icons'
import { ErrorState, Loading } from '../../components/States'
import { useAsync } from '../../hooks/useAsync'

/**
 * SCR-A7 — Người dùng (/admin/users).
 */
export default function AdminUsersPage() {
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [searchTerm, setSearchTerm] = useState('')
  const [roleFilter, setRoleFilter] = useState('')
  const [busyId, setBusyId] = useState<number | null>(null)
  const [feedback, setFeedback] = useState<{ type: 'success' | 'danger'; text: string } | null>(null)

  const listState = useAsync(
    `admin:users:${searchTerm}:${roleFilter}:${page}`,
    (signal) => getAdminUsers(searchTerm || undefined, roleFilter || undefined, page, 20, signal),
  )
  const users = listState.data?.items ?? []
  const total = listState.data?.total ?? 0

  // Create Admin modal
  const [modalOpen, setModalOpen] = useState(false)
  const [newEmail, setNewEmail] = useState('')
  const [newFullName, setNewFullName] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [creating, setCreating] = useState(false)
  const [modalError, setModalError] = useState<string | null>(null)

  const handleSearchSubmit = (e: FormEvent) => {
    e.preventDefault()
    setPage(1)
    setSearchTerm(searchInput.trim())
  }

  const handleToggleActive = async (u: AdminUser) => {
    setBusyId(u.id)
    setFeedback(null)
    try {
      const updated = u.isActive ? await lockAdminUser(u.id) : await unlockAdminUser(u.id)
      listState.reload()
      setFeedback({
        type: 'success',
        text: updated.isActive ? `Đã mở khóa tài khoản ${u.email}.` : `Đã khóa tài khoản ${u.email}.`,
      })
    } catch (err) {
      setFeedback({ type: 'danger', text: err instanceof Error ? err.message : 'Thao tác thất bại.' })
    } finally {
      setBusyId(null)
    }
  }

  const handleCreateAdmin = async (e: FormEvent) => {
    e.preventDefault()
    setModalError(null)
    setCreating(true)
    try {
      const created = await createAdminUser({
        email: newEmail.trim(),
        fullName: newFullName.trim(),
        password: newPassword,
      })
      setModalOpen(false)
      setNewEmail('')
      setNewFullName('')
      setNewPassword('')
      setPage(1)
      listState.reload()
      setFeedback({ type: 'success', text: `Đã tạo tài khoản Quản trị viên ${created.email}.` })
    } catch (err) {
      setModalError(err instanceof Error ? err.message : 'Không thể tạo tài khoản Quản trị viên.')
    } finally {
      setCreating(false)
    }
  }

  if (listState.loading && users.length === 0) return <Loading />
  if (listState.error) return <ErrorState error={listState.error} onRetry={listState.reload} />

  const totalPages = Math.max(1, Math.ceil(total / 20))

  return (
    <div className="admin-page">
      <div className="admin-page-header">
        <div>
          <h1>Người dùng</h1>
          <p className="admin-muted">
            Quản lý tài khoản khách hàng và quản trị viên · Luôn giữ ít nhất 1 Quản trị viên hoạt động (BR-11)
          </p>
        </div>
        <button type="button" className="btn btn-primary btn-sm" onClick={() => setModalOpen(true)}>
          + Tạo Quản trị viên
        </button>
      </div>

      {feedback && (
        <div className={`admin-alert admin-alert-${feedback.type}`} role="alert">
          {feedback.text}
        </div>
      )}

      <div className="admin-toolbar">
        <form onSubmit={handleSearchSubmit} className="admin-search-form">
          <input
            type="search"
            className="admin-inline-input"
            placeholder="Tìm theo email hoặc họ tên…"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
          />
          <button type="submit" className="btn btn-outline btn-xs">
            Tìm kiếm
          </button>
        </form>

        <div className="admin-filter-bar" style={{ margin: 0 }}>
          {[
            { value: '', label: 'Tất cả vai trò' },
            { value: 'Admin', label: 'Quản trị viên' },
            { value: 'Customer', label: 'Du khách' },
          ].map((r) => (
            <button
              key={r.value}
              type="button"
              className={`admin-pill ${roleFilter === r.value ? 'active' : ''}`}
              onClick={() => {
                setRoleFilter(r.value)
                setPage(1)
              }}
            >
              {r.label}
            </button>
          ))}
        </div>
      </div>

      <div className="admin-card admin-table-card">
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>MÃ</th>
                <th>EMAIL</th>
                <th>HỌ VÀ TÊN</th>
                <th>VAI TRÒ</th>
                <th>TRẠNG THÁI</th>
                <th>THAO TÁC</th>
              </tr>
            </thead>
            <tbody>
              {users.length === 0 ? (
                <tr>
                  <td colSpan={6} className="admin-empty-cell">
                    Không có tài khoản nào.
                  </td>
                </tr>
              ) : (
                users.map((u) => (
                  <tr key={u.id}>
                    <td>#{u.id}</td>
                    <td>
                      <strong>{u.email}</strong>
                    </td>
                    <td>{u.fullName}</td>
                    <td>
                      <span className={`admin-tag ${u.role === 'Admin' ? '' : 'food'}`}>
                        {u.role === 'Admin' ? 'Quản trị viên' : 'Du khách'}
                      </span>
                    </td>
                    <td>
                      <span className={`admin-badge ${u.isActive ? 'status-published' : 'status-missing'}`}>
                        {u.isActive ? 'Hoạt động' : 'Đã khóa'}
                      </span>
                    </td>
                    <td>
                      <button
                        type="button"
                        className={`btn ${u.isActive ? 'btn-danger-outline' : 'btn-outline'} btn-xs`}
                        onClick={() => void handleToggleActive(u)}
                        disabled={busyId === u.id}
                      >
                        {busyId === u.id ? '…' : u.isActive ? 'Khóa' : 'Mở khóa'}
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {totalPages > 1 && (
          <div className="admin-pagination">
            <button
              type="button"
              className="btn btn-outline btn-xs"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              ← Trước
            </button>
            <span>
              Trang {page} / {totalPages} ({total} tài khoản)
            </span>
            <button
              type="button"
              className="btn btn-outline btn-xs"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Sau →
            </button>
          </div>
        )}
      </div>

      {modalOpen && (
        <div className="admin-modal-backdrop" role="dialog" aria-modal="true" onClick={() => setModalOpen(false)}>
          <div className="admin-modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="admin-modal-header">
              <div>
                <h3>Tạo tài khoản Quản trị viên</h3>
                <p className="admin-muted">Mật khẩu tối thiểu 8 ký tự (BR-12)</p>
              </div>
              <button type="button" className="btn btn-ghost" onClick={() => setModalOpen(false)} aria-label="Đóng">
                <CloseIcon />
              </button>
            </div>

            <form onSubmit={handleCreateAdmin}>
              <div className="field">
                <label htmlFor="u-name">Họ và tên</label>
                <input
                  id="u-name"
                  type="text"
                  value={newFullName}
                  onChange={(e) => setNewFullName(e.target.value)}
                  placeholder="Quản trị viên Quận 6"
                  required
                />
              </div>

              <div className="field">
                <label htmlFor="u-email">Email</label>
                <input
                  id="u-email"
                  type="email"
                  value={newEmail}
                  onChange={(e) => setNewEmail(e.target.value)}
                  placeholder="admin2@tourvoice.local"
                  required
                />
              </div>

              <div className="field">
                <label htmlFor="u-pass">Mật khẩu (≥ 8 ký tự)</label>
                <input
                  id="u-pass"
                  type="password"
                  minLength={8}
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  placeholder="••••••••"
                  required
                />
              </div>

              {modalError && (
                <div className="admin-alert admin-alert-danger" role="alert" style={{ marginBottom: 14 }}>
                  {modalError}
                </div>
              )}

              <div className="admin-modal-actions">
                <button type="button" className="btn btn-outline" onClick={() => setModalOpen(false)}>
                  Hủy
                </button>
                <button type="submit" className="btn btn-primary" disabled={creating}>
                  {creating ? 'Đang tạo…' : 'Tạo Quản trị viên'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
