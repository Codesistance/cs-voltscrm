import type { ReactNode } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useFieldArray, useForm } from 'react-hook-form'
import { Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { config } from '@/app/config'
import { cn } from '@/lib/utils'
import { applyServerErrors } from '@/shared/lib/errors'
import { useInventoryList } from '@/features/inventory/api/queries'
import { BILLING_CYCLES, BILLING_TYPES, LINE_ITEM_ROLES } from '../api/types'
import { servicePlanSchema, type ServicePlanFormValues, type ServicePlanValues } from '../schema'

interface Props {
  mode: 'create' | 'edit'
  defaultValues: ServicePlanFormValues
  submitting: boolean
  onSubmit: (values: ServicePlanValues) => Promise<void>
  onCancel: () => void
}

export function ServicePlanForm({ mode, defaultValues, submitting, onSubmit, onCancel }: Props) {
  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors },
  } = useForm<ServicePlanFormValues, unknown, ServicePlanValues>({
    resolver: zodResolver(servicePlanSchema),
    defaultValues,
  })
  const { fields, append, remove } = useFieldArray({ control, name: 'lineItems' })
  const isEdit = mode === 'edit'

  const inventory = useInventoryList({ page: 1, pageSize: 200 })
  const items = inventory.data?.items ?? []

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
        <Field label="Plan code" error={errors.planCode?.message}>
          <Input {...register('planCode')} disabled={isEdit} aria-invalid={!!errors.planCode} />
        </Field>
        <Field label="Name" error={errors.name?.message}>
          <Input {...register('name')} aria-invalid={!!errors.name} />
        </Field>
        <Field label="Billing type" error={errors.billingType?.message}>
          <Select {...register('billingType')} disabled={isEdit}>
            {BILLING_TYPES.map((b) => (
              <option key={b} value={b}>
                {b}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Billing cycle" error={errors.billingCycle?.message}>
          <Select {...register('billingCycle')} disabled={isEdit}>
            {BILLING_CYCLES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </Select>
        </Field>
        <Field label={`Base price (${config.defaultCurrency})`} error={errors.basePrice?.amount?.message}>
          <Input type="number" step="0.01" min="0" {...register('basePrice.amount')} aria-invalid={!!errors.basePrice?.amount} />
        </Field>
      </div>

      <Field label="Description" error={errors.description?.message}>
        <textarea
          {...register('description')}
          rows={2}
          className={cn(
            'border-input flex w-full rounded-md border bg-transparent px-3 py-2 text-sm shadow-xs outline-none',
            'focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]',
          )}
        />
      </Field>

      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <Label>Included items</Label>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => append({ inventoryItemId: '', quantity: 1, role: 'Core' })}
          >
            <Plus className="size-4" /> Add item
          </Button>
        </div>
        {fields.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No items yet. A plan can bundle equipment, spare parts and services.
          </p>
        ) : (
          <div className="space-y-2">
            {fields.map((field, i) => (
              <div key={field.id} className="grid grid-cols-[1fr_5rem_8rem_auto] items-start gap-2">
                <div>
                  <Select
                    {...register(`lineItems.${i}.inventoryItemId`)}
                    aria-invalid={!!errors.lineItems?.[i]?.inventoryItemId}
                  >
                    <option value="">Select item…</option>
                    {items.map((o) => (
                      <option key={o.id} value={o.id}>
                        {o.itemCode} — {o.name}
                      </option>
                    ))}
                  </Select>
                  {errors.lineItems?.[i]?.inventoryItemId && (
                    <p className="mt-1 text-xs text-destructive">{errors.lineItems[i]?.inventoryItemId?.message}</p>
                  )}
                </div>
                <Input type="number" min="1" {...register(`lineItems.${i}.quantity`)} aria-invalid={!!errors.lineItems?.[i]?.quantity} />
                <Select {...register(`lineItems.${i}.role`)}>
                  {LINE_ITEM_ROLES.map((r) => (
                    <option key={r} value={r}>
                      {r}
                    </option>
                  ))}
                </Select>
                <Button type="button" variant="ghost" size="icon-sm" onClick={() => remove(i)} title="Remove">
                  <Trash2 className="size-4" />
                </Button>
              </div>
            ))}
          </div>
        )}
      </div>

      <input type="hidden" {...register('basePrice.currency')} />

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={submitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={submitting}>
          {submitting ? 'Saving…' : isEdit ? 'Save changes' : 'Create plan'}
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
