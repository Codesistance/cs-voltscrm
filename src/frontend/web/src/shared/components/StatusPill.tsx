import { Badge } from '@/components/ui/badge'
import { toneFor, type StatusDomain } from '@/shared/lib/enums'

export function StatusPill({ domain, status }: { domain: StatusDomain; status: string }) {
  return <Badge variant={toneFor(domain, status)}>{status}</Badge>
}
