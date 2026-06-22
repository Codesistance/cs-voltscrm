import { LayoutDashboard, MapPin, Users } from 'lucide-react'
import type { NavItem } from '@/features/layout/types'

/** Agent area. Fixed feature set — no dynamic permissions. */
export const agentNav: NavItem[] = [
  { label: 'Home', to: '/agent', icon: LayoutDashboard },
  { label: 'My Customers', to: '/agent/customers', icon: Users },
  { label: 'Route Map', to: '/agent/map', icon: MapPin },
]
