import type { Money } from '@/shared/api/types'

export interface DashboardSummary {
  activeCustomers: number
  outstandingBalance: Money
  collectionsMtd: Money
  overdueInvoices: number
}

export interface CollectionSummaryItem {
  date: string
  paymentCount: number
  totalAmount: Money
}

export interface CollectionSummary {
  from: string
  to: string
  totalCollected: Money
  daily: CollectionSummaryItem[]
}

export interface AgingBucket {
  bucket: string
  invoiceCount: number
  totalBalance: Money
}

export interface AgingReport {
  buckets: AgingBucket[]
}

export interface CustomerStatement {
  customerId: string
  customerName: string
  customerAccountNumber: string
  from: string | null
  to: string | null
  totalInvoiced: Money
  totalPaid: Money
  outstandingBalance: Money
  invoices: StatementInvoice[]
  payments: StatementPayment[]
}

export interface StatementInvoice {
  id: string
  dueDate: string
  amountDue: Money
  amountPaid: Money
  balance: Money
  status: string
}

export interface StatementPayment {
  id: string
  paymentDate: string
  amount: Money
  netAmount: Money
  method: string
  status: string
  reference: string | null
}
