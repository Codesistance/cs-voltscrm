import { del, get, post, put } from '@/shared/api/http'

export interface PermissionDto {
  key: string
  group: string
  description: string
}

export interface AdminRoleDto {
  id: string
  name: string
  description?: string | null
  isSystem: boolean
  permissions: string[]
  userCount: number
}

export interface SaveAdminRoleRequest {
  name: string
  description?: string | null
  permissions: string[]
}

export interface AdminUserDto {
  id: string
  userId: string
  email: string
  fullName: string
  isSuperAdmin: boolean
  isActive: boolean
  roleIds: string[]
}

export interface CreateAdminRequest {
  email: string
  firstName: string
  lastName: string
  roleIds: string[]
  isSuperAdmin: boolean
  /** Omit to have the server generate a temporary password (returned once); set to choose one. */
  password?: string
}

export interface ResetPasswordResult {
  temporaryPassword: string | null
}

export interface CreateAdminResult {
  admin: AdminUserDto
  /** Present only when the password was auto-generated; shown once so it can be handed over. */
  temporaryPassword: string | null
}

export const accessApi = {
  permissions: () => get<PermissionDto[]>('/admin/access/permissions'),
  roles: () => get<AdminRoleDto[]>('/admin/access/roles'),
  createRole: (body: SaveAdminRoleRequest) => post<AdminRoleDto>('/admin/access/roles', body),
  updateRole: (id: string, body: SaveAdminRoleRequest) => put<void>(`/admin/access/roles/${id}`, body),
  deleteRole: (id: string) => del<void>(`/admin/access/roles/${id}`),
  admins: () => get<AdminUserDto[]>('/admin/access/admins'),
  createAdmin: (body: CreateAdminRequest) => post<CreateAdminResult>('/admin/access/admins', body),
  assignRoles: (id: string, roleIds: string[]) => put<void>(`/admin/access/admins/${id}/roles`, { roleIds }),
  resetPassword: (id: string, newPassword?: string) =>
    post<ResetPasswordResult>(`/admin/access/admins/${id}/reset-password`, { newPassword }),
  disableAdmin: (id: string) => post<void>(`/admin/access/admins/${id}/disable`, {}),
  enableAdmin: (id: string) => post<void>(`/admin/access/admins/${id}/enable`, {}),
}
