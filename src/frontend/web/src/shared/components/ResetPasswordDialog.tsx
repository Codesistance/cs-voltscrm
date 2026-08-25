import { useState } from 'react'
import { Check, Copy } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { copyText } from '@/lib/clipboard'
import { ApiError } from '@/shared/api/http'

const MIN_PASSWORD_LENGTH = 8

/**
 * Resets a user's password without email: either the caller types a specific value, or the server
 * generates one that's shown here once. Either way the account must change it at next login.
 */
export function ResetPasswordDialog({
  open,
  onOpenChange,
  subjectLabel,
  onReset,
  onSuccess,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  subjectLabel: string
  onReset: (newPassword?: string) => Promise<{ temporaryPassword: string | null }>
  onSuccess?: () => void
}) {
  const [mode, setMode] = useState<'generate' | 'custom'>('generate')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [revealed, setRevealed] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)

  const reset = () => {
    setMode('generate')
    setPassword('')
    setRevealed(null)
    setCopied(false)
  }

  const close = () => {
    onOpenChange(false)
    // Let the close animation finish before clearing state.
    setTimeout(reset, 150)
  }

  const submit = async () => {
    setSubmitting(true)
    try {
      const { temporaryPassword } = await onReset(mode === 'custom' ? password : undefined)
      onSuccess?.()
      if (temporaryPassword) {
        setRevealed(temporaryPassword)
      } else {
        toast.success('Password set — they must change it at next login.')
        close()
      }
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : "Couldn't reset that password — give it another go.")
    } finally {
      setSubmitting(false)
    }
  }

  const copy = async () => {
    if (!revealed) return
    if (await copyText(revealed)) {
      setCopied(true)
      setTimeout(() => setCopied(false), 1600)
    } else {
      toast.error('Copy failed — select the text and copy it manually.')
    }
  }

  const customValid = mode === 'generate' || password.length >= MIN_PASSWORD_LENGTH

  return (
    <Dialog open={open} onOpenChange={(next) => (next ? onOpenChange(true) : close())}>
      <DialogContent className="max-w-md">
        {revealed ? (
          <>
            <DialogHeader>
              <DialogTitle>Password reset</DialogTitle>
              <DialogDescription>
                Share this with {subjectLabel} yourself — it won't be shown again. They must change it at next
                login.
              </DialogDescription>
            </DialogHeader>
            <div className="flex items-center gap-2">
              <Input readOnly value={revealed} className="font-mono" onFocus={(e) => e.target.select()} />
              <Button type="button" variant="outline" size="icon" onClick={copy} title="Copy">
                {copied ? <Check className="size-4" /> : <Copy className="size-4" />}
              </Button>
            </div>
            <DialogFooter>
              <Button onClick={close}>Done</Button>
            </DialogFooter>
          </>
        ) : (
          <>
            <DialogHeader>
              <DialogTitle>Reset password</DialogTitle>
              <DialogDescription>
                For {subjectLabel}. They'll be required to change it the next time they log in.
              </DialogDescription>
            </DialogHeader>

            <Tabs value={mode} onValueChange={(v) => setMode(v as 'generate' | 'custom')}>
              <TabsList className="w-full">
                <TabsTrigger value="generate" className="flex-1">
                  Generate random
                </TabsTrigger>
                <TabsTrigger value="custom" className="flex-1">
                  Set specific password
                </TabsTrigger>
              </TabsList>
              <TabsContent value="generate" className="text-sm text-muted-foreground">
                A random password is created and shown here once, so you can hand it to them directly.
              </TabsContent>
              <TabsContent value="custom" className="space-y-2">
                <Label htmlFor="reset-password-value">New password</Label>
                <Input
                  id="reset-password-value"
                  type="text"
                  autoComplete="off"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder={`At least ${MIN_PASSWORD_LENGTH} characters`}
                />
              </TabsContent>
            </Tabs>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={close} disabled={submitting}>
                Cancel
              </Button>
              <Button onClick={submit} disabled={submitting || !customValid}>
                {submitting ? 'Resetting…' : 'Reset password'}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}
