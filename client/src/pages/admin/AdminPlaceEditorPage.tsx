import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { useEffect, useMemo, useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { MapContainer, Marker, Rectangle, TileLayer, useMapEvents } from 'react-leaflet'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import {
  approvePlaceTranslation,
  createAdminPlace,
  deleteAdminPlace,
  getAdminLanguages,
  getAdminPlace,
  getUsage,
  hideAdminPlace,
  publishAdminPlace,
  translatePlace,
  updateAdminPlace,
  updatePlaceTranslation,
  uploadPlaceCover,
} from '../../api/admin'
import { ApiError } from '../../api/http'
import type { AdminPlaceDetail, AdminTranslation, Language, Quota } from '../../api/types'
import { CloseIcon, PauseIcon, PlayIcon } from '../../components/Icons'
import { QrCodePanel } from '../../components/QrCodeModal'
import { ErrorState, Loading } from '../../components/States'
import { useAsync } from '../../hooks/useAsync'
import { loadVoices, pickVoice, speechSupported } from '../../lib/speech'

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

const SERVICE_BOUNDS: [[number, number], [number, number]] = [
  [10.73, 106.615],
  [10.765, 106.665],
]

const LANG_LABELS: Record<string, string> = {
  vi: 'Tiếng Việt (vi) · bản gốc',
  en: 'Tiếng Anh (en)',
  zh: 'Tiếng Trung (zh)',
  ko: 'Tiếng Hàn (ko)',
  ja: 'Tiếng Nhật (ja)',
}

const DEFAULT_SPEECH_LANG: Record<string, string> = {
  vi: 'vi-VN',
  en: 'en-US',
  zh: 'zh-CN',
  ko: 'ko-KR',
  ja: 'ja-JP',
}

const MISSING_LABELS: Record<string, string> = {
  vi_narration_reviewed: 'Bản thuyết minh tiếng Việt (vi) phải có nội dung và đã được duyệt (Reviewed)',
  coordinates: 'Tọa độ phải nằm trong vùng phục vụ Quận 6 (10.730–10.765, 106.615–106.665)',
  cover_image: 'Phải tải lên ảnh bìa (JPG/PNG/WebP)',
  category: 'Điểm đến phải chọn danh mục (Di tích, Chợ, Phố ẩm thực, Công viên)',
  parent: 'Món ăn phải thuộc một điểm đến cấp trên',
}

const pinIcon = L.divIcon({
  className: '',
  html: '<div class="pin pin-dest selected"></div>',
  iconSize: [30, 30],
  iconAnchor: [15, 15],
})

function MapClickHandler({ onPick }: { onPick: (lat: number, lng: number) => void }) {
  useMapEvents({
    click(e) {
      onPick(Number(e.latlng.lat.toFixed(6)), Number(e.latlng.lng.toFixed(6)))
    },
  })
  return null
}

export default function AdminPlaceEditorPage() {
  const { id } = useParams<{ id: string }>()
  const placeId = Number(id)

  const loader = useAsync(
    Number.isFinite(placeId) ? `admin:place:${placeId}` : null,
    async (signal) => {
      const [place, languages, usage] = await Promise.all([
        getAdminPlace(placeId, signal),
        getAdminLanguages(signal).catch(() => [] as Language[]),
        getUsage(signal).catch(() => null),
      ])
      return { place, languages, quota: usage?.quota ?? null }
    },
  )

  useEffect(() => {
    return () => {
      if (speechSupported()) window.speechSynthesis.cancel()
    }
  }, [placeId])

  if (loader.loading && !loader.data) return <Loading />
  if (loader.error || !loader.data) return <ErrorState error={loader.error} onRetry={loader.reload} />

  return (
    <PlaceEditorInner
      key={loader.data.place.id}
      initialPlace={loader.data.place}
      languages={loader.data.languages}
      initialQuota={loader.data.quota}
    />
  )
}

function PlaceEditorInner({
  initialPlace,
  languages,
  initialQuota,
}: {
  initialPlace: AdminPlaceDetail
  languages: Language[]
  initialQuota: Quota | null
}) {
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const activeTab = params.get('tab') ?? 'general'
  const focusLang = params.get('lang')

  const [place, setPlace] = useState<AdminPlaceDetail>(initialPlace)
  const [quota, setQuota] = useState<Quota | null>(initialQuota)

  // Editable form fields
  const [slug, setSlug] = useState(initialPlace.slug)
  const [category, setCategory] = useState(initialPlace.category ?? 'Heritage')
  const [foodType, setFoodType] = useState(initialPlace.foodType ?? 'Noodles')
  const [latitude, setLatitude] = useState(String(initialPlace.latitude))
  const [longitude, setLongitude] = useState(String(initialPlace.longitude))
  const [address, setAddress] = useState(initialPlace.address ?? '')
  const [priceRange, setPriceRange] = useState(initialPlace.priceRange ?? '')
  const [openingHours, setOpeningHours] = useState(initialPlace.openingHours ?? '')
  const [sortOrder, setSortOrder] = useState(String(initialPlace.sortOrder))
  const [coverImageUrl, setCoverImageUrl] = useState(initialPlace.coverImageUrl ?? '')

  const [saving, setSaving] = useState(false)
  const [feedback, setFeedback] = useState<{ type: 'success' | 'danger' | 'warn'; text: string; items?: string[] } | null>(
    null,
  )
  const [uploadingCover, setUploadingCover] = useState(false)
  const [translating, setTranslating] = useState(false)

  const translationsByLang = useMemo(() => {
    const map: Record<string, AdminTranslation> = {}
    for (const tr of place.translations ?? []) {
      map[tr.languageCode] = tr
    }
    return map
  }, [place])

  const viTranslation = translationsByLang['vi']
  const placeTitle = viTranslation?.name || place.slug

  // Narration drawer state (initialized with ?lang= if present)
  const initialFocusTr = focusLang ? translationsByLang[focusLang] : undefined
  const [drawerLang, setDrawerLang] = useState<string | null>(
    focusLang && activeTab === 'narration' ? focusLang : null,
  )
  const [drawerName, setDrawerName] = useState(
    initialFocusTr?.name || viTranslation?.name || initialPlace.slug,
  )
  const [drawerText, setDrawerText] = useState(initialFocusTr?.narrationText ?? '')
  const [drawerBusy, setDrawerBusy] = useState(false)

  // Web Speech preview state
  const [speakingLang, setSpeakingLang] = useState<string | null>(null)
  const utterRef = useRef<SpeechSynthesisUtterance | null>(null)

  // Quick add FoodSpot state
  const [newFoodName, setNewFoodName] = useState('')
  const [newFoodSlug, setNewFoodSlug] = useState('')
  const [newFoodType, setNewFoodType] = useState('Noodles')
  const [newFoodPrice, setNewFoodPrice] = useState('')
  const [addingFood, setAddingFood] = useState(false)

  const syncForm = (p: AdminPlaceDetail) => {
    setSlug(p.slug)
    setCategory(p.category ?? 'Heritage')
    setFoodType(p.foodType ?? 'Noodles')
    setLatitude(String(p.latitude))
    setLongitude(String(p.longitude))
    setAddress(p.address ?? '')
    setPriceRange(p.priceRange ?? '')
    setOpeningHours(p.openingHours ?? '')
    setSortOrder(String(p.sortOrder))
    setCoverImageUrl(p.coverImageUrl ?? '')
  }

  const setTab = (tab: string) => {
    const next = new URLSearchParams(params)
    next.set('tab', tab)
    next.delete('lang')
    setParams(next, { replace: true })
  }

  const openDrawer = (langCode: string) => {
    const existing = translationsByLang[langCode]
    setDrawerLang(langCode)
    setDrawerName(existing?.name || viTranslation?.name || place.slug)
    setDrawerText(existing?.narrationText ?? '')
  }

  const handleSave = async (e?: FormEvent) => {
    e?.preventDefault()
    setSaving(true)
    setFeedback(null)
    try {
      const updated = await updateAdminPlace(place.id, {
        slug: place.slugLocked ? undefined : slug.trim(),
        category: place.kind === 'Destination' ? category || null : null,
        foodType: place.kind === 'Food' ? foodType || null : null,
        latitude: Number(latitude),
        longitude: Number(longitude),
        coverImageUrl: coverImageUrl.trim() || null,
        sortOrder: Number(sortOrder) || 0,
        address: address.trim() || null,
        priceRange: place.kind === 'Food' ? priceRange.trim() || null : null,
        openingHours: openingHours.trim() || null,
      })
      const refreshed = await getAdminPlace(updated.id)
      setPlace(refreshed)
      syncForm(refreshed)
      setFeedback({ type: 'success', text: 'Đã lưu thông tin địa điểm.' })
    } catch (err) {
      if (err instanceof ApiError) {
        setFeedback({ type: 'danger', text: err.message })
      } else {
        setFeedback({ type: 'danger', text: 'Lưu thất bại.' })
      }
    } finally {
      setSaving(false)
    }
  }

  const handleUploadCover = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    setUploadingCover(true)
    setFeedback(null)
    try {
      await uploadPlaceCover(place.id, file)
      const refreshed = await getAdminPlace(place.id)
      setPlace(refreshed)
      syncForm(refreshed)
      setFeedback({ type: 'success', text: 'Đã tải lên ảnh bìa mới.' })
    } catch (err) {
      setFeedback({ type: 'danger', text: err instanceof Error ? err.message : 'Tải ảnh bìa thất bại.' })
    } finally {
      setUploadingCover(false)
      e.target.value = ''
    }
  }

  const handlePublish = async () => {
    setFeedback(null)
    try {
      await publishAdminPlace(place.id)
      const refreshed = await getAdminPlace(place.id)
      setPlace(refreshed)
      syncForm(refreshed)
      setFeedback({ type: 'success', text: 'Đã xuất bản. Định danh URL (slug) đã được khóa để bảo vệ mã QR (BR-08).' })
    } catch (err) {
      if (err instanceof ApiError && err.missing?.length) {
        setFeedback({
          type: 'danger',
          text: 'Chưa đủ điều kiện xuất bản (BR-08):',
          items: err.missing.map((m) => MISSING_LABELS[m] ?? m),
        })
      } else {
        setFeedback({ type: 'danger', text: err instanceof Error ? err.message : 'Không thể xuất bản.' })
      }
    }
  }

  const handleHide = async () => {
    setFeedback(null)
    try {
      await hideAdminPlace(place.id)
      const refreshed = await getAdminPlace(place.id)
      setPlace(refreshed)
      syncForm(refreshed)
      setFeedback({ type: 'warn', text: 'Đã ẩn địa điểm khỏi trang du khách.' })
    } catch (err) {
      setFeedback({ type: 'danger', text: err instanceof Error ? err.message : 'Không thể ẩn địa điểm.' })
    }
  }

  const handleDelete = async () => {
    if (!window.confirm(`Xóa vĩnh viễn "${place.slug}"? Chỉ xóa được bản nháp chưa có món con (BR-13).`)) return
    setFeedback(null)
    try {
      await deleteAdminPlace(place.id)
      navigate('/admin/places', { replace: true })
    } catch (err) {
      setFeedback({ type: 'danger', text: err instanceof Error ? err.message : 'Không thể xóa địa điểm.' })
    }
  }

  const togglePreviewVoice = async (langCode: string, text: string | null | undefined) => {
    if (!speechSupported() || !text?.trim()) return
    const synth = window.speechSynthesis
    if (speakingLang === langCode) {
      synth.cancel()
      setSpeakingLang(null)
      return
    }
    synth.cancel()
    const voices = await loadVoices()
    const langCfg = languages.find((l) => l.code === langCode)
    const speechLang = langCfg?.speechLang ?? DEFAULT_SPEECH_LANG[langCode] ?? langCode
    const picked = pickVoice(voices, speechLang)
    const u = new SpeechSynthesisUtterance(text)
    if (picked) {
      u.voice = picked
      u.lang = picked.lang
    } else {
      u.lang = speechLang
    }
    u.onend = () => setSpeakingLang(null)
    u.onerror = () => setSpeakingLang(null)
    utterRef.current = u
    setSpeakingLang(langCode)
    synth.speak(u)
  }

  const handleSaveTranslation = async (andApprove: boolean) => {
    if (!drawerLang) return
    setDrawerBusy(true)
    setFeedback(null)
    try {
      await updatePlaceTranslation(place.id, drawerLang, {
        name: drawerName.trim() || placeTitle,
        narrationText: drawerText.trim() || null,
      })
      if (andApprove) {
        await approvePlaceTranslation(place.id, drawerLang)
      }
      const refreshed = await getAdminPlace(place.id)
      setPlace(refreshed)
      setDrawerLang(null)
      setFeedback({
        type: 'success',
        text: andApprove
          ? `Đã lưu và duyệt bản dịch [${drawerLang}].`
          : `Đã lưu bản nháp [${drawerLang}] (BR-07).`,
      })
    } catch (err) {
      setFeedback({ type: 'danger', text: err instanceof Error ? err.message : 'Lỗi lưu bản dịch.' })
    } finally {
      setDrawerBusy(false)
    }
  }

  const handleQuickApprove = async (langCode: string) => {
    setFeedback(null)
    try {
      await approvePlaceTranslation(place.id, langCode)
      const refreshed = await getAdminPlace(place.id)
      setPlace(refreshed)
      setFeedback({ type: 'success', text: `Đã duyệt bản dịch [${langCode}].` })
    } catch (err) {
      setFeedback({ type: 'danger', text: err instanceof Error ? err.message : 'Không thể duyệt bản dịch.' })
    }
  }

  const handleTranslateMissing = async () => {
    setTranslating(true)
    setFeedback(null)
    try {
      const res = await translatePlace(place.id)
      const [refreshed, u] = await Promise.all([getAdminPlace(place.id), getUsage().catch(() => null)])
      setPlace(refreshed)
      if (u) setQuota(u.quota)

      const summary = res.results
        .map((r) =>
          r.skipped
            ? `${r.languageCode}: giữ nguyên (BR-05)`
            : r.success
              ? `${r.languageCode}: +${r.charactersUsed} ký tự`
              : `${r.languageCode}: lỗi (${r.errorMessage})`,
        )
        .join(' · ')
      setFeedback({ type: 'success', text: `Kết quả dịch MyMemory: ${summary || 'Không có ngôn ngữ nào cần dịch.'}` })
    } catch (err) {
      setFeedback({ type: 'danger', text: err instanceof Error ? err.message : 'Lỗi gọi dịch máy MyMemory.' })
    } finally {
      setTranslating(false)
    }
  }

  const handleQuickAddFood = async (e: FormEvent) => {
    e.preventDefault()
    setAddingFood(true)
    setFeedback(null)
    try {
      const created = await createAdminPlace({
        slug: newFoodSlug.trim(),
        name: newFoodName.trim(),
        kind: 'Food',
        category: null,
        foodType: newFoodType,
        parentId: place.id,
        latitude: place.latitude,
        longitude: place.longitude,
        sortOrder: (place.foods?.length ?? 0) + 1,
        priceRange: newFoodPrice.trim() || null,
      })
      setNewFoodName('')
      setNewFoodSlug('')
      setNewFoodPrice('')
      const refreshed = await getAdminPlace(place.id)
      setPlace(refreshed)
      setFeedback({ type: 'success', text: `Đã thêm món "${created.slug}".` })
    } catch (err) {
      setFeedback({ type: 'danger', text: err instanceof Error ? err.message : 'Không thể thêm món ăn.' })
    } finally {
      setAddingFood(false)
    }
  }

  const orderedLangs = ['vi', 'en', 'zh', 'ko', 'ja']
  const reviewedCount = orderedLangs.filter(
    (code) => translationsByLang[code]?.status === 'Reviewed' && translationsByLang[code]?.narrationText?.trim(),
  ).length
  const canTranslateMissing = viTranslation?.status === 'Reviewed' && Boolean(viTranslation.narrationText?.trim())
  const guestPreviewPath =
    place.kind === 'Food' && place.parentId
      ? `/vi/explore`
      : `/vi/places/${encodeURIComponent(place.slug)}`

  return (
    <div className="admin-page">
      {/* Breadcrumb */}
      <div className="admin-breadcrumb">
        <Link to="/admin/places">Địa điểm</Link>
        {place.parentId && place.parentName && (
          <>
            <span>/</span>
            <Link to={`/admin/places/${place.parentId}`}>{place.parentName}</Link>
          </>
        )}
        <span>/</span>
        <strong>{placeTitle}</strong>
      </div>

      {/* Header */}
      <div className="admin-page-header">
        <div className="admin-editor-title-row">
          <h1>{placeTitle}</h1>
          <span className={`admin-badge status-${place.status.toLowerCase()}`}>
            {STATUS_LABELS[place.status] ?? place.status}
          </span>
        </div>

        <div className="admin-header-actions">
          <a href={guestPreviewPath} target="_blank" rel="noreferrer" className="btn btn-outline btn-sm">
            Xem trước ↗
          </a>
          <button type="button" className="btn btn-primary btn-sm" onClick={() => void handleSave()} disabled={saving}>
            {saving ? 'Đang lưu…' : 'Lưu'}
          </button>
        </div>
      </div>

      {feedback && (
        <div className={`admin-alert admin-alert-${feedback.type}`} role="alert">
          <div>{feedback.text}</div>
          {feedback.items && (
            <ul style={{ margin: '6px 0 0 18px', padding: 0 }}>
              {feedback.items.map((it, i) => (
                <li key={i}>{it}</li>
              ))}
            </ul>
          )}
        </div>
      )}

      {/* 4 Tabs */}
      <div className="admin-tabs" role="tablist">
        <button
          type="button"
          role="tab"
          aria-selected={activeTab === 'general'}
          className={`admin-tab ${activeTab === 'general' ? 'active' : ''}`}
          onClick={() => setTab('general')}
        >
          Thông tin chung
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={activeTab === 'location'}
          className={`admin-tab ${activeTab === 'location' ? 'active' : ''}`}
          onClick={() => setTab('location')}
        >
          Vị trí bản đồ
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={activeTab === 'narration'}
          className={`admin-tab ${activeTab === 'narration' ? 'active' : ''}`}
          onClick={() => setTab('narration')}
        >
          Thuyết minh
        </button>
        {place.kind === 'Destination' && (
          <button
            type="button"
            role="tab"
            aria-selected={activeTab === 'foods'}
            className={`admin-tab ${activeTab === 'foods' ? 'active' : ''}`}
            onClick={() => setTab('foods')}
          >
            Món ăn / Quán ({place.foods?.length ?? 0})
          </button>
        )}
      </div>

      {/* TAB 1: GENERAL */}
      {activeTab === 'general' && (
        <div className="admin-editor-grid">
          <form onSubmit={handleSave} className="admin-card admin-form-grid">
            <h2 className="admin-card-title" style={{ gridColumn: '1 / -1' }}>
              Thông tin chung
            </h2>

            <div className="field">
              <label htmlFor="ed-slug">
                Định danh URL (Slug) {place.slugLocked && <span className="admin-lock-tag">🔒 Đã khóa sau xuất bản (ADR-11)</span>}
              </label>
              <input
                id="ed-slug"
                type="text"
                value={slug}
                onChange={(e) => setSlug(e.target.value)}
                disabled={place.slugLocked}
              />
            </div>

            {place.kind === 'Destination' ? (
              <div className="field">
                <label htmlFor="ed-cat">Danh mục</label>
                <select
                  id="ed-cat"
                  className="admin-select"
                  value={category}
                  onChange={(e) => setCategory(e.target.value)}
                >
                  {CATEGORIES.map((c) => (
                    <option key={c.value} value={c.value}>
                      {c.label}
                    </option>
                  ))}
                </select>
              </div>
            ) : (
              <div className="field">
                <label htmlFor="ed-ft">Loại món ăn</label>
                <select
                  id="ed-ft"
                  className="admin-select"
                  value={foodType}
                  onChange={(e) => setFoodType(e.target.value)}
                >
                  {FOOD_TYPES.map((ft) => (
                    <option key={ft.value} value={ft.value}>
                      {ft.label}
                    </option>
                  ))}
                </select>
              </div>
            )}

            <div className="field">
              <label htmlFor="ed-addr">Địa chỉ (phường mới)</label>
              <input
                id="ed-addr"
                type="text"
                value={address}
                onChange={(e) => setAddress(e.target.value)}
                placeholder="57A Tháp Mười, phường Bình Tây"
              />
            </div>

            <div className="field">
              <label htmlFor="ed-hours">Giờ mở cửa</label>
              <input
                id="ed-hours"
                type="text"
                value={openingHours}
                onChange={(e) => setOpeningHours(e.target.value)}
                placeholder="06:00 – 18:00"
              />
            </div>

            {place.kind === 'Food' && (
              <div className="field">
                <label htmlFor="ed-price">Khoảng giá (VNĐ)</label>
                <input
                  id="ed-price"
                  type="text"
                  value={priceRange}
                  onChange={(e) => setPriceRange(e.target.value)}
                  placeholder="35.000 – 55.000 đ"
                />
              </div>
            )}

            <div className="field">
              <label htmlFor="ed-sort">Thứ tự hiển thị</label>
              <input
                id="ed-sort"
                type="number"
                value={sortOrder}
                onChange={(e) => setSortOrder(e.target.value)}
              />
            </div>

            <div className="field" style={{ gridColumn: '1 / -1' }}>
              <label htmlFor="ed-cover-url">Ảnh bìa (Đường dẫn ảnh hoặc tải file JPG/PNG/WebP ≤ 5 MB)</label>
              <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap', alignItems: 'center' }}>
                <input
                  id="ed-cover-url"
                  type="text"
                  style={{ flex: 1, minWidth: 220 }}
                  value={coverImageUrl}
                  onChange={(e) => setCoverImageUrl(e.target.value)}
                  placeholder="/uploads/covers/..."
                />
                <label className="btn btn-outline btn-sm" style={{ cursor: 'pointer' }}>
                  {uploadingCover ? 'Đang tải…' : 'Tải ảnh lên'}
                  <input
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    onChange={handleUploadCover}
                    hidden
                    disabled={uploadingCover}
                  />
                </label>
              </div>
              {coverImageUrl && (
                <div className="admin-cover-preview">
                  <img src={coverImageUrl} alt={placeTitle} />
                </div>
              )}
            </div>

            <div className="admin-lifecycle-bar" style={{ gridColumn: '1 / -1' }}>
              <div className="admin-lifecycle-actions">
                {place.status !== 'Published' && (
                  <button type="button" className="btn btn-primary btn-sm" onClick={handlePublish}>
                    Xuất bản (BR-08)
                  </button>
                )}
                {place.status === 'Published' && (
                  <button type="button" className="btn btn-outline btn-sm" onClick={handleHide}>
                    Ẩn địa điểm
                  </button>
                )}
                {place.status === 'Draft' && (
                  <button type="button" className="btn btn-danger-outline btn-sm" onClick={handleDelete}>
                    Xóa bản nháp (BR-13)
                  </button>
                )}
              </div>
              <button type="submit" className="btn btn-primary btn-sm" disabled={saving}>
                {saving ? 'Đang lưu…' : 'Lưu thay đổi'}
              </button>
            </div>
          </form>

          <div className="admin-card">
            <h2 className="admin-card-title">Mã QR tại điểm (BR-18, ADR-11)</h2>
            <QrCodePanel slug={place.slug} name={placeTitle} inline />
          </div>
        </div>
      )}

      {/* TAB 2: LOCATION */}
      {activeTab === 'location' && (
        <div className="admin-card">
          <div className="admin-location-head">
            <div>
              <h2 className="admin-card-title">Vị trí trên bản đồ Quận 6 (BR-03)</h2>
              <p className="admin-muted">
                Kéo ghim hoặc chạm lên bản đồ để đặt tọa độ. Vùng phục vụ hợp lệ: vĩ độ 10.730 – 10.765, kinh độ 106.615 – 106.665.
              </p>
            </div>
            <div className="admin-coord-inputs">
              <div className="field">
                <label htmlFor="loc-lat">Vĩ độ (Latitude)</label>
                <input
                  id="loc-lat"
                  type="number"
                  step="0.000001"
                  value={latitude}
                  onChange={(e) => setLatitude(e.target.value)}
                />
              </div>
              <div className="field">
                <label htmlFor="loc-lng">Kinh độ (Longitude)</label>
                <input
                  id="loc-lng"
                  type="number"
                  step="0.000001"
                  value={longitude}
                  onChange={(e) => setLongitude(e.target.value)}
                />
              </div>
            </div>
          </div>

          <div className="admin-map-box">
            <MapContainer
              center={[Number(latitude) || 10.749, Number(longitude) || 106.651]}
              zoom={15}
              scrollWheelZoom
            >
              <TileLayer
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
              />
              <Rectangle
                bounds={SERVICE_BOUNDS}
                pathOptions={{ color: '#147d78', weight: 2, fillOpacity: 0.05, dashArray: '6 4' }}
              />
              <MapClickHandler
                onPick={(lat, lng) => {
                  setLatitude(String(lat))
                  setLongitude(String(lng))
                }}
              />
              <Marker
                position={[Number(latitude) || 10.749, Number(longitude) || 106.651]}
                icon={pinIcon}
                draggable
                eventHandlers={{
                  dragend: (e) => {
                    const pos = e.target.getLatLng()
                    setLatitude(String(Number(pos.lat.toFixed(6))))
                    setLongitude(String(Number(pos.lng.toFixed(6))))
                  },
                }}
              />
            </MapContainer>
          </div>
        </div>
      )}

      {/* TAB 3: NARRATION */}
      {activeTab === 'narration' && (
        <>
          <div className="admin-card admin-table-card">
            <div className="admin-table-wrap">
              <table className="admin-table">
                <thead>
                  <tr>
                    <th>NGÔN NGỮ</th>
                    <th>TRẠNG THÁI</th>
                    <th>GIỌNG ĐỌC THIẾT BỊ</th>
                    <th>THAO TÁC TIẾP THEO</th>
                  </tr>
                </thead>
                <tbody>
                  {orderedLangs.map((code) => {
                    const tr = translationsByLang[code]
                    const hasText = Boolean(tr?.narrationText?.trim())
                    const isSource = code === 'vi'
                    const isSpeaking = speakingLang === code

                    let badgeClass = 'status-missing'
                    let badgeText = 'Chưa có'
                    if (hasText && tr) {
                      if (tr.status === 'Reviewed') {
                        badgeClass = 'status-published'
                        badgeText = tr.stale ? 'Đã duyệt · bản cũ' : 'Đã duyệt'
                      } else {
                        badgeClass = 'status-draft'
                        badgeText = tr.source === 'Machine' ? 'Bản nháp · dịch máy' : 'Bản nháp'
                        if (tr.stale) badgeText += ' · bản cũ'
                      }
                    }

                    return (
                      <tr key={code}>
                        <td>
                          <strong style={{ fontWeight: isSource ? 700 : 500 }}>
                            {LANG_LABELS[code] ?? code}
                          </strong>
                          {tr?.machineMatch != null && (
                            <span className="admin-match-pill">
                              độ khớp {Math.round(Number(tr.machineMatch) * 100)}%
                            </span>
                          )}
                        </td>
                        <td>
                          <span className={`admin-badge ${badgeClass}`}>{badgeText}</span>
                        </td>
                        <td>
                          {hasText ? (
                            <button
                              type="button"
                              className="admin-voice-preview-btn"
                              onClick={() => void togglePreviewVoice(code, tr?.narrationText)}
                            >
                              {isSpeaking ? <PauseIcon size={14} /> : <PlayIcon size={14} />}
                              <span>{isSpeaking ? 'Dừng' : 'Nghe thử'}</span>
                            </button>
                          ) : (
                            <span className="admin-muted">chưa có nội dung</span>
                          )}
                        </td>
                        <td>
                          <div className="admin-row-actions">
                            {!hasText && !isSource ? (
                              <>
                                <button
                                  type="button"
                                  className="btn btn-outline btn-xs"
                                  onClick={handleTranslateMissing}
                                  disabled={!canTranslateMissing || translating}
                                  title={!canTranslateMissing ? 'Cần duyệt bản tiếng Việt trước (BR-04)' : ''}
                                >
                                  Dịch tự động
                                </button>
                                <button
                                  type="button"
                                  className="btn btn-ghost btn-xs"
                                  onClick={() => openDrawer(code)}
                                >
                                  Sửa
                                </button>
                              </>
                            ) : tr?.status === 'Draft' ? (
                              <>
                                <button
                                  type="button"
                                  className="btn btn-primary btn-xs"
                                  onClick={() => openDrawer(code)}
                                >
                                  Kiểm duyệt
                                </button>
                                <button
                                  type="button"
                                  className="btn btn-outline btn-xs"
                                  onClick={() => void handleQuickApprove(code)}
                                >
                                  Duyệt ngay
                                </button>
                              </>
                            ) : (
                              <button
                                type="button"
                                className="btn btn-outline btn-xs"
                                onClick={() => openDrawer(code)}
                              >
                                Sửa
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </div>

          {/* Bottom bar */}
          <div className="admin-narration-footer-bar">
            <div>
              <strong>
                Đã duyệt {reviewedCount} / 5 ngôn ngữ
                {quota
                  ? ` · MyMemory hôm nay ${Math.round(quota.usedToday / 100) / 10}k / ${Math.round(quota.dailyLimit / 1000)}k`
                  : ''}
              </strong>
              {!canTranslateMissing && (
                <div className="admin-muted" style={{ fontSize: '0.84rem' }}>
                  BR-04: Hãy nhập và duyệt bản tiếng Việt trước khi bấm Dịch ngôn ngữ thiếu.
                </div>
              )}
            </div>
            <button
              type="button"
              className="btn btn-outline btn-sm"
              style={{ background: '#fff' }}
              onClick={handleTranslateMissing}
              disabled={!canTranslateMissing || translating}
            >
              {translating ? 'Đang dịch…' : 'Dịch ngôn ngữ thiếu'}
            </button>
          </div>

          <p className="admin-muted" style={{ marginTop: 10, fontSize: '0.86rem', fontStyle: 'italic' }}>
            Bấm Kiểm duyệt để mở ngăn kéo đối chiếu: bản gốc tiếng Việt ở bên trái, bản dịch chỉnh sửa ở bên phải. ▶ Nghe thử đọc văn bản bằng giọng của thiết bị này.
          </p>
        </>
      )}

      {/* TAB 4: FOOD SPOTS */}
      {activeTab === 'foods' && place.kind === 'Destination' && (
        <div className="admin-editor-grid">
          <div className="admin-card">
            <h2 className="admin-card-title">Danh sách món / quán thuộc {placeTitle}</h2>
            {(place.foods ?? []).length === 0 ? (
              <p className="admin-muted">Chưa có món ăn nào trong điểm đến này.</p>
            ) : (
              <div className="admin-table-wrap">
                <table className="admin-table">
                  <thead>
                    <tr>
                      <th>MÓN / QUÁN ĂN</th>
                      <th>PHÂN LOẠI</th>
                      <th>TRẠNG THÁI</th>
                      <th>THUYẾT MINH</th>
                      <th>THAO TÁC</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(place.foods ?? []).map((f) => (
                      <tr key={f.id}>
                        <td>
                          <Link to={`/admin/places/${f.id}`} className="admin-place-name">
                            {f.name}
                          </Link>
                          <div>
                            <code className="admin-slug-badge">{f.slug}</code>
                          </div>
                        </td>
                        <td>{f.foodType ? (FOOD_TYPE_LABELS[f.foodType] ?? f.foodType) : 'Món ăn'}</td>
                        <td>
                          <span className={`admin-badge status-${f.status.toLowerCase()}`}>
                            {STATUS_LABELS[f.status] ?? f.status}
                          </span>
                        </td>
                        <td>{f.reviewedLanguages}/5 đã duyệt</td>
                        <td>
                          <Link to={`/admin/places/${f.id}`} className="btn btn-soft btn-xs">
                            Sửa
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <form onSubmit={handleQuickAddFood} className="admin-card">
            <h2 className="admin-card-title">Thêm nhanh món ăn con</h2>
            <div className="field">
              <label htmlFor="qf-name">Tên món (tiếng Việt)</label>
              <input
                id="qf-name"
                type="text"
                value={newFoodName}
                onChange={(e) => setNewFoodName(e.target.value)}
                placeholder="Bánh canh cua Chợ Bình Tây"
                required
              />
            </div>
            <div className="field">
              <label htmlFor="qf-slug">Định danh URL (Slug)</label>
              <input
                id="qf-slug"
                type="text"
                value={newFoodSlug}
                onChange={(e) => setNewFoodSlug(e.target.value)}
                placeholder="banh-canh-cua-cho-binh-tay"
                required
              />
            </div>
            <div className="field">
              <label htmlFor="qf-type">Loại món</label>
              <select
                id="qf-type"
                className="admin-select"
                value={newFoodType}
                onChange={(e) => setNewFoodType(e.target.value)}
              >
                {FOOD_TYPES.map((ft) => (
                  <option key={ft.value} value={ft.value}>
                    {ft.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="field">
              <label htmlFor="qf-price">Khoảng giá</label>
              <input
                id="qf-price"
                type="text"
                value={newFoodPrice}
                onChange={(e) => setNewFoodPrice(e.target.value)}
                placeholder="40.000 – 60.000 đ"
              />
            </div>
            <button type="submit" className="btn btn-primary btn-sm" disabled={addingFood}>
              {addingFood ? 'Đang thêm…' : '+ Thêm món ăn'}
            </button>
          </form>
        </div>
      )}

      {/* Side Drawer for Reviewing / Editing Translation */}
      {drawerLang && (
        <div className="admin-drawer-backdrop" role="dialog" aria-modal="true" onClick={() => setDrawerLang(null)}>
          <div className="admin-drawer" onClick={(e) => e.stopPropagation()}>
            <div className="admin-modal-header">
              <div>
                <h3>Kiểm duyệt & Chỉnh sửa · {LANG_LABELS[drawerLang] ?? drawerLang}</h3>
                <p className="admin-muted">
                  Tối đa 4.000 ký tự (BR-04). Sửa văn bản sẽ chuyển trạng thái về Bản nháp cho tới khi bấm Lưu & Duyệt (BR-07).
                </p>
              </div>
              <button type="button" className="btn btn-ghost" onClick={() => setDrawerLang(null)} aria-label="Đóng">
                <CloseIcon />
              </button>
            </div>

            <div className="admin-drawer-body">
              {drawerLang !== 'vi' && (
                <div className="admin-drawer-source">
                  <h4>Bản gốc tiếng Việt (vi)</h4>
                  <div className="admin-source-box">
                    <strong>{viTranslation?.name ?? place.slug}</strong>
                    <p>{viTranslation?.narrationText || '(Chưa có bài thuyết minh tiếng Việt)'}</p>
                  </div>
                </div>
              )}

              <div className="admin-drawer-editor">
                <div className="field">
                  <label htmlFor="dr-name">Tên địa điểm / món ({drawerLang})</label>
                  <input
                    id="dr-name"
                    type="text"
                    value={drawerName}
                    onChange={(e) => setDrawerName(e.target.value)}
                    required
                  />
                </div>

                <div className="field">
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <label htmlFor="dr-text">Bài thuyết minh ({drawerLang})</label>
                    <span className="admin-muted" style={{ fontSize: '0.82rem' }}>
                      {drawerText.length} / 4000 ký tự
                    </span>
                  </div>
                  <textarea
                    id="dr-text"
                    className="admin-textarea"
                    rows={10}
                    maxLength={4000}
                    value={drawerText}
                    onChange={(e) => setDrawerText(e.target.value)}
                    placeholder="Nhập bài thuyết minh ngắn gọn theo từng câu…"
                  />
                </div>

                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 10 }}>
                  <button
                    type="button"
                    className="btn btn-soft btn-sm"
                    onClick={() => void togglePreviewVoice(drawerLang, drawerText)}
                    disabled={!drawerText.trim()}
                  >
                    {speakingLang === drawerLang ? <PauseIcon size={16} /> : <PlayIcon size={16} />}
                    <span>{speakingLang === drawerLang ? 'Dừng đọc thử' : '▶ Nghe thử bằng giọng thiết bị'}</span>
                  </button>

                  <div style={{ display: 'flex', gap: 10 }}>
                    <button
                      type="button"
                      className="btn btn-outline btn-sm"
                      onClick={() => void handleSaveTranslation(false)}
                      disabled={drawerBusy}
                    >
                      Lưu bản nháp
                    </button>
                    <button
                      type="button"
                      className="btn btn-primary btn-sm"
                      onClick={() => void handleSaveTranslation(true)}
                      disabled={drawerBusy || !drawerText.trim()}
                    >
                      Lưu & Duyệt
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
