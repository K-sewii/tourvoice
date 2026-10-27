import type { TFunction } from 'i18next'

export const categoryLabel = (t: TFunction, category: string | null | undefined) =>
  category ? t(`category.${category}`, { defaultValue: category }) : null

export const foodTypeLabel = (t: TFunction, foodType: string | null | undefined) =>
  foodType ? t(`foodType.${foodType}`, { defaultValue: foodType }) : null

/** "a · b · c" skipping empties. */
export const joinMeta = (...parts: (string | null | undefined | false)[]) => parts.filter(Boolean).join(' · ')

/** Case- and diacritic-insensitive match so "cho binh tay" finds "Chợ Bình Tây". */
export function matches(haystack: string, needle: string): boolean {
  const norm = (s: string) =>
    s
      .normalize('NFD')
      .replace(/\p{M}/gu, '')
      .replace(/đ/g, 'd')
      .replace(/Đ/g, 'D')
      .toLowerCase()
  return norm(haystack).includes(norm(needle.trim()))
}
