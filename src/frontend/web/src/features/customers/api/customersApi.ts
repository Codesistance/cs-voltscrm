import { get, post, put } from '@/shared/api/http'
import type { Id, ListParams, Paginated } from '@/shared/api/types'
import type {
  AddServiceLocation,
  CreateCustomer,
  Customer,
  CustomerListItem,
  ServiceLocation,
  UpdateCustomer,
} from './types'

export interface CustomerGeoItem {
  id: Id
  fullName: string
  accountNumber: string
  latitude: number | null
  longitude: number | null
  city: string
}

export interface CustomerGeoResult {
  items: CustomerGeoItem[]
}

export interface GeoBounds {
  minLng: number
  minLat: number
  maxLng: number
  maxLat: number
}

const BASE = '/customers'

export const customersApi = {
  list: (params: ListParams) => get<Paginated<CustomerListItem>>(BASE, { params }),
  detail: (id: Id) => get<Customer>(`${BASE}/${id}`),
  geo: (bounds: GeoBounds) => get<CustomerGeoResult>(`${BASE}/geo`, { params: bounds }),
  create: (body: CreateCustomer) => post<Customer>(BASE, body),
  update: (id: Id, body: UpdateCustomer) => put<Customer>(`${BASE}/${id}`, body),
  suspend: (id: Id) => post<void>(`${BASE}/${id}/suspend`, {}),
  reactivate: (id: Id) => post<void>(`${BASE}/${id}/reactivate`, {}),
  disconnect: (id: Id) => post<void>(`${BASE}/${id}/disconnect`, {}),
  addServiceLocation: (id: Id, body: AddServiceLocation) =>
    post<ServiceLocation>(`${BASE}/${id}/service-locations`, body),
  deactivateServiceLocation: (id: Id, locationId: Id) =>
    post<void>(`${BASE}/${id}/service-locations/${locationId}/deactivate`, {}),
}
