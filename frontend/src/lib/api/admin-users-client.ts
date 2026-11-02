import { authenticatedApiRequest } from './auth-client'
import type { components, operations } from './admin-users-schema'

export type AdminUser = components['schemas']['AdminUserDto']
export type UserRole = 'Admin' | 'Author'

export function listAdminUsers({
  search,
  role,
  page = 1,
  pageSize = 20,
}: {
  search?: string
  role?: UserRole
  page?: number
  pageSize?: number
}) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (search?.trim()) params.set('search', search.trim())
  if (role) params.set('role', role)
  return authenticatedApiRequest<
    operations['ListAdminUsers']['responses'][200]['content']['application/json']
  >(`/admin/users?${params}`, { cache: 'no-store' })
}
