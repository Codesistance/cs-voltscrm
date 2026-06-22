import type { ImportRowInput } from './types'

/** Map CSV rows + column mapping to the backend ImportRowInput shape. */
export function mapImportRows(
  rows: Record<string, string>[],
  mapping: Record<string, string>,
): ImportRowInput[] {
  return rows.map((row) => {
    const mapped: Record<string, string> = {}
    for (const [header, field] of Object.entries(mapping)) {
      if (field && row[header] !== undefined) mapped[field] = row[header]
    }

    const amountRaw = mapped.amount?.trim()
    const amount =
      amountRaw && amountRaw.length > 0 && !Number.isNaN(Number(amountRaw)) ? Number(amountRaw) : null

    return {
      accountNumber: mapped.accountNumber?.trim() || null,
      firstName: mapped.firstName?.trim() || null,
      lastName: mapped.lastName?.trim() || null,
      phone: mapped.phone?.trim() || null,
      email: mapped.email?.trim() || null,
      city: mapped.city?.trim() || null,
      amount,
    }
  })
}
