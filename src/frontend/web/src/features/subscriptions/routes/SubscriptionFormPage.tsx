import { useSearchParams, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Card, CardContent } from '@/components/ui/card'
import { PageHeader } from '@/shared/components/PageHeader'
import { config } from '@/app/config'
import { useCreateSubscription } from '../api/queries'
import { SubscriptionForm } from '../components/SubscriptionForm'
import type { SubscriptionValues } from '../schema'

export function SubscriptionFormPage() {
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const createMut = useCreateSubscription()

  const defaultCustomerId = params.get('customerId') ?? ''
  const defaultServicePlanId = params.get('servicePlanId') ?? ''

  const handleSubmit = async (values: SubscriptionValues) => {
    const startDate = new Date(values.startDate)
    startDate.setHours(0, 0, 0, 0)

    const created = await createMut.mutateAsync({
      customerId: values.customerId,
      servicePlanId: values.servicePlanId,
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
    toast.success('Subscription created.')
    navigate(`/admin/subscriptions/${created.id}`)
  }

  return (
    <div className="space-y-6">
      <PageHeader title="New subscription" description="Link a customer to a service plan." />
      <Card className="max-w-3xl">
        <CardContent className="pt-6">
          <SubscriptionForm
            defaultValues={{
              customerId: defaultCustomerId,
              servicePlanId: defaultServicePlanId,
            }}
            submitting={createMut.isPending}
            onSubmit={handleSubmit}
            onCancel={() => navigate('/admin/subscriptions')}
          />
        </CardContent>
      </Card>
    </div>
  )
}
