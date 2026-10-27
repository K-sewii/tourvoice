import { useEffect } from 'react'
import { API_URL, request } from '../api/http'
import { entrySource, getDeviceId } from '../lib/deviceId'

/** FR-19 / BR-15: heartbeat every 15 s; the server drops a device after 45 s of silence. */
export const HEARTBEAT_MS = 15_000

/**
 * Live-visitor counting (SD-04, ADR-10): plain HTTP heartbeats instead of a WebSocket, so it
 * survives screen lock and flaky mobile networks.
 * - every 15 s while the tab is visible (with the current place, if any);
 * - hidden tab → no heartbeats (and no leave: returning from Google Maps within 45 s keeps the count);
 * - visible again or page change → heartbeat immediately;
 * - pagehide → sendBeacon('/presence/leave') so the dashboard drops the device right away.
 */
export function usePresence(placeSlug: string | undefined, lang: string) {
  useEffect(() => {
    const beat = () => {
      if (document.visibilityState !== 'visible') return
      request<void>('POST', '/presence/heartbeat', {
        placeSlug: placeSlug ?? null,
        lang,
        source: entrySource(),
      }).catch(() => {
        // best-effort: the page keeps working when the API is unreachable
      })
    }

    beat()
    const timer = window.setInterval(beat, HEARTBEAT_MS)
    document.addEventListener('visibilitychange', beat)
    return () => {
      window.clearInterval(timer)
      document.removeEventListener('visibilitychange', beat)
    }
  }, [placeSlug, lang])

  useEffect(() => {
    const leave = () => {
      // text/plain keeps sendBeacon a "simple" CORS request (no preflight); the API parses the JSON.
      const body = new Blob([JSON.stringify({ deviceId: getDeviceId() })], { type: 'text/plain' })
      navigator.sendBeacon?.(`${API_URL}/api/v1/presence/leave`, body)
    }
    window.addEventListener('pagehide', leave)
    return () => window.removeEventListener('pagehide', leave)
  }, [])
}
