import { z } from 'zod'

export const installmentPlanSchema = z.object({
  subscriptionId: z.string().min(1, 'Select a subscription'),
  totalAmount: z.coerce.number().positive('Total amount must be greater than zero'),
  depositAmount: z.coerce.number().min(0, 'Deposit cannot be negative'),
  installmentCount: z.coerce.number().int().min(1, 'At least one installment is required'),
  startDate: z.date({ message: 'Start date is required' }),
})

export type InstallmentPlanFormValues = z.input<typeof installmentPlanSchema>
export type InstallmentPlanValues = z.output<typeof installmentPlanSchema>
