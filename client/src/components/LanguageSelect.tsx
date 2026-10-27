import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { useLang } from '../hooks/useLang'
import { useMediaQuery } from '../hooks/useMediaQuery'
import { useSwitchLang } from '../hooks/useSwitchLang'
import { LANGS, LANG_INFO, type Lang } from '../lib/languages'

/** `short` = "VI ▾" on phones (mockup), full names from 600px up unless `alwaysFull`. */
export function LanguageSelect({ alwaysFull = false }: { alwaysFull?: boolean }) {
  const { t } = useTranslation()
  const id = useId()
  const lang = useLang()
  const switchLang = useSwitchLang()
  const wide = useMediaQuery('(min-width: 600px)')
  const full = alwaysFull || wide

  return (
    <div className="lang-select">
      <label className="visually-hidden" htmlFor={id}>
        {t('nav.language')}
      </label>
      <select id={id} value={lang} onChange={(e) => switchLang(e.target.value as Lang)}>
        {LANGS.map((l) => (
          <option key={l} value={l} lang={l}>
            {full ? LANG_INFO[l].label : LANG_INFO[l].short}
          </option>
        ))}
      </select>
    </div>
  )
}
