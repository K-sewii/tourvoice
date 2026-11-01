// Raster basemap tiles used by every Leaflet map.
//
// The public OSM tile server (tile.openstreetmap.org) blocks many networks/IPs with
// "403 Access blocked", and its a/b/c subdomains are deprecated. CARTO's basemap CDN
// needs no API key and is fine for a low-traffic demo. To use another provider
// (self-hosted, MapTiler, Stadia...), set VITE_TILE_URL at build time.
//
// Both providers require attribution, so TILE_ATTRIBUTION ships with the template.
export const TILE_URL: string =
  import.meta.env.VITE_TILE_URL ??
  'https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png'

export const TILE_ATTRIBUTION =
  '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors ' +
  '&copy; <a href="https://carto.com/attributions">CARTO</a>'
