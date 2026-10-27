import { getUsage } from '../../api/admin'
import { ErrorState, Loading } from '../../components/States'
import { useAsync } from '../../hooks/useAsync'

function formatVnTime(iso: string): string {
  try {
    const dt = new Date(iso.endsWith('Z') ? iso : `${iso}Z`)
    return new Intl.DateTimeFormat('vi-VN', {
      timeZone: 'Asia/Ho_Chi_Minh',
      dateStyle: 'short',
      timeStyle: 'medium',
    }).format(dt)
  } catch {
    return iso
  }
}

/**
 * SCR-A8 — Hạn mức dịch máy (/admin/usage).
 */
export default function AdminUsagePage() {
  const usageState = useAsync('admin:usage', (signal) => getUsage(signal))
  const data = usageState.data

  if (usageState.loading && !data) return <Loading />
  if (usageState.error || !data) return <ErrorState error={usageState.error} onRetry={usageState.reload} />

  const { quota, recent } = data
  const avgPercent = Math.round((quota.average7Days / quota.dailyLimit) * 1000) / 10
  const blocked = quota.usedToday >= quota.blockThreshold

  return (
    <div className="admin-page">
      <div className="admin-page-header">
        <div>
          <h1>Hạn mức dịch máy</h1>
          <p className="admin-muted">
            Hạn mức dịch máy MyMemory hôm nay (UTC) và nhật ký 50 lần gọi gần nhất (BR-06)
          </p>
        </div>
        <button type="button" className="btn btn-outline btn-sm" onClick={usageState.reload}>
          Làm mới
        </button>
      </div>

      {blocked && (
        <div className="admin-alert admin-alert-danger" role="alert">
          Đã chạm ngưỡng chặn 90% ({quota.blockThreshold.toLocaleString()} / {quota.dailyLimit.toLocaleString()} ký tự). Hệ thống tạm chặn gọi MyMemory hôm nay để giữ chi phí 0đ (BR-06).
        </div>
      )}

      <div className="admin-card" style={{ marginBottom: 20 }}>
        <h2 className="admin-card-title">Hạn mức MyMemory</h2>

        <div className="admin-quota-row">
          <div className="admin-quota-head">
            <span>
              Hôm nay · <strong>{quota.usedToday.toLocaleString()}</strong> / {quota.dailyLimit.toLocaleString()} ký tự
            </span>
            <strong>{quota.percent}%</strong>
          </div>
          <div className="admin-progress-track">
            <div
              className={`admin-progress-fill ${blocked ? 'danger' : ''}`}
              style={{ width: `${Math.min(100, quota.percent)}%` }}
            />
            <div className="admin-quota-threshold" style={{ left: '90%' }} title="Ngưỡng chặn 90% (45.000 ký tự)" />
          </div>
        </div>

        <div className="admin-quota-row" style={{ marginTop: 16 }}>
          <div className="admin-quota-head">
            <span>
              Trung bình 7 ngày · <strong>{quota.average7Days.toLocaleString()}</strong> / {quota.dailyLimit.toLocaleString()} ký tự
            </span>
            <strong>{avgPercent}%</strong>
          </div>
          <div className="admin-progress-track">
            <div className="admin-progress-fill" style={{ width: `${Math.min(100, avgPercent)}%` }} />
            <div className="admin-quota-threshold" style={{ left: '90%' }} title="Ngưỡng chặn 90% (45.000 ký tự)" />
          </div>
        </div>

        <p className="admin-muted" style={{ marginTop: 14, fontSize: '0.86rem' }}>
          Hệ thống tự động dừng gọi ở mức 90% hạn mức ngày = {quota.blockThreshold.toLocaleString()} ký tự (BR-06). Nghe thuyết minh trên thiết bị du khách không tốn hạn mức.
        </p>
      </div>

      <div className="admin-card admin-table-card">
        <div style={{ padding: '16px 20px 0' }}>
          <h2 className="admin-card-title">Nhật ký 50 lần gọi MyMemory gần nhất</h2>
        </div>
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>THỜI GIAN (GIỜ VIỆT NAM)</th>
                <th>SỐ KÝ TỰ</th>
                <th>SỐ ĐOẠN (≤ 500B)</th>
                <th>KẾT QUẢ</th>
                <th>MÃ LỖI</th>
              </tr>
            </thead>
            <tbody>
              {recent.length === 0 ? (
                <tr>
                  <td colSpan={5} className="admin-empty-cell">
                    Chưa có lượt gọi MyMemory nào được ghi nhận.
                  </td>
                </tr>
              ) : (
                recent.map((log, i) => (
                  <tr key={`${log.createdAt}-${i}`}>
                    <td>{formatVnTime(log.createdAt)}</td>
                    <td>
                      <strong>{log.characters.toLocaleString()}</strong>
                    </td>
                    <td>{log.chunks}</td>
                    <td>
                      <span className={`admin-badge ${log.success ? 'status-published' : 'status-missing'}`}>
                        {log.success ? 'Thành công' : 'Thất bại'}
                      </span>
                    </td>
                    <td>
                      {log.errorCode ? <code>{log.errorCode}</code> : <span className="admin-muted">—</span>}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
