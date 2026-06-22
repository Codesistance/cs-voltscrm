import type { ReactNode } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { LocationPicker } from '@/features/location/components/LocationPicker'
import { applyServerErrors } from '@/shared/lib/errors'
import { serviceLocationSchema, type ServiceLocationFormValues, type ServiceLocationValues } from '../schema'

export function ServiceLocationForm({
  submitting,
  onSubmit,
}: {
  submitting: boolean
  onSubmit: (values: ServiceLocationValues) => Promise<void>
}) {
  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors },
  } = useForm<ServiceLocationFormValues, unknown, ServiceLocationValues>({
    resolver: zodResolver(serviceLocationSchema),
    defaultValues: {
      label: '',
      location: {
        address: { street: '', city: '', region: '', country: '' },
        coordinates: null,
      },
    },
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
      <Field label="Label" error={errors.label?.message}>
        <Input {...register('label')} placeholder="e.g. Main House, Warehouse" aria-invalid={!!errors.label} />
      </Field>
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
      <div className="flex justify-end">
        <Button type="submit" disabled={submitting}>
          {submitting ? 'Adding…' : 'Add location'}
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
