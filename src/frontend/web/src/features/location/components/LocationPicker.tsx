import { useEffect, useMemo, useState } from 'react'
import L from 'leaflet'
import { MapContainer, Marker, TileLayer, useMap, useMapEvents } from 'react-leaflet'
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png'
import markerIcon from 'leaflet/dist/images/marker-icon.png'
import markerShadow from 'leaflet/dist/images/marker-shadow.png'
import 'leaflet/dist/leaflet.css'
import { MapPin } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError } from '@/shared/api/http'
import { useGeocode } from '@/features/geocoding/api/queries'
import type { GeocodeResult } from '@/features/geocoding/api/geocodingApi'
import type { AddressInput, GpsCoordinatesInput, LocationInput } from '../types'

const defaultIcon = L.icon({
  iconUrl: markerIcon,
  iconRetinaUrl: markerIcon2x,
  shadowUrl: markerShadow,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41],
})

const FALLBACK_CENTER: [number, number] = [9.082, 8.6753] // Nigeria

interface AddressErrors {
  street?: string
  city?: string
  region?: string
  country?: string
}

interface Props {
  value: LocationInput
  onChange: (value: LocationInput) => void
  disabled?: boolean
  errors?: AddressErrors
}

export function LocationPicker({ value, onChange, disabled, errors }: Props) {
  const geocode = useGeocode()
  const [candidates, setCandidates] = useState<GeocodeResult[]>([])
  const [geocodeError, setGeocodeError] = useState<string | null>(null)

  const coords = value.coordinates
  // Initialised once; all programmatic coordinate changes flow through applyCoords, which keeps
  // these inputs in sync — so no setState-in-effect is needed.
  const [latStr, setLatStr] = useState(coords ? String(coords.latitude) : '')
  const [lonStr, setLonStr] = useState(coords ? String(coords.longitude) : '')

  const setAddress = (patch: Partial<AddressInput>) =>
    onChange({ ...value, address: { ...value.address, ...patch } })

  // Map-driven changes (geocode result, marker drag, map click): update both the parent and the inputs.
  const applyCoords = (next: GpsCoordinatesInput) => {
    onChange({ ...value, coordinates: next })
    setLatStr(String(next.latitude))
    setLonStr(String(next.longitude))
  }

  const commitManualCoords = (latText: string, lonText: string) => {
    const lat = Number(latText)
    const lon = Number(lonText)
    if (latText.trim() === '' && lonText.trim() === '') {
      onChange({ ...value, coordinates: null })
      return
    }
    if (Number.isFinite(lat) && lat >= -90 && lat <= 90 && Number.isFinite(lon) && lon >= -180 && lon <= 180)
      onChange({ ...value, coordinates: { latitude: lat, longitude: lon } })
  }

  const query = useMemo(
    () =>
      [value.address.street, value.address.city, value.address.region, value.address.country]
        .filter((p) => p && p.trim())
        .join(', '),
    [value.address],
  )

  const runGeocode = async () => {
    setGeocodeError(null)
    setCandidates([])
    try {
      const results = await geocode.mutateAsync(query)
      if (results.length === 0) {
        setGeocodeError('No matches found for that address.')
        return
      }
      setCandidates(results)
      const first = results[0]
      applyCoords({ latitude: first.latitude, longitude: first.longitude })
    } catch (e) {
      setGeocodeError(e instanceof ApiError ? e.message : 'Geocoding failed. Enter coordinates manually.')
    }
  }

  const center: [number, number] = coords ? [coords.latitude, coords.longitude] : FALLBACK_CENTER

  return (
    <div className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <Field label="Street" error={errors?.street}>
          <Input
            value={value.address.street ?? ''}
            onChange={(e) => setAddress({ street: e.target.value })}
            disabled={disabled}
          />
        </Field>
        <Field label="City" error={errors?.city}>
          <Input
            value={value.address.city}
            onChange={(e) => setAddress({ city: e.target.value })}
            disabled={disabled}
            aria-invalid={!!errors?.city}
          />
        </Field>
        <Field label="Region" error={errors?.region}>
          <Input
            value={value.address.region ?? ''}
            onChange={(e) => setAddress({ region: e.target.value })}
            disabled={disabled}
          />
        </Field>
        <Field label="Country" error={errors?.country}>
          <Input
            value={value.address.country}
            onChange={(e) => setAddress({ country: e.target.value })}
            disabled={disabled}
            aria-invalid={!!errors?.country}
          />
        </Field>
      </div>

      <div className="flex items-center gap-2">
        <Button
          type="button"
          variant="secondary"
          size="sm"
          onClick={runGeocode}
          disabled={disabled || geocode.isPending || query.trim() === ''}
        >
          <MapPin className="size-4" /> {geocode.isPending ? 'Locating…' : 'Locate on map'}
        </Button>
        <span className="text-xs text-muted-foreground">
          Optional — fills coordinates from the address. You can also drag the marker.
        </span>
      </div>

      {geocodeError && <p className="text-sm text-destructive">{geocodeError}</p>}

      {candidates.length > 1 && (
        <ul className="rounded-md border text-sm">
          {candidates.map((c, i) => (
            <li key={`${c.latitude},${c.longitude},${i}`}>
              <button
                type="button"
                className="w-full px-3 py-2 text-left hover:bg-muted"
                onClick={() => applyCoords({ latitude: c.latitude, longitude: c.longitude })}
              >
                {c.displayName}
              </button>
            </li>
          ))}
        </ul>
      )}

      <div className="h-64 overflow-hidden rounded-md border">
        <MapContainer center={center} zoom={coords ? 13 : 5} className="h-full w-full">
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <Recenter coords={coords} />
          <ClickToSet onSet={(lat, lon) => applyCoords({ latitude: lat, longitude: lon })} disabled={disabled} />
          {coords && (
            <Marker
              position={[coords.latitude, coords.longitude]}
              icon={defaultIcon}
              draggable={!disabled}
              eventHandlers={{
                dragend: (e) => {
                  const { lat, lng } = e.target.getLatLng()
                  applyCoords({ latitude: lat, longitude: lng })
                },
              }}
            />
          )}
        </MapContainer>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label="Latitude">
          <Input
            type="number"
            step="any"
            value={latStr}
            onChange={(e) => setLatStr(e.target.value)}
            onBlur={() => commitManualCoords(latStr, lonStr)}
            disabled={disabled}
          />
        </Field>
        <Field label="Longitude">
          <Input
            type="number"
            step="any"
            value={lonStr}
            onChange={(e) => setLonStr(e.target.value)}
            onBlur={() => commitManualCoords(latStr, lonStr)}
            disabled={disabled}
          />
        </Field>
      </div>
    </div>
  )
}

function Recenter({ coords }: { coords: GpsCoordinatesInput | null }) {
  const map = useMap()
  useEffect(() => {
    if (coords) map.flyTo([coords.latitude, coords.longitude], Math.max(map.getZoom(), 13))
  }, [coords, map])
  return null
}

function ClickToSet({ onSet, disabled }: { onSet: (lat: number, lon: number) => void; disabled?: boolean }) {
  useMapEvents({
    click: (e) => {
      if (!disabled) onSet(e.latlng.lat, e.latlng.lng)
    },
  })
  return null
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-2">
      <Label>{label}</Label>
      {children}
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  )
}
