import { get, post } from '@/shared/api/http'
import type { Id, ListParams, Paginated } from '@/shared/api/types'
import type { CreateInstallmentPlan, InstallmentPlan, InstallmentPlanListItem } from './types'

const BASE = '/installment-plans'

export const installmentPlansApi = {
  list: (params: ListParams) => get<Paginated<InstallmentPlanListItem>>(BASE, { params }),
  detail: (id: Id) => get<InstallmentPlan>(`${BASE}/${id}`),
  create: (body: CreateInstallmentPlan) => post<InstallmentPlan>(BASE, body),
}
