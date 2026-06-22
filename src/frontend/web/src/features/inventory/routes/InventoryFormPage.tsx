import { useNavigate, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { config } from '@/app/config'
import { Card, CardContent } from '@/components/ui/card'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { PageHeader } from '@/shared/components/PageHeader'
import { useCreateInventoryItem, useInventoryItem, useUpdateInventoryItem } from '../api/queries'
import { InventoryForm } from '../components/InventoryForm'
import type { InventoryItemFormValues, InventoryItemValues } from '../schema'

export function InventoryFormPage() {
  const { id } = useParams()
  const isEdit = !!id
  const navigate = useNavigate()

  const itemQuery = useInventoryItem(id ?? '')
  const createMut = useCreateInventoryItem()
  const updateMut = useUpdateInventoryItem(id ?? '')

  if (isEdit && itemQuery.isLoading) return <LoadingState label="Loading item…" />
  if (isEdit && (itemQuery.isError || !itemQuery.data))
    return <ErrorState error={itemQuery.error} onRetry={itemQuery.refetch} />

  const item = itemQuery.data
  const defaultValues: InventoryItemFormValues =
    isEdit && item
      ? {
          itemCode: item.itemCode,
          name: item.name,
          description: item.description ?? '',
          categoryId: item.categoryId,
          unitOfMeasure: item.unitOfMeasure,
          unitCost: { amount: item.unitCost.amount, currency: item.unitCost.currency },
          quantityOnHand: item.quantityOnHand ?? undefined,
          reorderLevel: item.reorderLevel ?? undefined,
        }
      : {
          itemCode: '',
          name: '',
          description: '',
          categoryId: '',
          unitOfMeasure: 'unit',
          unitCost: { amount: 0, currency: config.defaultCurrency },
          quantityOnHand: undefined,
          reorderLevel: undefined,
        }

  const handleSubmit = async (values: InventoryItemValues) => {
    if (isEdit && id) {
      await updateMut.mutateAsync({
        name: values.name,
        description: values.description ?? null,
        unitCost: values.unitCost,
        reorderLevel: values.reorderLevel ?? null,
      })
      toast.success("Saved — the item's up to date.")
      navigate(`/admin/inventory/${id}`)
    } else {
      const created = await createMut.mutateAsync({
        itemCode: values.itemCode,
        name: values.name,
        description: values.description ?? null,
        categoryId: values.categoryId,
        unitOfMeasure: values.unitOfMeasure,
        unitCost: values.unitCost,
        quantityOnHand: values.quantityOnHand ?? null,
        reorderLevel: values.reorderLevel ?? null,
      })
      toast.success('Added to the catalogue.')
      navigate(`/admin/inventory/${created.id}`)
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader title={isEdit ? 'Edit item' : 'New item'} />
      <Card className="max-w-3xl">
        <CardContent className="pt-6">
          <InventoryForm
            key={item?.id ?? 'new'}
            mode={isEdit ? 'edit' : 'create'}
            defaultValues={defaultValues}
            submitting={createMut.isPending || updateMut.isPending}
            onSubmit={handleSubmit}
            onCancel={() => navigate(isEdit && id ? `/admin/inventory/${id}` : '/admin/inventory')}
          />
        </CardContent>
      </Card>
    </div>
  )
}
