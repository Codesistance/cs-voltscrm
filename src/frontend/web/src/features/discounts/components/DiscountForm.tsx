import type { ReactNode } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { cn } from '@/lib/utils'
import { applyServerErrors } from '@/shared/lib/errors'
import { useCustomerList } from '@/features/customers/api/queries'
import { DISCOUNT_SCOPES, DISCOUNT_TYPES } from '../api/types'
import { grantDiscountSchema, type GrantDiscountFormValues, type GrantDiscountValues } from '../schema'

export function DiscountForm({
  submitting,
  onSubmit,
  onCancel,
}: {
  submitting: boolean
  onSubmit: (values: GrantDiscountValues) => Promise<void>
  onCancel: () => void
}) {
  const {
    register,
    handleSubmit,
    watch,
    setError,
    formState: { errors },
  } = useForm<GrantDiscountFormValues, unknown, GrantDiscountValues>({
    resolver: zodResolver(grantDiscountSchema),
    defaultValues: {
      customerId: '',
      discountType: 'FixedAmount',
      value: 0,
      scope: 'Invoice',
      isRecurring: false,
      reason: '',
    },
  })
  const customers = useCustomerList({ page: 1, pageSize: 200 })
  const discountType = watch('discountType')

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
        <Field label="Customer" error={errors.customerId?.message}>
          <Select {...register('customerId')} aria-invalid={!!errors.customerId}>
            <option value="">Select customer…</option>
            {(customers.data?.items ?? []).map((c) => (
              <option key={c.id} value={c.id}>
                {c.accountNumber} · {c.fullName}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Type" error={errors.discountType?.message}>
          <Select {...register('discountType')}>
            {DISCOUNT_TYPES.map((t) => (
              <option key={t} value={t}>
                {t === 'FixedAmount' ? 'Fixed amount' : 'Percentage'}
              </option>
            ))}
          </Select>
        </Field>
        <Field
          label={discountType === 'Percentage' ? 'Percentage (%)' : 'Amount'}
          error={errors.value?.message}
        >
          <Input type="number" step="0.01" min="0.01" {...register('value')} aria-invalid={!!errors.value} />
        </Field>
        <Field label="Scope" error={errors.scope?.message}>
          <Select {...register('scope')}>
            {DISCOUNT_SCOPES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </Select>
        </Field>
      </div>
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" {...register('isRecurring')} className="size-4 rounded border" />
        Recurring discount (applies to each billing period)
      </label>
      <Field label="Reason" error={errors.reason?.message}>
        <Input {...register('reason')} maxLength={500} placeholder="e.g. loyalty adjustment" />
      </Field>
      <div className="flex gap-2 pt-2">
        <Button type="submit" disabled={submitting}>
          {submitting ? 'Granting…' : 'Grant discount'}
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
  children: ReactNode
}) {
  return (
    <div className="space-y-1.5">
      <Label>{label}</Label>
      {children}
      {error && <p className={cn('text-sm text-destructive')}>{error}</p>}
    </div>
  )
}
