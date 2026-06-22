import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { ArrowLeft, Pencil, Plus } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { PageHeader } from '@/shared/components/PageHeader'
import { StatusPill } from '@/shared/components/StatusPill'
import { useAuth } from '@/features/auth/AuthContext'
import { PERMISSIONS } from '@/features/auth/permissions'
import {
  useAddServiceLocation,
  useCustomer,
  useCustomerStatus,
  useDeactivateServiceLocation,
  type StatusAction,
} from '../api/queries'
import type { ServiceLocation } from '../api/types'
import { PrepaidBalancePanel } from '../components/PrepaidBalancePanel'
import { ServiceLocationForm } from '../components/ServiceLocationForm'

const STATUS_ACTIONS: Record<string, { action: StatusAction; label: string; destructive?: boolean }[]> = {
  Active: [
    { action: 'suspend', label: 'Suspend' },
    { action: 'disconnect', label: 'Disconnect', destructive: true },
  ],
  Suspended: [
    { action: 'reactivate', label: 'Reactivate' },
    { action: 'disconnect', label: 'Disconnect', destructive: true },
  ],
  Disconnected: [{ action: 'reactivate', label: 'Reactivate' }],
}

export function CustomerDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const canManage = useAuth().hasPermission(PERMISSIONS.customersManage)

  const { data: customer, isLoading, isError, error, refetch } = useCustomer(id)
  const statusMut = useCustomerStatus(id)
  const addLocMut = useAddServiceLocation(id)
  const deactivateLocMut = useDeactivateServiceLocation(id)

  const [addOpen, setAddOpen] = useState(false)
  const [confirmDisconnect, setConfirmDisconnect] = useState(false)
  const [deactivateTarget, setDeactivateTarget] = useState<ServiceLocation | null>(null)

  if (isLoading) return <LoadingState label="Loading customer…" />
  if (isError || !customer) return <ErrorState error={error} onRetry={refetch} />

  const runStatus = (action: StatusAction) =>
    statusMut.mutate(action, { onSuccess: () => toast.success('Got it — customer details saved.') })

  const actions = STATUS_ACTIONS[customer.status] ?? []

  const locationColumns: Column<ServiceLocation>[] = [
    { header: 'Label', cell: (l) => <span className="font-medium">{l.label}</span> },
    {
      header: 'Address',
      cell: (l) => (
        <span className="text-muted-foreground">
          {[l.location.address.city, l.location.address.country].filter(Boolean).join(', ')}
        </span>
      ),
    },
    { header: 'Active', cell: (l) => (l.isActive ? <Badge variant="success">Active</Badge> : <Badge variant="muted">Inactive</Badge>) },
    {
      header: '',
      className: 'text-right',
      cell: (l) =>
        canManage && l.isActive ? (
          <Button variant="ghost" size="sm" onClick={() => setDeactivateTarget(l)}>
            Deactivate
          </Button>
        ) : null,
    },
  ]

  return (
    <div className="space-y-6">
      <Button asChild variant="ghost" size="sm" className="-ml-2 w-fit">
        <Link to="/admin/customers">
          <ArrowLeft className="size-4" /> Back to customers
        </Link>
      </Button>

      <PageHeader
        title={customer.personalInfo.fullName}
        description={customer.accountNumber}
        actions={
          canManage ? (
            <>
              <Button variant="outline" onClick={() => navigate(`/admin/customers/${id}/edit`)}>
                <Pencil className="size-4" /> Edit
              </Button>
              {actions.map((a) => (
                <Button
                  key={a.action}
                  variant="outline"
                  disabled={statusMut.isPending}
                  onClick={() => (a.destructive ? setConfirmDisconnect(true) : runStatus(a.action))}
                >
                  {a.label}
                </Button>
              ))}
            </>
          ) : null
        }
      />

      <div className="flex flex-wrap items-center gap-3">
        <StatusPill domain="customer" status={customer.status} />
        <span className="text-sm text-muted-foreground">{customer.personalInfo.phone}</span>
        {customer.personalInfo.email && (
          <span className="text-sm text-muted-foreground">{customer.personalInfo.email}</span>
        )}
      </div>

      <Tabs defaultValue="overview">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="prepaid">Prepaid</TabsTrigger>
        </TabsList>
        <TabsContent value="overview" className="space-y-6 pt-2">
          <div className="grid gap-4 sm:grid-cols-2">
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Address</CardTitle>
              </CardHeader>
              <CardContent className="text-sm">
                {[
                  customer.location.address.street,
                  customer.location.address.city,
                  customer.location.address.region,
                  customer.location.address.country,
                ]
                  .filter(Boolean)
                  .join(', ')}
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">Coordinates</CardTitle>
              </CardHeader>
              <CardContent className="text-sm tabular-nums">
                {customer.location.coordinates
                  ? `${customer.location.coordinates.latitude}, ${customer.location.coordinates.longitude}`
                  : '—'}
              </CardContent>
            </Card>
          </div>

          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold">Service locations</h2>
              {canManage && (
                <Button onClick={() => setAddOpen(true)}>
                  <Plus className="size-4" /> Add location
                </Button>
              )}
            </div>
            <DataTable
              columns={locationColumns}
              data={customer.serviceLocations}
              rowKey={(l) => l.id}
              emptyTitle="No service locations"
              emptyDescription="Add the sites where this customer receives service."
            />
          </div>
        </TabsContent>
        <TabsContent value="prepaid" className="pt-2">
          <PrepaidBalancePanel customerId={customer.id} />
        </TabsContent>
      </Tabs>

      <Dialog open={addOpen} onOpenChange={setAddOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add service location</DialogTitle>
          </DialogHeader>
          <ServiceLocationForm
            submitting={addLocMut.isPending}
            onSubmit={async (values) => {
              await addLocMut.mutateAsync({ label: values.label, location: values.location })
              toast.success('Service location added.')
              setAddOpen(false)
            }}
          />
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={confirmDisconnect}
        onOpenChange={setConfirmDisconnect}
        title="Disconnect customer?"
        description={`"${customer.personalInfo.fullName}" will be marked as disconnected.`}
        confirmText="Disconnect"
        destructive
        loading={statusMut.isPending}
        onConfirm={() => {
          setConfirmDisconnect(false)
          runStatus('disconnect')
        }}
      />

      <ConfirmDialog
        open={deactivateTarget !== null}
        onOpenChange={(open) => !open && setDeactivateTarget(null)}
        title="Deactivate location?"
        description={deactivateTarget ? `"${deactivateTarget.label}" will be deactivated.` : ''}
        confirmText="Deactivate"
        destructive
        loading={deactivateLocMut.isPending}
        onConfirm={async () => {
          if (!deactivateTarget) return
          await deactivateLocMut.mutateAsync(deactivateTarget.id)
          toast.success('Service location switched off.')
          setDeactivateTarget(null)
        }}
      />
    </div>
  )
}
