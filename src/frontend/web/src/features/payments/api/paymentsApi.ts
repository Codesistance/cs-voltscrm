import { get, post } from '@/shared/api/http'
import type { Id, ListParams, Paginated } from '@/shared/api/types'
import type { LifecycleAction, Payment, PaymentListItem, RecordPayment } from './types'

const BASE = '/payments'

export const paymentsApi = {
  list: (params: ListParams) => get<Paginated<PaymentListItem>>(BASE, { params }),
  detail: (id: Id) => get<Payment>(`${BASE}/${id}`),
  create: (body: RecordPayment) => post<Payment>(BASE, body),
  lifecycle: (id: Id, action: LifecycleAction) => post<void>(`${BASE}/${id}/${action}`, {}),
}
