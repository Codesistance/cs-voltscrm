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
import { copyText } from '@/lib/clipboard'

/** Shows a password once with a (HTTP-safe) copy button. Used after generating a credential. */
export function PasswordRevealDialog({
  open,
  onOpenChange,
  password,
  title = 'Temporary password',
  description,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  password: string
  title?: string
  description?: string
}) {
  const [copied, setCopied] = useState(false)

  const copy = async () => {
    if (await copyText(password)) {
      setCopied(true)
      setTimeout(() => setCopied(false), 1600)
    } else {
      toast.error('Copy failed — select the text and copy it manually.')
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>
            {description ??
              "Share this with the user yourself — it won't be shown again. They must change it at next login."}
          </DialogDescription>
        </DialogHeader>
        <div className="flex items-center gap-2">
          <Input readOnly value={password} className="font-mono" onFocus={(e) => e.target.select()} />
          <Button type="button" variant="outline" size="icon" onClick={copy} title="Copy">
            {copied ? <Check className="size-4" /> : <Copy className="size-4" />}
          </Button>
        </div>
        <DialogFooter>
          <Button onClick={() => onOpenChange(false)}>Done</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
