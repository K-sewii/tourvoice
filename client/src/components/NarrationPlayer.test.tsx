import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createRef } from 'react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import i18n from '../i18n'
import { installSpeechMock } from '../test/speechMock'
import { NarrationPlayer, type NarrationPlayerHandle } from './NarrationPlayer'

const TEXT = 'Câu một. Câu hai. Câu ba.'

function renderPlayer(props: Partial<React.ComponentProps<typeof NarrationPlayer>> = {}) {
  void i18n.changeLanguage('en')
  return render(
    <MemoryRouter initialEntries={['/en/places/x']}>
      <Routes>
        <Route path="/:lang/places/x" element={<NarrationPlayer text={TEXT} contentLang="vi" {...props} />} />
      </Routes>
    </MemoryRouter>,
  )
}

let mock: ReturnType<typeof installSpeechMock> | null = null
afterEach(() => {
  mock?.uninstall()
  mock = null
})

describe('NarrationPlayer', () => {
  it('speaks sentence by sentence with the matching voice', async () => {
    mock = installSpeechMock(['en-US', 'vi-VN'])
    const user = userEvent.setup()
    renderPlayer()

    const play = await screen.findByRole('button', { name: 'Play narration' })
    await user.click(play)

    expect(mock.spoken).toHaveLength(1)
    expect(mock.spoken[0].text).toBe('Câu một.')
    expect(mock.spoken[0].voice?.lang).toBe('vi-VN')
    expect(screen.getByText('Sentence 1 / 3')).toBeInTheDocument()

    act(() => mock!.finishCurrent())
    expect(mock.spoken[1].text).toBe('Câu hai.')
    expect(screen.getByText('Sentence 2 / 3')).toBeInTheDocument()
  })

  it('pauses by cancelling and resumes from the same sentence', async () => {
    mock = installSpeechMock(['vi-VN'])
    const user = userEvent.setup()
    renderPlayer()

    await user.click(await screen.findByRole('button', { name: 'Play narration' }))
    act(() => mock!.finishCurrent())
    await user.click(screen.getByRole('button', { name: 'Pause' }))
    expect(mock.synth.cancel).toHaveBeenCalled()

    await user.click(screen.getByRole('button', { name: 'Resume' }))
    expect(mock.spoken.at(-1)?.text).toBe('Câu hai.')
  })

  it('changes rate 1 → 1.25 and applies it to the next utterance', async () => {
    mock = installSpeechMock(['vi-VN'])
    const user = userEvent.setup()
    renderPlayer()

    await user.click(await screen.findByRole('button', { name: 'Reading speed 1.25×' }))
    await user.click(screen.getByRole('button', { name: 'Play narration' }))
    expect(mock.spoken[0].rate).toBe(1.25)
  })

  it('falls back to speechLang when the device has no matching voice object', async () => {
    mock = installSpeechMock(['en-US'])
    const user = userEvent.setup()
    renderPlayer()

    const play = await screen.findByRole('button', { name: 'Play narration' })
    expect(play).toBeEnabled()
    await user.click(play)
    expect(mock.spoken).toHaveLength(1)
    expect(mock.spoken[0].lang).toBe('vi-VN')
  })

  it('picks up a voice that arrives after the initial timeout', async () => {
    mock = installSpeechMock([])
    const user = userEvent.setup()
    renderPlayer()

    act(() => mock!.addVoices(['vi-VN']))
    const play = await screen.findByRole('button', { name: 'Play narration' })
    expect(play).toBeEnabled()
    await user.click(play)
    expect(mock.spoken[0].voice?.lang).toBe('vi-VN')
  })

  it('start() via ref begins playback but never pauses it', async () => {
    mock = installSpeechMock(['vi-VN'])
    const ref = createRef<NarrationPlayerHandle>()
    renderPlayer({ ref })
    await screen.findByRole('button', { name: 'Play narration' })
    await waitFor(() => expect(screen.getByRole('button', { name: 'Play narration' })).toBeEnabled())

    act(() => ref.current!.start())
    expect(mock.spoken).toHaveLength(1)
    act(() => ref.current!.start())
    expect(mock.synth.cancel).toHaveBeenCalledTimes(1) // only the initial cancel before speaking
    expect(screen.getByRole('button', { name: 'Pause' })).toBeInTheDocument()
  })

  it('reports a listen once 10 s have been spoken (BR-09)', async () => {
    mock = installSpeechMock(['vi-VN'])
    const now = vi.spyOn(performance, 'now')
    now.mockReturnValue(0)
    const onListened = vi.fn()
    const user = userEvent.setup()
    renderPlayer({ onListened })

    await user.click(await screen.findByRole('button', { name: 'Play narration' }))
    now.mockReturnValue(4_000)
    act(() => mock!.finishCurrent())
    expect(onListened).not.toHaveBeenCalled()

    now.mockReturnValue(11_000)
    act(() => mock!.finishCurrent())
    expect(onListened).toHaveBeenCalledWith(11)
    now.mockRestore()
  })
})
