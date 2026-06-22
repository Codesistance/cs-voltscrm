import type { ReactNode } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm, useWatch } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { config } from '@/app/config'
import { cn } from '@/lib/utils'
import { applyServerErrors } from '@/shared/lib/errors'
import { useInventoryCategories } from '../api/queries'
import { UNIT_OF_MEASURE_OPTIONS } from '../api/types'
import { inventoryItemSchema, type InventoryItemFormValues, type InventoryItemValues } from '../schema'

interface Props {
  mode: 'create' | 'edit'
  defaultValues: InventoryItemFormValues
  submitting: boolean
  onSubmit: (values: InventoryItemValues) => Promise<void>
  onCancel: () => void
}

export function InventoryForm({ mode, defaultValues, submitting, onSubmit, onCancel }: Props) {
  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors },
  } = useForm<InventoryItemFormValues, unknown, InventoryItemValues>({
    resolver: zodResolver(inventoryItemSchema),
    defaultValues,
  })

  const isEdit = mode === 'edit'
  const { data: categories } = useInventoryCategories()
  const categoryId = useWatch({ control, name: 'categoryId' })
  const selectedCategory = categories?.find((c) => c.id === categoryId)
  // Default to tracking while categories load so stock fields aren't hidden prematurely.
  const tracksStock = selectedCategory?.tracksStock ?? true

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(values)
    } catch (error) {
      applyServerErrors(error, setError)
    }
  })

  return (
    <form onSubmit={submit} className="space-y-5" noValidate>
      <div className="grid gap-4 sm:grid-cols-2">
        <Field label="Item code" error={errors.itemCode?.message}>
          <Input {...register('itemCode')} disabled={isEdit} aria-invalid={!!errors.itemCode} />
        </Field>
        <Field label="Name" error={errors.name?.message}>
          <Input {...register('name')} aria-invalid={!!errors.name} />
        </Field>
        <Field label="Category" error={errors.categoryId?.message}>
          <Select {...register('categoryId')} disabled={isEdit}>
            <option value="">Select a category…</option>
            {categories?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Unit of measure" error={errors.unitOfMeasure?.message}>
          <Select {...register('unitOfMeasure')} disabled={isEdit}>
            {UNIT_OF_MEASURE_OPTIONS.map((u) => (
              <option key={u} value={u}>
                {u}
              </option>
            ))}
          </Select>
        </Field>
        <Field label={`Unit cost (${config.defaultCurrency})`} error={errors.unitCost?.amount?.message}>
          <Input type="number" step="0.01" min="0" {...register('unitCost.amount')} aria-invalid={!!errors.unitCost?.amount} />
        </Field>
        {tracksStock && (
          <Field label="Reorder level" error={errors.reorderLevel?.message}>
            <Input type="number" min="0" {...register('reorderLevel')} />
          </Field>
        )}
        {tracksStock && !isEdit && (
          <Field label="Opening quantity" error={errors.quantityOnHand?.message}>
            <Input type="number" min="0" {...register('quantityOnHand')} />
          </Field>
        )}
      </div>

      <Field label="Description" error={errors.description?.message}>
        <textarea
          {...register('description')}
          rows={3}
          className={cn(
            'border-input flex w-full rounded-md border bg-transparent px-3 py-2 text-sm shadow-xs outline-none',
            'focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px] disabled:opacity-50',
          )}
        />
      </Field>

      <input type="hidden" {...register('unitCost.currency')} />

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={submitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={submitting}>
          {submitting ? 'Saving…' : isEdit ? 'Save changes' : 'Create item'}
        </Button>
      </div>
    </form>
  )
}

function Field({ label, error, children }: { label: string; error?: string; children: ReactNode }) {
  return (
    <div className="space-y-2">
      <Label>{label}</Label>
      {children}
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  )
}
