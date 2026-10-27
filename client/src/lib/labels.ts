import type { TFunction } from 'i18next'
import type { Lang } from './languages'

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

const SAMPLE_PREFIX: Record<Lang, string> = {
  vi: '[Mẫu]',
  en: '[Sample]',
  zh: '［示例］',
  ko: '[샘플]',
  ja: '［サンプル］',
}

const KNOWN_TITLES: Record<string, Record<Lang, string>> = {
  'Đình Bình Tiên': {
    vi: 'Đình Bình Tiên',
    en: 'Binh Tien Communal Temple',
    zh: '平仙亭',
    ko: '빈띠엔 사당',
    ja: 'ビンティエン寺院',
  },
  'Chợ Bình Tây': {
    vi: 'Chợ Bình Tây',
    en: 'Binh Tay Market',
    zh: '平西市场',
    ko: '빈떠이 시장',
    ja: 'ビンタイ市場',
  },
  'Phố ẩm thực Cư xá Phú Lâm': {
    vi: 'Phố ẩm thực Cư xá Phú Lâm',
    en: 'Phu Lam Food Street',
    zh: '富林美食街',
    ko: '푸럼 음식 거리',
    ja: 'フーラム美食街',
  },
  'Công viên Bình Phú': {
    vi: 'Công viên Bình Phú',
    en: 'Binh Phu Park',
    zh: '平富公园',
    ko: '빈푸 공원',
    ja: 'ビンフー公園',
  },
  '[Mẫu] Phở bò': {
    vi: '[Mẫu] Phở bò',
    en: '[Sample] Beef Pho Noodle Soup',
    zh: '［示例］牛肉河粉',
    ko: '[샘플] 소고기 쌀국수 퍼보',
    ja: '［サンプル］牛肉フォー',
  },
  '[Mẫu] Cơm tấm sườn': {
    vi: '[Mẫu] Cơm tấm sườn',
    en: '[Sample] Broken Rice with Grilled Pork Chop',
    zh: '［示例］烤猪排碎米饭',
    ko: '[샘플] 돼지갈비 껌떰',
    ja: '［サンプル］豚スペアリブのコムタム',
  },
  '[Mẫu] Hủ tiếu Nam Vang': {
    vi: '[Mẫu] Hủ tiếu Nam Vang',
    en: '[Sample] Phnom Penh Style Noodle Soup',
    zh: '［示例］金边粿条',
    ko: '[샘플] 남방식 후띠에우',
    ja: '［サンプル］プノンペン風フーティウ',
  },
  '[Mẫu] Bánh bao Chợ Lớn': {
    vi: '[Mẫu] Bánh bao Chợ Lớn',
    en: '[Sample] Cholon Steamed Pork Bun',
    zh: '［示例］堤岸叉烧大包',
    ko: '[샘플] 쩌런식 찐빵 반바오',
    ja: '［サンプル］チョロン風中華まん',
  },
  '[Mẫu] Sâm bổ lượng': {
    vi: '[Mẫu] Sâm bổ lượng',
    en: '[Sample] Cholon Herbal Sweet Soup',
    zh: '［示例］清补凉甜汤',
    ko: '[샘플] 한방 냉디저트 섬보르엉',
    ja: '［サンプル］漢方スイーツ・サムボールオン',
  },
  '[Mẫu] Chè Tàu xíu': {
    vi: '[Mẫu] Chè Tàu xíu',
    en: '[Sample] Chinese Style Sweet Soup',
    zh: '［示例］广式传统糖水',
    ko: '[샘플] 화교식 전통 디저트 쩨',
    ja: '［サンプル］中華風お汁粉チェー',
  },
  '[Mẫu] Bánh xèo': {
    vi: '[Mẫu] Bánh xèo',
    en: '[Sample] Crispy Vietnamese Savoury Pancake',
    zh: '［示例］越南香脆煎饼',
    ko: '[샘플] 베트남식 부침개 반쎄오',
    ja: '［サンプル］ベトナム風お好み焼きバインセオ',
  },
  '[Mẫu] Hủ tiếu mì': {
    vi: '[Mẫu] Hủ tiếu mì',
    en: '[Sample] Rice & Egg Noodle Soup',
    zh: '［示例］鲜虾猪肉粿条面',
    ko: '[샘플] 쌀국수와 에그누들 후띠에우미',
    ja: '［サンプル］米麺と中華麺のフーティウミー',
  },
  '[Mẫu] Chè ba màu': {
    vi: '[Mẫu] Chè ba màu',
    en: '[Sample] Three-Color Sweet Bean Dessert',
    zh: '［示例］三色椰奶甜汤',
    ko: '[샘플] 삼색 코코넛 디저트 쩨바마우',
    ja: '［サンプル］三色チェー',
  },
  '[Mẫu] Cơm chiên Dương Châu': {
    vi: '[Mẫu] Cơm chiên Dương Châu',
    en: '[Sample] Yangzhou Style Fried Rice',
    zh: '［示例］扬州炒饭',
    ko: '[샘플] 양주식 볶음밥 껌찌엔',
    ja: '［サンプル］五目チャーハン',
  },
  '[Mẫu] Nước mía': {
    vi: '[Mẫu] Nước mía',
    en: '[Sample] Fresh Sugarcane Juice with Kumquat',
    zh: '［示例］鲜榨金桔甘蔗汁',
    ko: '[샘플] 금귤 사탕수수 주스 느억미아',
    ja: '［サンプル］絞りたてサトウキビジュース',
  },
  '[Mẫu] Bột chiên': {
    vi: '[Mẫu] Bột chiên',
    en: '[Sample] Pan-Fried Crispy Rice Flour Cakes',
    zh: '［示例］香煎鸡蛋萝卜糕',
    ko: '[샘플] 계란 쌀떡구이 봇찌엔',
    ja: '［サンプル］揚げ餅の卵焼きボッチエン',
  },
}

/**
 * Localizes place/food titles:
 * - Uses the translated title if available in KNOWN_TITLES (when DB falls back to vi).
 * - Otherwise, for non-Vietnamese languages, replaces "[Mẫu]" and strips Vietnamese diacritics.
 */
export function localizeTitle(name: string | null | undefined, lang: Lang): string {
  if (!name) return ''
  if (lang === 'vi') return name
  const known = KNOWN_TITLES[name.trim()]
  if (known) return known[lang]
  const replaced = name.replace(/\[Mẫu\]/gi, SAMPLE_PREFIX[lang])
  return stripDiacritics(replaced)
}

/**
 * Localizes Vietnamese ward/city names and strips diacritics from street addresses when `lang !== 'vi'`.
 */
export function localizeAddress(address: string | null | undefined, lang: Lang): string | null {
  if (!address) return null
  if (lang === 'vi') return address

  const wardReplacements: Array<[RegExp, Record<Exclude<Lang, 'vi'>, string>]> = [
    [
      /phường\s+Bình\s+Tây/gi,
      {
        en: 'Binh Tay Ward',
        zh: '平西坊',
        ko: '빈떠이동',
        ja: 'ビンタイ坊',
      },
    ],
    [
      /phường\s+Phú\s+Lâm/gi,
      {
        en: 'Phu Lam Ward',
        zh: '富林坊',
        ko: '푸럼동',
        ja: 'フーラム坊',
      },
    ],
    [
      /phường\s+Bình\s+Phú/gi,
      {
        en: 'Binh Phu Ward',
        zh: '平富坊',
        ko: '빈푸동',
        ja: 'ビンフー坊',
      },
    ],
    [
      /TP\.?\s*HCM|Thành\s+phố\s+Hồ\s+Chí\s+Minh/gi,
      {
        en: 'Ho Chi Minh City',
        zh: '胡志明市',
        ko: '호찌민시',
        ja: 'ホーチミン市',
      },
    ],
  ]

  let result = address
  for (const [pattern, map] of wardReplacements) {
    result = result.replace(pattern, map[lang])
  }
  return stripDiacritics(result)
}

/**
 * Formats Vietnamese price strings ("40.000đ - 70.000đ") into international format ("40,000 - 70,000 VND") when `lang !== 'vi'`.
 */
export function localizePrice(price: string | null | undefined, lang: Lang): string | null {
  if (!price) return null
  if (lang === 'vi') return price
  return stripDiacritics(price)
    .replace(/(\d+)\.(\d{3})/g, '$1,$2')
    .replace(/\s*d\b/gi, ' VND')
}
