// Raster basemap tiles used by every Leaflet map.
//
// Mặc định dùng tile Google Maps. Có thể override bằng VITE_TILE_URL lúc build
// (vd. tự host tiles hoặc dùng provider khác).
export const TILE_URL: string =
  import.meta.env.VITE_TILE_URL ??
  'https://mt1.google.com/vt/lyrs=m&x={x}&y={y}&z={z}'

export const TILE_ATTRIBUTION = '&copy; Google Maps'
