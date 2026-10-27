import { request } from './http'

export interface ListenEvent {
  placeSlug: string
  languageCode: string
  listenedSeconds: number
}

/**
 * BR-09: record one listen of ≥ 10 s (the threshold is enforced by useNarrationPlayer and the API).
 * Anonymous for guests; the API attaches the user when a token is present.
 * Fire-and-forget — a failed analytics call must never interrupt narration.
 */
export async function recordListen(event: ListenEvent): Promise<void> {
  try {
    await request<void>('POST', '/listen-events', event)
  } catch {
    // ignore
  }
}
