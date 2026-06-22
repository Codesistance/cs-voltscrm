import { useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { LoadingState } from '@/shared/components/LoadingState'
import { PageHeader } from '@/shared/components/PageHeader'
import { useAutoDebitSettings, useSaveAutoDebitSettings } from '../api/queries'
import type { AutoDebitSettings } from '../api/types'

export function AutoDebitMandatesPage() {
  const { data, isLoading } = useAutoDebitSettings()
  if (isLoading) return <LoadingState label="Loading auto-debit settings…" />
  return <AutoDebitSettingsForm key={data?.provider ?? 'new'} data={data} />
}

function AutoDebitSettingsForm({ data }: { data: AutoDebitSettings | null | undefined }) {
  const saveMut = useSaveAutoDebitSettings()
  const [provider, setProvider] = useState(data?.provider ?? 'NIBSS eMandate')
  const [retryDays, setRetryDays] = useState(String(data?.retryDays ?? 3))
  const [enabled, setEnabled] = useState(data?.enabled ?? true)

  return (
    <div className="space-y-6">
      <PageHeader title="Auto-debit mandates" description="Configure mandate provider and retry behaviour." />
      <Card className="max-w-3xl">
        <CardHeader>
          <CardTitle>Mandate defaults</CardTitle>
          <CardDescription>Settings apply to scheduled auto-debit collection runs.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-1.5">
            <Label>Provider</Label>
            <Input value={provider} onChange={(e) => setProvider(e.target.value)} />
          </div>
          <div className="space-y-1.5">
            <Label>Retry window (days)</Label>
            <Input type="number" min="1" max="30" value={retryDays} onChange={(e) => setRetryDays(e.target.value)} />
          </div>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={enabled}
              onChange={(e) => setEnabled(e.target.checked)}
              className="size-4 rounded border"
            />
            Enable automatic mandate retries
          </label>
          <Button
            disabled={saveMut.isPending}
            onClick={async () => {
              await saveMut.mutateAsync({
                provider,
                retryDays: Number(retryDays),
                enabled,
              })
              toast.success('Auto-debit settings saved.')
            }}
          >
            {saveMut.isPending ? 'Saving…' : 'Save settings'}
          </Button>
        </CardContent>
      </Card>
    </div>
  )
}
