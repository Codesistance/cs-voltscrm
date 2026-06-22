import { useState } from 'react'
import { toast } from 'sonner'
import { RotateCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { useGenerateInvoices } from '../api/queries'

export function GenerateInvoicesButton() {
  const generateMut = useGenerateInvoices()
  const [open, setOpen] = useState(false)

  return (
    <>
      <Button variant="outline" onClick={() => setOpen(true)}>
        <RotateCw className="size-4" /> Generate invoices
      </Button>
      <ConfirmDialog
        open={open}
        onOpenChange={setOpen}
        title="Generate invoices now?"
        description="This will generate due invoices for active billable subscriptions."
        confirmText="Generate"
        loading={generateMut.isPending}
        onConfirm={async () => {
          const result = await generateMut.mutateAsync()
          toast.success(result.message ?? `Generated ${result.generatedCount} invoice(s).`)
          setOpen(false)
        }}
      />
    </>
  )
}
