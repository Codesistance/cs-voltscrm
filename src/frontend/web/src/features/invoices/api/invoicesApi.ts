import { get, post } from '@/shared/api/http'
import type { Id, ListParams, Paginated } from '@/shared/api/types'
import type { GenerateInvoicesResult, Invoice, InvoiceListItem, PaymentAccount } from './types'

const BASE = '/invoices'

export const invoicesApi = {
  list: (params: ListParams) => get<Paginated<InvoiceListItem>>(BASE, { params }),
  detail: (id: Id) => get<Invoice>(`${BASE}/${id}`),
  generate: async (): Promise<GenerateInvoicesResult> => {
    const created = await post<Invoice[]>(`${BASE}/generate`, {})
    const count = created.length
    return {
      generatedCount: count,
      message: count === 0 ? 'No new invoices were generated.' : `Generated ${count} invoice(s).`,
    }
  },
  paymentAccountByCustomer: (customerId: Id) =>
    get<PaymentAccount>(`${BASE}/customers/${customerId}/payment-account`),
}
