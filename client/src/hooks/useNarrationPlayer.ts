import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { splitSentences } from '../lib/sentences'
import { loadVoices, pickVoice, speechSupported } from '../lib/speech'

export type PlayerStatus = 'idle' | 'playing' | 'paused' | 'ended'
export const RATES = [0.8, 1, 1.25] as const
export type Rate = (typeof RATES)[number]

/** BR-09: a listen counts once ≥ 10 s have been spoken. */
export const LISTEN_THRESHOLD_SECONDS = 10

interface Options {
  /** BCP-47 speech language, e.g. "ko-KR". */
  speechLang: string
  /** Called once per mount when the spoken time first reaches LISTEN_THRESHOLD_SECONDS. */
  onListened?: (seconds: number) => void
}

/**
 * Web Speech narration player (FR-08): one utterance per sentence, pause/resume,
 * previous/next sentence, rate 0.8/1/1.25. Remount (via `key`) to load another text.
 *
 * Pause is implemented as cancel + remember the sentence, because speechSynthesis.pause()
 * is unreliable on Android Chrome; resume restarts the current sentence.
 */
export function useNarrationPlayer(text: string | null | undefined, { speechLang, onListened }: Options) {
  const sentences = useMemo(() => splitSentences(text), [text])
  const supported = speechSupported()

  const [status, setStatus] = useState<PlayerStatus>('idle')
  const [index, setIndex] = useState(0)
  const [rate, setRateState] = useState<Rate>(1)
  const [voice, setVoice] = useState<SpeechSynthesisVoice | null>(null)
  const [voicesReady, setVoicesReady] = useState(false)

  const generation = useRef(0)
  const spokenMs = useRef(0)
  const startedAt = useRef<number | null>(null)
  const reported = useRef(false)
  const onListenedRef = useRef(onListened)

  useEffect(() => {
    onListenedRef.current = onListened
  }, [onListened])

  useEffect(() => {
    if (!speechSupported()) return
    let alive = true
    const synth = window.speechSynthesis
    // loadVoices' timeout only decides when "no voice" may first be shown; voices that arrive
    // later (slow Android, voice packs installed meanwhile) are still picked up via voiceschanged.
    loadVoices().then((voices) => {
      if (!alive) return
      setVoice(pickVoice(voices, speechLang))
      setVoicesReady(true)
    })
    const onChange = () => {
      const picked = pickVoice(synth.getVoices(), speechLang)
      if (picked) setVoice(picked)
    }
    synth.addEventListener('voiceschanged', onChange)
    return () => {
      alive = false
      synth.removeEventListener('voiceschanged', onChange)
    }
  }, [speechLang])

  const stopClock = useCallback(() => {
    if (startedAt.current !== null) {
      spokenMs.current += performance.now() - startedAt.current
      startedAt.current = null
    }
    const seconds = Math.floor(spokenMs.current / 1000)
    if (!reported.current && seconds >= LISTEN_THRESHOLD_SECONDS) {
      reported.current = true
      onListenedRef.current?.(seconds)
    }
  }, [])

  const speakFrom = useCallback(
    (start: number, r: Rate = rate) => {
      if (!supported || !voice || start < 0 || start >= sentences.length) return
      const synth = window.speechSynthesis
      // Any utterance from an older generation (cancelled/replaced) ignores its callbacks.
      const gen = ++generation.current
      synth.cancel()

      const speak = (i: number) => {
        const u = new SpeechSynthesisUtterance(sentences[i])
        u.voice = voice
        u.lang = voice.lang
        u.rate = r
        u.onstart = () => {
          if (gen !== generation.current) return
          startedAt.current ??= performance.now()
        }
        u.onend = () => {
          if (gen !== generation.current) return
          stopClock()
          if (i + 1 < sentences.length) {
            setIndex(i + 1)
            speak(i + 1)
          } else {
            setStatus('ended')
          }
        }
        u.onerror = (e) => {
          if (gen !== generation.current) return
          if (e.error === 'interrupted' || e.error === 'canceled') return
          stopClock()
          setStatus('paused')
        }
        synth.speak(u)
      }

      setIndex(start)
      setStatus('playing')
      speak(start)
    },
    [rate, sentences, stopClock, supported, voice],
  )

  const pause = useCallback(() => {
    generation.current++
    window.speechSynthesis?.cancel()
    stopClock()
    setStatus('paused')
  }, [stopClock])

  /** Must run inside a user gesture (iOS). */
  const play = useCallback(() => {
    speakFrom(status === 'ended' ? 0 : index)
  }, [index, speakFrom, status])

  const toggle = useCallback(() => (status === 'playing' ? pause() : play()), [pause, play, status])

  /** Like play(), but a no-op while already playing — for "Listen" buttons outside the player. */
  const start = useCallback(() => {
    if (status !== 'playing') play()
  }, [play, status])

  const seek = useCallback(
    (i: number) => {
      const next = Math.max(0, Math.min(sentences.length - 1, i))
      if (status === 'playing') speakFrom(next)
      else {
        setIndex(next)
        if (status === 'ended') setStatus('paused')
      }
    },
    [sentences.length, speakFrom, status],
  )

  const setRate = useCallback(
    (r: Rate) => {
      setRateState(r)
      if (status === 'playing') speakFrom(index, r)
    },
    [index, speakFrom, status],
  )

  useEffect(
    () => () => {
      generation.current++
      if (speechSupported()) window.speechSynthesis.cancel()
      stopClock()
    },
    [stopClock],
  )

  return {
    sentences,
    status,
    index,
    rate,
    supported,
    /** true once voices loaded and none matches the language → show text only. */
    noVoice: supported && voicesReady && !voice,
    canPlay: supported && !!voice && sentences.length > 0,
    play,
    start,
    pause,
    toggle,
    prev: () => seek(index - 1),
    next: () => seek(index + 1),
    seek,
    setRate,
  }
}
