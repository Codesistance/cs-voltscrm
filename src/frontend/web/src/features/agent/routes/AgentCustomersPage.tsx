import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { PageHeader } from '@/shared/components/PageHeader'
import { Pagination } from '@/shared/components/Pagination'
import { StatusPill } from '@/shared/components/StatusPill'
import { usePagination } from '@/shared/hooks/usePagination'
import { useCustomerList } from '@/features/customers/api/queries'
import type { CustomerListItem } from '@/features/customers/api/types'

export function AgentCustomersPage() {
  const { page, pageSize, q, getParam, setPage, setSearch, setFilter } = usePagination()
  const status = getParam('status')
  const [term, setTerm] = useState(q)
  const { data, isLoading, isError, error, refetch } = useCustomerList({
    page,
    pageSize,
    q: q || undefined,
    status: status || undefined,
  })

  const columns: Column<CustomerListItem>[] = [
    { header: 'Account', cell: (c) => <span className="font-medium">{c.accountNumber}</span> },
    { header: 'Name', cell: (c) => c.fullName },
    { header: 'Phone', cell: (c) => c.phone },
    { header: 'City', cell: (c) => c.city },
    { header: 'Status', cell: (c) => <StatusPill domain="customer" status={c.status} /> },
    {
      header: '',
      className: 'text-right',
      cell: (c) => (
        <Button asChild variant="ghost" size="sm">
          <Link to={`/admin/customers/${c.id}`}>Open</Link>
        </Button>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <PageHeader title="My customers" description="Customers assigned to your service route." />
      <div className="flex flex-wrap items-center gap-2">
        <form
          className="flex items-center gap-2"
          onSubmit={(e) => {
            e.preventDefault()
            setSearch(term)
          }}
        >
          <Input
            value={term}
            onChange={(e) => setTerm(e.target.value)}
            className="w-72"
            placeholder="Search by customer, account or phone…"
          />
          <Button type="submit" variant="secondary">
            Search
          </Button>
        </form>
        <select
          value={status}
          onChange={(e) => setFilter('status', e.target.value)}
          className="h-9 w-44 rounded-md border bg-background px-3 text-sm"
        >
          <option value="">All statuses</option>
          <option value="Active">Active</option>
          <option value="Suspended">Suspended</option>
          <option value="Disconnected">Disconnected</option>
        </select>
      </div>
      <DataTable
        columns={columns}
        data={data?.items}
        rowKey={(c) => c.id}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={refetch}
        emptyTitle="No customers"
      />
      {data && <Pagination page={page} pageSize={pageSize} total={data.total} onPageChange={setPage} />}
    </div>
  )
}
