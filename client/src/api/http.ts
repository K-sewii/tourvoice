import { getDeviceId } from '../lib/deviceId'

export const API_URL: string = (import.meta.env.VITE_API_URL ?? 'http://localhost:5049').replace(/\/$/, '')

const TOKEN_KEY = 'tv.token'

export function getToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY)
  } catch {
    return null
  }
}

export function setToken(token: string | null) {
  try {
    if (token) localStorage.setItem(TOKEN_KEY, token)
    else localStorage.removeItem(TOKEN_KEY)
  } catch {
    // storage blocked (private mode) — session simply won't persist
  }
}

let unauthorizedHandler: (() => void) | null = null

/** AuthProvider registers this: any 401 on a request that carried a token means the session is gone. */
export function setUnauthorizedHandler(handler: (() => void) | null) {
  unauthorizedHandler = handler
}

export class ApiError extends Error {
  readonly status: number
  readonly code?: string
  readonly missing?: string[]
  constructor(status: number, message: string, code?: string, missing?: string[]) {
    super(message)
    this.status = status
    this.code = code
    this.missing = missing
  }
}

type Method = 'GET' | 'POST' | 'PUT' | 'DELETE'

export async function request<T>(method: Method, path: string, body?: unknown, signal?: AbortSignal): Promise<T> {
  // X-Device-Id: presence key and per-device rate limit (PRD §8: 120 req/min per device).
  const isFormData = typeof FormData !== 'undefined' && body instanceof FormData
  const headers: Record<string, string> = { Accept: 'application/json', 'X-Device-Id': getDeviceId() }
  if (body !== undefined && !isFormData) headers['Content-Type'] = 'application/json'
  const token = getToken()
  if (token) headers.Authorization = `Bearer ${token}`

  let res: Response
  try {
    res = await fetch(`${API_URL}/api/v1${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : isFormData ? (body as FormData) : JSON.stringify(body),
      signal,
    })
  } catch (err) {
    if (err instanceof DOMException && err.name === 'AbortError') throw err
    throw new ApiError(0, 'network')
  }

  if (!res.ok) {
    if (res.status === 401 && token) unauthorizedHandler?.()
    // API returns either { message } or RFC 7807 ProblemDetails { title, detail, code, missing }
    let message = res.statusText
    let code: string | undefined
    let missing: string[] | undefined
    try {
      const data = await res.json()
      message = data.message ?? data.detail ?? data.title ?? message
      if (typeof data.code === 'string') code = data.code
      if (Array.isArray(data.missing)) missing = data.missing
    } catch {
      // empty body
    }
    throw new ApiError(res.status, message, code, missing)
  }

  if (res.status === 204) return undefined as T
  const text = await res.text()
  return (text ? JSON.parse(text) : undefined) as T
}
