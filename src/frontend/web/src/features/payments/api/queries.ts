import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Id, ListParams } from '@/shared/api/types'
import { paymentsApi } from './paymentsApi'
import type { LifecycleAction, RecordPayment } from './types'

export const paymentKeys = {
  all: ['payments'] as const,
  lists: () => [...paymentKeys.all, 'list'] as const,
  list: (p: ListParams) => [...paymentKeys.lists(), p] as const,
  details: () => [...paymentKeys.all, 'detail'] as const,
  detail: (id: Id) => [...paymentKeys.details(), id] as const,
}

export function usePaymentList(params: ListParams) {
  return useQuery({
    queryKey: paymentKeys.list(params),
    queryFn: () => paymentsApi.list(params),
    placeholderData: keepPreviousData,
  })
}

export function usePayment(id: Id) {
  return useQuery({
    queryKey: paymentKeys.detail(id),
    queryFn: () => paymentsApi.detail(id),
    enabled: !!id,
  })
}

export function useRecordPayment() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: RecordPayment) => paymentsApi.create(body),
    onSuccess: (payment) => {
      qc.invalidateQueries({ queryKey: paymentKeys.lists() })
      qc.setQueryData(paymentKeys.detail(payment.id), payment)
    },
  })
}

export function usePaymentLifecycle(id: Id) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (action: LifecycleAction) => paymentsApi.lifecycle(id, action),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: paymentKeys.detail(id) })
      qc.invalidateQueries({ queryKey: paymentKeys.lists() })
    },
  })
}
