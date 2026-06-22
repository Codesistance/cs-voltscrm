import { get, post, put } from '@/shared/api/http'
import type { Id, ListParams, Paginated } from '@/shared/api/types'
import type {
  CreateSubscription,
  DeployedItem,
  LifecycleAction,
  Subscription,
  SubscriptionListItem,
  RecordDeployedItem,
  UpdateSubscription,
} from './types'

const BASE = '/subscriptions'

export const subscriptionsApi = {
  list: (params: ListParams) => get<Paginated<SubscriptionListItem>>(BASE, { params }),
  detail: (id: Id) => get<Subscription>(`${BASE}/${id}`),
  create: (body: CreateSubscription) => post<Subscription>(BASE, body),
  update: (id: Id, body: UpdateSubscription) => put<Subscription>(`${BASE}/${id}`, body),
  lifecycle: (id: Id, action: LifecycleAction) => post<void>(`${BASE}/${id}/${action}`, {}),
  recordDeployedItem: (id: Id, body: RecordDeployedItem) =>
    post<DeployedItem>(`${BASE}/${id}/deployed-items`, body),
}
