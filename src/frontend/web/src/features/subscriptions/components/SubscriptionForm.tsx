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
import { useCustomer, useCustomerList } from '@/features/customers/api/queries'
import { useServicePlan, useServicePlanList } from '@/features/serviceplans/api/queries'
import { Money } from '@/shared/components/Money'
import { subscriptionSchema, type SubscriptionFormValues, type SubscriptionValues } from '../schema'

interface Props {
  mode?: 'create' | 'edit'
  defaultValues?: Partial<SubscriptionFormValues>
  submitting: boolean
  onSubmit: (values: SubscriptionValues) => Promise<void>
  onCancel: () => void
}

export function SubscriptionForm({ mode = 'create', defaultValues, submitting, onSubmit, onCancel }: Props) {
  const {
    register,
    handleSubmit,
    control,
    watch,
    setError,
    formState: { errors },
  } = useForm<SubscriptionFormValues, unknown, SubscriptionValues>({
    resolver: zodResolver(subscriptionSchema),
    defaultValues: {
      customerId: '',
      servicePlanId: '',
      useNegotiatedPrice: false,
      negotiatedPrice: { amount: 0, currency: config.defaultCurrency },
      serviceLocationId: '',
      ...defaultValues,
    },
  })

  const customerId = watch('customerId')
  const servicePlanId = watch('servicePlanId')
  const useNegotiatedPrice = watch('useNegotiatedPrice')

  const customers = useCustomerList({ page: 1, pageSize: 200 })
  const plans = useServicePlanList({ page: 1, pageSize: 200, status: 'Active' })
  const customer = useCustomer(customerId)
  const plan = useServicePlan(servicePlanId)

  const locations = (customer.data?.serviceLocations ?? []).filter((l) => l.isActive)

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
          <Select {...register('customerId')} disabled={mode === 'edit'} aria-invalid={!!errors.customerId}>
            <option value="">Select customer…</option>
            {(customers.data?.items ?? []).map((c) => (
              <option key={c.id} value={c.id}>
                {c.accountNumber} · {c.fullName}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Service plan" error={errors.servicePlanId?.message}>
          <Select {...register('servicePlanId')} disabled={mode === 'edit'} aria-invalid={!!errors.servicePlanId}>
            <option value="">Select plan…</option>
            {(plans.data?.items ?? []).map((p) => (
              <option key={p.id} value={p.id}>
                {p.planCode} · {p.name}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Start date" error={errors.startDate?.message}>
          <Controller
            control={control}
            name="startDate"
            render={({ field }) => (
              <DatePicker
                value={field.value}
                onChange={field.onChange}
                aria-invalid={!!errors.startDate}
              />
            )}
          />
        </Field>
        <Field label="Service location" error={errors.serviceLocationId?.message}>
          <Select
            {...register('serviceLocationId')}
            disabled={!customerId || locations.length === 0}
          >
            <option value="">None</option>
            {locations.map((l) => (
              <option key={l.id} value={l.id}>
                {l.label}
                {l.activeSubscriptionId ? ' (has subscription)' : ''}
              </option>
            ))}
          </Select>
        </Field>
      </div>

      {plan.data && (
        <p className="text-sm text-muted-foreground">
          Plan billing: {plan.data.billingType} · base price{' '}
          <Money value={plan.data.basePrice} />
        </p>
      )}

      <div className="space-y-3">
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" {...register('useNegotiatedPrice')} className="size-4 rounded border" />
          Override with negotiated price
        </label>
        {useNegotiatedPrice && (
          <Field label={`Negotiated price (${config.defaultCurrency})`} error={errors.negotiatedPrice?.amount?.message}>
            <Input
              type="number"
              step="0.01"
              min="0"
              {...register('negotiatedPrice.amount')}
              aria-invalid={!!errors.negotiatedPrice?.amount}
            />
          </Field>
        )}
      </div>

      <div className="flex gap-2 pt-2">
        <Button type="submit" disabled={submitting}>
          {submitting ? (mode === 'edit' ? 'Saving…' : 'Creating…') : mode === 'edit' ? 'Save changes' : 'Create subscription'}
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
