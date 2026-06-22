import { CreditCard, LayoutDashboard, Receipt, Repeat, UserRound } from 'lucide-react'
import type { NavItem } from '@/features/layout/types'

/** Customer self-service portal. Fixed feature set — no dynamic permissions. */
export const portalNav: NavItem[] = [
  { label: 'Overview', to: '/portal', icon: LayoutDashboard },
  { label: 'My Services', to: '/portal/services', icon: Repeat },
  { label: 'Invoices', to: '/portal/invoices', icon: Receipt },
  { label: 'Payments', to: '/portal/payments', icon: CreditCard },
  { label: 'Profile', to: '/portal/profile', icon: UserRound },
]
