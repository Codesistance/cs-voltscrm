import { Hammer } from 'lucide-react'
import { EmptyState } from './EmptyState'

/** Lightweight stand-in for area routes that are wired up but not yet implemented. */
export function PlaceholderPage({ title, description }: { title: string; description?: string }) {
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <EmptyState
        icon={<Hammer className="size-8" />}
        title="Coming soon"
        description={description ?? 'This area is under construction.'}
      />
    </div>
  )
}
