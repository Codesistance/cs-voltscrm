import { keepPreviousData, useMutation, useQueries, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Id, ListParams } from '@/shared/api/types'
import { installmentPlansApi } from './installmentPlansApi'
import type { CreateInstallmentPlan, Installment } from './types'

export const installmentPlanKeys = {
  all: ['installment-plans'] as const,
  lists: () => [...installmentPlanKeys.all, 'list'] as const,
  list: (p: ListParams) => [...installmentPlanKeys.lists(), p] as const,
  details: () => [...installmentPlanKeys.all, 'detail'] as const,
  detail: (id: Id) => [...installmentPlanKeys.details(), id] as const,
}

export function useInstallmentPlanList(params: ListParams) {
  return useQuery({
    queryKey: installmentPlanKeys.list(params),
    queryFn: () => installmentPlansApi.list(params),
    placeholderData: keepPreviousData,
  })
}

export function useInstallmentPlan(id: Id) {
  return useQuery({
    queryKey: installmentPlanKeys.detail(id),
    queryFn: () => installmentPlansApi.detail(id),
    enabled: !!id,
  })
}

export interface OpenInstallmentOption {
  id: Id
  label: string
  amount: number
}

/** Pending/overdue installments for a customer (loads plan details as needed). */
export function useCustomerOpenInstallments(customerId: Id) {
  const plans = useInstallmentPlanList({
    page: 1,
    pageSize: 50,
    customerId: customerId || undefined,
  })

  const planIds = (plans.data?.items ?? []).map((plan) => plan.id)
  const details = useQueries({
    queries: planIds.map((id) => ({
      queryKey: installmentPlanKeys.detail(id),
      queryFn: () => installmentPlansApi.detail(id),
      enabled: !!customerId && planIds.length > 0,
    })),
  })

  const installments: OpenInstallmentOption[] = details.flatMap((result) => {
    const plan = result.data
    if (!plan) return []
    return plan.installments
      .filter((inst: Installment) => inst.status === 'Pending' || inst.status === 'Overdue')
      .map((inst: Installment) => ({
        id: inst.id,
        label: `Due ${inst.dueDate.slice(0, 10)} · ${inst.amount.amount.toFixed(2)} (${inst.status})`,
        amount: inst.amount.amount,
      }))
  })

  return {
    installments,
    isLoading: plans.isLoading || details.some((q) => q.isLoading),
  }
}

export function useCreateInstallmentPlan() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateInstallmentPlan) => installmentPlansApi.create(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: installmentPlanKeys.lists() })
    },
  })
}
