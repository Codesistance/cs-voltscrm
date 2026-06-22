import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'

export function MessageScreen({
  code,
  title,
  description,
}: {
  code: string
  title: string
  description?: string
}) {
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-3 text-center">
      <p className="text-4xl font-bold text-muted-foreground">{code}</p>
      <h1 className="text-xl font-semibold">{title}</h1>
      {description && <p className="text-sm text-muted-foreground">{description}</p>}
      <Button asChild variant="outline" size="sm">
        <Link to="/">Back to home</Link>
      </Button>
    </div>
  )
}
