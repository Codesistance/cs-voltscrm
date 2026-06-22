import { useEffect } from "react"
import L from "leaflet"
import { MapContainer, Marker, Popup, TileLayer, useMap } from "react-leaflet"
import markerIcon2x from "leaflet/dist/images/marker-icon-2x.png"
import markerIcon from "leaflet/dist/images/marker-icon.png"
import markerShadow from "leaflet/dist/images/marker-shadow.png"

import { cn } from "@/lib/utils"

// Fix Leaflet's default marker icons under a bundler (otherwise icons 404).
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2x,
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
})

export interface MapMarker {
  id: string
  lat: number
  lng: number
  label?: string
}

interface MapProps {
  markers?: MapMarker[]
  center?: [number, number]
  zoom?: number
  className?: string
  onMarkerClick?: (id: string) => void
}

/** Recenters the map when `center`/`zoom` change after mount. */
function Recenter({ center, zoom }: { center: [number, number]; zoom: number }) {
  const map = useMap()
  useEffect(() => {
    map.setView(center, zoom)
  }, [map, center, zoom])
  return null
}

/**
 * Leaflet map for customer/field-agent locations. The container needs an explicit
 * height — pass one via `className` (e.g. "h-96") or a sized parent.
 */
export function Map({
  markers = [],
  center = [-1.286389, 36.817223], // Nairobi default
  zoom = 12,
  className,
  onMarkerClick,
}: MapProps) {
  return (
    <MapContainer
      center={center}
      zoom={zoom}
      scrollWheelZoom
      className={cn("h-full w-full rounded-lg", className)}
    >
      <Recenter center={center} zoom={zoom} />
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      {markers.map((m) => (
        <Marker
          key={m.id}
          position={[m.lat, m.lng]}
          eventHandlers={onMarkerClick ? { click: () => onMarkerClick(m.id) } : undefined}
        >
          {m.label && <Popup>{m.label}</Popup>}
        </Marker>
      ))}
    </MapContainer>
  )
}
