import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import { detectLang } from '../lib/languages'
import en from './locales/en'
import ja from './locales/ja'
import ko from './locales/ko'
import vi from './locales/vi'
import zh from './locales/zh'

void i18n.use(initReactI18next).init({
  resources: {
    vi: { translation: vi },
    en: { translation: en },
    zh: { translation: zh },
    ko: { translation: ko },
    ja: { translation: ja },
  },
  lng: detectLang(),
  fallbackLng: 'en',
  interpolation: { escapeValue: false },
})

export default i18n
