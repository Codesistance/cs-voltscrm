import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Id, ListParams } from '@/shared/api/types'
import { invoicesApi } from './invoicesApi'

export const invoiceKeys = {
  all: ['invoices'] as const,
  lists: () => [...invoiceKeys.all, 'list'] as const,
  list: (p: ListParams) => [...invoiceKeys.lists(), p] as const,
  details: () => [...invoiceKeys.all, 'detail'] as const,
  detail: (id: Id) => [...invoiceKeys.details(), id] as const,
  paymentAccount: (customerId: Id) => [...invoiceKeys.all, 'payment-account', customerId] as const,
}

export function useInvoiceList(params: ListParams) {
  return useQuery({
    queryKey: invoiceKeys.list(params),
    queryFn: () => invoicesApi.list(params),
    placeholderData: keepPreviousData,
  })
}

export function useInvoice(id: Id) {
  return useQuery({
    queryKey: invoiceKeys.detail(id),
    queryFn: () => invoicesApi.detail(id),
    enabled: !!id,
  })
}

export function useGenerateInvoices() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: invoicesApi.generate,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: invoiceKeys.lists() })
    },
  })
}

export function useCustomerPaymentAccount(customerId: Id) {
  return useQuery({
    queryKey: invoiceKeys.paymentAccount(customerId),
    queryFn: () => invoicesApi.paymentAccountByCustomer(customerId),
    enabled: !!customerId,
  })
}
