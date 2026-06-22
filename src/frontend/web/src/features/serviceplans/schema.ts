import { z } from 'zod'
import { config } from '@/app/config'

export const servicePlanSchema = z.object({
  planCode: z.string().min(1, 'Plan code is required').max(30),
  name: z.string().min(1, 'Name is required').max(200),
  description: z.string().max(1000).optional(),
  billingType: z.enum(['Prepaid', 'Postpaid', 'Installment', 'Subscription']),
  billingCycle: z.enum(['OneTime', 'Monthly', 'Quarterly', 'Annual']),
  basePrice: z.object({
    amount: z.coerce.number().min(0, 'Must be 0 or more'),
    currency: z.string().length(3).default(config.defaultCurrency),
  }),
  lineItems: z.array(
    z.object({
      inventoryItemId: z.string().min(1, 'Select an item'),
      quantity: z.coerce.number().int().min(1, 'Min 1'),
      role: z.enum(['Core', 'Optional', 'AddOn']),
    }),
  ),
})

export type ServicePlanFormValues = z.input<typeof servicePlanSchema>
export type ServicePlanValues = z.output<typeof servicePlanSchema>
