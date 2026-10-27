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
