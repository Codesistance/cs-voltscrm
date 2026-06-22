import { useNavigate, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { Card, CardContent } from '@/components/ui/card'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { PageHeader } from '@/shared/components/PageHeader'
import { config } from '@/app/config'
import { useSubscription, useUpdateSubscription } from '../api/queries'
import { SubscriptionForm } from '../components/SubscriptionForm'
import type { SubscriptionFormValues, SubscriptionValues } from '../schema'

export function SubscriptionEditPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { data: sub, isLoading, isError, error, refetch } = useSubscription(id)
  const updateMut = useUpdateSubscription(id)

  if (isLoading) return <LoadingState label="Loading subscription…" />
  if (isError || !sub) return <ErrorState error={error} onRetry={refetch} />

  if (sub.status !== 'Pending') {
    return (
      <div className="space-y-4">
        <ErrorState error={new Error('Only pending subscriptions can be edited.')} />
        <button
          type="button"
          className="text-sm text-primary underline"
          onClick={() => navigate(`/admin/subscriptions/${id}`)}
        >
          Back to subscription
        </button>
      </div>
    )
  }

  const defaultValues: Partial<SubscriptionFormValues> = {
    customerId: sub.customerId,
    servicePlanId: sub.servicePlanId,
    startDate: new Date(sub.startDate),
    useNegotiatedPrice: sub.negotiatedPrice != null,
    negotiatedPrice: sub.negotiatedPrice ?? { amount: 0, currency: config.defaultCurrency },
    serviceLocationId: '',
  }

  const handleSubmit = async (values: SubscriptionValues) => {
    const startDate = new Date(values.startDate)
    startDate.setHours(0, 0, 0, 0)

    await updateMut.mutateAsync({
      startDate: startDate.toISOString(),
      negotiatedPrice:
        values.useNegotiatedPrice && values.negotiatedPrice
          ? {
              amount: values.negotiatedPrice.amount,
              currency: values.negotiatedPrice.currency ?? config.defaultCurrency,
            }
          : null,
      serviceLocationId: values.serviceLocationId || null,
    })

    toast.success('Subscription updated.')
    navigate(`/admin/subscriptions/${id}`)
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Edit subscription" description={`${sub.customerName} · ${sub.planName}`} />
      <Card className="max-w-3xl">
        <CardContent className="pt-6">
          <SubscriptionForm
            mode="edit"
            defaultValues={defaultValues}
            submitting={updateMut.isPending}
            onSubmit={handleSubmit}
            onCancel={() => navigate(`/admin/subscriptions/${id}`)}
          />
        </CardContent>
      </Card>
    </div>
  )
}
