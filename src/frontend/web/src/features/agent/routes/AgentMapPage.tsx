import { useEffect, useMemo, useState } from 'react'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { MapContainer, Marker, Popup, TileLayer, useMap, useMapEvents } from 'react-leaflet'
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png'
import markerIcon from 'leaflet/dist/images/marker-icon.png'
import markerShadow from 'leaflet/dist/images/marker-shadow.png'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { PageHeader } from '@/shared/components/PageHeader'
import { useCustomer } from '@/features/customers/api/queries'
import { useCustomersGeo } from '@/features/customers/api/queries'
import type { GeoBounds } from '@/features/customers/api/customersApi'

L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2x,
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
})

const DEFAULT_BOUNDS: GeoBounds = {
  minLng: 3.2,
  minLat: 6.3,
  maxLng: 3.5,
  maxLat: 6.7,
}

export function AgentMapPage() {
  const [selectedId, setSelectedId] = useState('')
  const [bounds, setBounds] = useState<GeoBounds | null>(DEFAULT_BOUNDS)
  const geo = useCustomersGeo(bounds)
  const selected = useCustomer(selectedId)

  const markers = useMemo(
    () =>
      (geo.data?.items ?? []).filter(
        (customer) => customer.latitude != null && customer.longitude != null,
      ),
    [geo.data?.items],
  )

  return (
    <div className="space-y-6">
      <PageHeader title="Route map" description="View customer locations for route planning." />
      <div className="grid gap-4 lg:grid-cols-[2fr_1fr]">
        <Card className="overflow-hidden">
          <CardContent className="p-0">
            <MapContainer center={[6.5244, 3.3792]} zoom={11} className="h-[560px] w-full">
              <TileLayer
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
              />
              <BoundsWatcher onBoundsChange={setBounds} />
              {markers.map((customer) => (
                <Marker
                  key={customer.id}
                  position={[customer.latitude!, customer.longitude!]}
                  eventHandlers={{ click: () => setSelectedId(customer.id) }}
                >
                  <Popup>
                    <p className="font-medium">{customer.fullName}</p>
                    <p>{customer.accountNumber}</p>
                    <p>{customer.city}</p>
                  </Popup>
                </Marker>
              ))}
            </MapContainer>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Selected customer</CardTitle>
          </CardHeader>
          <CardContent className="space-y-1 text-sm">
            {geo.isError && <ErrorState error={geo.error} onRetry={geo.refetch} />}
            {!geo.isError && geo.isLoading && bounds && (
              <LoadingState label="Loading map pins…" className="py-4" />
            )}
            {!selectedId && !geo.isLoading && (
              <p className="text-muted-foreground">Click a marker to inspect customer details.</p>
            )}
            {selectedId && selected.isLoading && <p className="text-muted-foreground">Loading details…</p>}
            {selected.data && (
              <>
                <p className="font-medium">{selected.data.personalInfo.fullName}</p>
                <p>{selected.data.accountNumber}</p>
                <p className="text-muted-foreground">{selected.data.personalInfo.phone}</p>
                <p className="text-muted-foreground">{selected.data.location.address.city}</p>
              </>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

function BoundsWatcher({ onBoundsChange }: { onBoundsChange: (bounds: GeoBounds) => void }) {
  const map = useMap()

  useEffect(() => {
    const b = map.getBounds()
    onBoundsChange({
      minLng: b.getWest(),
      minLat: b.getSouth(),
      maxLng: b.getEast(),
      maxLat: b.getNorth(),
    })
  }, [map, onBoundsChange])

  useMapEvents({
    moveend: () => {
      const b = map.getBounds()
      onBoundsChange({
        minLng: b.getWest(),
        minLat: b.getSouth(),
        maxLng: b.getEast(),
        maxLat: b.getNorth(),
      })
    },
  })

  return null
}
