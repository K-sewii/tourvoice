const KEY = 'tv.device'

let cached: string | null = null

/**
 * Anonymous per-browser id (BR-14): a random UUID kept in localStorage; the server only stores
 * its SHA-256. Many tabs on one device share it, so they count as one visitor.
 */
export function getDeviceId(): string {
  if (cached) return cached
  try {
    const saved = localStorage.getItem(KEY)
    if (saved) return (cached = saved)
  } catch {
    // storage blocked — fall through to an in-memory id for this page load
  }
  const id =
    typeof crypto !== 'undefined' && 'randomUUID' in crypto
      ? crypto.randomUUID()
      : `${Date.now().toString(16)}-${Math.random().toString(16).slice(2, 10)}-4000-8000-${Math.random().toString(16).slice(2, 14)}`
  try {
    localStorage.setItem(KEY, id)
  } catch {
    // ignore
  }
  return (cached = id)
}

const ENTRY_KEY = 'tv.entry'

/** Remember that this browsing session started from a QR code (AccessSessions.Source = QR). */
export function markQrEntry() {
  try {
    sessionStorage.setItem(ENTRY_KEY, 'qr')
  } catch {
    // ignore
  }
}

export function entrySource(): 'qr' | 'web' {
  try {
    return sessionStorage.getItem(ENTRY_KEY) === 'qr' ? 'qr' : 'web'
  } catch {
    return 'web'
  }
}
