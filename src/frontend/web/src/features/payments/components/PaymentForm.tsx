import type { ReactNode } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { DatePicker } from '@/components/ui/date-picker'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { cn } from '@/lib/utils'
import { config } from '@/app/config'
import { applyServerErrors } from '@/shared/lib/errors'
import { useCustomerList } from '@/features/customers/api/queries'
import { PAYMENT_CHANNELS, PAYMENT_METHODS } from '../api/types'
import { PaymentAllocationsEditor } from './PaymentAllocationsEditor'
import { paymentSchema, type PaymentFormValues, type PaymentValues } from '../schema'

interface Props {
  defaultValues?: Partial<PaymentFormValues>
  submitting: boolean
  onSubmit: (values: PaymentValues) => Promise<void>
  onCancel: () => void
}

const PLATFORM_METHODS = new Set(['MobileMoney', 'BankTransfer', 'Online'])

export function PaymentForm({ defaultValues, submitting, onSubmit, onCancel }: Props) {
  const {
    register,
    handleSubmit,
    control,
    watch,
    setError,
    formState: { errors },
  } = useForm<PaymentFormValues, unknown, PaymentValues>({
    resolver: zodResolver(paymentSchema),
    defaultValues: {
      customerId: '',
      amount: 0,
      currency: config.defaultCurrency,
      method: 'Cash',
      channel: 'Agent',
      paymentDate: new Date(),
      completeImmediately: true,
      allocations: [],
      ...defaultValues,
    },
  })

  const method = watch('method')
  const customerId = watch('customerId')
  const showPlatform = PLATFORM_METHODS.has(method)

  const customers = useCustomerList({ page: 1, pageSize: 200 })

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
        <Field label={`Amount (${config.defaultCurrency})`} error={errors.amount?.message}>
          <Input
            type="number"
            step="0.01"
            min="0.01"
            {...register('amount')}
            aria-invalid={!!errors.amount}
          />
        </Field>
        <Field label="Method" error={errors.method?.message}>
          <Select {...register('method')} aria-invalid={!!errors.method}>
            {PAYMENT_METHODS.map((m) => (
              <option key={m} value={m}>
                {m}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Channel" error={errors.channel?.message}>
          <Select {...register('channel')} aria-invalid={!!errors.channel}>
            {PAYMENT_CHANNELS.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Payment date" error={errors.paymentDate?.message}>
          <Controller
            control={control}
            name="paymentDate"
            render={({ field }) => (
              <DatePicker
                value={field.value}
                onChange={field.onChange}
                aria-invalid={!!errors.paymentDate}
              />
            )}
          />
        </Field>
      </div>

      {showPlatform && (
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Platform provider" error={errors.platformProvider?.message}>
            <Input {...register('platformProvider')} maxLength={100} />
          </Field>
          <Field label="Platform reference" error={errors.platformReference?.message}>
            <Input {...register('platformReference')} maxLength={100} />
          </Field>
        </div>
      )}

      <PaymentAllocationsEditor
        control={control}
        customerId={customerId}
        error={typeof errors.allocations?.message === 'string' ? errors.allocations.message : undefined}
      />

      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" {...register('completeImmediately')} className="size-4 rounded border" />
        Mark as completed immediately
      </label>
      <p className="text-sm text-muted-foreground">
        Cash payments complete automatically. Leave unchecked to record as pending and complete later from the
        payment detail page.
      </p>

      <div className="flex gap-2 pt-2">
        <Button type="submit" disabled={submitting}>
          {submitting ? 'Recording…' : 'Record payment'}
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
