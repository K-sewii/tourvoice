/**
 * Mốc thời gian từ API là UTC. Dữ liệu đọc từ MySQL (UsageLog.CreatedAt) có thể về dạng
 * không có hậu tố "Z", còn snapshot presence trong bộ nhớ thì có; thiếu offset thì coi là UTC.
 * Mọi màn hình admin hiển thị giờ Việt Nam, bất kể múi giờ của máy đang mở.
 */
const hasOffset = (iso: string) => /(?:Z|[+-]\d{2}:?\d{2})$/i.test(iso)

function parseUtc(iso: string): Date | null {
  if (!iso) return null
  const dt = new Date(hasOffset(iso) ? iso : `${iso}Z`)
  return Number.isNaN(dt.getTime()) ? null : dt
}

const TIME = new Intl.DateTimeFormat('vi-VN', {
  timeZone: 'Asia/Ho_Chi_Minh',
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
})

/** "14:32:05" theo giờ Việt Nam; null khi giá trị rỗng hoặc không hợp lệ. */
export function formatVnTime(iso: string): string | null {
  const dt = parseUtc(iso)
  return dt ? TIME.format(dt) : null
}

const DATE_TIME = new Intl.DateTimeFormat('vi-VN', {
  timeZone: 'Asia/Ho_Chi_Minh',
  dateStyle: 'short',
  timeStyle: 'medium',
})

/** "27/10/26 20:32:05" theo giờ Việt Nam; giá trị không đọc được thì trả nguyên văn để bảng không trống. */
export function formatVnDateTime(iso: string): string {
  const dt = parseUtc(iso)
  return dt ? DATE_TIME.format(dt) : iso
}
