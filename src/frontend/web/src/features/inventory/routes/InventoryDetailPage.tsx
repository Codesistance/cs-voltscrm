import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { ArrowLeft, Pencil, Trash2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { DataTable, type Column } from '@/shared/components/DataTable'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { Money } from '@/shared/components/Money'
import { PageHeader } from '@/shared/components/PageHeader'
import { Pagination } from '@/shared/components/Pagination'
import { formatDateTime } from '@/shared/lib/format'
import { usePagination } from '@/shared/hooks/usePagination'
import { useAuth } from '@/features/auth/AuthContext'
import { PERMISSIONS } from '@/features/auth/permissions'
import {
  useDeactivateInventoryItem,
  useInventoryItem,
  useRecordStockMovement,
  useStockMovements,
} from '../api/queries'
import type { StockMovement } from '../api/types'
import { StockMovementForm } from '../components/StockMovementForm'

export function InventoryDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const isAdmin = useAuth().hasPermission(PERMISSIONS.inventoryManage)

  const { data: item, isLoading, isError, error, refetch } = useInventoryItem(id)
  const { page, pageSize, setPage } = usePagination({ pageSize: 10 })
  const movements = useStockMovements(id, { page, pageSize })
  const recordMut = useRecordStockMovement(id)
  const deactivateMut = useDeactivateInventoryItem()

  const [moveOpen, setMoveOpen] = useState(false)
  const [confirmOpen, setConfirmOpen] = useState(false)

  if (isLoading) return <LoadingState label="Loading item…" />
  if (isError || !item) return <ErrorState error={error} onRetry={refetch} />

  const tracksStock = item.categoryTracksStock

  const columns: Column<StockMovement>[] = [
    { header: 'Date', cell: (m) => formatDateTime(m.createdAt) },
    { header: 'Type', cell: (m) => m.movementType },
    {
      header: 'Qty',
      headClassName: 'text-right',
      className: 'text-right',
      cell: (m) => <span className="tabular-nums">{m.quantity}</span>,
    },
    { header: 'Reference', cell: (m) => m.reference ?? '—' },
    { header: 'Notes', cell: (m) => m.notes ?? '—' },
  ]

  return (
    <div className="space-y-6">
      <Button asChild variant="ghost" size="sm" className="-ml-2 w-fit">
        <Link to="/admin/inventory">
          <ArrowLeft className="size-4" /> Back to inventory
        </Link>
      </Button>

      <PageHeader
        title={item.name}
        description={`${item.itemCode} · ${item.categoryName}`}
        actions={
          isAdmin ? (
            <>
              <Button variant="outline" onClick={() => navigate(`/admin/inventory/${id}/edit`)}>
                <Pencil className="size-4" /> Edit
              </Button>
              <Button variant="outline" onClick={() => setConfirmOpen(true)}>
                <Trash2 className="size-4" /> Deactivate
              </Button>
            </>
          ) : null
        }
      />

      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Unit cost</CardTitle>
          </CardHeader>
          <CardContent>
            <Money value={item.unitCost} className="text-lg" />
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">On hand</CardTitle>
          </CardHeader>
          <CardContent className="flex items-center gap-2">
            {item.quantityOnHand == null ? (
              <span className="text-lg text-muted-foreground">—</span>
            ) : (
              <>
                <span className="text-lg tabular-nums">
                  {item.quantityOnHand} {item.unitOfMeasure}
                </span>
                {item.isLowStock && <Badge variant="warning">Low</Badge>}
              </>
            )}
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Reorder level</CardTitle>
          </CardHeader>
          <CardContent className="text-lg tabular-nums">{item.reorderLevel ?? '—'}</CardContent>
        </Card>
      </div>

      {item.description && <p className="text-sm text-muted-foreground">{item.description}</p>}

      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Stock movements</h2>
          {tracksStock && <Button onClick={() => setMoveOpen(true)}>Record movement</Button>}
        </div>
        <DataTable
          columns={columns}
          data={movements.data?.items}
          rowKey={(m) => m.id}
          isLoading={movements.isLoading}
          isError={movements.isError}
          error={movements.error}
          onRetry={movements.refetch}
          emptyTitle="No movements yet"
        />
        {movements.data && (
          <Pagination page={page} pageSize={pageSize} total={movements.data.total} onPageChange={setPage} />
        )}
      </div>

      <Dialog open={moveOpen} onOpenChange={setMoveOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Record stock movement</DialogTitle>
          </DialogHeader>
          <StockMovementForm
            submitting={recordMut.isPending}
            onSubmit={async (values) => {
              await recordMut.mutateAsync({
                movementType: values.movementType,
                quantity: values.quantity,
                reference: values.reference || null,
                notes: values.notes || null,
              })
              toast.success('Stock movement logged.')
              setMoveOpen(false)
            }}
          />
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={confirmOpen}
        onOpenChange={setConfirmOpen}
        title="Deactivate item?"
        description={`"${item.name}" will be hidden from the catalogue.`}
        confirmText="Deactivate"
        destructive
        loading={deactivateMut.isPending}
        onConfirm={async () => {
          await deactivateMut.mutateAsync(id)
          toast.success('Item taken off the catalogue.')
          navigate('/admin/inventory')
        }}
      />
    </div>
  )
}
