import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { portalApi } from './portalApi'

export const portalKeys = {
  all: ['portal'] as const,
  summary: () => [...portalKeys.all, 'summary'] as const,
  subscriptions: () => [...portalKeys.all, 'subscriptions'] as const,
  invoices: () => [...portalKeys.all, 'invoices'] as const,
  payments: () => [...portalKeys.all, 'payments'] as const,
  profile: () => [...portalKeys.all, 'profile'] as const,
  gateways: () => [...portalKeys.all, 'gateways'] as const,
}

export function usePortalSummary() {
  return useQuery({ queryKey: portalKeys.summary(), queryFn: portalApi.summary })
}

export function usePortalServices() {
  return useQuery({
    queryKey: portalKeys.subscriptions(),
    queryFn: () => portalApi.subscriptions(),
    select: (data) => data.items,
  })
}

export function usePortalInvoices() {
  return useQuery({
    queryKey: portalKeys.invoices(),
    queryFn: () => portalApi.invoices(),
    select: (data) => data.items,
  })
}

export function usePortalPayments() {
  return useQuery({
    queryKey: portalKeys.payments(),
    queryFn: () => portalApi.payments(),
    select: (data) => data.items,
  })
}

export function usePortalProfile() {
  return useQuery({ queryKey: portalKeys.profile(), queryFn: portalApi.profile })
}

export function usePortalGateways() {
  return useQuery({ queryKey: portalKeys.gateways(), queryFn: portalApi.gateways })
}

export function usePayInvoice() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: portalApi.pay,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: portalKeys.invoices() })
      qc.invalidateQueries({ queryKey: portalKeys.summary() })
      qc.invalidateQueries({ queryKey: portalKeys.payments() })
    },
  })
}
