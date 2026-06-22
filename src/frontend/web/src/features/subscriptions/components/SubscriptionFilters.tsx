import { Button } from '@/components/ui/button'
import { Select } from '@/components/ui/select'
import { useCustomerList } from '@/features/customers/api/queries'
import { useServicePlanList } from '@/features/serviceplans/api/queries'
import { SUBSCRIPTION_STATUSES } from '../api/types'

export function SubscriptionFilters({
  status,
  customerId,
  servicePlanId,
  onStatus,
  onCustomer,
  onServicePlan,
}: {
  status: string
  customerId: string
  servicePlanId: string
  onStatus: (value: string) => void
  onCustomer: (value: string) => void
  onServicePlan: (value: string) => void
}) {
  const customers = useCustomerList({ page: 1, pageSize: 200 })
  const plans = useServicePlanList({ page: 1, pageSize: 200 })

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Select value={status} onChange={(e) => onStatus(e.target.value)} className="w-40">
        <option value="">All statuses</option>
        {SUBSCRIPTION_STATUSES.map((s) => (
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
      <Select value={servicePlanId} onChange={(e) => onServicePlan(e.target.value)} className="w-56">
        <option value="">All plans</option>
        {(plans.data?.items ?? []).map((p) => (
          <option key={p.id} value={p.id}>
            {p.planCode} · {p.name}
          </option>
        ))}
      </Select>
      {(status || customerId || servicePlanId) && (
        <Button
          variant="ghost"
          size="sm"
          onClick={() => {
            onStatus('')
            onCustomer('')
            onServicePlan('')
          }}
        >
          Clear filters
        </Button>
      )}
    </div>
  )
}
