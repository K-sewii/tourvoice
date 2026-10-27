import { vi } from 'vitest'

/** Minimal Web Speech fake: records utterances and lets tests finish them on demand. */
export function installSpeechMock(voiceLangs: string[]) {
  const makeVoice = (lang: string, i: number) =>
    ({ lang, name: `Voice ${i}`, localService: true, default: i === 0, voiceURI: `v${i}` }) as SpeechSynthesisVoice
  let voices = voiceLangs.map(makeVoice)
  const listeners = new Set<() => void>()
  const spoken: SpeechSynthesisUtterance[] = []

  class FakeUtterance {
    text: string
    voice: SpeechSynthesisVoice | null = null
    lang = ''
    rate = 1
    onstart: (() => void) | null = null
    onend: (() => void) | null = null
    onerror: ((e: { error: string }) => void) | null = null
    constructor(text: string) {
      this.text = text
    }
  }

  const synth = {
    getVoices: () => voices,
    speak: vi.fn((u: SpeechSynthesisUtterance) => {
      spoken.push(u)
      u.onstart?.(new Event('start') as SpeechSynthesisEvent)
    }),
    cancel: vi.fn(),
    pause: vi.fn(),
    resume: vi.fn(),
    addEventListener: vi.fn((type: string, fn: () => void) => {
      if (type === 'voiceschanged') listeners.add(fn)
    }),
    removeEventListener: vi.fn((type: string, fn: () => void) => {
      if (type === 'voiceschanged') listeners.delete(fn)
    }),
  }

  Object.defineProperty(window, 'speechSynthesis', { value: synth, configurable: true })
  Object.defineProperty(window, 'SpeechSynthesisUtterance', { value: FakeUtterance, configurable: true })
  vi.stubGlobal('SpeechSynthesisUtterance', FakeUtterance)

  return {
    synth,
    spoken,
    /** Fire `onend` on the most recent utterance. */
    finishCurrent: () => spoken.at(-1)?.onend?.(new Event('end') as SpeechSynthesisEvent),
    /** Simulate voices arriving later (fires `voiceschanged`). */
    addVoices: (langs: string[]) => {
      voices = [...voices, ...langs.map((l, i) => makeVoice(l, voices.length + i))]
      listeners.forEach((l) => l())
    },
    uninstall: () => {
      // @ts-expect-error test cleanup
      delete window.speechSynthesis
      vi.unstubAllGlobals()
    },
  }
}
