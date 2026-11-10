// Raster basemap tiles used by every Leaflet map.
//
// Dùng `||` chứ KHÔNG dùng `??`: client/Dockerfile và docker-compose luôn truyền
// VITE_TILE_URL="" khi không cấu hình. `??` chỉ thay thế null/undefined nên chuỗi rỗng
// sẽ lọt qua, TileLayer nhận url rỗng và bản đồ chỉ hiện nền xám.
//
// Mặc định dùng tile OpenStreetMap (không cần key). Muốn dùng provider khác
// (CARTO có key, MapTiler, tự host...) thì đặt VITE_TILE_URL lúc build.
export const TILE_URL: string =
  import.meta.env.VITE_TILE_URL ||
  'https://tile.openstreetmap.org/{z}/{x}/{y}.png'

const OSM =
  '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'

// CARTO yêu cầu ghi nguồn thêm "© CARTO" khi bản đồ dùng tile của họ.
export const TILE_ATTRIBUTION = TILE_URL.includes('cartocdn.com')
  ? `${OSM} &copy; <a href="https://carto.com/attributions">CARTO</a>`
  : OSM