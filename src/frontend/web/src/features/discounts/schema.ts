import { z } from 'zod'
import { DISCOUNT_SCOPES, DISCOUNT_TYPES } from './api/types'

export const grantDiscountSchema = z.object({
  customerId: z.string().min(1, 'Select a customer'),
  discountType: z.enum(DISCOUNT_TYPES),
  value: z.coerce.number().positive('Value must be greater than zero'),
  scope: z.enum(DISCOUNT_SCOPES),
  targetId: z.string().optional(),
  isRecurring: z.boolean(),
  validFrom: z.string().optional(),
  validUntil: z.string().optional(),
  reason: z.string().trim().max(500).optional(),
})

export type GrantDiscountFormValues = z.input<typeof grantDiscountSchema>
export type GrantDiscountValues = z.output<typeof grantDiscountSchema>
