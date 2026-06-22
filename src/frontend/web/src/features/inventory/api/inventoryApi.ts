import { get, post, put } from '@/shared/api/http'
import type { Id, ListParams, Paginated } from '@/shared/api/types'
import type {
  CreateInventoryCategory,
  CreateInventoryItem,
  InventoryCategory,
  InventoryItem,
  InventoryItemListItem,
  RecordStockMovement,
  StockMovement,
  UpdateInventoryCategory,
  UpdateInventoryItem,
} from './types'

const BASE = '/inventory'

export const inventoryApi = {
  list: (params: ListParams) => get<Paginated<InventoryItemListItem>>(BASE, { params }),
  detail: (id: Id) => get<InventoryItem>(`${BASE}/${id}`),
  create: (body: CreateInventoryItem) => post<InventoryItem>(BASE, body),
  update: (id: Id, body: UpdateInventoryItem) => put<InventoryItem>(`${BASE}/${id}`, body),
  deactivate: (id: Id) => post<void>(`${BASE}/${id}/deactivate`, {}),
  movements: (id: Id, params: ListParams) =>
    get<Paginated<StockMovement>>(`${BASE}/${id}/movements`, { params }),
  recordMovement: (id: Id, body: RecordStockMovement) =>
    post<StockMovement>(`${BASE}/${id}/stock-movements`, body),

  categories: () => get<InventoryCategory[]>(`${BASE}/categories`),
  createCategory: (body: CreateInventoryCategory) => post<InventoryCategory>(`${BASE}/categories`, body),
  updateCategory: (id: Id, body: UpdateInventoryCategory) =>
    put<InventoryCategory>(`${BASE}/categories/${id}`, body),
  deactivateCategory: (id: Id) => post<void>(`${BASE}/categories/${id}/deactivate`, {}),
}
