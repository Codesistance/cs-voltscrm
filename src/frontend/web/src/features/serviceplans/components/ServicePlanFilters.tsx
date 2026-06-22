import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { PLAN_STATUSES } from '../api/types'

export function ServicePlanFilters({
  q,
  status,
  onSearch,
  onStatus,
}: {
  q: string
  status: string
  onSearch: (value: string) => void
  onStatus: (value: string) => void
}) {
  const [term, setTerm] = useState(q)

  return (
    <div className="flex flex-wrap items-center gap-2">
      <form
        className="flex items-center gap-2"
        onSubmit={(e) => {
          e.preventDefault()
          onSearch(term)
        }}
      >
        <Input
          placeholder="Search code or name…"
          value={term}
          onChange={(e) => setTerm(e.target.value)}
          className="w-64"
        />
        <Button type="submit" variant="secondary">
          Search
        </Button>
      </form>
      <Select value={status} onChange={(e) => onStatus(e.target.value)} className="w-40">
        <option value="">All statuses</option>
        {PLAN_STATUSES.map((s) => (
          <option key={s} value={s}>
            {s}
          </option>
        ))}
      </Select>
    </div>
  )
}
