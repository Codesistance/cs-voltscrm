/** Maps domain status enums to a Badge tone, used by <StatusPill/>. */
export type StatusTone = 'success' | 'warning' | 'destructive' | 'info' | 'muted' | 'default'

const customer: Record<string, StatusTone> = {
  Active: 'success',
  Suspended: 'warning',
  Disconnected: 'destructive',
}
const invoice: Record<string, StatusTone> = {
  Paid: 'success',
  PartiallyPaid: 'info',
  Pending: 'muted',
  Overdue: 'destructive',
}
const subscription: Record<string, StatusTone> = {
  Active: 'success',
  Pending: 'muted',
  Suspended: 'warning',
  Terminated: 'destructive',
}
const payment: Record<string, StatusTone> = {
  Completed: 'success',
  Pending: 'muted',
  Failed: 'destructive',
  Reversed: 'warning',
}
const plan: Record<string, StatusTone> = { Active: 'success', Archived: 'muted' }
const installment: Record<string, StatusTone> = {
  Paid: 'success',
  Pending: 'muted',
  Overdue: 'destructive',
}
const discount: Record<string, StatusTone> = {
  Active: 'success',
  Applied: 'info',
  Revoked: 'destructive',
  Expired: 'muted',
}

const statusTones = { customer, invoice, subscription, payment, plan, installment, discount }

export type StatusDomain = keyof typeof statusTones

export function toneFor(domain: StatusDomain, status: string): StatusTone {
  return statusTones[domain][status] ?? 'default'
}
