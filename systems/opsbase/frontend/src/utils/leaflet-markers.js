import L from 'leaflet'

export function createWarehouseMarkerIcon(options = {}) {
  const color = options.color || '#16a34a'
  const size = Number(options.size) > 0 ? Number(options.size) : 22
  const selected = Boolean(options.selected)

  const markerClass = selected
    ? 'sb-warehouse-marker is-selected'
    : 'sb-warehouse-marker'

  return L.divIcon({
    className: 'sb-warehouse-marker-wrap',
    html: `<div class="${markerClass}" style="--sb-marker-color:${color};--sb-marker-size:${size}px;"><i class="mdi mdi-warehouse"></i></div>`,
    iconSize: [size, size],
    iconAnchor: [size / 2, size / 2]
  })
}
