import { useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { LoadingState } from '@/shared/components/LoadingState'
import { PageHeader } from '@/shared/components/PageHeader'
import { useSaveTokenVendingSettings, useTokenVendingSettings } from '../api/queries'
import type { TokenVendingSettings } from '../api/types'

const MASKED = '••••••••'

export function TokenVendingPage() {
  const { data, isLoading } = useTokenVendingSettings()
  if (isLoading) return <LoadingState label="Loading token vending settings…" />
  return <TokenVendingSettingsForm key={data?.provider ?? 'new'} data={data} />
}

function TokenVendingSettingsForm({ data }: { data: TokenVendingSettings | null | undefined }) {
  const saveMut = useSaveTokenVendingSettings()
  const [provider, setProvider] = useState(data?.provider ?? 'Integrated Token Service')
  const [apiKey, setApiKey] = useState('')
  const [apiKeyTouched, setApiKeyTouched] = useState(false)
  const [active, setActive] = useState(data?.active ?? true)

  const maskedStored = data?.apiKey === MASKED

  return (
    <div className="space-y-6">
      <PageHeader title="Token vending" description="Manage token vending provider configuration." />
      <Card className="max-w-3xl">
        <CardHeader>
          <CardTitle>Token service provider</CardTitle>
          <CardDescription>API keys are masked on load — enter a new value only to rotate.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-1.5">
            <Label>Provider name</Label>
            <Input value={provider} onChange={(e) => setProvider(e.target.value)} />
          </div>
          <div className="space-y-1.5">
            <Label>API key</Label>
            <Input
              value={apiKey}
              onChange={(e) => {
                setApiKey(e.target.value)
                setApiKeyTouched(true)
              }}
              type="password"
              placeholder={maskedStored ? 'Stored key is masked — leave blank to keep' : 'Enter API key'}
            />
          </div>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={active}
              onChange={(e) => setActive(e.target.checked)}
              className="size-4 rounded border"
            />
            Enable token vending integration
          </label>
          <Button
            disabled={saveMut.isPending}
            onClick={async () => {
              await saveMut.mutateAsync({
                provider,
                apiKey: apiKeyTouched && apiKey.trim() ? apiKey.trim() : null,
                active,
              })
              toast.success('Token vending settings saved.')
              setApiKey('')
              setApiKeyTouched(false)
            }}
          >
            {saveMut.isPending ? 'Saving…' : 'Save settings'}
          </Button>
        </CardContent>
      </Card>
    </div>
  )
}
