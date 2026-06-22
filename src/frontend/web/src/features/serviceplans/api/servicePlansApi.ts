import { get, post, put } from '@/shared/api/http'
import type { Id, ListParams, Paginated } from '@/shared/api/types'
import type { CreateServicePlan, ServicePlan, ServicePlanListItem, UpdateServicePlan } from './types'

const BASE = '/service-plans'

export const servicePlansApi = {
  list: (params: ListParams) => get<Paginated<ServicePlanListItem>>(BASE, { params }),
  detail: (id: Id) => get<ServicePlan>(`${BASE}/${id}`),
  create: (body: CreateServicePlan) => post<ServicePlan>(BASE, body),
  update: (id: Id, body: UpdateServicePlan) => put<ServicePlan>(`${BASE}/${id}`, body),
  archive: (id: Id) => post<void>(`${BASE}/${id}/archive`, {}),
  restore: (id: Id) => post<void>(`${BASE}/${id}/restore`, {}),
}
