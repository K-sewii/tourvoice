import { request } from './http'
import type { AuthResponse } from './types'

export const login = (email: string, password: string) =>
  request<AuthResponse>('POST', '/auth/login', { email, password })

export const register = (email: string, password: string, fullName: string) =>
  request<AuthResponse>('POST', '/auth/register', { email, password, fullName })
