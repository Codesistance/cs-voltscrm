import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { cn } from '@/lib/utils'
import { applyServerErrors } from '@/shared/lib/errors'
import { useInventoryList } from '@/features/inventory/api/queries'
import { deployedItemSchema, type DeployedItemFormValues, type DeployedItemValues } from '../schema'

interface Props {
  submitting: boolean
  onSubmit: (values: DeployedItemValues) => Promise<void>
  onCancel: () => void
}

export function RecordDeployedItemForm({ submitting, onSubmit, onCancel }: Props) {
  const {
    register,
    handleSubmit,
    watch,
    setError,
    formState: { errors },
  } = useForm<DeployedItemFormValues, unknown, DeployedItemValues>({
    resolver: zodResolver(deployedItemSchema),
    defaultValues: { inventoryItemId: '', quantity: 1, serialNumber: '' },
  })

  const inventoryItemId = watch('inventoryItemId')
  const inventory = useInventoryList({ page: 1, pageSize: 200 })
  // Only stock-tracked items can be dispatched; non-stock categories have a null quantity.
  const dispatchable = (inventory.data?.items ?? []).filter((i) => i.quantityOnHand !== null)
  const selected = dispatchable.find((i) => i.id === inventoryItemId)

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(values)
    } catch (error) {
      applyServerErrors(error, setError)
    }
  })

  return (
    <form onSubmit={submit} className="space-y-4" noValidate>
      <Field label="Inventory item" error={errors.inventoryItemId?.message}>
        <Select {...register('inventoryItemId')} aria-invalid={!!errors.inventoryItemId}>
          <option value="">Select item…</option>
          {dispatchable.map((i) => (
            <option key={i.id} value={i.id}>
              {i.itemCode} · {i.name} (on hand: {i.quantityOnHand ?? '—'})
            </option>
          ))}
        </Select>
      </Field>
      {selected && selected.quantityOnHand !== null && (
        <p className="text-sm text-muted-foreground">
          {selected.quantityOnHand} {selected.unitOfMeasure} available
        </p>
      )}
      <Field label="Quantity" error={errors.quantity?.message}>
        <Input
          type="number"
          min="1"
          step="1"
          {...register('quantity')}
          aria-invalid={!!errors.quantity}
        />
      </Field>
      <Field label="Serial number (optional)" error={errors.serialNumber?.message}>
        <Input {...register('serialNumber')} maxLength={100} />
      </Field>
      <div className="flex gap-2 pt-2">
        <Button type="submit" disabled={submitting}>
          {submitting ? 'Recording…' : 'Record dispatch'}
        </Button>
        <Button type="button" variant="outline" onClick={onCancel} disabled={submitting}>
          Cancel
        </Button>
      </div>
    </form>
  )
}

function Field({
  label,
  error,
  children,
}: {
  label: string
  error?: string
  children: React.ReactNode
}) {
  return (
    <div className="space-y-1.5">
      <Label>{label}</Label>
      {children}
      {error && <p className={cn('text-sm text-destructive')}>{error}</p>}
    </div>
  )
}
