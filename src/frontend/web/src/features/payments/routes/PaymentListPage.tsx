import { Link, useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { Money } from '@/shared/components/Money'
import { PageHeader } from '@/shared/components/PageHeader'
import { Pagination } from '@/shared/components/Pagination'
import { StatusPill } from '@/shared/components/StatusPill'
import { formatDateTime } from '@/shared/lib/format'
import { usePagination } from '@/shared/hooks/usePagination'
import { useAuth } from '@/features/auth/AuthContext'
import { PERMISSIONS } from '@/features/auth/permissions'
import { usePaymentList } from '../api/queries'
import type { PaymentListItem } from '../api/types'
import { PaymentFilters } from '../components/PaymentFilters'

export function PaymentListPage() {
  const { page, pageSize, getParam, setPage, setFilter } = usePagination()
  const status = getParam('status')
  const method = getParam('method')
  const customerId = getParam('customerId')
  const from = getParam('from')
  const to = getParam('to')
  const navigate = useNavigate()
  const { hasPermission } = useAuth()

  const { data, isLoading, isError, error, refetch } = usePaymentList({
    page,
    pageSize,
    status: status || undefined,
    method: method || undefined,
    customerId: customerId || undefined,
    from: from || undefined,
    to: to || undefined,
  })

  const columns: Column<PaymentListItem>[] = [
    {
      header: 'Customer',
      cell: (p) => (
        <Link to={`/admin/customers/${p.customerId}`} className="font-medium hover:underline">
          {p.customerName}
        </Link>
      ),
    },
    {
      header: 'Amount',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (p) => <Money value={p.amount} />,
    },
    {
      header: 'Net',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (p) => <Money value={p.netAmount} />,
    },
    { header: 'Method', cell: (p) => <span className="text-muted-foreground">{p.method}</span> },
    { header: 'Channel', cell: (p) => <span className="text-muted-foreground">{p.channel}</span> },
    { header: 'Date', cell: (p) => formatDateTime(p.paymentDate) },
    {
      header: 'Reference',
      cell: (p) => <span className="text-muted-foreground">{p.platformReference ?? '—'}</span>,
    },
    { header: 'Status', cell: (p) => <StatusPill domain="payment" status={p.status} /> },
    {
      header: '',
      className: 'text-right',
      cell: (p) => (
        <Button asChild variant="ghost" size="sm">
          <Link to={`/admin/payments/${p.id}`}>View</Link>
        </Button>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <PageHeader
        title="Payments"
        description="Record and track customer payments — cash and other methods."
        actions={
          hasPermission(PERMISSIONS.paymentsRecord) ? (
            <Button onClick={() => navigate('/admin/payments/new')}>
              <Plus className="size-4" /> Record payment
            </Button>
          ) : null
        }
      />
      <PaymentFilters
        status={status}
        method={method}
        customerId={customerId}
        from={from}
        to={to}
        onStatus={(v) => setFilter('status', v)}
        onMethod={(v) => setFilter('method', v)}
        onCustomer={(v) => setFilter('customerId', v)}
        onFrom={(v) => setFilter('from', v)}
        onTo={(v) => setFilter('to', v)}
      />
      <DataTable
        columns={columns}
        data={data?.items}
        rowKey={(p) => p.id}
        isLoading={isLoading}
        isError={isError}
        error={error}
        onRetry={refetch}
        emptyTitle="No payments"
        emptyDescription="Record a payment to get started."
      />
      {data && <Pagination page={page} pageSize={pageSize} total={data.total} onPageChange={setPage} />}
    </div>
  )
}
