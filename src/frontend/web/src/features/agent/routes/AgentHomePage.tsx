import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ErrorState } from '@/shared/components/ErrorState'
import { LoadingState } from '@/shared/components/LoadingState'
import { Money } from '@/shared/components/Money'
import { useAuth } from '@/features/auth/AuthContext'
import { useAgentKpis } from '../api/queries'

const KPI_LABELS = [
  { key: 'assignedCustomers' as const, label: 'Assigned customers' },
  { key: 'visitsToday' as const, label: 'Visits today' },
  { key: 'paymentsCollected' as const, label: 'Payments collected' },
  { key: 'openTasks' as const, label: 'Open tasks' },
]

export function AgentHomePage() {
  const { user } = useAuth()
  const { data, isLoading, isError, error, refetch } = useAgentKpis()
  const firstName = user?.fullName?.split(' ')[0] ?? 'there'

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Hi {firstName}</h1>
        <p className="text-sm text-muted-foreground">Your field activity for today.</p>
      </div>

      {isLoading && <LoadingState label="Loading KPIs…" />}
      {isError && <ErrorState error={error} onRetry={refetch} />}

      {data && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {KPI_LABELS.map(({ key, label }) => (
            <Card key={key}>
              <CardHeader className="pb-2">
                <CardDescription>{label}</CardDescription>
                <CardTitle className="text-2xl">
                  {key === 'paymentsCollected' ? (
                    <Money value={{ amount: data.paymentsCollected, currency: data.paymentsCurrency }} />
                  ) : (
                    data[key]
                  )}
                </CardTitle>
              </CardHeader>
              <CardContent className="text-xs text-muted-foreground">Updated from live summary</CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
