import QRCode from 'qrcode'
import { useEffect, useState } from 'react'
import { escapeHtml } from '../lib/html'
import { CloseIcon } from './Icons'

interface Props {
  slug: string
  name: string
  onClose?: () => void
  inline?: boolean
}

const SCAN_PROMPTS = [
  { lang: 'vi', text: 'Quét để nghe thuyết minh' },
  { lang: 'en', text: 'Scan to listen to the audio guide' },
  { lang: 'zh', text: '扫码收听语音导览' },
  { lang: 'ko', text: 'QR을 스캔하여 오디오 가이드를 들으세요' },
  { lang: 'ja', text: 'QRをスキャンして音声ガイドを聴く' },
]

function getQrTargetUrl(slug: string): string {
  const origin = typeof window !== 'undefined' ? window.location.origin : 'https://tourvoice.vercel.app'
  return `${origin}/q/${encodeURIComponent(slug)}`
}

/**
 * Generates QR code in-browser using `qrcode` (ADR-11, BR-18):
 * Error correction level M, margin 4, downloadable as SVG / PNG / printable A6 sheet with 5-language prompt.
 */
export function QrCodePanel({ slug, name, onClose, inline = false }: Props) {
  const targetUrl = getQrTargetUrl(slug)
  const [svgMarkup, setSvgMarkup] = useState<string>('')
  const [pngDataUrl, setPngDataUrl] = useState<string>('')

  useEffect(() => {
    let active = true
    QRCode.toString(targetUrl, {
      type: 'svg',
      errorCorrectionLevel: 'M',
      margin: 4,
      width: 240,
      color: { dark: '#202b33', light: '#ffffff' },
    })
      .then((svg) => {
        if (active) setSvgMarkup(svg)
      })
      .catch(() => {})

    QRCode.toDataURL(targetUrl, {
      errorCorrectionLevel: 'M',
      margin: 4,
      width: 600,
      color: { dark: '#202b33', light: '#ffffff' },
    })
      .then((url) => {
        if (active) setPngDataUrl(url)
      })
      .catch(() => {})

    return () => {
      active = false
    }
  }, [targetUrl])

  const downloadSvg = () => {
    if (!svgMarkup) return
    const blob = new Blob([svgMarkup], { type: 'image/svg+xml;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `qr-${slug}.svg`
    a.click()
    URL.revokeObjectURL(url)
  }

  const downloadPng = () => {
    if (!pngDataUrl) return
    const a = document.createElement('a')
    a.href = pngDataUrl
    a.download = `qr-${slug}.png`
    a.click()
  }

  const printA6 = () => {
    if (!svgMarkup) return
    const win = window.open('', '_blank', 'width=520,height=720')
    if (!win) return
    win.document.write(`<!doctype html>
<html>
<head>
  <meta charset="utf-8" />
  <title>QR A6 - ${escapeHtml(name)}</title>
  <style>
    @page { size: A6 portrait; margin: 10mm; }
    body {
      font-family: 'Inter', 'Segoe UI', system-ui, sans-serif;
      color: #202b33;
      margin: 0;
      padding: 16px;
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
    }
    .brand { color: #147d78; font-weight: 700; font-size: 14px; letter-spacing: 0.04em; text-transform: uppercase; }
    h1 { font-size: 20px; margin: 6px 0 12px; }
    .qr-box { width: 180px; height: 180px; border: 2px solid #147d78; border-radius: 12px; padding: 8px; }
    .qr-box svg { width: 100%; height: 100%; display: block; }
    .prompts { margin-top: 12px; font-size: 12px; line-height: 1.5; color: #202b33; }
    .prompts div { margin: 2px 0; }
    .url { margin-top: 10px; font-family: monospace; font-size: 11px; color: #5f6e77; }
  </style>
</head>
<body>
  <div class="brand">TourVoice · Quận 6</div>
  <h1>${escapeHtml(name)}</h1>
  <div class="qr-box">${svgMarkup}</div>
  <div class="prompts">
    ${SCAN_PROMPTS.map((p) => `<div>${escapeHtml(p.text)}</div>`).join('')}
  </div>
  <div class="url">${escapeHtml(targetUrl)}</div>
  <script>window.onload = () => { window.print(); }</script>
</body>
</html>`)
    win.document.close()
  }

  const content = (
    <div className={inline ? 'admin-qr-inline' : 'admin-modal-card'} onClick={(e) => e.stopPropagation()}>
      {!inline && (
        <div className="admin-modal-header">
          <div>
            <h3>Mã QR · {name}</h3>
            <p className="admin-muted">Mức sửa lỗi M · viền 4 ô (BR-18, ADR-11)</p>
          </div>
          {onClose && (
            <button type="button" className="btn btn-ghost" onClick={onClose} aria-label="Đóng">
              <CloseIcon />
            </button>
          )}
        </div>
      )}

      <div className="admin-qr-body">
        <div
          className="admin-qr-preview"
          aria-label={`QR code for ${slug}`}
          dangerouslySetInnerHTML={{ __html: svgMarkup }}
        />
        <div className="admin-qr-info">
          <div className="admin-qr-url">
            <span>URL mã hóa (không gắn ngôn ngữ):</span>
            <code>{targetUrl}</code>
          </div>
          <ul className="admin-qr-prompts">
            {SCAN_PROMPTS.map((p) => (
              <li key={p.lang}>
                <strong>{p.lang.toUpperCase()}:</strong> {p.text}
              </li>
            ))}
          </ul>
          <div className="admin-qr-actions">
            <button type="button" className="btn btn-outline btn-sm" onClick={downloadSvg} disabled={!svgMarkup}>
              Tải SVG
            </button>
            <button type="button" className="btn btn-outline btn-sm" onClick={downloadPng} disabled={!pngDataUrl}>
              Tải PNG
            </button>
            <button type="button" className="btn btn-primary btn-sm" onClick={printA6} disabled={!svgMarkup}>
              In mẫu A6
            </button>
          </div>
        </div>
      </div>
    </div>
  )

  if (inline) return content

  return (
    <div className="admin-modal-backdrop" role="dialog" aria-modal="true" onClick={onClose}>
      {content}
    </div>
  )
}
