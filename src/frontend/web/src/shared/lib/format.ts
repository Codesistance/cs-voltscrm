import { config } from '@/app/config'
import type { Money } from '@/shared/api/types'

export function formatMoney(money: Money | null | undefined): string {
  if (!money) return '—'
  return new Intl.NumberFormat(config.defaultLocale, {
    style: 'currency',
    currency: money.currency || config.defaultCurrency,
  }).format(money.amount)
}

export function formatDate(value: string | Date | null | undefined): string {
  if (!value) return '—'
  return new Intl.DateTimeFormat(config.defaultLocale, { dateStyle: 'medium' }).format(new Date(value))
}

export function formatDateTime(value: string | Date | null | undefined): string {
  if (!value) return '—'
  return new Intl.DateTimeFormat(config.defaultLocale, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function formatPeriod(year: number, month: number): string {
  return new Intl.DateTimeFormat(config.defaultLocale, { month: 'long', year: 'numeric' }).format(
    new Date(year, month - 1, 1),
  )
}
