import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { settingsApi } from './settingsApi'
import type {
  UpdateAutoDebitSettings,
  UpsertPaymentGatewayConfig,
  UpdateTokenVendingSettings,
} from './types'

export const settingsKeys = {
  all: ['settings'] as const,
  paymentGateways: () => [...settingsKeys.all, 'payment-gateways'] as const,
  autoDebit: () => [...settingsKeys.all, 'auto-debit'] as const,
  tokenVending: () => [...settingsKeys.all, 'token-vending'] as const,
}

export function usePaymentGateways() {
  return useQuery({
    queryKey: settingsKeys.paymentGateways(),
    queryFn: settingsApi.paymentGateways.list,
  })
}

export function useUpsertPaymentGateway() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ keyName, body }: { keyName: string; body: UpsertPaymentGatewayConfig }) =>
      settingsApi.paymentGateways.upsert(keyName, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: settingsKeys.paymentGateways() }),
  })
}

export function useSetPaymentGatewayVisibility() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ keyName, visible }: { keyName: string; visible: boolean }) =>
      settingsApi.paymentGateways.setVisibility(keyName, visible),
    onSuccess: () => qc.invalidateQueries({ queryKey: settingsKeys.paymentGateways() }),
  })
}

export function useAutoDebitSettings() {
  return useQuery({
    queryKey: settingsKeys.autoDebit(),
    queryFn: settingsApi.autoDebit.get,
  })
}

export function useSaveAutoDebitSettings() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateAutoDebitSettings) => settingsApi.autoDebit.save(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: settingsKeys.autoDebit() }),
  })
}

export function useTokenVendingSettings() {
  return useQuery({
    queryKey: settingsKeys.tokenVending(),
    queryFn: settingsApi.tokenVending.get,
  })
}

export function useSaveTokenVendingSettings() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateTokenVendingSettings) => settingsApi.tokenVending.save(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: settingsKeys.tokenVending() }),
  })
}
