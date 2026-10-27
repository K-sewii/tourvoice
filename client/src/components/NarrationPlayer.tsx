import { useEffect, useImperativeHandle, useRef, type Ref } from 'react'
import { useTranslation } from 'react-i18next'
import { RATES, useNarrationPlayer } from '../hooks/useNarrationPlayer'
import { LANG_INFO, type Lang } from '../lib/languages'
import { NextIcon, PauseIcon, PlayIcon, PrevIcon } from './Icons'
import { LanguageSelect } from './LanguageSelect'

export interface NarrationPlayerHandle {
  /** Start or resume playback; does nothing if already playing (unlike the toggle button). */
  start: () => void
}

interface Props {
  text: string | null
  /** Language the text is actually in (may be a fallback). */
  contentLang: Lang
  /** Try to start right away (user tapped "Listen" on a card). */
  autoplay?: boolean
  onListened?: (seconds: number) => void
  ref?: Ref<NarrationPlayerHandle>
}

/**
 * FR-08 narration player. Reads the approved text with the device voice (Web Speech).
 * The transcript is always available below (NFR-06), and is the only output when Web Speech is unsupported.
 */
export function NarrationPlayer({ text, contentLang, autoplay, onListened, ref }: Props) {
  const { t } = useTranslation()
  const speechLang = LANG_INFO[contentLang].speech
  const p = useNarrationPlayer(text, { speechLang, onListened })
  const currentRef = useRef<HTMLLIElement>(null)
  const autoplayed = useRef(false)
  const { start, canPlay } = p

  useImperativeHandle(ref, () => ({ start }), [start])

  useEffect(() => {
    if (autoplay && canPlay && !autoplayed.current) {
      autoplayed.current = true
      start()
    }
  }, [autoplay, canPlay, start])

  // Keep the current sentence visible inside the transcript box only — never scroll the page.
  useEffect(() => {
    const li = currentRef.current
    const list = li?.parentElement
    if (p.status !== 'playing' || !li || !list) return
    list.scrollTop = li.offsetTop - list.offsetTop - list.clientHeight / 3
  }, [p.index, p.status])

  const total = p.sentences.length
  const playing = p.status === 'playing'
  const nextRate = RATES[(RATES.indexOf(p.rate) + 1) % RATES.length]
  const textOnly = !p.supported

  const transcript = (
    <ol lang={contentLang}>
      {p.sentences.map((s, i) => (
        <li key={i} ref={i === p.index ? currentRef : undefined} className={i === p.index ? 'current' : undefined}>
          {textOnly ? (
            s
          ) : (
            <button type="button" onClick={() => p.seek(i)}>
              {s}
            </button>
          )}
        </li>
      ))}
    </ol>
  )

  return (
    <section className="player" id="listen" aria-labelledby="player-title">
      <div className="player-head">
        <h2 id="player-title">{t('player.title')}</h2>
        <LanguageSelect alwaysFull />
      </div>

      <div className="player-main">
        <button
          type="button"
          className="play-btn"
          onClick={p.toggle}
          disabled={!p.canPlay}
          aria-label={playing ? t('player.pause') : p.status === 'paused' ? t('player.resume') : t('player.play')}
        >
          {playing ? <PauseIcon size={28} /> : <PlayIcon size={28} />}
        </button>
        <div className="player-progress">
          <input
            type="range"
            min={1}
            max={Math.max(total, 1)}
            value={Math.min(p.index + 1, Math.max(total, 1))}
            onChange={(e) => p.seek(Number(e.target.value) - 1)}
            disabled={total === 0}
            aria-label={t('player.progress', { current: p.index + 1, total })}
            aria-valuetext={t('player.progress', { current: p.index + 1, total })}
          />
          <div className="count" aria-live="polite">
            {total > 0 ? t('player.progress', { current: p.index + 1, total }) : t('place.noNarration')}
          </div>
        </div>
      </div>

      <div className="player-controls">
        <button
          type="button"
          className="pill"
          onClick={() => p.setRate(nextRate)}
          aria-label={t('player.rate', { rate: nextRate })}
          title={t('player.rate', { rate: p.rate })}
        >
          {p.rate}×
        </button>
        <div className="pill-group">
          <button type="button" className="pill" onClick={p.prev} disabled={p.index === 0} aria-label={t('player.prev')}>
            <PrevIcon size={18} />
          </button>
          <button
            type="button"
            className="pill"
            onClick={p.next}
            disabled={p.index >= total - 1}
            aria-label={t('player.next')}
          >
            <NextIcon size={18} />
          </button>
        </div>
      </div>

      {!p.supported && <p className="notice">{t('player.noSpeech')}</p>}

      {total > 0 &&
        (textOnly ? (
          <div className="transcript">
            <h3 className="visually-hidden">{t('player.transcript')}</h3>
            {transcript}
          </div>
        ) : (
          <details className="transcript">
            <summary>{t('player.transcript')}</summary>
            {transcript}
          </details>
        ))}
    </section>
  )
}
