import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { Money } from '@/shared/components/Money'
import { PageHeader } from '@/shared/components/PageHeader'
import { StatusPill } from '@/shared/components/StatusPill'
import { formatDateTime } from '@/shared/lib/format'
import { usePortalPayments } from '../api/queries'
import type { PortalPayment } from '../api/types'

export function PortalPaymentsPage() {
  const { data, isLoading, isError, error, refetch } = usePortalPayments()
  const columns: Column<PortalPayment>[] = [
    { header: 'Date', cell: (p) => formatDateTime(p.paymentDate) },
    {
      header: 'Amount',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (p) => <Money value={p.netAmount} />,
    },
    { header: 'Method', cell: (p) => p.method },
    { header: 'Reference', cell: (p) => p.platformReference ?? '—' },
    { header: 'Status', cell: (p) => <StatusPill domain="payment" status={p.status} /> },
  ]

  return (
    <div className="space-y-6">
      <PageHeader title="Payments" description="Payment records posted to your account." />
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Payment history</CardTitle>
        </CardHeader>
        <CardContent>
          <DataTable
            columns={columns}
            data={data}
            rowKey={(p) => p.id}
            isLoading={isLoading}
            isError={isError}
            error={error}
            onRetry={refetch}
            emptyTitle="No payments"
          />
        </CardContent>
      </Card>
    </div>
  )
}
