import { z } from 'zod'
import { config } from '@/app/config'

const optionalInt = z.preprocess(
  (v) => (v === '' || v === null || v === undefined ? undefined : v),
  z.coerce.number().int().min(0).optional(),
)

export const inventoryItemSchema = z.object({
  itemCode: z.string().min(1, 'Item code is required').max(50),
  name: z.string().min(1, 'Name is required').max(200),
  description: z.string().max(1000).optional(),
  categoryId: z.string().min(1, 'Category is required'),
  unitOfMeasure: z.string().min(1, 'Unit is required').max(30),
  unitCost: z.object({
    amount: z.coerce.number().min(0, 'Must be 0 or more'),
    currency: z.string().length(3).default(config.defaultCurrency),
  }),
  quantityOnHand: optionalInt,
  reorderLevel: optionalInt,
})

export type InventoryItemFormValues = z.input<typeof inventoryItemSchema>
export type InventoryItemValues = z.output<typeof inventoryItemSchema>

export const stockMovementSchema = z.object({
  movementType: z.enum(['In', 'Out', 'Dispatched', 'Transfer', 'Adjustment']),
  quantity: z.coerce.number().int().refine((n) => n !== 0, 'Quantity must be non-zero'),
  reference: z.string().max(100).optional(),
  notes: z.string().max(500).optional(),
})

export type StockMovementFormValues = z.input<typeof stockMovementSchema>
export type StockMovementValues = z.output<typeof stockMovementSchema>
