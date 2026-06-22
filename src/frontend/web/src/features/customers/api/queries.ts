import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Id, ListParams } from '@/shared/api/types'
import { customersApi, type GeoBounds } from './customersApi'
import type { AddServiceLocation, CreateCustomer, UpdateCustomer } from './types'

export const customerKeys = {
  all: ['customers'] as const,
  lists: () => [...customerKeys.all, 'list'] as const,
  list: (p: ListParams) => [...customerKeys.lists(), p] as const,
  details: () => [...customerKeys.all, 'detail'] as const,
  detail: (id: Id) => [...customerKeys.details(), id] as const,
  geo: (bounds: GeoBounds) => [...customerKeys.all, 'geo', bounds] as const,
}

export type StatusAction = 'suspend' | 'reactivate' | 'disconnect'

export function useCustomerList(params: ListParams) {
  return useQuery({
    queryKey: customerKeys.list(params),
    queryFn: () => customersApi.list(params),
    placeholderData: keepPreviousData,
  })
}

export function useCustomersGeo(bounds: GeoBounds | null) {
  return useQuery({
    queryKey: customerKeys.geo(bounds ?? { minLng: 0, minLat: 0, maxLng: 0, maxLat: 0 }),
    queryFn: () => customersApi.geo(bounds!),
    enabled: bounds !== null,
    placeholderData: keepPreviousData,
  })
}

export function useCustomer(id: Id) {
  return useQuery({
    queryKey: customerKeys.detail(id),
    queryFn: () => customersApi.detail(id),
    enabled: !!id,
  })
}

export function useCreateCustomer() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateCustomer) => customersApi.create(body),
    onSuccess: (customer) => {
      qc.invalidateQueries({ queryKey: customerKeys.lists() })
      qc.setQueryData(customerKeys.detail(customer.id), customer)
    },
  })
}

export function useUpdateCustomer(id: Id) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateCustomer) => customersApi.update(id, body),
    onSuccess: (customer) => {
      qc.invalidateQueries({ queryKey: customerKeys.lists() })
      qc.setQueryData(customerKeys.detail(id), customer)
    },
  })
}

export function useCustomerStatus(id: Id) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (action: StatusAction) => customersApi[action](id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: customerKeys.detail(id) })
      qc.invalidateQueries({ queryKey: customerKeys.lists() })
    },
  })
}

export function useAddServiceLocation(id: Id) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: AddServiceLocation) => customersApi.addServiceLocation(id, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: customerKeys.detail(id) }),
  })
}

export function useDeactivateServiceLocation(id: Id) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (locationId: Id) => customersApi.deactivateServiceLocation(id, locationId),
    onSuccess: () => qc.invalidateQueries({ queryKey: customerKeys.detail(id) }),
  })
}
