import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { applyServerErrors } from '@/shared/lib/errors'
import { STOCK_MOVEMENT_TYPES } from '../api/types'
import { stockMovementSchema, type StockMovementFormValues, type StockMovementValues } from '../schema'

export function StockMovementForm({
  submitting,
  onSubmit,
}: {
  submitting: boolean
  onSubmit: (values: StockMovementValues) => Promise<void>
}) {
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<StockMovementFormValues, unknown, StockMovementValues>({
    resolver: zodResolver(stockMovementSchema),
    defaultValues: { movementType: 'In', quantity: 1, reference: '', notes: '' },
  })

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(values)
    } catch (error) {
      applyServerErrors(error, setError)
    }
  })

  return (
    <form onSubmit={submit} className="space-y-4" noValidate>
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label>Type</Label>
          <Select {...register('movementType')} aria-invalid={!!errors.movementType}>
            {STOCK_MOVEMENT_TYPES.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </Select>
          {errors.movementType && <p className="text-sm text-destructive">{errors.movementType.message}</p>}
        </div>
        <div className="space-y-2">
          <Label>Quantity</Label>
          <Input type="number" {...register('quantity')} aria-invalid={!!errors.quantity} />
          {errors.quantity && <p className="text-sm text-destructive">{errors.quantity.message}</p>}
        </div>
      </div>
      <div className="space-y-2">
        <Label>Reference</Label>
        <Input {...register('reference')} placeholder="e.g. PO-1024" />
      </div>
      <div className="space-y-2">
        <Label>Notes</Label>
        <Input {...register('notes')} />
      </div>
      <div className="flex justify-end">
        <Button type="submit" disabled={submitting}>
          {submitting ? 'Recording…' : 'Record movement'}
        </Button>
      </div>
    </form>
  )
}
