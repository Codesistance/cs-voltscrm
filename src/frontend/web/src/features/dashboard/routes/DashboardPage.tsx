import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { Money } from '@/shared/components/Money'
import { useAuth } from '@/features/auth/AuthContext'
import { useDashboardSummary } from '@/features/reports/api/queries'

export function DashboardPage() {
  const { user } = useAuth()
  const firstName = user?.fullName?.split(' ')[0] ?? 'there'
  const { data, isLoading, isError, error, refetch } = useDashboardSummary()

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Welcome back, {firstName}</h1>
        <p className="text-sm text-muted-foreground">Here's an overview of your operations.</p>
      </div>
      {isLoading && <LoadingState label="Loading dashboard metrics…" />}
      {isError && <ErrorState error={error} onRetry={refetch} />}
      {data && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <Card>
            <CardHeader className="pb-2">
              <CardDescription>Active customers</CardDescription>
              <CardTitle className="text-2xl">{data.activeCustomers.toLocaleString()}</CardTitle>
            </CardHeader>
            <CardContent className="text-xs text-muted-foreground">Customers with active service</CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardDescription>Outstanding balance</CardDescription>
              <CardTitle className="text-2xl">
                <Money value={data.outstandingBalance} />
              </CardTitle>
            </CardHeader>
            <CardContent className="text-xs text-muted-foreground">Open receivables</CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardDescription>Collections (MTD)</CardDescription>
              <CardTitle className="text-2xl">
                <Money value={data.collectionsMtd} />
              </CardTitle>
            </CardHeader>
            <CardContent className="text-xs text-muted-foreground">Collected this month</CardContent>
          </Card>
          <Card>
            <CardHeader className="pb-2">
              <CardDescription>Overdue invoices</CardDescription>
              <CardTitle className="text-2xl">{data.overdueInvoices.toLocaleString()}</CardTitle>
            </CardHeader>
            <CardContent className="text-xs text-muted-foreground">Invoices past due date</CardContent>
          </Card>
        </div>
      )}
    </div>
  )
}
