import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { useInventoryCategories } from '../api/queries'

export function InventoryFilters({
  q,
  category,
  onSearch,
  onCategory,
}: {
  q: string
  category: string
  onSearch: (value: string) => void
  onCategory: (value: string) => void
}) {
  const [term, setTerm] = useState(q)
  const { data: categories } = useInventoryCategories()

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
      <Select value={category} onChange={(e) => onCategory(e.target.value)} className="w-44">
        <option value="">All categories</option>
        {categories?.map((c) => (
          <option key={c.id} value={c.id}>
            {c.name}
          </option>
        ))}
      </Select>
    </div>
  )
}
