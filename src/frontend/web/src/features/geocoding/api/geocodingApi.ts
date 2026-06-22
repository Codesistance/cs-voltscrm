import { get } from '@/shared/api/http'

export interface GeocodeResult {
  displayName: string
  latitude: number
  longitude: number
}

export const geocodingApi = {
  search: (q: string) => get<GeocodeResult[]>('/geocoding/search', { params: { q } }),
}
