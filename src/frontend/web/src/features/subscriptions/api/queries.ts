import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Id, ListParams } from '@/shared/api/types'
import { inventoryKeys } from '@/features/inventory/api/queries'
import { subscriptionsApi } from './subscriptionsApi'
import type { CreateSubscription, LifecycleAction, RecordDeployedItem, UpdateSubscription } from './types'

export const subscriptionKeys = {
  all: ['subscriptions'] as const,
  lists: () => [...subscriptionKeys.all, 'list'] as const,
  list: (p: ListParams) => [...subscriptionKeys.lists(), p] as const,
  details: () => [...subscriptionKeys.all, 'detail'] as const,
  detail: (id: Id) => [...subscriptionKeys.details(), id] as const,
}

export function useSubscriptionList(params: ListParams) {
  return useQuery({
    queryKey: subscriptionKeys.list(params),
    queryFn: () => subscriptionsApi.list(params),
    placeholderData: keepPreviousData,
  })
}

export function useSubscription(id: Id) {
  return useQuery({
    queryKey: subscriptionKeys.detail(id),
    queryFn: () => subscriptionsApi.detail(id),
    enabled: !!id,
  })
}

export function useCreateSubscription() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateSubscription) => subscriptionsApi.create(body),
    onSuccess: (sub) => {
      qc.invalidateQueries({ queryKey: subscriptionKeys.lists() })
      qc.setQueryData(subscriptionKeys.detail(sub.id), sub)
    },
  })
}

export function useUpdateSubscription(id: Id) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateSubscription) => subscriptionsApi.update(id, body),
    onSuccess: (sub) => {
      qc.invalidateQueries({ queryKey: subscriptionKeys.lists() })
      qc.setQueryData(subscriptionKeys.detail(id), sub)
    },
  })
}

export function useSubscriptionLifecycle(id: Id) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (action: LifecycleAction) => subscriptionsApi.lifecycle(id, action),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: subscriptionKeys.detail(id) })
      qc.invalidateQueries({ queryKey: subscriptionKeys.lists() })
    },
  })
}

export function useRecordDeployedItem(id: Id) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: RecordDeployedItem) => subscriptionsApi.recordDeployedItem(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: subscriptionKeys.detail(id) })
      qc.invalidateQueries({ queryKey: inventoryKeys.lists() })
    },
  })
}
