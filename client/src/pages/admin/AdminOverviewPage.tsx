import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { getAccessStats, getDashboard, getNarrationMatrix } from '../../api/admin'
import { ErrorState, Loading } from '../../components/States'
import { useAdminPresence } from '../../hooks/useAdminPresence'
import { useAsync } from '../../hooks/useAsync'

function formatCompact(n: number): string {
  if (n >= 1000) return `${Math.round(n / 100) / 10}k`
  return String(n)
}

/**
 * SCR-A2 — Tổng quan quản trị (/admin).
 */
export default function AdminOverviewPage() {
  const [daysRange, setDaysRange] = useState<number>(14)

  const overview = useAsync('admin:overview', async (signal) => {
    const [dashboard, matrix] = await Promise.all([
      getDashboard(signal),
      getNarrationMatrix('all', signal),
    ])
    return { dashboard, matrix }
  })

  const customStats = useAsync(
    daysRange === 14 ? null : `admin:stats:${daysRange}`,
    (signal) => getAccessStats(daysRange, signal),
  )

  const dashboard = overview.data?.dashboard
  const matrix = overview.data?.matrix
  const { snapshot } = useAdminPresence(dashboard?.live ?? 0)

  const activeDays = useMemo(
    () => (daysRange === 14 ? (dashboard?.days ?? []) : (customStats.data?.days ?? dashboard?.days ?? [])),
    [customStats.data?.days, dashboard?.days, daysRange],
  )
  const totalListens = useMemo(() => activeDays.reduce((s, d) => s + d.listens, 0), [activeDays])
  const totalQrScans = useMemo(() => activeDays.reduce((s, d) => s + d.qrScans, 0), [activeDays])
  const totalSessions = useMemo(() => activeDays.reduce((s, d) => s + d.sessions, 0), [activeDays])
  const maxListens = useMemo(() => Math.max(1, ...activeDays.map((d) => d.listens)), [activeDays])

  const placeNamesBySlug = useMemo(() => {
    const map: Record<string, string> = {}
    for (const row of matrix?.rows ?? []) {
      map[row.slug] = row.name
    }
    return map
  }, [matrix])

  const missingPlacesCount = useMemo(() => {
    if (!matrix) return 0
    return matrix.rows.filter((r) => matrix.languages.some((l) => r.cells[l.code]?.status === 'None')).length
  }, [matrix])

  const staleCount = useMemo(() => {
    if (!matrix) return 0
    let c = 0
    for (const r of matrix.rows) {
      for (const cell of Object.values(r.cells)) {
        if (cell.stale) c++
      }
    }
    return c
  }, [matrix])

  if (overview.loading && !dashboard) return <Loading />
  if (overview.error || !dashboard) return <ErrorState error={overview.error} onRetry={overview.reload} />

  const liveEntries = Object.entries(snapshot.byPlace).sort((a, b) => b[1] - a[1])
  const liveMax = Math.max(1, snapshot.live, ...liveEntries.map(([, v]) => v))
  const avgPercent = Math.round((dashboard.quota.average7Days / dashboard.quota.dailyLimit) * 1000) / 10
  const webSessions = Math.max(0, totalSessions - totalQrScans)

  return (
    <div className="admin-page">
      <div className="admin-page-header">
        <div>
          <h1>Tổng quan</h1>
          <p className="admin-muted">
            Trực tuyến cập nhật trong 5 giây · {dashboard.publishedPlaces} địa điểm đã xuất bản
          </p>
        </div>
        <div>
          <label htmlFor="overview-range" className="visually-hidden">
            Khoảng thời gian
          </label>
          <select
            id="overview-range"
            className="admin-select"
            value={daysRange}
            onChange={(e) => setDaysRange(Number(e.target.value))}
          >
            <option value={7}>7 ngày qua</option>
            <option value={14}>14 ngày qua</option>
            <option value={30}>30 ngày qua</option>
          </select>
        </div>
      </div>

      {/* Top 4 metric cards */}
      <div className="admin-kpi-grid">
        <div className="admin-kpi-card live">
          <div className="admin-kpi-label">
            <span className="admin-live-dot" aria-hidden="true" />
            Đang trực tuyến
          </div>
          <div className="admin-kpi-value">
            <strong>{snapshot.live}</strong> <span>thiết bị</span>
          </div>
        </div>

        <div className="admin-kpi-card">
          <div className="admin-kpi-label">Lượt nghe · {daysRange} ngày</div>
          <div className="admin-kpi-value">
            <strong>{totalListens.toLocaleString()}</strong>
          </div>
        </div>

        <div className="admin-kpi-card">
          <div className="admin-kpi-label">Quét QR · {daysRange} ngày</div>
          <div className="admin-kpi-value">
            <strong>{totalQrScans.toLocaleString()}</strong>
          </div>
        </div>

        <div className="admin-kpi-card">
          <div className="admin-kpi-label">MyMemory hôm nay</div>
          <div className="admin-kpi-value">
            <strong>{Math.round(dashboard.quota.percent)}%</strong>
          </div>
        </div>
      </div>

      {/* Middle row: Listens per day chart + Live by place */}
      <div className="admin-overview-mid">
        <section className="admin-card">
          <h2 className="admin-card-title">Lượt nghe mỗi ngày</h2>
          <div className="admin-bar-chart" role="img" aria-label={`Biểu đồ lượt nghe ${daysRange} ngày`}>
            {activeDays.map((d, idx) => {
              const isToday = idx === activeDays.length - 1
              const pct = d.listens > 0 ? Math.max(8, Math.round((d.listens / maxListens) * 100)) : 4
              return (
                <div
                  key={d.date}
                  className="admin-bar-col"
                  title={`${d.date}: ${d.listens} lượt nghe, ${d.qrScans} quét QR, đỉnh đồng thời ${d.peakConcurrent}`}
                >
                  <div
                    className={`admin-bar ${isToday ? 'today' : ''}`}
                    style={{ height: `${pct}%` }}
                  />
                </div>
              )
            })}
          </div>
          <div className="admin-bar-axis">
            <span>{daysRange} ngày trước</span>
            <span>hôm nay ({dashboard.today.listens} lượt nghe)</span>
          </div>
        </section>

        <section className="admin-card admin-live-by-place">
          <h2 className="admin-card-title">Trực tuyến theo địa điểm</h2>
          {liveEntries.length === 0 ? (
            <p className="admin-muted" style={{ flex: 1 }}>
              Hiện chưa có thiết bị nào đang mở trang địa điểm.
            </p>
          ) : (
            <div className="admin-place-bars">
              {liveEntries.map(([slug, count]) => (
                <div key={slug} className="admin-place-bar-item">
                  <div className="admin-place-bar-head">
                    <span>{placeNamesBySlug[slug] ?? slug}</span>
                    <strong>{count}</strong>
                  </div>
                  <div className="admin-progress-track">
                    <div
                      className="admin-progress-fill"
                      style={{ width: `${Math.min(100, Math.round((count / liveMax) * 100))}%` }}
                    />
                  </div>
                </div>
              ))}
            </div>
          )}
          <div className="admin-live-footer">
            qua QR {totalQrScans} · qua web {webSessions}
          </div>
        </section>
      </div>

      {/* Bottom row: Needs attention + MyMemory quota */}
      <div className="admin-overview-bottom">
        <section className="admin-card">
          <h2 className="admin-card-title">Cần xử lý</h2>
          <div className="admin-attention-list">
            <Link to="/admin/narrations?filter=draft" className="admin-attention-item warn">
              <span className="admin-dot warn" />
              <strong className="grow">{dashboard.draftTranslations} bản dịch nháp chờ duyệt</strong>
              <span className="admin-attention-cta">Duyệt ›</span>
            </Link>

            <Link to="/admin/narrations?filter=missing" className="admin-attention-item danger">
              <span className="admin-dot danger" />
              <strong className="grow">
                {missingPlacesCount} địa điểm thiếu ngôn ngữ
                {staleCount > 0 ? ` · ${staleCount} bản cũ` : ''}
              </strong>
              <span className="admin-attention-cta">Dịch ngay ›</span>
            </Link>
          </div>
        </section>

        <section className="admin-card">
          <h2 className="admin-card-title">Hạn mức MyMemory</h2>

          <div className="admin-quota-row">
            <div className="admin-quota-head">
              <span>
                Hôm nay · {formatCompact(dashboard.quota.usedToday)} / {formatCompact(dashboard.quota.dailyLimit)} ký tự
              </span>
              <strong>{dashboard.quota.percent}%</strong>
            </div>
            <div className="admin-progress-track">
              <div
                className={`admin-progress-fill ${dashboard.quota.usedToday >= dashboard.quota.blockThreshold ? 'danger' : ''}`}
                style={{ width: `${Math.min(100, dashboard.quota.percent)}%` }}
              />
              <div className="admin-quota-threshold" style={{ left: '90%' }} title="Ngưỡng chặn 90% (45.000 ký tự)" />
            </div>
          </div>

          <div className="admin-quota-row" style={{ marginTop: 14 }}>
            <div className="admin-quota-head">
              <span>
                Trung bình 7 ngày · {formatCompact(dashboard.quota.average7Days)} / {formatCompact(dashboard.quota.dailyLimit)} ký tự
              </span>
              <strong>{avgPercent}%</strong>
            </div>
            <div className="admin-progress-track">
              <div
                className="admin-progress-fill"
                style={{ width: `${Math.min(100, avgPercent)}%` }}
              />
              <div className="admin-quota-threshold" style={{ left: '90%' }} title="Ngưỡng chặn 90% (45.000 ký tự)" />
            </div>
          </div>

          <p className="admin-muted" style={{ marginTop: 12, fontSize: '0.84rem' }}>
            Tự động dừng gọi khi chạm 90% hạn mức ngày = {dashboard.quota.blockThreshold.toLocaleString()} ký tự (BR-06)
          </p>
        </section>
      </div>
    </div>
  )
}
