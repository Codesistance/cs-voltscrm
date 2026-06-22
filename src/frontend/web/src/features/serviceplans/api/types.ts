import type { Id, Money } from '@/shared/api/types'

export const BILLING_TYPES = ['Prepaid', 'Postpaid', 'Installment', 'Subscription'] as const
export type BillingType = (typeof BILLING_TYPES)[number]

export const BILLING_CYCLES = ['OneTime', 'Monthly', 'Quarterly', 'Annual'] as const
export type BillingCycle = (typeof BILLING_CYCLES)[number]

export const PLAN_STATUSES = ['Active', 'Archived'] as const
export type PlanStatus = (typeof PLAN_STATUSES)[number]

export const LINE_ITEM_ROLES = ['Core', 'Optional', 'AddOn'] as const
export type LineItemRole = (typeof LINE_ITEM_ROLES)[number]

export interface ServicePlanLineItem {
  id: Id
  inventoryItemId: Id
  inventoryItemCode: string
  inventoryItemName: string
  quantity: number
  role: string
}

export interface ServicePlanListItem {
  id: Id
  planCode: string
  name: string
  billingType: string
  billingCycle: string
  basePrice: Money
  status: string
  lineItemCount: number
}

export interface ServicePlan {
  id: Id
  planCode: string
  name: string
  description: string | null
  billingType: string
  billingCycle: string
  basePrice: Money
  status: string
  lineItems: ServicePlanLineItem[]
  createdAt: string
  updatedAt: string
}

export interface ServicePlanLineItemInput {
  inventoryItemId: Id
  quantity: number
  role: string
}

export interface CreateServicePlan {
  planCode: string
  name: string
  description?: string | null
  billingType: string
  billingCycle: string
  basePrice: Money
  lineItems: ServicePlanLineItemInput[]
}

export interface UpdateServicePlan {
  name: string
  description?: string | null
  basePrice: Money
  lineItems: ServicePlanLineItemInput[]
}
