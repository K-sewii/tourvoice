// Mirrors TourVoice.BLL.Dtos (camelCase JSON).

export type PlaceKind = 'Destination' | 'Food'

export interface PlaceSummary {
  id: number
  slug: string
  name: string
  kind: PlaceKind
  category: string | null
  foodType: string | null
  coverImageUrl: string | null
  latitude: number
  longitude: number
  address: string | null
  priceRange: string | null
  openingHours: string | null
}

export interface FoodPlaceSummary {
  id: number
  slug: string
  name: string
  foodType: string | null
  coverImageUrl: string | null
  latitude: number
  longitude: number
  address: string | null
  priceRange: string | null
  openingHours: string | null
}

export interface PlaceDetail {
  id: number
  slug: string
  name: string
  narrationText: string | null
  kind: PlaceKind
  category: string | null
  foodType: string | null
  coverImageUrl: string | null
  latitude: number
  longitude: number
  address: string | null
  priceRange: string | null
  openingHours: string | null
  /** Language of the returned text; differs from the requested one when fallbackUsed (BR-01). */
  languageCode: string
  fallbackUsed: boolean
  parentSlug: string | null
  parentName: string | null
  foodPlaces: FoodPlaceSummary[]
  /** Only set when the request carries a Customer JWT. */
  isFavorite: boolean | null
  isVisited: boolean | null
}

/** One row of GET /explore: a destination or a food spot (with its parent). */
export interface ExploreItem {
  id: number
  slug: string
  name: string
  kind: PlaceKind
  category: string | null
  foodType: string | null
  coverImageUrl: string | null
  latitude: number
  longitude: number
  address: string | null
  priceRange: string | null
  openingHours: string | null
  parentSlug: string | null
  parentName: string | null
  /** Geofence trigger radius in metres (default 100 on the server). */
  geofenceRadiusM?: number
}

export interface MapMarker {
  id: number
  slug: string
  name: string
  kind: PlaceKind
  category: string | null
  foodType: string | null
  latitude: number
  longitude: number
  parentSlug: string | null
}

export interface Language {
  code: string
  mtCode: string
  speechLang: string
  isEnabled: boolean
}

export interface AuthResponse {
  token: string
  email: string
  fullName: string
  role: string
}

export interface UserProfile {
  id: number
  email: string
  fullName: string
  role: string
  preferredLanguageCode: string | null
}

/** A place the customer saved/visited/listened to; parentSlug is set for food spots. */
export interface MyPlace {
  placeId: number
  slug: string
  name: string
  kind: PlaceKind
  parentSlug: string | null
}

export interface FavoritePlace {
  place: MyPlace
  createdAt: string
}

export interface VisitedPlace {
  place: MyPlace
  visitedAt: string
}

/** BR-10 progress: visited destinations / published destinations. */
export interface Visits {
  items: VisitedPlace[]
  visitedDestinations: number
  publishedDestinations: number
}

export interface ListenHistoryItem {
  place: MyPlace
  languageCode: string
  listenedSeconds: number
  playedAt: string
}

// ===== Admin DTOs =====

export type PlaceStatus = 'Draft' | 'Published' | 'Hidden'
export type TranslationStatus = 'Draft' | 'Reviewed'
export type TranslationSource = 'Machine' | 'Manual'

export interface Quota {
  usedToday: number
  dailyLimit: number
  blockThreshold: number
  percent: number
  average7Days: number
  averageDays: number
}

export interface DayStat {
  date: string
  listens: number
  sessions: number
  qrScans: number
  peakConcurrent: number
}

export interface PresenceSettings {
  refreshSeconds: number
  ttlSeconds: number
}

export interface DashboardData {
  live: number
  publishedPlaces: number
  draftTranslations: number
  today: DayStat
  quota: Quota
  days: DayStat[]
  presence: PresenceSettings
}

export interface AccessStats {
  days: DayStat[]
}

export interface UsageLog {
  createdAt: string
  characters: number
  chunks: number
  success: boolean
  errorCode: string | null
}

export interface UsageData {
  quota: Quota
  recent: UsageLog[]
  recentLimit: number
}

export interface PresenceSnapshot {
  live: number
  byPlace: Record<string, number>
  atUtc: string
}

export interface AdminTranslation {
  languageCode: string
  name: string
  narrationText: string | null
  status: TranslationStatus
  source: TranslationSource
  stale: boolean
  machineMatch: number | null
}

export interface AdminPlaceNode {
  id: number
  slug: string
  name: string
  kind: PlaceKind
  category: string | null
  foodType: string | null
  status: PlaceStatus
  sortOrder: number
  coverImageUrl: string | null
  hasVietnameseNarration: boolean
  reviewedLanguages: number
  draftLanguages: number
  children: AdminPlaceNode[]
}

export interface AdminPlaceDetail {
  id: number
  slug: string
  kind: PlaceKind
  category: string | null
  foodType: string | null
  parentId: number | null
  latitude: number
  longitude: number
  coverImageUrl: string | null
  status: PlaceStatus
  sortOrder: number
  address: string | null
  priceRange: string | null
  openingHours: string | null
  slugLocked: boolean
  parentName: string | null
  parentSlug: string | null
  translations: AdminTranslation[]
  foods: AdminPlaceNode[] | null
  geofenceRadiusM?: number
}

export interface CreatePlaceRequest {
  slug: string
  kind: PlaceKind
  category: string | null
  foodType: string | null
  parentId: number | null
  latitude: number
  longitude: number
  sortOrder: number
  address?: string | null
  priceRange?: string | null
  openingHours?: string | null
  name?: string | null
}

export interface UpdatePlaceRequest {
  category: string | null
  foodType: string | null
  latitude: number
  longitude: number
  coverImageUrl: string | null
  sortOrder: number
  address?: string | null
  priceRange?: string | null
  openingHours?: string | null
  slug?: string | null
  geofenceRadiusM?: number
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface LanguageTranslationResult {
  languageCode: string
  success: boolean
  charactersUsed: number
  errorMessage: string | null
  skipped: boolean
}

export interface TranslatePlaceResult {
  results: LanguageTranslationResult[]
}

export interface UpdateTranslationRequest {
  name: string
  narrationText: string | null
}

export interface MatrixCell {
  status: 'None' | 'Draft' | 'Reviewed'
  stale: boolean
}

export interface MatrixRow {
  placeId: number
  slug: string
  name: string
  kind: PlaceKind
  status: PlaceStatus
  parentName: string | null
  cells: Record<string, MatrixCell>
  canTranslate: boolean
}

export interface NarrationMatrix {
  languages: Language[]
  rows: MatrixRow[]
}

export interface UpdateLanguageRequest {
  mtCode: string
  speechLang: string
  isEnabled: boolean
}

export interface AdminUser {
  id: number
  email: string
  fullName: string
  role: 'Customer' | 'Admin'
  isActive: boolean
}

export interface CreateAdminRequest {
  email: string
  password: string
  fullName: string
}

/** Map radius settings (km) */
export const MapRadiusDefaultKm = 0.1
export const MapRadiusMaxKm = 0.5
export const MapRadiusMinKm = 0.05


/** Default center for the map (District 6, HCMC) */
export const MapDefaultCenter: [number, number] = [10.748, 106.64]
