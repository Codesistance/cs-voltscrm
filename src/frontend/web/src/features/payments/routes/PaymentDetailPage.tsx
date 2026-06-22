import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { Money } from '@/shared/components/Money'
import { PageHeader } from '@/shared/components/PageHeader'
import { StatusPill } from '@/shared/components/StatusPill'
import { formatDateTime } from '@/shared/lib/format'
import { config } from '@/app/config'
import { useAuth } from '@/features/auth/AuthContext'
import { PERMISSIONS } from '@/features/auth/permissions'
import { usePayment, usePaymentLifecycle } from '../api/queries'
import type { LifecycleAction } from '../api/types'
import { PaymentAllocationsPanel } from '../components/PaymentAllocationsPanel'

const STATUS_ACTIONS: Record<
  string,
  { action: LifecycleAction; label: string; destructive?: boolean; confirm?: boolean }[]
> = {
  Pending: [
    { action: 'complete', label: 'Complete' },
    { action: 'fail', label: 'Fail', destructive: true, confirm: true },
  ],
  Completed: [{ action: 'reverse', label: 'Reverse', destructive: true, confirm: true }],
}

export function PaymentDetailPage() {
  const { id = '' } = useParams()
  const canRecord = useAuth().hasPermission(PERMISSIONS.paymentsRecord)

  const { data: payment, isLoading, isError, error, refetch } = usePayment(id)
  const lifecycleMut = usePaymentLifecycle(id)

  const [pendingAction, setPendingAction] = useState<{
    action: LifecycleAction
    label: string
  } | null>(null)

  if (isLoading) return <LoadingState label="Loading payment…" />
  if (isError || !payment) return <ErrorState error={error} onRetry={refetch} />

  const actions = STATUS_ACTIONS[payment.status] ?? []

  const runLifecycle = (action: LifecycleAction) =>
    lifecycleMut.mutate(action, {
      onSuccess: () => toast.success('Payment updated.'),
    })

  const handleAction = (a: (typeof actions)[number]) => {
    if (a.confirm) {
      setPendingAction({ action: a.action, label: a.label })
    } else {
      runLifecycle(a.action)
    }
  }

  return (
    <div className="space-y-6">
      <Button asChild variant="ghost" size="sm" className="-ml-2 w-fit">
        <Link to="/admin/payments">
          <ArrowLeft className="size-4" /> Back to payments
        </Link>
      </Button>

      <PageHeader
        title={payment.customerName}
        description={`${payment.customerAccountNumber} · ${formatDateTime(payment.paymentDate)}`}
        actions={
          canRecord ? (
            <>
              {actions.map((a) => (
                <Button
                  key={a.action}
                  variant={a.destructive ? 'destructive' : 'outline'}
                  disabled={lifecycleMut.isPending}
                  onClick={() => handleAction(a)}
                >
                  {a.label}
                </Button>
              ))}
            </>
          ) : null
        }
      />

      <div className="flex flex-wrap items-center gap-3">
        <StatusPill domain="payment" status={payment.status} />
        <span className="text-sm text-muted-foreground">{payment.method}</span>
        <span className="text-sm text-muted-foreground">{payment.channel}</span>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Customer</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">
            <Link to={`/admin/customers/${payment.customerId}`} className="font-medium hover:underline">
              {payment.customerName}
            </Link>
            <p className="text-muted-foreground">{payment.customerAccountNumber}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Amount</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">
            <Money value={payment.amount} className="font-medium text-base" />
            {payment.discountApplied > 0 && (
              <p className="mt-1 text-muted-foreground">
                Discount:{' '}
                <Money value={{ amount: payment.discountApplied, currency: payment.amount.currency }} />
              </p>
            )}
            <p className="mt-1 text-muted-foreground">
              Net: <Money value={payment.netAmount} />
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Platform</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">
            <p>{payment.platformProvider ?? '—'}</p>
            <p className="text-muted-foreground">{payment.platformReference ?? 'No reference'}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Payment date</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">{formatDateTime(payment.paymentDate)}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Created</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">{formatDateTime(payment.createdAt)}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Currency</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">{payment.amount.currency ?? config.defaultCurrency}</CardContent>
        </Card>
      </div>

      <PaymentAllocationsPanel allocations={payment.allocations} />

      <ConfirmDialog
        open={pendingAction !== null}
        onOpenChange={(open) => !open && setPendingAction(null)}
        title={`${pendingAction?.label ?? 'Confirm'} payment?`}
        description={
          pendingAction
            ? `This payment will be marked as ${pendingAction.label.toLowerCase()}d.`
            : ''
        }
        confirmText={pendingAction?.label ?? 'Confirm'}
        destructive={pendingAction?.action === 'fail' || pendingAction?.action === 'reverse'}
        loading={lifecycleMut.isPending}
        onConfirm={() => {
          if (!pendingAction) return
          setPendingAction(null)
          runLifecycle(pendingAction.action)
        }}
      />
    </div>
  )
}
