import type { LucideIcon } from 'lucide-react'

export interface NavItem {
  label: string
  to: string
  icon: LucideIcon
  /** Admin areas only: hide this item unless the user holds this permission. Undefined = always shown. */
  permission?: string
  /** Hide this item unless the user is a super admin. */
  superAdmin?: boolean
}
