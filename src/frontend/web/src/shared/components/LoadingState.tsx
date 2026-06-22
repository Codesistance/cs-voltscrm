import { Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'

export function LoadingState({ className, label = 'Loading…' }: { className?: string; label?: string }) {
  return (
    <div className={cn('flex items-center justify-center gap-2 py-12 text-muted-foreground', className)}>
      <Loader2 className="size-4 animate-spin" />
      <span className="text-sm">{label}</span>
    </div>
  )
}
