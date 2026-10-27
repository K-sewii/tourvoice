import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { getNarrationMatrix, translatePlace } from '../../api/admin'
import { ErrorState, Loading } from '../../components/States'
import { useAsync } from '../../hooks/useAsync'

/**
 * SCR-A5 — Ma trận thuyết minh (/admin/narrations).
 */
export default function AdminNarrationsPage() {
  const [params, setParams] = useSearchParams()
  const filter = params.get('filter') ?? 'all'

  const matrixState = useAsync(`admin:matrix:${filter}`, (signal) => getNarrationMatrix(filter, signal))
  const matrix = matrixState.data

  const [translatingId, setTranslatingId] = useState<number | null>(null)
  const [batchBusy, setBatchBusy] = useState(false)
  const [feedback, setFeedback] = useState<{ type: 'success' | 'danger'; text: string } | null>(null)

  const setFilter = (val: string) => {
    const next = new URLSearchParams(params)
    if (val === 'all') next.delete('filter')
    else next.set('filter', val)
    setParams(next, { replace: true })
  }

  const handleTranslateRow = async (placeId: number) => {
    setTranslatingId(placeId)
    setFeedback(null)
    try {
      const res = await translatePlace(placeId)
      const chars = res.results.reduce((s, r) => s + r.charactersUsed, 0)
      matrixState.reload()
      setFeedback({ type: 'success', text: `Đã dịch xong địa điểm #${placeId} (${chars} ký tự).` })
    } catch (err) {
      setFeedback({ type: 'danger', text: err instanceof Error ? err.message : 'Lỗi dịch máy.' })
    } finally {
      setTranslatingId(null)
    }
  }

  const handleTranslateAllVisible = async () => {
    if (!matrix) return
    const candidates = matrix.rows.filter((r) => r.canTranslate)
    if (candidates.length === 0) return
    setBatchBusy(true)
    setFeedback(null)
    let done = 0
    try {
      // ADR-08: sequential calls from frontend
      for (const row of candidates) {
        setTranslatingId(row.placeId)
        await translatePlace(row.placeId)
        done++
      }
      matrixState.reload()
      setFeedback({ type: 'success', text: `Đã dịch tuần tự xong ${done}/${candidates.length} mục.` })
    } catch (err) {
      matrixState.reload()
      setFeedback({
        type: 'danger',
        text: `Đã dịch ${done}/${candidates.length} mục trước khi dừng: ${err instanceof Error ? err.message : 'Lỗi'}`,
      })
    } finally {
      setTranslatingId(null)
      setBatchBusy(false)
    }
  }

  if (matrixState.loading && !matrix) return <Loading />
  if (matrixState.error || !matrix) return <ErrorState error={matrixState.error} onRetry={matrixState.reload} />

  const translatableCount = matrix.rows.filter((r) => r.canTranslate).length

  return (
    <div className="admin-page">
      <div className="admin-page-header">
        <div>
          <h1>Thuyết minh</h1>
          <p className="admin-muted">
            Ma trận Địa điểm × {matrix.languages.length} ngôn ngữ · Bấm vào từng ô để mở trình kiểm duyệt & sửa bản dịch
          </p>
        </div>
        <button
          type="button"
          className="btn btn-primary btn-sm"
          onClick={handleTranslateAllVisible}
          disabled={batchBusy || translatableCount === 0}
        >
          {batchBusy ? 'Đang dịch tuần tự…' : `Dịch phần thiếu (${translatableCount})`}
        </button>
      </div>

      {feedback && (
        <div className={`admin-alert admin-alert-${feedback.type}`} role="alert">
          {feedback.text}
        </div>
      )}

      {/* Filter pills */}
      <div className="admin-filter-bar" role="group" aria-label="Bộ lọc ma trận bản dịch">
        {[
          { value: 'all', label: 'Tất cả địa điểm' },
          { value: 'draft', label: 'Bản nháp chờ duyệt' },
          { value: 'stale', label: 'Bản cũ (bản gốc đã đổi)' },
          { value: 'missing', label: 'Thiếu ngôn ngữ' },
        ].map((f) => (
          <button
            key={f.value}
            type="button"
            className={`admin-pill ${filter === f.value ? 'active' : ''}`}
            onClick={() => setFilter(f.value)}
          >
            {f.label}
          </button>
        ))}
      </div>

      <div className="admin-card admin-table-card">
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>ĐỊA ĐIỂM</th>
                <th>PHÂN LOẠI</th>
                {matrix.languages.map((l) => (
                  <th key={l.code} style={{ textAlign: 'center' }}>
                    {l.code.toUpperCase()}
                  </th>
                ))}
                <th>THAO TÁC</th>
              </tr>
            </thead>
            <tbody>
              {matrix.rows.length === 0 ? (
                <tr>
                  <td colSpan={matrix.languages.length + 3} className="admin-empty-cell">
                    Không có mục nào khớp bộ lọc.
                  </td>
                </tr>
              ) : (
                matrix.rows.map((row) => (
                  <tr key={row.placeId}>
                    <td>
                      <div className="admin-place-cell">
                        <Link to={`/admin/places/${row.placeId}?tab=narration`} className="admin-place-name">
                          {row.name}
                        </Link>
                        {row.parentName && <span className="admin-muted">thuộc {row.parentName}</span>}
                      </div>
                    </td>
                    <td>
                      <span className={`admin-tag ${row.kind === 'Food' ? 'food' : ''}`}>
                        {row.kind === 'Food' ? 'Món ăn' : 'Điểm đến'}
                      </span>
                    </td>
                    {matrix.languages.map((l) => {
                      const cell = row.cells[l.code] ?? { status: 'None', stale: false }
                      let cls = 'status-missing'
                      let label = 'Chưa có'
                      if (cell.status === 'Reviewed') {
                        cls = 'status-published'
                        label = cell.stale ? 'Đã duyệt · cũ' : 'Đã duyệt'
                      } else if (cell.status === 'Draft') {
                        cls = 'status-draft'
                        label = cell.stale ? 'Nháp · cũ' : 'Bản nháp'
                      }
                      return (
                        <td key={l.code} style={{ textAlign: 'center' }}>
                          <Link
                            to={`/admin/places/${row.placeId}?tab=narration&lang=${l.code}`}
                            className={`admin-badge ${cls} clickable`}
                            title={`Mở trình duyệt ${l.code} của ${row.name}`}
                          >
                            {label}
                          </Link>
                        </td>
                      )
                    })}
                    <td>
                      <div className="admin-row-actions">
                        <button
                          type="button"
                          className="btn btn-outline btn-xs"
                          disabled={!row.canTranslate || translatingId === row.placeId || batchBusy}
                          onClick={() => void handleTranslateRow(row.placeId)}
                          title={
                            row.canTranslate
                              ? 'Dịch phần thiếu bằng MyMemory'
                              : 'Cần bản tiếng Việt đã duyệt và còn ngôn ngữ thiếu/cũ (BR-04, BR-05)'
                          }
                        >
                          {translatingId === row.placeId ? 'Đang dịch…' : 'Dịch phần thiếu'}
                        </button>
                      </div>
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
