import type { ReactNode } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { DatePicker } from '@/components/ui/date-picker'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { cn } from '@/lib/utils'
import { applyServerErrors } from '@/shared/lib/errors'
import { useSubscriptionList } from '@/features/subscriptions/api/queries'
import { installmentPlanSchema, type InstallmentPlanFormValues, type InstallmentPlanValues } from '../schema'

interface Props {
  defaultValues?: Partial<InstallmentPlanFormValues>
  submitting: boolean
  onSubmit: (values: InstallmentPlanValues) => Promise<void>
  onCancel: () => void
}

export function InstallmentPlanForm({ defaultValues, submitting, onSubmit, onCancel }: Props) {
  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors },
  } = useForm<InstallmentPlanFormValues, unknown, InstallmentPlanValues>({
    resolver: zodResolver(installmentPlanSchema),
    defaultValues: {
      subscriptionId: '',
      totalAmount: 0,
      depositAmount: 0,
      installmentCount: 3,
      startDate: new Date(),
      ...defaultValues,
    },
  })

  const subscriptions = useSubscriptionList({ page: 1, pageSize: 200, status: 'Active' })

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
        <Field label="Subscription" error={errors.subscriptionId?.message}>
          <Select {...register('subscriptionId')} aria-invalid={!!errors.subscriptionId}>
            <option value="">Select subscription…</option>
            {(subscriptions.data?.items ?? []).map((sub) => (
              <option key={sub.id} value={sub.id}>
                {sub.customerAccountNumber} · {sub.planName}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Installment count" error={errors.installmentCount?.message}>
          <Input type="number" min="1" step="1" {...register('installmentCount')} />
        </Field>
        <Field label="Total amount" error={errors.totalAmount?.message}>
          <Input type="number" step="0.01" min="0.01" {...register('totalAmount')} />
        </Field>
        <Field label="Deposit amount" error={errors.depositAmount?.message}>
          <Input type="number" step="0.01" min="0" {...register('depositAmount')} />
        </Field>
        <Field label="Start date" error={errors.startDate?.message}>
          <Controller
            control={control}
            name="startDate"
            render={({ field }) => (
              <DatePicker value={field.value} onChange={field.onChange} aria-invalid={!!errors.startDate} />
            )}
          />
        </Field>
      </div>

      <div className="flex gap-2 pt-2">
        <Button type="submit" disabled={submitting}>
          {submitting ? 'Creating…' : 'Create plan'}
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
