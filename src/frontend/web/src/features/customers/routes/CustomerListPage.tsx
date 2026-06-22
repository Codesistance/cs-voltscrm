import { Link, useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { PageHeader } from '@/shared/components/PageHeader'
import { Pagination } from '@/shared/components/Pagination'
import { StatusPill } from '@/shared/components/StatusPill'
import { usePagination } from '@/shared/hooks/usePagination'
import { useAuth } from '@/features/auth/AuthContext'
import { PERMISSIONS } from '@/features/auth/permissions'
import { useCustomerList } from '../api/queries'
import type { CustomerListItem } from '../api/types'
import { CustomerFilters } from '../components/CustomerFilters'

export function CustomerListPage() {
  const { page, pageSize, q, getParam, setPage, setSearch, setFilter } = usePagination()
  const status = getParam('status')
  const navigate = useNavigate()
  const { hasPermission } = useAuth()

  const { data, isLoading, isError, error, refetch } = useCustomerList({
    page,
    pageSize,
    q: q || undefined,
    status: status || undefined,
  })

  const columns: Column<CustomerListItem>[] = [
    { header: 'Account', cell: (c) => <span className="font-medium">{c.accountNumber}</span> },
    { header: 'Name', cell: (c) => c.fullName },
    { header: 'Phone', cell: (c) => <span className="text-muted-foreground">{c.phone}</span> },
    { header: 'City', cell: (c) => c.city },
    { header: 'Status', cell: (c) => <StatusPill domain="customer" status={c.status} /> },
    {
      header: '',
      className: 'text-right',
      cell: (c) => (
        <Button asChild variant="ghost" size="sm">
          <Link to={`/admin/customers/${c.id}`}>View</Link>
        </Button>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <PageHeader
        title="Customers"
        description="People and organisations that subscribe to your service plans."
        actions={
          hasPermission(PERMISSIONS.customersManage) ? (
            <Button onClick={() => navigate('/admin/customers/new')}>
              <Plus className="size-4" /> New customer
            </Button>
          ) : null
        }
      />
      <CustomerFilters
        q={q}
        status={status}
        onSearch={setSearch}
        onStatus={(v) => setFilter('status', v)}
      />
      <DataTable
        columns={columns}
        data={data?.items}
        rowKey={(c) => c.id}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={refetch}
        emptyTitle="No customers"
        emptyDescription="Create your first customer to get started."
      />
      {data && <Pagination page={page} pageSize={pageSize} total={data.total} onPageChange={setPage} />}
    </div>
  )
}
