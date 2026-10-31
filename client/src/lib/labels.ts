import type { TFunction } from 'i18next'

export const categoryLabel = (t: TFunction, category: string | null | undefined) =>
  category ? t(`category.${category}`, { defaultValue: category }) : null

export const foodTypeLabel = (t: TFunction, foodType: string | null | undefined) =>
  foodType ? t(`foodType.${foodType}`, { defaultValue: foodType }) : null

/** "a · b · c" skipping empties. */
export const joinMeta = (...parts: (string | null | undefined | false)[]) => parts.filter(Boolean).join(' · ')

/** Strip Vietnamese diacritics ("Chợ Bình Tây" -> "Cho Binh Tay"). */
export function stripDiacritics(s: string): string {
  return s
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
}

/** Case- and diacritic-insensitive match so "cho binh tay" finds "Chợ Bình Tây". */
export function matches(haystack: string, needle: string): boolean {
  return stripDiacritics(haystack).toLowerCase().includes(stripDiacritics(needle.trim()).toLowerCase())
}

/**
 * BR-01: names and addresses come from the database. If a translation is missing the API falls back
 * to Vietnamese and the page shows exactly that — the client must not invent translations.
 * Only the number format is localized here.
 */
export function localizePrice(price: string | null | undefined, lang: string): string | null {
  if (!price) return null
  if (lang === 'vi') return price
  return price
    .replace(/(\d{1,3}(?:\.\d{3})+)/g, (groups) => groups.replace(/\./g, ','))
    .replace(/\s*đ/gi, ' VND')
}
