/**
 * Frontend mirror of the backend permission keys (VoltsCRM.Application.Authorization.Permissions).
 * Used for nav visibility and route guards. The server is always authoritative — these only keep the
 * UI honest. Keep in sync when adding permissions on the backend.
 */
export const PERMISSIONS = {
  accessManage: 'access.manage',
  customersView: 'customers.view',
  customersManage: 'customers.manage',
  agentsView: 'agents.view',
  agentsManage: 'agents.manage',
  inventoryView: 'inventory.view',
  inventoryManage: 'inventory.manage',
  inventoryCategoriesManage: 'inventory.categories.manage',
  servicePlansView: 'serviceplans.view',
  servicePlansManage: 'serviceplans.manage',
  subscriptionsView: 'subscriptions.view',
  subscriptionsManage: 'subscriptions.manage',
  invoicesView: 'invoices.view',
  invoicesManage: 'invoices.manage',
  paymentsView: 'payments.view',
  paymentsRecord: 'payments.record',
  discountsView: 'discounts.view',
  discountsManage: 'discounts.manage',
  reportsView: 'reports.view',
  settingsManage: 'settings.manage',
} as const

export type PermissionKey = (typeof PERMISSIONS)[keyof typeof PERMISSIONS]
