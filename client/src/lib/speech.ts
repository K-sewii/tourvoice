/**
 * Voice choice for Web Speech (PRD ch.04):
 * 1. exact BCP-47 match (prefer localService), 2. same language prefix, 3. none → text only.
 */
export function pickVoice(voices: readonly SpeechSynthesisVoice[], speechLang: string): SpeechSynthesisVoice | null {
  const target = speechLang.toLowerCase()
  const norm = (v: SpeechSynthesisVoice) => v.lang.toLowerCase().replace('_', '-')
  const byLocal = (a: SpeechSynthesisVoice, b: SpeechSynthesisVoice) => Number(b.localService) - Number(a.localService)

  const exact = voices.filter((v) => norm(v) === target).sort(byLocal)
  if (exact.length) return exact[0]

  const prefix = target.split('-')[0]
  const same = voices.filter((v) => norm(v).split('-')[0] === prefix).sort(byLocal)
  return same[0] ?? null
}

export const speechSupported = () => typeof window !== 'undefined' && 'speechSynthesis' in window

/** Voices load asynchronously on most browsers; resolve once they are available (or after a timeout). */
export function loadVoices(timeoutMs = 1500): Promise<SpeechSynthesisVoice[]> {
  if (!speechSupported()) return Promise.resolve([])
  const synth = window.speechSynthesis
  const now = synth.getVoices()
  if (now.length) return Promise.resolve(now)
  return new Promise((resolve) => {
    const done = () => {
      synth.removeEventListener('voiceschanged', done)
      clearTimeout(timer)
      resolve(synth.getVoices())
    }
    const timer = setTimeout(done, timeoutMs)
    synth.addEventListener('voiceschanged', done)
  })
}
