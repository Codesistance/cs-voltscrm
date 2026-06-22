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
import { formatDate, formatDateTime } from '@/shared/lib/format'
import { useAuth } from '@/features/auth/AuthContext'
import { PERMISSIONS } from '@/features/auth/permissions'
import { useSubscription, useSubscriptionLifecycle } from '../api/queries'
import type { LifecycleAction } from '../api/types'
import { DeployedItemsPanel } from '../components/DeployedItemsPanel'

const STATUS_ACTIONS: Record<
  string,
  { action: LifecycleAction; label: string; destructive?: boolean; confirm?: boolean }[]
> = {
  Pending: [{ action: 'activate', label: 'Activate' }],
  Active: [
    { action: 'suspend', label: 'Suspend', confirm: true },
    { action: 'terminate', label: 'Terminate', destructive: true, confirm: true },
  ],
  Suspended: [
    { action: 'resume', label: 'Resume' },
    { action: 'terminate', label: 'Terminate', destructive: true, confirm: true },
  ],
}

export function SubscriptionDetailPage() {
  const { id = '' } = useParams()
  const canManage = useAuth().hasPermission(PERMISSIONS.subscriptionsManage)

  const { data: sub, isLoading, isError, error, refetch } = useSubscription(id)
  const lifecycleMut = useSubscriptionLifecycle(id)

  const [pendingAction, setPendingAction] = useState<{
    action: LifecycleAction
    label: string
  } | null>(null)

  if (isLoading) return <LoadingState label="Loading subscription…" />
  if (isError || !sub) return <ErrorState error={error} onRetry={refetch} />

  const actions = STATUS_ACTIONS[sub.status] ?? []

  const runLifecycle = (action: LifecycleAction) =>
    lifecycleMut.mutate(action, {
      onSuccess: () => toast.success('Subscription updated.'),
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
        <Link to="/admin/subscriptions">
          <ArrowLeft className="size-4" /> Back to subscriptions
        </Link>
      </Button>

      <PageHeader
        title={sub.customerName}
        description={`${sub.customerAccountNumber} · ${sub.planCode} — ${sub.planName}`}
        actions={
          canManage ? (
            <>
              {sub.status === 'Pending' && (
                <Button asChild variant="outline">
                  <Link to={`/admin/subscriptions/${sub.id}/edit`}>Edit</Link>
                </Button>
              )}
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
        <StatusPill domain="subscription" status={sub.status} />
        <span className="text-sm text-muted-foreground">{sub.billingType}</span>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Customer</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">
            <Link to={`/admin/customers/${sub.customerId}`} className="font-medium hover:underline">
              {sub.customerName}
            </Link>
            <p className="text-muted-foreground">{sub.customerAccountNumber}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Service plan</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">
            <Link to={`/admin/service-plans/${sub.servicePlanId}`} className="font-medium hover:underline">
              {sub.planName}
            </Link>
            <p className="text-muted-foreground">{sub.planCode}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Effective price</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">
            <Money value={sub.effectivePrice} className="font-medium" />
            {sub.negotiatedPrice && (
              <p className="mt-1 text-muted-foreground">
                Negotiated: <Money value={sub.negotiatedPrice} />
              </p>
            )}
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Start date</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">{formatDate(sub.startDate)}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">End date</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">{formatDate(sub.endDate)}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Created</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">{formatDateTime(sub.createdAt)}</CardContent>
        </Card>
      </div>

      <DeployedItemsPanel subscription={sub} canManage={canManage} />

      <ConfirmDialog
        open={pendingAction !== null}
        onOpenChange={(open) => !open && setPendingAction(null)}
        title={`${pendingAction?.label ?? 'Confirm'} subscription?`}
        description={
          pendingAction
            ? `This subscription will be ${pendingAction.label.toLowerCase()}d.`
            : ''
        }
        confirmText={pendingAction?.label ?? 'Confirm'}
        destructive={pendingAction?.action === 'terminate'}
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
