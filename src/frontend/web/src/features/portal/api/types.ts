import type { Money, Paginated } from '@/shared/api/types'

export interface AvailableGateway {
  keyName: string
  displayName: string
}

export interface InitiatePaymentResult {
  paymentId: string
  status: string
  checkoutUrl: string | null
}

export interface PortalSummary {
  outstandingBalance: Money
  pendingInvoices: number
  activeSubscriptions: number
  paidThisMonth: Money
}

export interface PortalSubscription {
  id: string
  servicePlanId: string
  planName: string
  status: string
  startDate: string
  endDate: string | null
  effectivePrice: Money
}

export interface PortalInvoice {
  id: string
  periodYear: number
  periodMonth: number
  amountDue: Money
  amountPaid: Money
  balance: Money
  dueDate: string
  status: string
}

export interface PortalPayment {
  id: string
  amount: Money
  netAmount: Money
  method: string
  channel: string
  status: string
  paymentDate: string
  platformReference: string | null
}

export interface PortalProfile {
  accountNumber: string
  fullName: string
  phone: string
  email: string | null
  status: string
  address: { street: string; city: string; region: string; country: string }
}

export type PortalInvoicesPage = Paginated<PortalInvoice>
export type PortalPaymentsPage = Paginated<PortalPayment>
export type PortalSubscriptionsPage = Paginated<PortalSubscription>

export function formatPortalInvoicePeriod(year: number, month: number) {
  return `${year}-${String(month).padStart(2, '0')}`
}
