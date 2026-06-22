import { z } from 'zod'
import { config } from '@/app/config'

const moneySchema = z.object({
  amount: z.coerce.number().min(0, 'Amount must be zero or greater'),
  currency: z.string().length(3).default(config.defaultCurrency),
})

export const subscriptionSchema = z.object({
  customerId: z.string().min(1, 'Select a customer'),
  servicePlanId: z.string().min(1, 'Select a service plan'),
  startDate: z.date({ message: 'Start date is required' }),
  useNegotiatedPrice: z.boolean().default(false),
  negotiatedPrice: moneySchema.optional(),
  serviceLocationId: z.string().optional(),
})

export type SubscriptionFormValues = z.input<typeof subscriptionSchema>
export type SubscriptionValues = z.output<typeof subscriptionSchema>

export const deployedItemSchema = z.object({
  inventoryItemId: z.string().min(1, 'Select an inventory item'),
  quantity: z.coerce.number().int().min(1, 'Quantity must be at least 1'),
  serialNumber: z.string().max(100).optional(),
})

export type DeployedItemFormValues = z.input<typeof deployedItemSchema>
export type DeployedItemValues = z.output<typeof deployedItemSchema>
