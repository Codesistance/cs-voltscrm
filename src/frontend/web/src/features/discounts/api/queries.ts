import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Id, ListParams } from '@/shared/api/types'
import { discountsApi } from './discountsApi'
import type { GrantDiscount } from './types'

export const discountKeys = {
  all: ['discounts'] as const,
  lists: () => [...discountKeys.all, 'list'] as const,
  list: (p: ListParams) => [...discountKeys.lists(), p] as const,
}

export function useDiscountList(params: ListParams) {
  return useQuery({
    queryKey: discountKeys.list(params),
    queryFn: () => discountsApi.list(params),
    placeholderData: keepPreviousData,
  })
}

export function useGrantDiscount() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: GrantDiscount) => discountsApi.create(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: discountKeys.lists() }),
  })
}

export function useRevokeDiscount() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: Id) => discountsApi.revoke(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: discountKeys.lists() }),
  })
}
