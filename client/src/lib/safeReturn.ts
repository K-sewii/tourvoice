/**
 * Validates a `returnUrl` query value. Resolves it like the browser would and accepts it only
 * when it stays on this origin — prefix checks alone miss tricks like `/\evil.example`,
 * which URL parsing turns into `//evil.example`. Returns an in-app path or null.
 */
export function safeReturn(value: string | null, origin: string = window.location.origin): string | null {
  if (!value || !value.startsWith('/')) return null
  let url: URL
  try {
    url = new URL(value, origin)
  } catch {
    return null
  }
  if (url.origin !== origin) return null
  return `${url.pathname}${url.search}${url.hash}`
}
