import { useCallback } from 'react'
import { useParams } from 'react-router-dom'
import { isLang, type Lang } from '../lib/languages'

/** Current UI/content language from the `/:lang` route segment. */
export function useLang(): Lang {
  const { lang } = useParams()
  return isLang(lang) ? lang : 'en'
}

/** Builds `/:lang/...` links for the current language. */
export function useLocalePath() {
  const lang = useLang()
  return useCallback((path = '') => `/${lang}${path.startsWith('/') || path === '' ? path : `/${path}`}`, [lang])
}
