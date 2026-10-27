export const LANGS = ['vi', 'en', 'zh', 'ko', 'ja'] as const
export type Lang = (typeof LANGS)[number]

export const LANG_INFO: Record<Lang, { label: string; short: string; speech: string }> = {
  vi: { label: 'Tiếng Việt', short: 'VI', speech: 'vi-VN' },
  en: { label: 'English', short: 'EN', speech: 'en-US' },
  zh: { label: '中文', short: '中', speech: 'zh-CN' },
  ko: { label: '한국어', short: '한', speech: 'ko-KR' },
  ja: { label: '日本語', short: '日', speech: 'ja-JP' },
}

const STORAGE_KEY = 'tv.lang'

export const isLang = (value: unknown): value is Lang =>
  typeof value === 'string' && (LANGS as readonly string[]).includes(value)

export function getSavedLang(): Lang | null {
  try {
    const v = localStorage.getItem(STORAGE_KEY)
    return isLang(v) ? v : null
  } catch {
    return null
  }
}

export function saveLang(lang: Lang) {
  try {
    localStorage.setItem(STORAGE_KEY, lang)
  } catch {
    // ignore
  }
}

/** PRD "Phân phối nội dung đa ngôn ngữ": saved choice → navigator.language (if one of the 5) → vi. */
export function detectLang(saved: string | null = getSavedLang(), navLangs: readonly string[] = navigator.languages ?? [navigator.language]): Lang {
  if (isLang(saved)) return saved
  for (const l of navLangs) {
    const base = l?.toLowerCase().split('-')[0]
    if (isLang(base)) return base
  }
  return 'vi'
}
