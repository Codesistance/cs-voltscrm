import type { Id, Money } from '@/shared/api/types'

export interface InstallmentPlanListItem {
  id: Id
  customerId: Id
  customerSubscriptionId: Id
  customerName: string
  customerAccountNumber: string
  totalAmount: Money
  depositAmount: Money
  remainingAmount: Money
  startDate: string
  installmentCount: number
}

export interface Installment {
  id: Id
  amount: Money
  dueDate: string
  paidDate: string | null
  status: string
}

export interface InstallmentPlan {
  id: Id
  customerId: Id
  customerSubscriptionId: Id
  customerName: string
  customerAccountNumber: string
  totalAmount: Money
  depositAmount: Money
  remainingAmount: Money
  startDate: string
  installments: Installment[]
  createdAt: string
  updatedAt: string
}

export interface CreateInstallmentPlan {
  subscriptionId: Id
  totalAmount: number
  depositAmount: number
  installmentCount: number
  startDate: string
}
