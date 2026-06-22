import { get, post } from '@/shared/api/http'
import type { Id, ListParams, Paginated } from '@/shared/api/types'
import type { DiscountGrant, GrantDiscount } from './types'

const BASE = '/discounts'

export const discountsApi = {
  list: (params: ListParams) => get<Paginated<DiscountGrant>>(BASE, { params }),
  create: (body: GrantDiscount) => post<DiscountGrant>(BASE, body),
  revoke: (id: Id) => post<void>(`${BASE}/${id}/revoke`, {}),
}
