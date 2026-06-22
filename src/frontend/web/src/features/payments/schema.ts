import { z } from 'zod'
import { config } from '@/app/config'
import { PAYMENT_CHANNELS, PAYMENT_METHODS } from './api/types'

const allocationRowSchema = z.object({
  targetType: z.enum(['invoice', 'installment']),
  targetId: z.string().min(1, 'Select a target'),
  amount: z.coerce.number().positive('Allocation amount must be greater than zero'),
})

export const paymentSchema = z
  .object({
    customerId: z.string().min(1, 'Select a customer'),
    amount: z.coerce.number().positive('Amount must be greater than zero'),
    currency: z.string().length(3).default(config.defaultCurrency),
    method: z.enum(PAYMENT_METHODS),
    channel: z.enum(PAYMENT_CHANNELS),
    paymentDate: z.date({ message: 'Payment date is required' }),
    platformProvider: z.string().max(100).optional(),
    platformReference: z.string().max(100).optional(),
    completeImmediately: z.boolean().default(true),
    allocations: z.array(allocationRowSchema).default([]),
  })
  .refine(
    (data) => data.allocations.reduce((sum, row) => sum + row.amount, 0) <= data.amount,
    { message: 'Total allocations cannot exceed payment amount', path: ['allocations'] },
  )

export type PaymentFormValues = z.input<typeof paymentSchema>
export type PaymentValues = z.output<typeof paymentSchema>
