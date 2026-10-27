/** Split narration into sentences for one-utterance-per-sentence playback. Handles Latin and CJK punctuation. */
export function splitSentences(text: string | null | undefined): string[] {
  if (!text) return []
  const parts = text
    .replace(/\s+/g, ' ')
    .match(/[^.!?。！？]+[.!?。！？]+["'”’）)]*|[^.!?。！？]+$/g)
  return (parts ?? []).map((s) => s.trim()).filter(Boolean)
}
