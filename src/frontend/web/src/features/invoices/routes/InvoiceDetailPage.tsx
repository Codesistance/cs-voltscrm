import { Link, useParams } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { Money } from '@/shared/components/Money'
import { PageHeader } from '@/shared/components/PageHeader'
import { StatusPill } from '@/shared/components/StatusPill'
import { formatDate, formatDateTime } from '@/shared/lib/format'
import { config } from '@/app/config'
import { useInvoice } from '../api/queries'
import type { InvoiceLineItem } from '../api/types'
import { formatInvoicePeriod } from '../api/types'

function lineAmount(line: InvoiceLineItem) {
  const amount = line.isDiscount ? -Math.abs(line.amount) : line.amount
  return { amount, currency: config.defaultCurrency }
}

export function InvoiceDetailPage() {
  const { id = '' } = useParams()
  const { data: invoice, isLoading, isError, error, refetch } = useInvoice(id)

  if (isLoading) return <LoadingState label="Loading invoice…" />
  if (isError || !invoice) return <ErrorState error={error} onRetry={refetch} />

  const period = formatInvoicePeriod(invoice.periodYear, invoice.periodMonth)

  const lineColumns: Column<InvoiceLineItem>[] = [
    { header: 'Description', cell: (l) => <span className="font-medium">{l.description}</span> },
    {
      header: 'Type',
      cell: (l) => <span className="text-muted-foreground">{l.isDiscount ? 'Discount' : 'Charge'}</span>,
    },
    {
      header: 'Amount',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (l) => <Money value={lineAmount(l)} />,
    },
  ]

  return (
    <div className="space-y-6">
      <Button asChild variant="ghost" size="sm" className="-ml-2 w-fit">
        <Link to="/admin/invoices">
          <ArrowLeft className="size-4" /> Back to invoices
        </Link>
      </Button>

      <PageHeader
        title={`Invoice ${period}`}
        description={`${invoice.customerName} · ${invoice.customerAccountNumber}`}
      />

      <div className="flex items-center gap-3">
        <StatusPill domain="invoice" status={invoice.status} />
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Period</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">{period}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Due</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">{formatDate(invoice.dueDate)}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Amount due</CardTitle>
          </CardHeader>
          <CardContent>
            <Money value={invoice.amountDue} className="text-base font-medium" />
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Balance</CardTitle>
          </CardHeader>
          <CardContent>
            <Money value={invoice.balance} className="text-base font-medium" />
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-medium text-muted-foreground">Totals</CardTitle>
        </CardHeader>
        <CardContent className="space-y-1 text-sm">
          <p>
            Gross: <Money value={invoice.grossAmount} />
          </p>
          <p>
            Discount:{' '}
            <Money value={{ ...invoice.discountAmount, amount: -Math.abs(invoice.discountAmount.amount) }} />
          </p>
          <p>
            Paid: <Money value={invoice.amountPaid} />
          </p>
          <p>
            Outstanding: <Money value={invoice.balance} />
          </p>
          <p className="text-muted-foreground">Last updated: {formatDateTime(invoice.updatedAt)}</p>
        </CardContent>
      </Card>

      <div className="space-y-3">
        <h2 className="text-lg font-semibold">Line items</h2>
        <DataTable
          columns={lineColumns}
          data={invoice.lineItems}
          rowKey={(l) => l.id}
          emptyTitle="No line items"
          emptyDescription="This invoice does not have itemized lines."
        />
      </div>
    </div>
  )
}
