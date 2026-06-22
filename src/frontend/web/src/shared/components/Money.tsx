import type { Money as MoneyValue } from '@/shared/api/types'
import { formatMoney } from '@/shared/lib/format'
import { cn } from '@/lib/utils'

/** Renders a Money value; negative amounts (e.g. discount lines) are shown in the destructive colour. */
export function Money({ value, className }: { value: MoneyValue | null | undefined; className?: string }) {
  const negative = (value?.amount ?? 0) < 0
  return (
    <span className={cn('tabular-nums', negative && 'text-destructive', className)}>
      {formatMoney(value)}
    </span>
  )
}
