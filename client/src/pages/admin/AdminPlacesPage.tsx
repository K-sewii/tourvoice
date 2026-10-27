import { Fragment, useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { createAdminPlace, getAdminPlacesTree } from '../../api/admin'
import { ApiError } from '../../api/http'
import type { PlaceKind } from '../../api/types'
import { CloseIcon } from '../../components/Icons'
import { QrCodePanel } from '../../components/QrCodeModal'
import { ErrorState, Loading } from '../../components/States'
import { useAsync } from '../../hooks/useAsync'

const CATEGORIES: { value: string; label: string }[] = [
  { value: 'Heritage', label: 'Di tích (Heritage)' },
  { value: 'Market', label: 'Chợ (Market)' },
  { value: 'FoodStreet', label: 'Phố ẩm thực (FoodStreet)' },
  { value: 'Park', label: 'Công viên (Park)' },
]

const FOOD_TYPES: { value: string; label: string }[] = [
  { value: 'Noodles', label: 'Món nước / sợi (Noodles)' },
  { value: 'Rice', label: 'Cơm (Rice)' },
  { value: 'Snack', label: 'Ăn vặt / bánh (Snack)' },
  { value: 'Drink', label: 'Thức uống (Drink)' },
  { value: 'Dessert', label: 'Chè / tráng miệng (Dessert)' },
]

const CATEGORY_LABELS: Record<string, string> = {
  Heritage: 'Di tích',
  Market: 'Chợ',
  FoodStreet: 'Phố ẩm thực',
  Park: 'Công viên',
}

const FOOD_TYPE_LABELS: Record<string, string> = {
  Noodles: 'Món nước / sợi',
  Rice: 'Cơm',
  Snack: 'Ăn vặt / bánh',
  Drink: 'Thức uống',
  Dessert: 'Chè / tráng miệng',
}

const STATUS_LABELS: Record<string, string> = {
  Published: 'Đã xuất bản',
  Draft: 'Bản nháp',
  Hidden: 'Đã ẩn',
}

/** SCR-A3 — Quản lý địa điểm phân cấp (Điểm đến → Món ăn), lọc trạng thái, xuất QR, tạo mới. */
export default function AdminPlacesPage() {
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const statusFilter = params.get('status') ?? ''

  const tree = useAsync(`admin:places:${statusFilter}`, (signal) =>
    getAdminPlacesTree(statusFilter || undefined, 1, 50, signal),
  )
  const nodes = tree.data?.items ?? []
  const total = tree.data?.total ?? 0

  const [qrTarget, setQrTarget] = useState<{ slug: string; name: string } | null>(null)

  // Create modal state
  const [createOpen, setCreateOpen] = useState(false)
  const [createKind, setCreateKind] = useState<PlaceKind>('Destination')
  const [createParentId, setCreateParentId] = useState<number | ''>('')
  const [createSlug, setCreateSlug] = useState('')
  const [createName, setCreateName] = useState('')
  const [createCategory, setCreateCategory] = useState<string>('Heritage')
  const [createFoodType, setCreateFoodType] = useState<string>('Noodles')
  const [createLat, setCreateLat] = useState('10.749')
  const [createLng, setCreateLng] = useState('106.651')
  const [createAddress, setCreateAddress] = useState('')
  const [createPriceRange, setCreatePriceRange] = useState('')
  const [createOpeningHours, setCreateOpeningHours] = useState('')
  const [createSortOrder, setCreateSortOrder] = useState('1')
  const [createErr, setCreateErr] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)

  const openCreateModal = (kind: PlaceKind = 'Destination', parentId?: number) => {
    setCreateKind(kind)
    setCreateParentId(parentId ?? (nodes[0]?.id ?? ''))
    setCreateSlug('')
    setCreateName('')
    setCreateCategory('Heritage')
    setCreateFoodType('Noodles')
    setCreateLat('10.749')
    setCreateLng('106.651')
    setCreateAddress('')
    setCreatePriceRange('')
    setCreateOpeningHours('')
    setCreateSortOrder('1')
    setCreateErr(null)
    setCreateOpen(true)
  }

  const handleCreate = async (e: FormEvent) => {
    e.preventDefault()
    setCreateErr(null)
    setCreating(true)
    try {
      const created = await createAdminPlace({
        slug: createSlug.trim(),
        name: createName.trim() || createSlug.trim(),
        kind: createKind,
        category: createKind === 'Destination' ? createCategory : null,
        foodType: createKind === 'Food' ? createFoodType : null,
        parentId: createKind === 'Food' && createParentId !== '' ? Number(createParentId) : null,
        latitude: Number(createLat),
        longitude: Number(createLng),
        sortOrder: Number(createSortOrder) || 0,
        address: createAddress.trim() || null,
        priceRange: createKind === 'Food' ? createPriceRange.trim() || null : null,
        openingHours: createOpeningHours.trim() || null,
      })
      setCreateOpen(false)
      navigate(`/admin/places/${created.id}`)
    } catch (err) {
      if (err instanceof ApiError) {
        setCreateErr(err.message)
      } else {
        setCreateErr('Không thể tạo địa điểm.')
      }
    } finally {
      setCreating(false)
    }
  }

  const setStatus = (val: string) => {
    const next = new URLSearchParams(params)
    if (val) next.set('status', val)
    else next.delete('status')
    setParams(next, { replace: true })
  }

  if (tree.loading && nodes.length === 0) return <Loading />
  if (tree.error) return <ErrorState error={tree.error} onRetry={tree.reload} />

  return (
    <div className="admin-page">
      <div className="admin-page-header">
        <div>
          <h1>Địa điểm</h1>
          <p className="admin-muted">
            Quản lý điểm đến và món ăn / quán ăn · {total} điểm đến
          </p>
        </div>
        <div className="admin-header-actions">
          <button type="button" className="btn btn-outline btn-sm" onClick={() => openCreateModal('Food')}>
            + Thêm món ăn
          </button>
          <button type="button" className="btn btn-primary btn-sm" onClick={() => openCreateModal('Destination')}>
            + Thêm điểm đến
          </button>
        </div>
      </div>

      {/* Filter pills */}
      <div className="admin-filter-bar" role="group" aria-label="Lọc theo trạng thái">
        {[
          { value: '', label: 'Tất cả trạng thái' },
          { value: 'Published', label: 'Đã xuất bản' },
          { value: 'Draft', label: 'Bản nháp' },
          { value: 'Hidden', label: 'Đã ẩn' },
        ].map((f) => (
          <button
            key={f.value}
            type="button"
            className={`admin-pill ${statusFilter === f.value ? 'active' : ''}`}
            onClick={() => setStatus(f.value)}
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
                <th>TRẠNG THÁI</th>
                <th>THUYẾT MINH (5 NGÔN NGỮ)</th>
                <th>THAO TÁC</th>
              </tr>
            </thead>
            <tbody>
              {nodes.length === 0 ? (
                <tr>
                  <td colSpan={5} className="admin-empty-cell">
                    Không có địa điểm nào khớp bộ lọc.
                  </td>
                </tr>
              ) : (
                nodes.map((dest) => (
                  <Fragment key={dest.id}>
                    <tr className="admin-row-dest">
                      <td>
                        <div className="admin-place-cell">
                          <Link to={`/admin/places/${dest.id}`} className="admin-place-name">
                            {dest.name}
                          </Link>
                          <code className="admin-slug-badge">{dest.slug}</code>
                        </div>
                      </td>
                      <td>
                        <span className="admin-tag">
                          {dest.category ? (CATEGORY_LABELS[dest.category] ?? dest.category) : 'Điểm đến'}
                        </span>
                      </td>
                      <td>
                        <span className={`admin-badge status-${dest.status.toLowerCase()}`}>
                          {STATUS_LABELS[dest.status] ?? dest.status}
                        </span>
                      </td>
                      <td>
                        <span className="admin-lang-progress">
                          <strong>{dest.reviewedLanguages}/5</strong> đã duyệt
                          {dest.draftLanguages > 0 && (
                            <span className="admin-draft-hint"> · {dest.draftLanguages} nháp</span>
                          )}
                          {!dest.hasVietnameseNarration && (
                            <span className="admin-missing-hint"> · thiếu bản vi</span>
                          )}
                        </span>
                      </td>
                      <td>
                        <div className="admin-row-actions">
                          <button
                            type="button"
                            className="btn btn-outline btn-xs"
                            onClick={() => setQrTarget({ slug: dest.slug, name: dest.name })}
                          >
                            Mã QR
                          </button>
                          <button
                            type="button"
                            className="btn btn-ghost btn-xs"
                            onClick={() => openCreateModal('Food', dest.id)}
                            title="Thêm món con vào điểm đến này"
                          >
                            + Món ăn
                          </button>
                          <Link to={`/admin/places/${dest.id}`} className="btn btn-soft btn-xs">
                            Sửa
                          </Link>
                        </div>
                      </td>
                    </tr>

                    {dest.children.map((food) => (
                      <tr key={food.id} className="admin-row-child">
                        <td>
                          <div className="admin-place-cell child">
                            <span className="admin-tree-branch" aria-hidden="true">
                              ↳
                            </span>
                            <Link to={`/admin/places/${food.id}`} className="admin-place-name">
                              {food.name}
                            </Link>
                            <code className="admin-slug-badge">{food.slug}</code>
                          </div>
                        </td>
                        <td>
                          <span className="admin-tag food">
                            {food.foodType ? (FOOD_TYPE_LABELS[food.foodType] ?? food.foodType) : 'Món ăn'}
                          </span>
                        </td>
                        <td>
                          <span className={`admin-badge status-${food.status.toLowerCase()}`}>
                            {STATUS_LABELS[food.status] ?? food.status}
                          </span>
                        </td>
                        <td>
                          <span className="admin-lang-progress">
                            <strong>{food.reviewedLanguages}/5</strong> đã duyệt
                            {food.draftLanguages > 0 && (
                              <span className="admin-draft-hint"> · {food.draftLanguages} nháp</span>
                            )}
                          </span>
                        </td>
                        <td>
                          <div className="admin-row-actions">
                            <button
                              type="button"
                              className="btn btn-outline btn-xs"
                              onClick={() => setQrTarget({ slug: food.slug, name: food.name })}
                            >
                              Mã QR
                            </button>
                            <Link to={`/admin/places/${food.id}`} className="btn btn-soft btn-xs">
                              Sửa
                            </Link>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </Fragment>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {qrTarget && <QrCodePanel slug={qrTarget.slug} name={qrTarget.name} onClose={() => setQrTarget(null)} />}

      {createOpen && (
        <div className="admin-modal-backdrop" role="dialog" aria-modal="true" onClick={() => setCreateOpen(false)}>
          <div className="admin-modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="admin-modal-header">
              <div>
                <h3>{createKind === 'Destination' ? 'Thêm điểm đến mới' : 'Thêm món / quán ăn mới'}</h3>
                <p className="admin-muted">Tạo mới ở trạng thái Bản nháp (BR-02, BR-03)</p>
              </div>
              <button type="button" className="btn btn-ghost" onClick={() => setCreateOpen(false)} aria-label="Đóng">
                <CloseIcon />
              </button>
            </div>

            <form onSubmit={handleCreate} className="admin-form-grid">
              <div className="field">
                <label htmlFor="c-kind">Loại địa điểm</label>
                <select
                  id="c-kind"
                  className="admin-select"
                  value={createKind}
                  onChange={(e) => setCreateKind(e.target.value as PlaceKind)}
                >
                  <option value="Destination">Điểm đến (Destination)</option>
                  <option value="Food">Món / quán ăn (FoodSpot)</option>
                </select>
              </div>

              {createKind === 'Food' ? (
                <>
                  <div className="field">
                    <label htmlFor="c-parent">Thuộc điểm đến (BR-02)</label>
                    <select
                      id="c-parent"
                      className="admin-select"
                      value={createParentId}
                      onChange={(e) => setCreateParentId(Number(e.target.value))}
                      required
                    >
                      <option value="">-- Chọn điểm đến --</option>
                      {nodes.map((d) => (
                        <option key={d.id} value={d.id}>
                          {d.name} ({d.slug})
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="field">
                    <label htmlFor="c-foodtype">Loại món ăn</label>
                    <select
                      id="c-foodtype"
                      className="admin-select"
                      value={createFoodType}
                      onChange={(e) => setCreateFoodType(e.target.value)}
                    >
                      {FOOD_TYPES.map((ft) => (
                        <option key={ft.value} value={ft.value}>
                          {ft.label}
                        </option>
                      ))}
                    </select>
                  </div>
                </>
              ) : (
                <div className="field">
                  <label htmlFor="c-category">Danh mục</label>
                  <select
                    id="c-category"
                    className="admin-select"
                    value={createCategory}
                    onChange={(e) => setCreateCategory(e.target.value)}
                  >
                    {CATEGORIES.map((cat) => (
                      <option key={cat.value} value={cat.value}>
                        {cat.label}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              <div className="field">
                <label htmlFor="c-name">Tên tiếng Việt</label>
                <input
                  id="c-name"
                  type="text"
                  value={createName}
                  onChange={(e) => setCreateName(e.target.value)}
                  placeholder="Chợ Bình Tây"
                  required
                />
              </div>

              <div className="field">
                <label htmlFor="c-slug">Định danh URL (Slug: chữ thường, số, gạch ngang)</label>
                <input
                  id="c-slug"
                  type="text"
                  value={createSlug}
                  onChange={(e) => setCreateSlug(e.target.value)}
                  placeholder="cho-binh-tay"
                  required
                />
              </div>

              <div className="field">
                <label htmlFor="c-lat">Vĩ độ (10.730 – 10.765)</label>
                <input
                  id="c-lat"
                  type="number"
                  step="0.000001"
                  value={createLat}
                  onChange={(e) => setCreateLat(e.target.value)}
                  required
                />
              </div>

              <div className="field">
                <label htmlFor="c-lng">Kinh độ (106.615 – 106.665)</label>
                <input
                  id="c-lng"
                  type="number"
                  step="0.000001"
                  value={createLng}
                  onChange={(e) => setCreateLng(e.target.value)}
                  required
                />
              </div>

              <div className="field">
                <label htmlFor="c-addr">Địa chỉ (phường mới)</label>
                <input
                  id="c-addr"
                  type="text"
                  value={createAddress}
                  onChange={(e) => setCreateAddress(e.target.value)}
                  placeholder="57A Tháp Mười, phường Bình Tây"
                />
              </div>

              <div className="field">
                <label htmlFor="c-hours">Giờ mở cửa</label>
                <input
                  id="c-hours"
                  type="text"
                  value={createOpeningHours}
                  onChange={(e) => setCreateOpeningHours(e.target.value)}
                  placeholder="06:00 – 18:00"
                />
              </div>

              {createKind === 'Food' && (
                <div className="field">
                  <label htmlFor="c-price">Khoảng giá (VNĐ)</label>
                  <input
                    id="c-price"
                    type="text"
                    value={createPriceRange}
                    onChange={(e) => setCreatePriceRange(e.target.value)}
                    placeholder="35.000 – 55.000 đ"
                  />
                </div>
              )}

              <div className="field">
                <label htmlFor="c-sort">Thứ tự hiển thị</label>
                <input
                  id="c-sort"
                  type="number"
                  value={createSortOrder}
                  onChange={(e) => setCreateSortOrder(e.target.value)}
                />
              </div>

              {createErr && (
                <div className="admin-alert admin-alert-danger" style={{ gridColumn: '1 / -1' }} role="alert">
                  {createErr}
                </div>
              )}

              <div className="admin-modal-actions" style={{ gridColumn: '1 / -1' }}>
                <button type="button" className="btn btn-outline" onClick={() => setCreateOpen(false)}>
                  Hủy
                </button>
                <button type="submit" className="btn btn-primary" disabled={creating}>
                  {creating ? 'Đang tạo…' : 'Tạo địa điểm'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
