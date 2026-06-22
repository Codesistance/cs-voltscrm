import type { ReactNode } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { applyServerErrors } from '@/shared/lib/errors'
import { LocationPicker } from '@/features/location/components/LocationPicker'
import { GENDERS } from '../api/types'
import { customerSchema, type CustomerFormValues, type CustomerValues } from '../schema'

interface Props {
  mode: 'create' | 'edit'
  defaultValues: CustomerFormValues
  submitting: boolean
  onSubmit: (values: CustomerValues) => Promise<void>
  onCancel: () => void
}

export function CustomerForm({ mode, defaultValues, submitting, onSubmit, onCancel }: Props) {
  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors },
  } = useForm<CustomerFormValues, unknown, CustomerValues>({
    resolver: zodResolver(customerSchema),
    defaultValues,
  })
  const isEdit = mode === 'edit'

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(values)
    } catch (error) {
      applyServerErrors(error, setError)
    }
  })

  return (
    <form onSubmit={submit} className="space-y-6" noValidate>
      <Section title="Account">
        <Field label="Account number" error={errors.accountNumber?.message}>
          <Input {...register('accountNumber')} disabled={isEdit} aria-invalid={!!errors.accountNumber} />
        </Field>
      </Section>

      <Section title="Personal info">
        <Field label="First name" error={errors.personalInfo?.firstName?.message}>
          <Input {...register('personalInfo.firstName')} aria-invalid={!!errors.personalInfo?.firstName} />
        </Field>
        <Field label="Last name" error={errors.personalInfo?.lastName?.message}>
          <Input {...register('personalInfo.lastName')} aria-invalid={!!errors.personalInfo?.lastName} />
        </Field>
        <Field label="Gender" error={errors.personalInfo?.gender?.message}>
          <Select {...register('personalInfo.gender')}>
            {GENDERS.map((g) => (
              <option key={g} value={g}>
                {g}
              </option>
            ))}
          </Select>
        </Field>
        <Field label="Phone" error={errors.personalInfo?.phone?.message}>
          <Input {...register('personalInfo.phone')} aria-invalid={!!errors.personalInfo?.phone} />
        </Field>
        <Field label="Email" error={errors.personalInfo?.email?.message}>
          <Input type="email" {...register('personalInfo.email')} />
        </Field>
      </Section>

      <section className="space-y-4">
        <h3 className="text-sm font-semibold text-muted-foreground">Location</h3>
        <Controller
          control={control}
          name="location"
          render={({ field }) => (
            <LocationPicker
              value={field.value}
              onChange={field.onChange}
              disabled={submitting}
              errors={{
                street: errors.location?.address?.street?.message,
                city: errors.location?.address?.city?.message,
                region: errors.location?.address?.region?.message,
                country: errors.location?.address?.country?.message,
              }}
            />
          )}
        />
      </section>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={submitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={submitting}>
          {submitting ? 'Saving…' : isEdit ? 'Save changes' : 'Create customer'}
        </Button>
      </div>
    </form>
  )
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="space-y-4">
      <h3 className="text-sm font-semibold text-muted-foreground">{title}</h3>
      <div className="grid gap-4 sm:grid-cols-2">{children}</div>
    </section>
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
