import { useEffect } from 'react'
import { useMap } from 'react-leaflet'

/**
 * Leaflet đo kích thước container đúng một lần khi map được tạo. Nếu map mount lúc
 * container đang ẩn (tab/accordion) hoặc cao 0px, Leaflet không bao giờ gọi tải tile
 * và chỉ hiện nền xám dù marker/control vẫn render.
 *
 * Component này gọi map.invalidateSize() sau khi layout ổn định và mỗi khi container
 * đổi kích thước. Phải đặt bên trong <MapContainer>.
 */
export function MapAutoResize() {
  const map = useMap()

  useEffect(() => {
    const invalidate = () => {
      const container = map.getContainer()
      // Container ẩn thì đo cũng vô nghĩa; chờ ResizeObserver báo lại khi hiện.
      if (container.clientWidth > 0 && container.clientHeight > 0) {
        map.invalidateSize()
      }
    }

    // Chờ paint xong, rồi kiểm tra lại sau khi CSS transition/layout kết thúc.
    const raf = requestAnimationFrame(invalidate)
    const timer = window.setTimeout(invalidate, 250)

    const observer =
      typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(invalidate)
    observer?.observe(map.getContainer())

    window.addEventListener('resize', invalidate)
    window.addEventListener('orientationchange', invalidate)

    return () => {
      cancelAnimationFrame(raf)
      window.clearTimeout(timer)
      observer?.disconnect()
      window.removeEventListener('resize', invalidate)
      window.removeEventListener('orientationchange', invalidate)
    }
  }, [map])

  return null
}
