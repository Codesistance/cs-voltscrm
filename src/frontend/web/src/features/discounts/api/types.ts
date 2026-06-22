import type { Id } from '@/shared/api/types'

export const DISCOUNT_TYPES = ['Percentage', 'FixedAmount'] as const
export type DiscountType = (typeof DISCOUNT_TYPES)[number]

export const DISCOUNT_SCOPES = ['Invoice', 'Subscription', 'Payment'] as const
export type DiscountScope = (typeof DISCOUNT_SCOPES)[number]

export const DISCOUNT_STATUSES = ['Active', 'Applied', 'Revoked', 'Expired'] as const
export type DiscountStatus = (typeof DISCOUNT_STATUSES)[number]

export interface DiscountGrant {
  id: Id
  customerId: Id
  customerName: string
  discountType: string
  value: number
  scope: string
  targetId: Id | null
  isRecurring: boolean
  validFrom: string
  validUntil: string | null
  grantedByUserId: string
  grantedAt: string
  reason: string | null
  status: string
  createdAt: string
  updatedAt: string
}

export interface GrantDiscount {
  customerId: Id
  discountType: string
  value: number
  scope: string
  targetId?: Id | null
  isRecurring: boolean
  validFrom?: string | null
  validUntil?: string | null
  reason?: string | null
}
