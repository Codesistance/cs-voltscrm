import { useState } from 'react'
import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError } from '@/shared/api/http'
import { phoenixApi, type PhoenixResetResult } from './api/phoenixApi'

/**
 * Phoenix — super-admin break-glass account recovery. Enter a user's email; the account is reset to
 * a fresh temporary password (shown once) and, if it was disabled, re-activated. It never reveals an
 * existing password — the user must change the temporary one at next login.
 */
export function PhoenixPage() {
  const [email, setEmail] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [result, setResult] = useState<PhoenixResetResult | null>(null)

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!email.trim()) {
      toast.error('Enter the email of the account to recover.')
      return
    }
    setSubmitting(true)
    setResult(null)
    try {
      const res = await phoenixApi.reset(email.trim())
      setResult(res)
      toast.success(
        res.reactivated
          ? 'Account recovered and re-activated.'
          : 'Account recovered — temporary password issued.',
      )
    } catch (error) {
      toast.error(
        error instanceof ApiError
          ? error.status === 404
            ? 'No account found with that email.'
            : error.message
          : "Couldn't recover that account — try again.",
      )
    } finally {
      setSubmitting(false)
    }
  }

  const copyPassword = async () => {
    if (!result) return
    try {
      await navigator.clipboard.writeText(result.temporaryPassword)
      toast.success('Temporary password copied.')
    } catch {
      toast.error('Copy failed — select and copy it manually.')
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/40 p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-1">
          <CardTitle className="text-xl">Account recovery</CardTitle>
          <CardDescription>
            Reset any account to a one-time temporary password. The user must change it at next login;
            a disabled account is re-activated. Super admins only.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <form onSubmit={onSubmit} className="space-y-4" noValidate>
            <div className="space-y-2">
              <Label htmlFor="email">Account email</Label>
              <Input
                id="email"
                type="email"
                autoComplete="off"
                placeholder="user@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={submitting}
              />
            </div>
            <Button type="submit" className="w-full" disabled={submitting}>
              {submitting ? 'Recovering…' : 'Recover account'}
            </Button>
          </form>

          {result && (
            <div className="space-y-2 rounded-md border bg-background p-4">
              <div className="text-sm text-muted-foreground">{result.email}</div>
              <div className="flex items-center justify-between gap-2">
                <code className="break-all font-mono text-sm">{result.temporaryPassword}</code>
                <Button type="button" variant="outline" size="sm" onClick={copyPassword}>
                  Copy
                </Button>
              </div>
              {result.reactivated && (
                <p className="text-xs text-muted-foreground">
                  This account was disabled and has been re-activated.
                </p>
              )}
              <p className="text-xs text-muted-foreground">
                Hand this to the user out-of-band. They'll be prompted to set their own password on
                first login.
              </p>
            </div>
          )}

          <div className="text-center text-sm">
            <Link to="/" className="text-muted-foreground hover:text-foreground">
              Back to app
            </Link>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
