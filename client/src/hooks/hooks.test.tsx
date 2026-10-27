import { act, render, renderHook, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { useAsync } from './useAsync'
import { useSwitchLang } from './useSwitchLang'

function deferred<T>() {
  let resolve!: (v: T) => void
  const promise = new Promise<T>((r) => (resolve = r))
  return { promise, resolve }
}

describe('useAsync', () => {
  it('aborts the previous request when the key changes and never shows its data', async () => {
    const signals: AbortSignal[] = []
    const pending = new Map<string, ReturnType<typeof deferred<string>>>()
    const { result, rerender } = renderHook(({ k }) =>
      useAsync(k, (signal) => {
        signals.push(signal)
        const d = deferred<string>()
        pending.set(k, d)
        return d.promise
      }),
      { initialProps: { k: 'a' } },
    )
    expect(result.current.loading).toBe(true)

    rerender({ k: 'b' })
    expect(signals[0].aborted).toBe(true)

    await act(async () => pending.get('a')!.resolve('A'))
    expect(result.current.data).toBeUndefined()

    await act(async () => pending.get('b')!.resolve('B'))
    expect(result.current).toMatchObject({ data: 'B', loading: false, refreshing: false })
  })

  it('keeps the previous data while reloading the same key (no flicker)', async () => {
    let n = 0
    const next = deferred<number>()
    const fn = vi.fn(() => (++n === 1 ? Promise.resolve(1) : next.promise))
    const { result } = renderHook(() => useAsync('k', fn))
    await waitFor(() => expect(result.current.data).toBe(1))

    act(() => result.current.reload())
    expect(result.current).toMatchObject({ data: 1, loading: false, refreshing: true })

    await act(async () => next.resolve(2))
    expect(result.current).toMatchObject({ data: 2, loading: false, refreshing: false })
  })

  it('does not show another key’s data as stale', async () => {
    const { result, rerender } = renderHook(({ k }) => useAsync(k, () => new Promise<string>(() => {})), {
      initialProps: { k: 'x' },
    })
    rerender({ k: 'y' })
    expect(result.current).toMatchObject({ data: undefined, loading: true })
  })
})

describe('useSwitchLang', () => {
  function Probe() {
    const switchLang = useSwitchLang()
    const loc = useLocation()
    return (
      <>
        <span data-testid="loc">{loc.pathname + loc.search + loc.hash}</span>
        <button onClick={() => switchLang('ko')}>ko</button>
      </>
    )
  }

  it('swaps the :lang segment and keeps path, query and hash', async () => {
    render(
      <MemoryRouter initialEntries={['/vi/explore?category=food&q=che#top']}>
        <Routes>
          <Route path="*" element={<Probe />} />
        </Routes>
      </MemoryRouter>,
    )
    await userEvent.click(screen.getByText('ko'))
    expect(screen.getByTestId('loc')).toHaveTextContent('/ko/explore?category=food&q=che#top')
  })
})
