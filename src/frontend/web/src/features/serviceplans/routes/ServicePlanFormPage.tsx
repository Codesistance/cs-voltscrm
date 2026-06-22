import { useNavigate, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { config } from '@/app/config'
import { Card, CardContent } from '@/components/ui/card'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { PageHeader } from '@/shared/components/PageHeader'
import { useCreateServicePlan, useServicePlan, useUpdateServicePlan } from '../api/queries'
import type { BillingCycle, BillingType, LineItemRole } from '../api/types'
import { ServicePlanForm } from '../components/ServicePlanForm'
import type { ServicePlanFormValues, ServicePlanValues } from '../schema'

export function ServicePlanFormPage() {
  const { id } = useParams()
  const isEdit = !!id
  const navigate = useNavigate()

  const planQuery = useServicePlan(id ?? '')
  const createMut = useCreateServicePlan()
  const updateMut = useUpdateServicePlan(id ?? '')

  if (isEdit && planQuery.isLoading) return <LoadingState label="Loading plan…" />
  if (isEdit && (planQuery.isError || !planQuery.data))
    return <ErrorState error={planQuery.error} onRetry={planQuery.refetch} />

  const plan = planQuery.data
  const defaultValues: ServicePlanFormValues =
    isEdit && plan
      ? {
          planCode: plan.planCode,
          name: plan.name,
          description: plan.description ?? '',
          billingType: plan.billingType as BillingType,
          billingCycle: plan.billingCycle as BillingCycle,
          basePrice: { amount: plan.basePrice.amount, currency: plan.basePrice.currency },
          lineItems: plan.lineItems.map((l) => ({
            inventoryItemId: l.inventoryItemId,
            quantity: l.quantity,
            role: l.role as LineItemRole,
          })),
        }
      : {
          planCode: '',
          name: '',
          description: '',
          billingType: 'Postpaid',
          billingCycle: 'Monthly',
          basePrice: { amount: 0, currency: config.defaultCurrency },
          lineItems: [],
        }

  const handleSubmit = async (values: ServicePlanValues) => {
    if (isEdit && id) {
      await updateMut.mutateAsync({
        name: values.name,
        description: values.description ?? null,
        basePrice: values.basePrice,
        lineItems: values.lineItems,
      })
      toast.success('Saved — your changes are in.')
      navigate(`/admin/service-plans/${id}`)
    } else {
      const created = await createMut.mutateAsync({
        planCode: values.planCode,
        name: values.name,
        description: values.description ?? null,
        billingType: values.billingType,
        billingCycle: values.billingCycle,
        basePrice: values.basePrice,
        lineItems: values.lineItems,
      })
      toast.success('Done — your new plan is live.')
      navigate(`/admin/service-plans/${created.id}`)
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader title={isEdit ? 'Edit plan' : 'New plan'} />
      <Card className="max-w-3xl">
        <CardContent className="pt-6">
          <ServicePlanForm
            key={plan?.id ?? 'new'}
            mode={isEdit ? 'edit' : 'create'}
            defaultValues={defaultValues}
            submitting={createMut.isPending || updateMut.isPending}
            onSubmit={handleSubmit}
            onCancel={() => navigate(isEdit && id ? `/admin/service-plans/${id}` : '/admin/service-plans')}
          />
        </CardContent>
      </Card>
    </div>
  )
}
