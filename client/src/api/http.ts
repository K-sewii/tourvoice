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

/** A request that hangs must not keep a spinner forever (and the server has 10 s per MyMemory call). */
export const REQUEST_TIMEOUT_MS = 15_000

export async function request<T>(method: Method, path: string, body?: unknown, signal?: AbortSignal): Promise<T> {
  // X-Device-Id: presence key and per-device rate limit (PRD §8: 120 req/min per device).
  const isFormData = typeof FormData !== 'undefined' && body instanceof FormData
  const headers: Record<string, string> = { Accept: 'application/json', 'X-Device-Id': getDeviceId() }
  if (body !== undefined && !isFormData) headers['Content-Type'] = 'application/json'
  const token = getToken()
  if (token) headers.Authorization = `Bearer ${token}`

  // Own controller so a timeout and the caller's abort (useAsync) share one signal.
  const controller = new AbortController()
  const timeout = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS)
  const abortFromCaller = () => controller.abort()
  if (signal?.aborted) controller.abort()
  else signal?.addEventListener('abort', abortFromCaller, { once: true })

  let res: Response
  try {
    res = await fetch(`${API_URL}/api/v1${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : isFormData ? (body as FormData) : JSON.stringify(body),
      signal: controller.signal,
    })
  } catch (err) {
    // Caller cancelled (page changed/unmounted): let useAsync ignore it instead of showing an error.
    if (signal?.aborted) throw err
    if (err instanceof DOMException && err.name === 'AbortError') throw new ApiError(0, 'timeout')
    throw new ApiError(0, 'network')
  } finally {
    clearTimeout(timeout)
    signal?.removeEventListener('abort', abortFromCaller)
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
  if (!text) return undefined as T
  try {
    return JSON.parse(text) as T
  } catch {
    throw new ApiError(res.status, 'invalid_response')
  }
}
