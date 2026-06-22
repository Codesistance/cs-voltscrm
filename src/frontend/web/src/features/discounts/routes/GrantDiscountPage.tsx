import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Card, CardContent } from '@/components/ui/card'
import { PageHeader } from '@/shared/components/PageHeader'
import { useGrantDiscount } from '../api/queries'
import { DiscountForm } from '../components/DiscountForm'
import type { GrantDiscountValues } from '../schema'

export function GrantDiscountPage() {
  const navigate = useNavigate()
  const grantMut = useGrantDiscount()

  const handleSubmit = async (values: GrantDiscountValues) => {
    await grantMut.mutateAsync({
      customerId: values.customerId,
      discountType: values.discountType,
      value: values.value,
      scope: values.scope,
      targetId: values.targetId || null,
      isRecurring: values.isRecurring,
      validFrom: values.validFrom || null,
      validUntil: values.validUntil || null,
      reason: values.reason || null,
    })
    toast.success('Discount granted.')
    navigate('/admin/discounts')
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Grant discount" description="Create a discount grant for a customer account." />
      <Card className="max-w-3xl">
        <CardContent className="pt-6">
          <DiscountForm
            submitting={grantMut.isPending}
            onSubmit={handleSubmit}
            onCancel={() => navigate('/admin/discounts')}
          />
        </CardContent>
      </Card>
    </div>
  )
}
