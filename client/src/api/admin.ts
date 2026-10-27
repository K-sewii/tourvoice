import { request } from './http'
import type {
  AccessStats,
  AdminPlaceDetail,
  AdminPlaceNode,
  AdminTranslation,
  AdminUser,
  CreateAdminRequest,
  CreatePlaceRequest,
  DashboardData,
  Language,
  NarrationMatrix,
  PagedResult,
  TranslatePlaceResult,
  UpdateLanguageRequest,
  UpdatePlaceRequest,
  UpdateTranslationRequest,
  UsageData,
} from './types'

export const getDashboard = (signal?: AbortSignal) => request<DashboardData>('GET', '/admin/dashboard', undefined, signal)

export const getAccessStats = (days = 14, signal?: AbortSignal) =>
  request<AccessStats>('GET', `/admin/stats/access?days=${days}`, undefined, signal)

export const getUsage = (signal?: AbortSignal) => request<UsageData>('GET', '/admin/usage', undefined, signal)

export const getAdminPlacesTree = (status?: string, page = 1, pageSize = 20, signal?: AbortSignal) => {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (status) params.set('status', status)
  return request<PagedResult<AdminPlaceNode>>('GET', `/admin/places?${params}`, undefined, signal)
}

export const getAdminPlace = (id: number, signal?: AbortSignal) =>
  request<AdminPlaceDetail>('GET', `/admin/places/${id}`, undefined, signal)

export const createAdminPlace = (body: CreatePlaceRequest) => request<AdminPlaceDetail>('POST', '/admin/places', body)

export const updateAdminPlace = (id: number, body: UpdatePlaceRequest) =>
  request<AdminPlaceDetail>('PUT', `/admin/places/${id}`, body)

export const publishAdminPlace = (id: number) => request<void>('POST', `/admin/places/${id}/publish`)

export const hideAdminPlace = (id: number) => request<void>('POST', `/admin/places/${id}/hide`)

export const deleteAdminPlace = (id: number) => request<void>('DELETE', `/admin/places/${id}`)

export const uploadPlaceCover = (id: number, file: File) => {
  const form = new FormData()
  form.append('file', file)
  return request<AdminPlaceDetail>('POST', `/admin/places/${id}/cover`, form)
}

export const translatePlace = (placeId: number) =>
  request<TranslatePlaceResult>('POST', `/admin/places/${placeId}/translate`)

export const updatePlaceTranslation = (placeId: number, lang: string, body: UpdateTranslationRequest) =>
  request<AdminTranslation>('PUT', `/admin/places/${placeId}/translations/${encodeURIComponent(lang)}`, body)

export const approvePlaceTranslation = (placeId: number, lang: string) =>
  request<void>('POST', `/admin/places/${placeId}/translations/${encodeURIComponent(lang)}/approve`)

export const getNarrationMatrix = (filter?: string, signal?: AbortSignal) => {
  const qs = filter ? `?filter=${encodeURIComponent(filter)}` : ''
  return request<NarrationMatrix>('GET', `/admin/narrations/matrix${qs}`, undefined, signal)
}

export const getAdminLanguages = (signal?: AbortSignal) =>
  request<Language[]>('GET', '/admin/languages', undefined, signal)

export const updateAdminLanguage = (code: string, body: UpdateLanguageRequest) =>
  request<Language>('PUT', `/admin/languages/${encodeURIComponent(code)}`, body)

export const getAdminUsers = (search?: string, role?: string, page = 1, pageSize = 20, signal?: AbortSignal) => {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (search) params.set('search', search)
  if (role) params.set('role', role)
  return request<PagedResult<AdminUser>>('GET', `/admin/users?${params}`, undefined, signal)
}

export const createAdminUser = (body: CreateAdminRequest) => request<AdminUser>('POST', '/admin/users', body)

export const lockAdminUser = (id: number) => request<AdminUser>('POST', `/admin/users/${id}/lock`)

export const unlockAdminUser = (id: number) => request<AdminUser>('POST', `/admin/users/${id}/unlock`)
