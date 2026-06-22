import type { Id, Money } from '@/shared/api/types'

export interface InventoryCategory {
  id: Id
  name: string
  code: string | null
  tracksStock: boolean
  isActive: boolean
  itemCount: number
}

export interface CreateInventoryCategory {
  name: string
  code?: string | null
  tracksStock: boolean
}

export interface UpdateInventoryCategory {
  name: string
  code?: string | null
  tracksStock: boolean
}

export const STOCK_MOVEMENT_TYPES = ['In', 'Out', 'Dispatched', 'Transfer', 'Adjustment'] as const
export type StockMovementType = (typeof STOCK_MOVEMENT_TYPES)[number]

export const UNIT_OF_MEASURE_OPTIONS = ['unit', 'box', 'metre', 'litre', 'kg', 'hour'] as const

export interface InventoryItemListItem {
  id: Id
  itemCode: string
  name: string
  categoryId: Id
  categoryName: string
  unitOfMeasure: string
  quantityOnHand: number | null
  reorderLevel: number | null
  unitCost: Money
  isLowStock: boolean
}

export interface InventoryItem extends InventoryItemListItem {
  categoryTracksStock: boolean
  description: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface StockMovement {
  id: Id
  inventoryItemId: Id
  movementType: string
  quantity: number
  reference: string | null
  notes: string | null
  movedByUserId: string | null
  createdAt: string
}

export interface CreateInventoryItem {
  itemCode: string
  name: string
  description?: string | null
  categoryId: Id
  unitOfMeasure: string
  unitCost: Money
  quantityOnHand?: number | null
  reorderLevel?: number | null
}

export interface UpdateInventoryItem {
  name: string
  description?: string | null
  unitCost: Money
  reorderLevel?: number | null
}

export interface RecordStockMovement {
  movementType: string
  quantity: number
  reference?: string | null
  notes?: string | null
}
