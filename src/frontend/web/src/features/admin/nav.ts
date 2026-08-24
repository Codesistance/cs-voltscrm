import {
  Boxes,
  ChartNoAxesCombined,
  ClipboardList,
  CreditCard,
  FileUp,
  LayoutDashboard,
  Layers,
  Percent,
  Receipt,
  Repeat,
  ScrollText,
  Settings,
  ShieldCheck,
  UserCog,
  Users,
} from 'lucide-react'
import type { NavItem } from '@/features/layout/types'
import { PERMISSIONS } from '@/features/auth/permissions'

/** Administration area. Items are hidden unless the admin holds the declared permission. */
export const adminNav: NavItem[] = [
  { label: 'Dashboard', to: '/admin', icon: LayoutDashboard },
  { label: 'Customers', to: '/admin/customers', icon: Users, permission: PERMISSIONS.customersView },
  { label: 'Agents', to: '/admin/agents', icon: UserCog, permission: PERMISSIONS.agentsView },
  { label: 'Inventory', to: '/admin/inventory', icon: Boxes, permission: PERMISSIONS.inventoryView },
  { label: 'Service Plans', to: '/admin/service-plans', icon: ClipboardList, permission: PERMISSIONS.servicePlansView },
  { label: 'Subscriptions', to: '/admin/subscriptions', icon: Repeat, permission: PERMISSIONS.subscriptionsView },
  { label: 'Invoices', to: '/admin/invoices', icon: Receipt, permission: PERMISSIONS.invoicesView },
  { label: 'Installments', to: '/admin/installments', icon: Layers, permission: PERMISSIONS.invoicesView },
  { label: 'Payments', to: '/admin/payments', icon: CreditCard, permission: PERMISSIONS.paymentsView },
  { label: 'Discounts', to: '/admin/discounts', icon: Percent, permission: PERMISSIONS.discountsView },
  { label: 'Reports', to: '/admin/reports', icon: ChartNoAxesCombined, permission: PERMISSIONS.reportsView },
  { label: 'Import', to: '/admin/import', icon: FileUp, permission: PERMISSIONS.customersManage },
  { label: 'Settings', to: '/admin/settings/payment-gateways', icon: Settings, permission: PERMISSIONS.settingsManage },
  { label: 'Access Control', to: '/admin/access', icon: ShieldCheck, permission: PERMISSIONS.accessManage },
  { label: 'Audit Log', to: '/admin/audit', icon: ScrollText, superAdmin: true },
]
