import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { Archive, ArchiveRestore, ArrowLeft, Pencil } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { Money } from '@/shared/components/Money'
import { PageHeader } from '@/shared/components/PageHeader'
import { StatusPill } from '@/shared/components/StatusPill'
import { useAuth } from '@/features/auth/AuthContext'
import { PERMISSIONS } from '@/features/auth/permissions'
import { useArchiveServicePlan, useRestoreServicePlan, useServicePlan } from '../api/queries'
import type { ServicePlanLineItem } from '../api/types'

export function ServicePlanDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const canManage = useAuth().hasPermission(PERMISSIONS.servicePlansManage)

  const { data: plan, isLoading, isError, error, refetch } = useServicePlan(id)
  const archiveMut = useArchiveServicePlan()
  const restoreMut = useRestoreServicePlan()
  const [confirmArchive, setConfirmArchive] = useState(false)

  if (isLoading) return <LoadingState label="Loading plan…" />
  if (isError || !plan) return <ErrorState error={error} onRetry={refetch} />

  const isArchived = plan.status === 'Archived'

  const columns: Column<ServicePlanLineItem>[] = [
    {
      header: 'Item',
      cell: (l) => (
        <span>
          <span className="font-medium">{l.inventoryItemCode}</span>{' '}
          <span className="text-muted-foreground">{l.inventoryItemName}</span>
        </span>
      ),
    },
    {
      header: 'Qty',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (l) => <span className="tabular-nums">{l.quantity}</span>,
    },
    { header: 'Role', cell: (l) => l.role },
  ]

  return (
    <div className="space-y-6">
      <Button asChild variant="ghost" size="sm" className="-ml-2 w-fit">
        <Link to="/admin/service-plans">
          <ArrowLeft className="size-4" /> Back to plans
        </Link>
      </Button>

      <PageHeader
        title={plan.name}
        description={`${plan.planCode} · ${plan.billingType} · ${plan.billingCycle}`}
        actions={
          canManage ? (
            <>
              <Button variant="outline" onClick={() => navigate(`/admin/service-plans/${id}/edit`)}>
                <Pencil className="size-4" /> Edit
              </Button>
              {isArchived ? (
                <Button
                  variant="outline"
                  disabled={restoreMut.isPending}
                  onClick={async () => {
                    await restoreMut.mutateAsync(id)
                    toast.success("Plan's back and active.")
                  }}
                >
                  <ArchiveRestore className="size-4" /> Restore
                </Button>
              ) : (
                <Button variant="outline" onClick={() => setConfirmArchive(true)}>
                  <Archive className="size-4" /> Archive
                </Button>
              )}
            </>
          ) : null
        }
      />

      <div className="flex flex-wrap items-center gap-3">
        <StatusPill domain="plan" status={plan.status} />
        <Money value={plan.basePrice} className="text-lg" />
        <span className="text-sm text-muted-foreground">base price</span>
      </div>

      {plan.description && <p className="text-sm text-muted-foreground">{plan.description}</p>}

      <div className="space-y-3">
        <h2 className="text-lg font-semibold">Included items</h2>
        <DataTable columns={columns} data={plan.lineItems} rowKey={(l) => l.id} emptyTitle="No items in this plan" />
      </div>

      <ConfirmDialog
        open={confirmArchive}
        onOpenChange={setConfirmArchive}
        title="Archive plan?"
        description={`"${plan.name}" will be hidden from new subscriptions.`}
        confirmText="Archive"
        destructive
        loading={archiveMut.isPending}
        onConfirm={async () => {
          await archiveMut.mutateAsync(id)
          toast.success('Plan tucked away in the archive.')
          setConfirmArchive(false)
        }}
      />
    </div>
  )
}
