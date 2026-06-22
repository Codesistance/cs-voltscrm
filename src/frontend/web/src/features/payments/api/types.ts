import type { Id, Money } from '@/shared/api/types'

export const PAYMENT_STATUSES = ['Pending', 'Completed', 'Failed', 'Reversed'] as const
export type PaymentStatus = (typeof PAYMENT_STATUSES)[number]

export const PAYMENT_METHODS = ['Cash', 'MobileMoney', 'AutoDebit', 'BankTransfer', 'Online'] as const
export type PaymentMethod = (typeof PAYMENT_METHODS)[number]

export const PAYMENT_CHANNELS = ['Agent', 'CustomerPortal', 'System', 'Import'] as const
export type PaymentChannel = (typeof PAYMENT_CHANNELS)[number]

export interface PaymentListItem {
  id: Id
  customerId: Id
  customerName: string
  amount: Money
  netAmount: Money
  method: string
  channel: string
  status: string
  paymentDate: string
  platformReference: string | null
}

export interface PaymentAllocation {
  id: Id
  invoiceId: Id | null
  installmentId: Id | null
  amount: number
}

export interface PaymentAllocationInput {
  invoiceId?: Id | null
  installmentId?: Id | null
  amount: number
}

export interface Payment {
  id: Id
  customerId: Id
  customerName: string
  customerAccountNumber: string
  amount: Money
  discountApplied: number
  netAmount: Money
  method: string
  channel: string
  status: string
  platformProvider: string | null
  platformReference: string | null
  collectedByAgentId: string | null
  paymentDate: string
  allocations: PaymentAllocation[]
  createdAt: string
  updatedAt: string
}

export interface RecordPayment {
  customerId: Id
  amount: number
  currency: string
  method: string
  channel: string
  paymentDate?: string | null
  platformProvider?: string | null
  platformReference?: string | null
  collectedByAgentId?: string | null
  allocations?: PaymentAllocationInput[] | null
  autoComplete?: boolean
}

export type LifecycleAction = 'complete' | 'fail' | 'reverse'
