import { useNavigate, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { Card, CardContent } from '@/components/ui/card'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { PageHeader } from '@/shared/components/PageHeader'
import { useCreateCustomer, useCustomer, useUpdateCustomer } from '../api/queries'
import type { Gender } from '../api/types'
import { CustomerForm } from '../components/CustomerForm'
import type { CustomerFormValues, CustomerValues } from '../schema'

export function CustomerFormPage() {
  const { id } = useParams()
  const isEdit = !!id
  const navigate = useNavigate()

  const customerQuery = useCustomer(id ?? '')
  const createMut = useCreateCustomer()
  const updateMut = useUpdateCustomer(id ?? '')

  if (isEdit && customerQuery.isLoading) return <LoadingState label="Loading customer…" />
  if (isEdit && (customerQuery.isError || !customerQuery.data))
    return <ErrorState error={customerQuery.error} onRetry={customerQuery.refetch} />

  const customer = customerQuery.data
  const defaultValues: CustomerFormValues =
    isEdit && customer
      ? {
          accountNumber: customer.accountNumber,
          personalInfo: {
            firstName: customer.personalInfo.firstName,
            lastName: customer.personalInfo.lastName,
            gender: customer.personalInfo.gender as Gender,
            phone: customer.personalInfo.phone,
            email: customer.personalInfo.email ?? '',
          },
          location: {
            address: {
              street: customer.location.address.street,
              city: customer.location.address.city,
              region: customer.location.address.region,
              country: customer.location.address.country,
            },
            coordinates: customer.location.coordinates,
          },
        }
      : {
          accountNumber: '',
          personalInfo: { firstName: '', lastName: '', gender: 'Male', phone: '', email: '' },
          location: {
            address: { street: '', city: '', region: '', country: '' },
            coordinates: null,
          },
        }

  const handleSubmit = async (values: CustomerValues) => {
    const personalInfo = { ...values.personalInfo, email: values.personalInfo.email || null }

    if (isEdit && id) {
      await updateMut.mutateAsync({ personalInfo, location: values.location })
      toast.success('Saved — customer details are up to date.')
      navigate(`/admin/customers/${id}`)
    } else {
      const created = await createMut.mutateAsync({
        accountNumber: values.accountNumber,
        personalInfo,
        location: values.location,
      })
      toast.success('Nice — new customer added.')
      navigate(`/admin/customers/${created.id}`)
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader title={isEdit ? 'Edit customer' : 'New customer'} />
      <Card className="max-w-3xl">
        <CardContent className="pt-6">
          <CustomerForm
            key={customer?.id ?? 'new'}
            mode={isEdit ? 'edit' : 'create'}
            defaultValues={defaultValues}
            submitting={createMut.isPending || updateMut.isPending}
            onSubmit={handleSubmit}
            onCancel={() => navigate(isEdit && id ? `/admin/customers/${id}` : '/admin/customers')}
          />
        </CardContent>
      </Card>
    </div>
  )
}
