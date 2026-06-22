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
  roleIds: string[]
}

export interface CreateAdminRequest {
  email: string
  firstName: string
  lastName: string
  roleIds: string[]
  isSuperAdmin: boolean
}

export const accessApi = {
  permissions: () => get<PermissionDto[]>('/admin/access/permissions'),
  roles: () => get<AdminRoleDto[]>('/admin/access/roles'),
  createRole: (body: SaveAdminRoleRequest) => post<AdminRoleDto>('/admin/access/roles', body),
  updateRole: (id: string, body: SaveAdminRoleRequest) => put<void>(`/admin/access/roles/${id}`, body),
  deleteRole: (id: string) => del<void>(`/admin/access/roles/${id}`),
  admins: () => get<AdminUserDto[]>('/admin/access/admins'),
  createAdmin: (body: CreateAdminRequest) => post<AdminUserDto>('/admin/access/admins', body),
  assignRoles: (id: string, roleIds: string[]) => put<void>(`/admin/access/admins/${id}/roles`, { roleIds }),
}
