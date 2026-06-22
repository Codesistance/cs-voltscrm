import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { CUSTOMER_STATUSES } from '../api/types'

export function CustomerFilters({
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
          placeholder="Search name, account or phone…"
          value={term}
          onChange={(e) => setTerm(e.target.value)}
          className="w-72"
        />
        <Button type="submit" variant="secondary">
          Search
        </Button>
      </form>
      <Select value={status} onChange={(e) => onStatus(e.target.value)} className="w-40">
        <option value="">All statuses</option>
        {CUSTOMER_STATUSES.map((s) => (
          <option key={s} value={s}>
            {s}
          </option>
        ))}
      </Select>
    </div>
  )
}
