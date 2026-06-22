import type { Id, Money } from '@/shared/api/types'

export const SUBSCRIPTION_STATUSES = ['Pending', 'Active', 'Suspended', 'Terminated'] as const
export type SubscriptionStatus = (typeof SUBSCRIPTION_STATUSES)[number]

export interface SubscriptionListItem {
  id: Id
  customerId: Id
  customerName: string
  customerAccountNumber: string
  servicePlanId: Id
  planName: string
  billingType: string
  status: string
  startDate: string
  endDate: string | null
}

export interface DeployedItem {
  id: Id
  inventoryItemId: Id
  inventoryItemCode: string
  inventoryItemName: string
  serialNumber: string | null
  stockMovementId: Id
  dispatchedDate: string
}

export interface Subscription {
  id: Id
  customerId: Id
  customerName: string
  customerAccountNumber: string
  servicePlanId: Id
  planCode: string
  planName: string
  billingType: string
  status: string
  negotiatedPrice: Money | null
  effectivePrice: Money
  startDate: string
  endDate: string | null
  deployedItems: DeployedItem[]
  createdAt: string
  updatedAt: string
}

export interface CreateSubscription {
  customerId: Id
  servicePlanId: Id
  startDate: string
  negotiatedPrice?: Money | null
  serviceLocationId?: Id | null
}

export interface UpdateSubscription {
  startDate: string
  negotiatedPrice?: Money | null
  serviceLocationId?: Id | null
}

export interface RecordDeployedItem {
  inventoryItemId: Id
  quantity: number
  serialNumber?: string | null
}

export type LifecycleAction = 'activate' | 'suspend' | 'resume' | 'terminate'
