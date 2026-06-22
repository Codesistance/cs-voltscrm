import { Link, useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { Money } from '@/shared/components/Money'
import { PageHeader } from '@/shared/components/PageHeader'
import { Pagination } from '@/shared/components/Pagination'
import { StatusPill } from '@/shared/components/StatusPill'
import { usePagination } from '@/shared/hooks/usePagination'
import { useAuth } from '@/features/auth/AuthContext'
import { PERMISSIONS } from '@/features/auth/permissions'
import { useServicePlanList } from '../api/queries'
import type { ServicePlanListItem } from '../api/types'
import { ServicePlanFilters } from '../components/ServicePlanFilters'

export function ServicePlanListPage() {
  const { page, pageSize, q, getParam, setPage, setSearch, setFilter } = usePagination()
  const status = getParam('status')
  const navigate = useNavigate()
  const { hasPermission } = useAuth()

  const { data, isLoading, isError, error, refetch } = useServicePlanList({
    page,
    pageSize,
    q: q || undefined,
    status: status || undefined,
  })

  const columns: Column<ServicePlanListItem>[] = [
    { header: 'Code', cell: (p) => <span className="font-medium">{p.planCode}</span> },
    { header: 'Name', cell: (p) => p.name },
    {
      header: 'Billing',
      cell: (p) => (
        <span className="text-muted-foreground">
          {p.billingType} · {p.billingCycle}
        </span>
      ),
    },
    {
      header: 'Items',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (p) => <span className="tabular-nums">{p.lineItemCount}</span>,
    },
    {
      header: 'Base price',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (p) => <Money value={p.basePrice} />,
    },
    { header: 'Status', cell: (p) => <StatusPill domain="plan" status={p.status} /> },
    {
      header: '',
      className: 'text-right',
      cell: (p) => (
        <Button asChild variant="ghost" size="sm">
          <Link to={`/admin/service-plans/${p.id}`}>View</Link>
        </Button>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <PageHeader
        title="Service Plans"
        description="Bundles of equipment and services that customers subscribe to."
        actions={
          hasPermission(PERMISSIONS.servicePlansManage) ? (
            <Button onClick={() => navigate('/admin/service-plans/new')}>
              <Plus className="size-4" /> New plan
            </Button>
          ) : null
        }
      />
      <ServicePlanFilters
        q={q}
        status={status}
        onSearch={setSearch}
        onStatus={(v) => setFilter('status', v)}
      />
      <DataTable
        columns={columns}
        data={data?.items}
        rowKey={(p) => p.id}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={refetch}
        emptyTitle="No service plans"
        emptyDescription="Create your first plan to get started."
      />
      {data && <Pagination page={page} pageSize={pageSize} total={data.total} onPageChange={setPage} />}
    </div>
  )
}
