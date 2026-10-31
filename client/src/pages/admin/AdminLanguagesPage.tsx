import { useEffect, useState } from 'react'
import { getAdminLanguages, updateAdminLanguage } from '../../api/admin'
import type { Language } from '../../api/types'
import { PauseIcon, PlayIcon } from '../../components/Icons'
import { ErrorState, Loading } from '../../components/States'
import { useAsync } from '../../hooks/useAsync'
import { loadVoices, pickVoice, speechSupported } from '../../lib/speech'

const SAMPLE_SENTENCES: Record<string, string> = {
  vi: 'Chào mừng bạn đến với Chợ Bình Tây, trung tâm giao thương sầm uất của khu vực Chợ Lớn.',
  en: 'Welcome to Binh Tay Market, the bustling trading heart of Saigon Cholon.',
  zh: '欢迎来到平西市场，这里是堤岸地区热闹的贸易中心。',
  ko: '쩌런 지역의 활기찬 교역 중심지인 빈떠이 시장에 오신 것을 환영합니다.',
  ja: 'チョロン地区の活気ある交易の中心、ビンタイ市場へようこそ。',
}

const LANGUAGE_NAMES: Record<string, string> = {
  vi: 'Tiếng Việt',
  en: 'Tiếng Anh (English)',
  zh: 'Tiếng Trung (中文)',
  ko: 'Tiếng Hàn (한국어)',
  ja: 'Tiếng Nhật (日本語)',
}

/**
 * SCR-A6 — Ngôn ngữ (/admin/languages).
 */
export default function AdminLanguagesPage() {
  const loader = useAsync('admin:languages', (signal) => getAdminLanguages(signal))
  const [edits, setEdits] = useState<Record<string, Partial<Language>>>({})
  const [savingCode, setSavingCode] = useState<string | null>(null)
  const [speakingCode, setSpeakingCode] = useState<string | null>(null)
  const [feedback, setFeedback] = useState<{ type: 'success' | 'danger'; text: string } | null>(null)

  useEffect(() => {
    return () => {
      if (speechSupported()) window.speechSynthesis.cancel()
    }
  }, [])

  if (loader.loading && !loader.data) return <Loading />
  if (loader.error || !loader.data) return <ErrorState error={loader.error} onRetry={loader.reload} />

  const items: Language[] = loader.data.map((l) => ({ ...l, ...edits[l.code] }))

  const updateLocal = (code: string, patch: Partial<Language>) => {
    setEdits((prev) => ({ ...prev, [code]: { ...prev[code], ...patch } }))
  }

  const handleSave = async (lang: Language) => {
    setSavingCode(lang.code)
    setFeedback(null)
    try {
      const updated = await updateAdminLanguage(lang.code, {
        mtCode: lang.mtCode.trim(),
        speechLang: lang.speechLang.trim(),
        isEnabled: lang.isEnabled,
      })
      updateLocal(lang.code, updated)
      setFeedback({ type: 'success', text: `Đã cập nhật cấu hình ngôn ngữ [${lang.code}].` })
    } catch (err) {
      setFeedback({ type: 'danger', text: err instanceof Error ? err.message : 'Cập nhật thất bại.' })
    } finally {
      setSavingCode(null)
    }
  }

  const handleTestVoice = async (lang: Language) => {
    if (!speechSupported()) return
    const synth = window.speechSynthesis
    if (speakingCode === lang.code) {
      synth.cancel()
      setSpeakingCode(null)
      return
    }
    synth.cancel()
    const voices = await loadVoices()
    const picked = pickVoice(voices, lang.speechLang)
    const sample = SAMPLE_SENTENCES[lang.code] ?? SAMPLE_SENTENCES.en
    const u = new SpeechSynthesisUtterance(sample)
    if (picked) {
      u.voice = picked
      u.lang = picked.lang
    } else {
      u.lang = lang.speechLang
    }
    u.onend = () => setSpeakingCode(null)
    u.onerror = () => setSpeakingCode(null)
    setSpeakingCode(lang.code)
    synth.speak(u)
  }

  return (
    <div className="admin-page">
      <div className="admin-page-header">
        <div>
          <h1>Ngôn ngữ</h1>
          <p className="admin-muted">
            Cấu hình {items.length} ngôn ngữ: mã dịch máy MyMemory (<code>MtCode</code>), mã giọng đọc BCP-47 (<code>SpeechLang</code>) và nghe thử trên thiết bị
          </p>
        </div>
      </div>

      {feedback && (
        <div className={`admin-alert admin-alert-${feedback.type}`} role="alert">
          {feedback.text}
        </div>
      )}

      <div className="admin-card admin-table-card">
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>MÃ</th>
                <th>NGÔN NGỮ</th>
                <th>MÃ DỊCH MÁY (MYMEMORY)</th>
                <th>MÃ GIỌNG ĐỌC (BCP-47)</th>
                <th>TRẠNG THÁI</th>
                <th>KIỂM TRA GIỌNG ĐỌC</th>
                <th>THAO TÁC</th>
              </tr>
            </thead>
            <tbody>
              {items.map((l) => {
                const isSource = l.code === 'vi'
                return (
                  <tr key={l.code}>
                    <td>
                      <code>{l.code}</code>
                    </td>
                    <td>
                      <strong>{LANGUAGE_NAMES[l.code] ?? l.code}</strong>
                      {isSource && <span className="admin-tag" style={{ marginLeft: 8 }}>Ngôn ngữ nguồn</span>}
                    </td>
                    <td>
                      <input
                        type="text"
                        className="admin-inline-input"
                        value={l.mtCode}
                        onChange={(e) => updateLocal(l.code, { mtCode: e.target.value })}
                        aria-label={`Mã MyMemory cho ${l.code}`}
                      />
                    </td>
                    <td>
                      <input
                        type="text"
                        className="admin-inline-input"
                        value={l.speechLang}
                        onChange={(e) => updateLocal(l.code, { speechLang: e.target.value })}
                        aria-label={`Mã SpeechLang cho ${l.code}`}
                      />
                    </td>
                    <td>
                      <label className="admin-switch">
                        <input
                          type="checkbox"
                          checked={l.isEnabled}
                          disabled={isSource}
                          onChange={(e) => updateLocal(l.code, { isEnabled: e.target.checked })}
                        />
                        <span>{l.isEnabled ? 'Đang bật' : 'Đang tắt'}</span>
                      </label>
                    </td>
                    <td>
                      <button
                        type="button"
                        className="admin-voice-preview-btn"
                        onClick={() => void handleTestVoice(l)}
                      >
                        {speakingCode === l.code ? <PauseIcon size={14} /> : <PlayIcon size={14} />}
                        <span>{speakingCode === l.code ? 'Dừng' : 'Nghe thử'}</span>
                      </button>
                    </td>
                    <td>
                      <button
                        type="button"
                        className="btn btn-primary btn-xs"
                        onClick={() => void handleSave(l)}
                        disabled={savingCode === l.code}
                      >
                        {savingCode === l.code ? 'Đang lưu…' : 'Lưu'}
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
