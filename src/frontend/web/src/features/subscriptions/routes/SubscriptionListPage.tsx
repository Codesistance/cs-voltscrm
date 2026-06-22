import { Link, useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { PageHeader } from '@/shared/components/PageHeader'
import { Pagination } from '@/shared/components/Pagination'
import { StatusPill } from '@/shared/components/StatusPill'
import { formatDate } from '@/shared/lib/format'
import { usePagination } from '@/shared/hooks/usePagination'
import { useAuth } from '@/features/auth/AuthContext'
import { PERMISSIONS } from '@/features/auth/permissions'
import { useSubscriptionList } from '../api/queries'
import type { SubscriptionListItem } from '../api/types'
import { SubscriptionFilters } from '../components/SubscriptionFilters'

export function SubscriptionListPage() {
  const { page, pageSize, getParam, setPage, setFilter } = usePagination()
  const status = getParam('status')
  const customerId = getParam('customerId')
  const servicePlanId = getParam('servicePlanId')
  const navigate = useNavigate()
  const { hasPermission } = useAuth()

  const { data, isLoading, isError, error, refetch } = useSubscriptionList({
    page,
    pageSize,
    status: status || undefined,
    customerId: customerId || undefined,
    servicePlanId: servicePlanId || undefined,
  })

  const columns: Column<SubscriptionListItem>[] = [
    {
      header: 'Account',
      cell: (s) => (
        <Link to={`/admin/customers/${s.customerId}`} className="font-medium hover:underline">
          {s.customerAccountNumber}
        </Link>
      ),
    },
    { header: 'Customer', cell: (s) => s.customerName },
    {
      header: 'Plan',
      cell: (s) => (
        <Link to={`/admin/service-plans/${s.servicePlanId}`} className="hover:underline">
          {s.planName}
        </Link>
      ),
    },
    { header: 'Billing', cell: (s) => <span className="text-muted-foreground">{s.billingType}</span> },
    { header: 'Start', cell: (s) => formatDate(s.startDate) },
    { header: 'End', cell: (s) => formatDate(s.endDate) },
    { header: 'Status', cell: (s) => <StatusPill domain="subscription" status={s.status} /> },
    {
      header: '',
      className: 'text-right',
      cell: (s) => (
        <Button asChild variant="ghost" size="sm">
          <Link to={`/admin/subscriptions/${s.id}`}>View</Link>
        </Button>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <PageHeader
        title="Subscriptions"
        description="Customer subscriptions to service plans — lifecycle and equipment dispatch."
        actions={
          hasPermission(PERMISSIONS.subscriptionsManage) ? (
            <Button onClick={() => navigate('/admin/subscriptions/new')}>
              <Plus className="size-4" /> New subscription
            </Button>
          ) : null
        }
      />
      <SubscriptionFilters
        status={status}
        customerId={customerId}
        servicePlanId={servicePlanId}
        onStatus={(v) => setFilter('status', v)}
        onCustomer={(v) => setFilter('customerId', v)}
        onServicePlan={(v) => setFilter('servicePlanId', v)}
      />
      <DataTable
        columns={columns}
        data={data?.items}
        rowKey={(s) => s.id}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={refetch}
        emptyTitle="No subscriptions"
        emptyDescription="Create a subscription to link a customer to a service plan."
      />
      {data && <Pagination page={page} pageSize={pageSize} total={data.total} onPageChange={setPage} />}
    </div>
  )
}
