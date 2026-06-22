import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { Money } from '@/shared/components/Money'
import { formatDateTime } from '@/shared/lib/format'
import { useCustomerPaymentAccount } from '@/features/invoices/api/queries'

export function PrepaidBalancePanel({ customerId }: { customerId: string }) {
  const { data, isLoading, isError, error, refetch } = useCustomerPaymentAccount(customerId)

  return (
    <Card>
      <CardHeader>
        <CardTitle>Prepaid balance</CardTitle>
        <CardDescription>Balance available in the customer payment account.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-2">
        {isLoading && <LoadingState label="Loading prepaid balance…" />}
        {isError && <ErrorState error={error} onRetry={refetch} />}
        {data && (
          <>
            <p className="text-sm text-muted-foreground">Current balance</p>
            <p className="text-2xl font-semibold">
              <Money value={data.balance} />
            </p>
            {data.lastPaymentDate && (
              <p className="text-xs text-muted-foreground">
                Last payment: {formatDateTime(data.lastPaymentDate)}
              </p>
            )}
            <p className="text-xs text-muted-foreground">Updated {formatDateTime(data.updatedAt)}</p>
          </>
        )}
      </CardContent>
    </Card>
  )
}
