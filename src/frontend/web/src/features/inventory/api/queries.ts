import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Id, ListParams } from '@/shared/api/types'
import { inventoryApi } from './inventoryApi'
import type {
  CreateInventoryCategory,
  CreateInventoryItem,
  RecordStockMovement,
  UpdateInventoryCategory,
  UpdateInventoryItem,
} from './types'

export const inventoryKeys = {
  all: ['inventory'] as const,
  lists: () => [...inventoryKeys.all, 'list'] as const,
  list: (p: ListParams) => [...inventoryKeys.lists(), p] as const,
  details: () => [...inventoryKeys.all, 'detail'] as const,
  detail: (id: Id) => [...inventoryKeys.details(), id] as const,
  movements: (id: Id, p: ListParams) => [...inventoryKeys.detail(id), 'movements', p] as const,
  categories: () => [...inventoryKeys.all, 'categories'] as const,
}

export function useInventoryList(params: ListParams) {
  return useQuery({
    queryKey: inventoryKeys.list(params),
    queryFn: () => inventoryApi.list(params),
    placeholderData: keepPreviousData,
  })
}

export function useInventoryItem(id: Id) {
  return useQuery({
    queryKey: inventoryKeys.detail(id),
    queryFn: () => inventoryApi.detail(id),
    enabled: !!id,
  })
}

export function useStockMovements(id: Id, params: ListParams) {
  return useQuery({
    queryKey: inventoryKeys.movements(id, params),
    queryFn: () => inventoryApi.movements(id, params),
    enabled: !!id,
    placeholderData: keepPreviousData,
  })
}

export function useCreateInventoryItem() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateInventoryItem) => inventoryApi.create(body),
    onSuccess: (item) => {
      qc.invalidateQueries({ queryKey: inventoryKeys.lists() })
      qc.setQueryData(inventoryKeys.detail(item.id), item)
    },
  })
}

export function useUpdateInventoryItem(id: Id) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateInventoryItem) => inventoryApi.update(id, body),
    onSuccess: (item) => {
      qc.invalidateQueries({ queryKey: inventoryKeys.lists() })
      qc.setQueryData(inventoryKeys.detail(id), item)
    },
  })
}

export function useDeactivateInventoryItem() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: Id) => inventoryApi.deactivate(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: inventoryKeys.all }),
  })
}

export function useRecordStockMovement(id: Id) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: RecordStockMovement) => inventoryApi.recordMovement(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: inventoryKeys.detail(id) })
      qc.invalidateQueries({ queryKey: inventoryKeys.lists() })
    },
  })
}

export function useInventoryCategories() {
  return useQuery({
    queryKey: inventoryKeys.categories(),
    queryFn: () => inventoryApi.categories(),
  })
}

export function useCreateInventoryCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateInventoryCategory) => inventoryApi.createCategory(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: inventoryKeys.categories() }),
  })
}

export function useUpdateInventoryCategory(id: Id) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateInventoryCategory) => inventoryApi.updateCategory(id, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: inventoryKeys.categories() }),
  })
}

export function useDeactivateInventoryCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: Id) => inventoryApi.deactivateCategory(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: inventoryKeys.categories() }),
  })
}
