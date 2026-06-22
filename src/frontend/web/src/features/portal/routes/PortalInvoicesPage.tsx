import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { Money } from '@/shared/components/Money'
import { PageHeader } from '@/shared/components/PageHeader'
import { StatusPill } from '@/shared/components/StatusPill'
import { formatDate } from '@/shared/lib/format'
import { hasOutstandingBalance, PayInvoiceDialog } from '../components/PayInvoiceDialog'
import { usePortalInvoices } from '../api/queries'
import type { PortalInvoice } from '../api/types'
import { formatPortalInvoicePeriod } from '../api/types'

export function PortalInvoicesPage() {
  const { data, isLoading, isError, error, refetch } = usePortalInvoices()
  const [payingInvoice, setPayingInvoice] = useState<PortalInvoice | null>(null)

  const columns: Column<PortalInvoice>[] = [
    {
      header: 'Period',
      cell: (i) => <span className="font-medium">{formatPortalInvoicePeriod(i.periodYear, i.periodMonth)}</span>,
    },
    { header: 'Due', cell: (i) => formatDate(i.dueDate) },
    {
      header: 'Amount due',
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
      cell: (i) =>
        hasOutstandingBalance(i.balance) ? (
          <Button size="sm" variant="outline" onClick={() => setPayingInvoice(i)}>
            Pay
          </Button>
        ) : null,
    },
  ]

  return (
    <div className="space-y-6">
      <PageHeader title="Invoices" description="View your invoice history and balances." />
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Invoice history</CardTitle>
        </CardHeader>
        <CardContent>
          <DataTable
            columns={columns}
            data={data}
            rowKey={(i) => i.id}
            isLoading={isLoading}
            isError={isError}
            error={error}
            onRetry={refetch}
            emptyTitle="No invoices"
          />
        </CardContent>
      </Card>

      <PayInvoiceDialog
        open={!!payingInvoice}
        onOpenChange={(open) => !open && setPayingInvoice(null)}
        invoice={payingInvoice ?? undefined}
      />
    </div>
  )
}
