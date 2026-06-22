import { useMemo } from 'react'
import { useFieldArray, useWatch, type Control } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { cn } from '@/lib/utils'
import { useCustomerOpenInstallments } from '@/features/installments/api/queries'
import { useInvoiceList } from '@/features/invoices/api/queries'
import type { PaymentFormValues } from '../schema'

interface Props {
  control: Control<PaymentFormValues>
  customerId: string
  error?: string
}

export function PaymentAllocationsEditor({ control, customerId, error }: Props) {
  const { fields, append, remove } = useFieldArray({ control, name: 'allocations' })
  const allocations = useWatch({ control, name: 'allocations' })

  const invoices = useInvoiceList({
    page: 1,
    pageSize: 100,
    customerId: customerId || undefined,
  })

  const { installments: openInstallments } = useCustomerOpenInstallments(customerId)

  const openInvoices = useMemo(
    () =>
      (invoices.data?.items ?? []).filter(
        (invoice) => !['Paid', 'Cancelled'].includes(invoice.status),
      ),
    [invoices.data?.items],
  )

  return (
    <div className="space-y-3 rounded-md border p-4">
      <div>
        <Label>Payment allocations</Label>
        <p className="text-sm text-muted-foreground">
          Optionally allocate this payment to open invoices or installments.
        </p>
      </div>

      {fields.length === 0 && (
        <p className="text-sm text-muted-foreground">No allocation rows — the full amount stays unallocated.</p>
      )}

      {fields.map((field, index) => {
        const targetType = allocations?.[index]?.targetType ?? 'invoice'
        return (
          <div key={field.id} className="grid gap-3 sm:grid-cols-[140px_1fr_140px_auto]">
            <Select {...control.register(`allocations.${index}.targetType` as const)}>
              <option value="invoice">Invoice</option>
              <option value="installment">Installment</option>
            </Select>
            <Select {...control.register(`allocations.${index}.targetId` as const)}>
              <option value="">Select target…</option>
              {targetType === 'invoice'
                ? openInvoices.map((invoice) => (
                    <option key={invoice.id} value={invoice.id}>
                      {invoice.periodYear}-{String(invoice.periodMonth).padStart(2, '0')} · balance{' '}
                      {invoice.balance.amount.toFixed(2)}
                    </option>
                  ))
                : openInstallments.map((installment) => (
                    <option key={installment.id} value={installment.id}>
                      {installment.label}
                    </option>
                  ))}
            </Select>
            <Input
              type="number"
              step="0.01"
              min="0.01"
              placeholder="Amount"
              {...control.register(`allocations.${index}.amount` as const)}
            />
            <Button type="button" variant="outline" size="sm" onClick={() => remove(index)}>
              Remove
            </Button>
          </div>
        )
      })}

      <Button
        type="button"
        variant="outline"
        size="sm"
        disabled={!customerId}
        onClick={() => append({ targetType: 'invoice', targetId: '', amount: 0 })}
      >
        Add allocation row
      </Button>

      {error && <p className={cn('text-sm text-destructive')}>{error}</p>}
    </div>
  )
}
