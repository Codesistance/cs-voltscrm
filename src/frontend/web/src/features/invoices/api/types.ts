import type { Id, Money } from '@/shared/api/types'

export const INVOICE_STATUSES = ['Pending', 'PartiallyPaid', 'Paid', 'Overdue', 'Cancelled'] as const
export type InvoiceStatus = (typeof INVOICE_STATUSES)[number]

export interface InvoiceListItem {
  id: Id
  customerId: Id
  customerSubscriptionId: Id
  customerName: string
  customerAccountNumber: string
  periodYear: number
  periodMonth: number
  grossAmount: Money
  discountAmount: Money
  amountDue: Money
  amountPaid: Money
  balance: Money
  dueDate: string
  status: string
}

export interface InvoiceLineItem {
  id: Id
  description: string
  amount: number
  isDiscount: boolean
}

export interface Invoice {
  id: Id
  customerId: Id
  customerSubscriptionId: Id
  customerName: string
  customerAccountNumber: string
  periodYear: number
  periodMonth: number
  grossAmount: Money
  discountAmount: Money
  amountDue: Money
  amountPaid: Money
  balance: Money
  dueDate: string
  status: string
  lineItems: InvoiceLineItem[]
  createdAt: string
  updatedAt: string
}

export interface GenerateInvoicesResult {
  generatedCount: number
  message: string
}

export interface PaymentAccount {
  id: Id
  customerId: Id
  balance: Money
  lastPaymentDate: string | null
  createdAt: string
  updatedAt: string
}

export function formatInvoicePeriod(year: number, month: number) {
  return `${year}-${String(month).padStart(2, '0')}`
}

export function formatInvoiceLabel(item: Pick<InvoiceListItem, 'periodYear' | 'periodMonth' | 'customerAccountNumber'>) {
  return `${item.customerAccountNumber} · ${formatInvoicePeriod(item.periodYear, item.periodMonth)}`
}
