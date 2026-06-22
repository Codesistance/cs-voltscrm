import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Id, ListParams } from '@/shared/api/types'
import { servicePlansApi } from './servicePlansApi'
import type { CreateServicePlan, UpdateServicePlan } from './types'

export const servicePlanKeys = {
  all: ['service-plans'] as const,
  lists: () => [...servicePlanKeys.all, 'list'] as const,
  list: (p: ListParams) => [...servicePlanKeys.lists(), p] as const,
  details: () => [...servicePlanKeys.all, 'detail'] as const,
  detail: (id: Id) => [...servicePlanKeys.details(), id] as const,
}

export function useServicePlanList(params: ListParams) {
  return useQuery({
    queryKey: servicePlanKeys.list(params),
    queryFn: () => servicePlansApi.list(params),
    placeholderData: keepPreviousData,
  })
}

export function useServicePlan(id: Id) {
  return useQuery({
    queryKey: servicePlanKeys.detail(id),
    queryFn: () => servicePlansApi.detail(id),
    enabled: !!id,
  })
}

export function useCreateServicePlan() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateServicePlan) => servicePlansApi.create(body),
    onSuccess: (plan) => {
      qc.invalidateQueries({ queryKey: servicePlanKeys.lists() })
      qc.setQueryData(servicePlanKeys.detail(plan.id), plan)
    },
  })
}

export function useUpdateServicePlan(id: Id) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateServicePlan) => servicePlansApi.update(id, body),
    onSuccess: (plan) => {
      qc.invalidateQueries({ queryKey: servicePlanKeys.lists() })
      qc.setQueryData(servicePlanKeys.detail(id), plan)
    },
  })
}

export function useArchiveServicePlan() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: Id) => servicePlansApi.archive(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: servicePlanKeys.all }),
  })
}

export function useRestoreServicePlan() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: Id) => servicePlansApi.restore(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: servicePlanKeys.all }),
  })
}
