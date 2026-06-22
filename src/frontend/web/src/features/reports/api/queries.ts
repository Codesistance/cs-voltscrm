import { useQuery } from '@tanstack/react-query'
import type { Id } from '@/shared/api/types'
import { reportsApi } from './reportsApi'

export const reportKeys = {
  all: ['reports'] as const,
  statement: (customerId: Id) => [...reportKeys.all, 'statement', customerId] as const,
  collections: () => [...reportKeys.all, 'collections'] as const,
  aging: () => [...reportKeys.all, 'aging'] as const,
  dashboardSummary: () => [...reportKeys.all, 'dashboard-summary'] as const,
}

export function useCustomerStatement(customerId: Id) {
  return useQuery({
    queryKey: reportKeys.statement(customerId),
    queryFn: () => reportsApi.customerStatement(customerId),
    enabled: !!customerId,
  })
}

export function useCollectionsReport() {
  return useQuery({
    queryKey: reportKeys.collections(),
    queryFn: reportsApi.collections,
    select: (data) =>
      data.daily.map((row) => ({
        period: row.date,
        amount: row.totalAmount.amount,
        count: row.paymentCount,
      })),
  })
}

export function useAgingReport() {
  return useQuery({
    queryKey: reportKeys.aging(),
    queryFn: reportsApi.aging,
    select: (data) =>
      data.buckets.map((row) => ({
        bucket: row.bucket,
        amount: row.totalBalance.amount,
        count: row.invoiceCount,
      })),
  })
}

export function useDashboardSummary() {
  return useQuery({
    queryKey: reportKeys.dashboardSummary(),
    queryFn: reportsApi.dashboardSummary,
  })
}
