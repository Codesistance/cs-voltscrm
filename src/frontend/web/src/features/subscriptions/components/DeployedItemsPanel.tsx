import { useState } from 'react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { formatDateTime } from '@/shared/lib/format'
import { useRecordDeployedItem } from '../api/queries'
import type { DeployedItem, Subscription } from '../api/types'
import { RecordDeployedItemForm } from './RecordDeployedItemForm'

interface Props {
  subscription: Subscription
  canManage: boolean
}

export function DeployedItemsPanel({ subscription, canManage }: Props) {
  const [open, setOpen] = useState(false)
  const recordMut = useRecordDeployedItem(subscription.id)
  const canDispatch = canManage && subscription.status !== 'Terminated'

  const columns: Column<DeployedItem>[] = [
    {
      header: 'Item',
      cell: (d) => (
        <Link
          to={`/admin/inventory/${d.inventoryItemId}`}
          className="font-medium hover:underline"
        >
          {d.inventoryItemCode}
        </Link>
      ),
    },
    { header: 'Name', cell: (d) => <span className="text-muted-foreground">{d.inventoryItemName}</span> },
    { header: 'Serial', cell: (d) => d.serialNumber ?? '—' },
    { header: 'Dispatched', cell: (d) => formatDateTime(d.dispatchedDate) },
  ]

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Deployed items</h2>
        {canDispatch && (
          <Button onClick={() => setOpen(true)}>
            <Plus className="size-4" /> Record dispatch
          </Button>
        )}
      </div>
      <DataTable
        columns={columns}
        data={subscription.deployedItems}
        rowKey={(d) => d.id}
        emptyTitle="No equipment dispatched"
        emptyDescription="Record equipment dispatched to this subscription."
      />

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Record dispatch</DialogTitle>
          </DialogHeader>
          <RecordDeployedItemForm
            submitting={recordMut.isPending}
            onCancel={() => setOpen(false)}
            onSubmit={async (values) => {
              await recordMut.mutateAsync({
                inventoryItemId: values.inventoryItemId,
                quantity: values.quantity,
                serialNumber: values.serialNumber || null,
              })
              toast.success('Dispatch recorded — stock updated.')
              setOpen(false)
            }}
          />
        </DialogContent>
      </Dialog>
    </div>
  )
}
