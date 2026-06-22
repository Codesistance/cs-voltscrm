import type { Id } from '@/shared/api/types'
import type {
  Address,
  AddressInput,
  GpsCoordinates,
  GpsCoordinatesInput,
  Location,
  LocationInput,
} from '@/features/location/types'

export type { Address, AddressInput, GpsCoordinates, GpsCoordinatesInput, Location, LocationInput }

export const GENDERS = ['Male', 'Female', 'Other', 'PreferNotToSay'] as const
export type Gender = (typeof GENDERS)[number]

export const CUSTOMER_STATUSES = ['Active', 'Suspended', 'Disconnected'] as const
export type CustomerStatus = (typeof CUSTOMER_STATUSES)[number]

export interface PersonalInfo {
  firstName: string
  lastName: string
  gender: string
  phone: string
  email: string | null
  fullName: string
}

export interface ServiceLocation {
  id: Id
  label: string
  location: Location
  activeSubscriptionId: Id | null
  isActive: boolean
}

export interface CustomerListItem {
  id: Id
  accountNumber: string
  fullName: string
  phone: string
  status: string
  city: string
}

export interface Customer {
  id: Id
  accountNumber: string
  personalInfo: PersonalInfo
  location: Location
  status: string
  serviceLocations: ServiceLocation[]
  createdAt: string
  updatedAt: string
}

// --- inputs ---
export interface PersonalInfoInput {
  firstName: string
  lastName: string
  gender: string
  phone: string
  email?: string | null
}

export interface CreateCustomer {
  accountNumber: string
  personalInfo: PersonalInfoInput
  location: LocationInput
}

export interface UpdateCustomer {
  personalInfo: PersonalInfoInput
  location: LocationInput
}

export interface AddServiceLocation {
  label: string
  location: LocationInput
}
