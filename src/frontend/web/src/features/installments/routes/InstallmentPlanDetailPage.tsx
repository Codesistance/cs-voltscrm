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
import { useInstallmentPlan } from '../api/queries'
import type { Installment } from '../api/types'

export function InstallmentPlanDetailPage() {
  const { id = '' } = useParams()
  const { data: plan, isLoading, isError, error, refetch } = useInstallmentPlan(id)

  if (isLoading) return <LoadingState label="Loading installment plan…" />
  if (isError || !plan) return <ErrorState error={error} onRetry={refetch} />

  const scheduleColumns: Column<Installment>[] = [
    { header: 'Due date', cell: (s) => formatDate(s.dueDate) },
    {
      header: 'Amount',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (s) => <Money value={s.amount} />,
    },
    { header: 'Paid', cell: (s) => (s.paidDate ? formatDate(s.paidDate) : '—') },
    { header: 'Status', cell: (s) => <StatusPill domain="installment" status={s.status} /> },
  ]

  return (
    <div className="space-y-6">
      <Button asChild variant="ghost" size="sm" className="-ml-2 w-fit">
        <Link to="/admin/installments">
          <ArrowLeft className="size-4" /> Back to installment plans
        </Link>
      </Button>

      <PageHeader title={`Installment plan`} description={`${plan.customerName} · ${plan.customerAccountNumber}`} />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Total</CardTitle>
          </CardHeader>
          <CardContent>
            <Money value={plan.totalAmount} className="text-base font-medium" />
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Deposit</CardTitle>
          </CardHeader>
          <CardContent>
            <Money value={plan.depositAmount} className="text-base font-medium" />
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Remaining</CardTitle>
          </CardHeader>
          <CardContent>
            <Money value={plan.remainingAmount} className="text-base font-medium" />
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Start date</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">{formatDate(plan.startDate)}</CardContent>
        </Card>
      </div>

      <div className="space-y-3">
        <h2 className="text-lg font-semibold">Repayment schedule</h2>
        <DataTable
          columns={scheduleColumns}
          data={plan.installments}
          rowKey={(s) => s.id}
          emptyTitle="No schedule items"
          emptyDescription="No installment rows are available for this plan."
        />
        <p className="text-sm text-muted-foreground">Last updated {formatDateTime(plan.updatedAt)}</p>
      </div>
    </div>
  )
}
