import { useNavigate, useSearchParams } from 'react-router-dom'
import { toast } from 'sonner'
import { Card, CardContent } from '@/components/ui/card'
import { PageHeader } from '@/shared/components/PageHeader'
import { useCreateInstallmentPlan } from '../api/queries'
import { InstallmentPlanForm } from '../components/InstallmentPlanForm'
import type { InstallmentPlanValues } from '../schema'

export function InstallmentPlanFormPage() {
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const createMut = useCreateInstallmentPlan()

  const defaultSubscriptionId = params.get('subscriptionId') ?? ''

  const handleSubmit = async (values: InstallmentPlanValues) => {
    const startDate = new Date(values.startDate)
    startDate.setHours(0, 0, 0, 0)

    const created = await createMut.mutateAsync({
      subscriptionId: values.subscriptionId,
      totalAmount: values.totalAmount,
      depositAmount: values.depositAmount,
      installmentCount: values.installmentCount,
      startDate: startDate.toISOString(),
    })

    toast.success('Installment plan created.')
    navigate(`/admin/installments/${created.id}`)
  }

  return (
    <div className="space-y-6">
      <PageHeader title="New installment plan" description="Create a payment schedule for a subscription." />
      <Card className="max-w-3xl">
        <CardContent className="pt-6">
          <InstallmentPlanForm
            defaultValues={{ subscriptionId: defaultSubscriptionId }}
            submitting={createMut.isPending}
            onSubmit={handleSubmit}
            onCancel={() => navigate('/admin/installments')}
          />
        </CardContent>
      </Card>
    </div>
  )
}
