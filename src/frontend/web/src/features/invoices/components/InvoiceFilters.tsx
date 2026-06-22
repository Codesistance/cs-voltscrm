import { Select } from '@/components/ui/select'
import { useCustomerList } from '@/features/customers/api/queries'
import { INVOICE_STATUSES } from '../api/types'

export function InvoiceFilters({
  status,
  customerId,
  onStatus,
  onCustomer,
}: {
  status: string
  customerId: string
  onStatus: (value: string) => void
  onCustomer: (value: string) => void
}) {
  const customers = useCustomerList({ page: 1, pageSize: 200 })

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Select value={status} onChange={(e) => onStatus(e.target.value)} className="w-44">
        <option value="">All statuses</option>
        {INVOICE_STATUSES.map((s) => (
          <option key={s} value={s}>
            {s}
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
    </div>
  )
}
