import { useState } from 'react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import { ArrowLeft, Pencil, Plus, Trash2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError } from '@/shared/api/http'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { PageHeader } from '@/shared/components/PageHeader'
import {
  useCreateInventoryCategory,
  useDeactivateInventoryCategory,
  useInventoryCategories,
  useUpdateInventoryCategory,
} from '../api/queries'
import type { InventoryCategory } from '../api/types'

type Editing = { mode: 'new' } | { mode: 'edit'; category: InventoryCategory } | null

export function InventoryCategoriesPage() {
  const { data: categories, isLoading, isError, error, refetch } = useInventoryCategories()
  const [editing, setEditing] = useState<Editing>(null)
  const [toDelete, setToDelete] = useState<InventoryCategory | null>(null)

  const createMut = useCreateInventoryCategory()
  const editId = editing?.mode === 'edit' ? editing.category.id : ''
  const updateMut = useUpdateInventoryCategory(editId)
  const deactivateMut = useDeactivateInventoryCategory()

  if (isLoading) return <LoadingState label="Loading categories…" />
  if (isError) return <ErrorState error={error} onRetry={refetch} />

  const handleDelete = async () => {
    if (!toDelete) return
    try {
      await deactivateMut.mutateAsync(toDelete.id)
      toast.success('Category deactivated.')
      setToDelete(null)
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : "Couldn't deactivate that category.")
    }
  }

  return (
    <div className="space-y-6">
      <Button asChild variant="ghost" size="sm" className="-ml-2 w-fit">
        <Link to="/admin/inventory">
          <ArrowLeft className="size-4" /> Back to inventory
        </Link>
      </Button>

      <PageHeader
        title="Inventory categories"
        description="Classify inventory items. Non-stock categories skip quantity tracking."
        actions={
          <Button onClick={() => setEditing({ mode: 'new' })}>
            <Plus className="size-4" /> New category
          </Button>
        }
      />

      {categories && categories.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          No categories yet. Create one to start adding inventory items.
        </p>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {categories?.map((c) => (
            <Card key={c.id}>
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between gap-2">
                  <CardTitle className="flex items-center gap-2">
                    {c.name}
                    {!c.tracksStock && <Badge variant="muted">No stock</Badge>}
                  </CardTitle>
                  <div className="flex gap-1">
                    <Button variant="ghost" size="icon-sm" title="Edit" onClick={() => setEditing({ mode: 'edit', category: c })}>
                      <Pencil className="size-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      title={c.itemCount > 0 ? 'In use by items' : 'Deactivate'}
                      disabled={c.itemCount > 0 || deactivateMut.isPending}
                      onClick={() => setToDelete(c)}
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="text-xs text-muted-foreground">
                {c.code && <span className="mr-2">Code: {c.code}</span>}
                {c.itemCount} {c.itemCount === 1 ? 'item' : 'items'}
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      <Dialog open={!!editing} onOpenChange={(open) => !open && setEditing(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editing?.mode === 'edit' ? 'Edit category' : 'New category'}</DialogTitle>
          </DialogHeader>
          {editing && (
            <CategoryForm
              category={editing.mode === 'edit' ? editing.category : undefined}
              saving={createMut.isPending || updateMut.isPending}
              onCancel={() => setEditing(null)}
              onSave={async (body) => {
                try {
                  if (editing.mode === 'edit') {
                    await updateMut.mutateAsync(body)
                    toast.success('Category updated.')
                  } else {
                    await createMut.mutateAsync(body)
                    toast.success('Category created.')
                  }
                  setEditing(null)
                } catch (e) {
                  toast.error(e instanceof ApiError ? e.message : "Couldn't save that category.")
                }
              }}
            />
          )}
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={!!toDelete}
        onOpenChange={(open) => !open && setToDelete(null)}
        title="Deactivate category?"
        description={`"${toDelete?.name}" will be hidden from new items.`}
        confirmText="Deactivate"
        destructive
        loading={deactivateMut.isPending}
        onConfirm={handleDelete}
      />
    </div>
  )
}

function CategoryForm({
  category,
  saving,
  onCancel,
  onSave,
}: {
  category?: InventoryCategory
  saving: boolean
  onCancel: () => void
  onSave: (body: { name: string; code: string | null; tracksStock: boolean }) => void
}) {
  const [name, setName] = useState(category?.name ?? '')
  const [code, setCode] = useState(category?.code ?? '')
  const [tracksStock, setTracksStock] = useState(category?.tracksStock ?? true)

  return (
    <form
      className="space-y-4"
      onSubmit={(e) => {
        e.preventDefault()
        onSave({ name: name.trim(), code: code.trim() || null, tracksStock })
      }}
    >
      <div className="space-y-2">
        <Label htmlFor="cat-name">Name</Label>
        <Input id="cat-name" value={name} onChange={(e) => setName(e.target.value)} required maxLength={100} />
      </div>
      <div className="space-y-2">
        <Label htmlFor="cat-code">Code (optional)</Label>
        <Input id="cat-code" value={code} onChange={(e) => setCode(e.target.value)} maxLength={30} />
      </div>
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" checked={tracksStock} onChange={(e) => setTracksStock(e.target.checked)} />
        Tracks stock (items carry quantity & reorder levels)
      </label>
      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={saving}>
          Cancel
        </Button>
        <Button type="submit" disabled={saving || !name.trim()}>
          {saving ? 'Saving…' : category ? 'Save changes' : 'Create category'}
        </Button>
      </div>
    </form>
  )
}
