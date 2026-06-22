import { useSearchParams, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Card, CardContent } from '@/components/ui/card'
import { PageHeader } from '@/shared/components/PageHeader'
import { config } from '@/app/config'
import { useRecordPayment } from '../api/queries'
import { PaymentForm } from '../components/PaymentForm'
import type { PaymentValues } from '../schema'

export function PaymentFormPage() {
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const recordMut = useRecordPayment()

  const defaultCustomerId = params.get('customerId') ?? ''

  const handleSubmit = async (values: PaymentValues) => {
    const paymentDate = new Date(values.paymentDate)
    paymentDate.setHours(12, 0, 0, 0)

    const allocations =
      values.allocations.length > 0
        ? values.allocations.map((row) => ({
            invoiceId: row.targetType === 'invoice' ? row.targetId : null,
            installmentId: row.targetType === 'installment' ? row.targetId : null,
            amount: row.amount,
          }))
        : null

    const created = await recordMut.mutateAsync({
      customerId: values.customerId,
      amount: values.amount,
      currency: values.currency ?? config.defaultCurrency,
      method: values.method,
      channel: values.channel,
      paymentDate: paymentDate.toISOString(),
      platformProvider: values.platformProvider || null,
      platformReference: values.platformReference || null,
      allocations,
      autoComplete: values.completeImmediately,
    })

    toast.success(
      created.status === 'Completed' || values.completeImmediately
        ? 'Payment recorded and completed.'
        : 'Payment recorded.',
    )
    navigate(`/admin/payments/${created.id}`)
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Record payment" description="Log a customer payment — cash or other methods." />
      <Card className="max-w-3xl">
        <CardContent className="pt-6">
          <PaymentForm
            defaultValues={{ customerId: defaultCustomerId }}
            submitting={recordMut.isPending}
            onSubmit={handleSubmit}
            onCancel={() => navigate('/admin/payments')}
          />
        </CardContent>
      </Card>
    </div>
  )
}
