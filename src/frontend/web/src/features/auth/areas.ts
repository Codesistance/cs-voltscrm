import type { UserType } from './api/authApi'

/** Each user type owns a distinct, top-level UI area. */
export const AREA_HOME: Record<UserType, string> = {
  Administration: '/admin',
  Agent: '/agent',
  Customer: '/portal',
}

/** The landing path for a user, based on their type. */
export function homePathFor(userType: UserType | undefined): string {
  return userType ? AREA_HOME[userType] : '/login'
}
