// Shared address/location shapes, used by any feature that owns a Location (Customer, Agent).

export interface Address {
  street: string
  city: string
  region: string
  country: string
}

export interface GpsCoordinates {
  latitude: number
  longitude: number
}

export interface Location {
  address: Address
  coordinates: GpsCoordinates | null
}

// --- inputs ---
export interface AddressInput {
  street?: string | null
  city: string
  region?: string | null
  country: string
}

export interface GpsCoordinatesInput {
  latitude: number
  longitude: number
}

export interface LocationInput {
  address: AddressInput
  coordinates: GpsCoordinatesInput | null
}
