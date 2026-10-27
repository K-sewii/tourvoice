import { request } from './http'
import type { FavoritePlace, ListenHistoryItem, UserProfile, Visits } from './types'

const q = (lang: string) => `lang=${encodeURIComponent(lang)}`

export const getProfile = () => request<UserProfile>('GET', '/me')
export const getFavorites = (lang: string) => request<FavoritePlace[]>('GET', `/me/favorites?${q(lang)}`)
export const addFavorite = (placeId: number) => request<void>('POST', `/me/favorites/${placeId}`)
export const removeFavorite = (placeId: number) => request<void>('DELETE', `/me/favorites/${placeId}`)
export const getVisits = (lang: string) => request<Visits>('GET', `/me/visits?${q(lang)}`)
export const markVisited = (placeId: number) => request<void>('POST', `/me/visits/${placeId}`)
export const getHistory = (lang: string) => request<ListenHistoryItem[]>('GET', `/me/history?${q(lang)}`)
