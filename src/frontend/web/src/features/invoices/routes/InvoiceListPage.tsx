import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { Money } from '@/shared/components/Money'
import { PageHeader } from '@/shared/components/PageHeader'
import { Pagination } from '@/shared/components/Pagination'
import { StatusPill } from '@/shared/components/StatusPill'
import { formatDate } from '@/shared/lib/format'
import { usePagination } from '@/shared/hooks/usePagination'
import { useInvoiceList } from '../api/queries'
import type { InvoiceListItem } from '../api/types'
import { formatInvoiceLabel } from '../api/types'
import { GenerateInvoicesButton } from '../components/GenerateInvoicesButton'
import { InvoiceFilters } from '../components/InvoiceFilters'

export function InvoiceListPage() {
  const { page, pageSize, getParam, setPage, setFilter } = usePagination()
  const status = getParam('status')
  const customerId = getParam('customerId')

  const { data, isLoading, isError, error, refetch } = useInvoiceList({
    page,
    pageSize,
    status: status || undefined,
    customerId: customerId || undefined,
  })

  const columns: Column<InvoiceListItem>[] = [
    { header: 'Period', cell: (i) => <span className="font-medium">{formatInvoiceLabel(i)}</span> },
    {
      header: 'Customer',
      cell: (i) => (
        <Link to={`/admin/customers/${i.customerId}`} className="hover:underline">
          {i.customerName}
        </Link>
      ),
    },
    { header: 'Due', cell: (i) => formatDate(i.dueDate) },
    {
      header: 'Due amount',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (i) => <Money value={i.amountDue} />,
    },
    {
      header: 'Balance',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (i) => <Money value={i.balance} />,
    },
    { header: 'Status', cell: (i) => <StatusPill domain="invoice" status={i.status} /> },
    {
      header: '',
      className: 'text-right',
      cell: (i) => (
        <Button asChild variant="ghost" size="sm">
          <Link to={`/admin/invoices/${i.id}`}>View</Link>
        </Button>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <PageHeader
        title="Invoices"
        description="Track issued invoices, balances and payment status."
        actions={<GenerateInvoicesButton />}
      />
      <InvoiceFilters
        status={status}
        customerId={customerId}
        onStatus={(value) => setFilter('status', value)}
        onCustomer={(value) => setFilter('customerId', value)}
      />
      <DataTable
        columns={columns}
        data={data?.items}
        rowKey={(i) => i.id}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={refetch}
        emptyTitle="No invoices"
        emptyDescription="Generate invoices to populate this list."
      />
      {data && <Pagination page={page} pageSize={pageSize} total={data.total} onPageChange={setPage} />}
    </div>
  )
}
