import { Button } from '@/components/ui/button'
import { DatePicker } from '@/components/ui/date-picker'
import { Select } from '@/components/ui/select'
import { useCustomerList } from '@/features/customers/api/queries'
import { PAYMENT_METHODS, PAYMENT_STATUSES } from '../api/types'

export function PaymentFilters({
  status,
  method,
  customerId,
  from,
  to,
  onStatus,
  onMethod,
  onCustomer,
  onFrom,
  onTo,
}: {
  status: string
  method: string
  customerId: string
  from: string
  to: string
  onStatus: (value: string) => void
  onMethod: (value: string) => void
  onCustomer: (value: string) => void
  onFrom: (value: string) => void
  onTo: (value: string) => void
}) {
  const customers = useCustomerList({ page: 1, pageSize: 200 })
  const fromDate = from ? new Date(from) : undefined
  const toDate = to ? new Date(to) : undefined

  const hasFilters = status || method || customerId || from || to

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Select value={status} onChange={(e) => onStatus(e.target.value)} className="w-40">
        <option value="">All statuses</option>
        {PAYMENT_STATUSES.map((s) => (
          <option key={s} value={s}>
            {s}
          </option>
        ))}
      </Select>
      <Select value={method} onChange={(e) => onMethod(e.target.value)} className="w-44">
        <option value="">All methods</option>
        {PAYMENT_METHODS.map((m) => (
          <option key={m} value={m}>
            {m}
          </option>
        ))}
      </Select>
      <Select value={customerId} onChange={(e) => onCustomer(e.target.value)} className="w-56">
        <option value="">All customers</option>
        {(customers.data?.items ?? []).map((c) => (
          <option key={c.id} value={c.id}>
            {c.accountNumber} · {c.fullName}
          </option>
        ))}
      </Select>
      <DatePicker
        value={fromDate}
        onChange={(d) => onFrom(d ? startOfDayIso(d) : '')}
        placeholder="From date"
        className="w-40"
      />
      <DatePicker
        value={toDate}
        onChange={(d) => onTo(d ? endOfDayIso(d) : '')}
        placeholder="To date"
        className="w-40"
      />
      {hasFilters && (
        <Button
          variant="ghost"
          size="sm"
          onClick={() => {
            onStatus('')
            onMethod('')
            onCustomer('')
            onFrom('')
            onTo('')
          }}
        >
          Clear filters
        </Button>
      )}
    </div>
  )
}

function startOfDayIso(d: Date): string {
  const copy = new Date(d)
  copy.setHours(0, 0, 0, 0)
  return copy.toISOString()
}

function endOfDayIso(d: Date): string {
  const copy = new Date(d)
  copy.setHours(23, 59, 59, 999)
  return copy.toISOString()
}
