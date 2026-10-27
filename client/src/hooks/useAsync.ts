import { useCallback, useEffect, useEffectEvent, useState } from 'react'

interface State<T> {
  /** `key` passed by the caller (without the reload nonce). */
  key: string | null
  /** `key#nonce` the result belongs to. */
  fullKey: string
  data?: T
  error?: unknown
}

export interface AsyncResult<T> {
  data: T | undefined
  error: unknown
  /** True only while there is nothing to show yet (first load or key change). */
  loading: boolean
  /** True while `reload()` refetches; `data` still holds the previous result. */
  refreshing: boolean
  reload: () => void
}

/**
 * Runs `fn` whenever `key` changes (null = skip). Aborts the previous request on change/unmount.
 * `reload()` keeps showing the previous data until the new result arrives (stale-while-revalidate),
 * but a different `key` never shows another key's data.
 * Loading is derived from the key so no state is set synchronously inside the effect.
 */
export function useAsync<T>(key: string | null, fn: (signal: AbortSignal) => Promise<T>): AsyncResult<T> {
  const [state, setState] = useState<State<T>>({ key: null, fullKey: '' })
  const [nonce, setNonce] = useState(0)
  const fullKey = key === null ? null : `${key}#${nonce}`

  const run = useEffectEvent((signal: AbortSignal) => fn(signal))

  useEffect(() => {
    if (fullKey === null) return
    const ac = new AbortController()
    run(ac.signal).then(
      (data) => setState({ key, fullKey, data }),
      (error) => {
        if (!ac.signal.aborted) setState({ key, fullKey, error })
      },
    )
    return () => ac.abort()
    // `key` is part of `fullKey`
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fullKey])

  const reload = useCallback(() => setNonce((n) => n + 1), [])
  const fresh = fullKey !== null && state.fullKey === fullKey
  const stale = !fresh && fullKey !== null && state.key === key && state.data !== undefined

  return {
    data: fresh || stale ? state.data : undefined,
    error: fresh ? state.error : undefined,
    loading: fullKey !== null && !fresh && !stale,
    refreshing: stale,
    reload,
  }
}
