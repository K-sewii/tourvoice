import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useEffect, useState } from 'react'
import { API_URL, getToken } from '../api/http'
import type { PresenceSnapshot } from '../api/types'

/**
 * Connects to /hubs/presence (BR-17) for the Admin Overview screen (SCR-A2).
 * Server immediately pushes "PresenceUpdated" upon join and whenever devices enter/leave.
 */
export function useAdminPresence(initialLive = 0) {
  const [liveSnapshot, setLiveSnapshot] = useState<PresenceSnapshot | null>(null)
  const [connected, setConnected] = useState(false)

  useEffect(() => {
    const token = getToken()
    if (!token) return

    let disposed = false
    const conn = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/presence`, {
        accessTokenFactory: () => getToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    conn.on('PresenceUpdated', (data: PresenceSnapshot) => {
      if (!disposed && data) {
        setLiveSnapshot({
          live: data.live ?? 0,
          byPlace: data.byPlace ?? {},
          atUtc: data.atUtc ?? new Date().toISOString(),
        })
      }
    })

    conn.onreconnected(() => {
      if (!disposed) setConnected(true)
    })
    conn.onclose(() => {
      if (!disposed) setConnected(false)
    })

    conn
      .start()
      .then(() => {
        if (!disposed) setConnected(true)
      })
      .catch(() => {
        // Fallback: initial dashboard value stays displayed if WebSocket/SignalR fails
      })

    return () => {
      disposed = true
      void conn.stop()
    }
  }, [])

  const snapshot: PresenceSnapshot = liveSnapshot ?? {
    live: initialLive,
    byPlace: {},
    atUtc: '',
  }

  return { snapshot, connected }
}
