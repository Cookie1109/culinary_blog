'use client'

import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { listAdminUsers, type UserRole } from '@/lib/api/admin-users-client'
import { useAuth } from '../../contexts/AuthContext'

const roleLabels: Record<string, string> = { Admin: 'Quản trị viên', Author: 'Tác giả' }
const PAGE_SIZE = 7
const dateFormat = new Intl.DateTimeFormat('vi-VN', { timeZone: 'Asia/Bangkok' })

export function UsersAdmin() {
  const { user } = useAuth()
  if (user?.role !== 'admin') {
    return <div role="alert">Chỉ quản trị viên có thể xem danh sách người dùng.</div>
  }
  return <UserDirectory userId={user.id} />
}

function UserDirectory({ userId }: { userId: string }) {
  const [search, setSearch] = useState('')
  const [filters, setFilters] = useState({
    search: '',
    role: '' as UserRole | '',
    page: 1,
    pageSize: PAGE_SIZE,
  })
  const usersQuery = useQuery({
    queryKey: ['admin-users', userId, filters],
    queryFn: () => listAdminUsers({ ...filters, role: filters.role || undefined }),
    gcTime: 0,
  })
  const users = usersQuery.data?.data ?? []
  const meta = usersQuery.data?.meta

  return (
    <div>
      <h1 className="font-serif text-3xl text-foreground lg:text-4xl">Danh sách người dùng</h1>
      <p className="mt-1 text-muted-foreground">Xem tài khoản quản trị viên và tác giả trong hệ thống.</p>

      <form
        className="my-6 flex flex-wrap items-end gap-3 border border-border bg-card p-4"
        onSubmit={(event) => {
          event.preventDefault()
          setFilters((current) => ({ ...current, search: search.trim(), page: 1 }))
        }}
      >
        <div className="min-w-0 flex-1 basis-60">
          <label
            htmlFor="user-search"
            className="mb-1 block text-xs uppercase tracking-widest text-muted-foreground"
          >
            Tìm tên hoặc email
          </label>
          <input
            id="user-search"
            type="search"
            maxLength={100}
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            className="min-h-11 w-full border border-border bg-background px-3 py-2 text-sm focus:border-primary focus:outline-none"
          />
        </div>
        <button
          type="submit"
          className="min-h-11 bg-primary px-5 py-2 text-sm uppercase tracking-widest text-primary-foreground hover:bg-primary/90"
        >
          Tìm kiếm
        </button>
        <div>
          <label
            htmlFor="user-role"
            className="mb-1 block text-xs uppercase tracking-widest text-muted-foreground"
          >
            Vai trò
          </label>
          <select
            id="user-role"
            value={filters.role}
            onChange={(event) =>
              setFilters((current) => ({ ...current, role: event.target.value as UserRole | '', page: 1 }))
            }
            className="min-h-11 border border-border bg-background px-3 py-2 text-sm focus:border-primary focus:outline-none"
          >
            <option value="">Tất cả vai trò</option>
            <option value="Admin">Quản trị viên</option>
            <option value="Author">Tác giả</option>
          </select>
        </div>
      </form>

      {usersQuery.isPending && <p role="status">Đang tải danh sách người dùng…</p>}
      {usersQuery.isError && (
        <div role="alert" className="border border-red-300 p-4">
          <p>Không thể tải danh sách người dùng. Hãy kiểm tra kết nối và quyền truy cập.</p>
          <button type="button" onClick={() => usersQuery.refetch()} className="mt-2 underline">
            Thử lại
          </button>
        </div>
      )}
      {usersQuery.isSuccess && (
        <>
          <p role="status" className="mb-3 text-sm text-muted-foreground">
            Tổng cộng {meta?.total} người dùng
          </p>
          {users.length === 0 ? (
            <p className="border border-border p-6">Không tìm thấy người dùng phù hợp.</p>
          ) : (
            <>
              <div className="hidden overflow-x-auto border border-border bg-background md:block">
                <table className="w-full text-left text-sm">
                  <caption className="sr-only">Tài khoản quản trị viên và tác giả</caption>
                  <thead className="border-b border-border bg-secondary/30 text-xs uppercase tracking-widest text-muted-foreground">
                    <tr>
                      {['Họ tên', 'Email', 'Vai trò', 'Trạng thái', 'Ngày tạo'].map((label) => (
                        <th key={label} scope="col" className="px-5 py-3 font-medium">
                          {label}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {users.map((user) => (
                      <tr
                        key={user.id}
                        className="border-b border-border transition-colors last:border-0 hover:bg-secondary/20"
                      >
                        <td className="break-words px-5 py-3 font-medium text-foreground">
                          {user.displayName}
                        </td>
                        <td className="break-all px-5 py-3 text-muted-foreground">{user.email ?? '—'}</td>
                        <td className="px-5 py-3">
                          {user.roles.map((role) => roleLabels[role] ?? role).join(', ') || '—'}
                        </td>
                        <td className="px-5 py-3">
                          <span
                            className={`inline-block border px-2.5 py-1 text-xs ${
                              user.isActive
                                ? 'border-emerald-200 bg-emerald-50 text-emerald-700'
                                : 'border-border bg-secondary text-muted-foreground'
                            }`}
                          >
                            {user.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động'}
                          </span>
                        </td>
                        <td className="whitespace-nowrap px-5 py-3 text-muted-foreground">
                          {dateFormat.format(new Date(user.createdAt))}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <ul className="space-y-3 md:hidden" aria-label="Tài khoản quản trị viên và tác giả">
                {users.map((user) => (
                  <li key={user.id} className="break-words border border-border p-4">
                    <p className="font-medium">{user.displayName}</p>
                    <p className="break-all text-sm">{user.email ?? '—'}</p>
                    <p className="mt-2 text-sm">
                      Vai trò: {user.roles.map((role) => roleLabels[role] ?? role).join(', ') || '—'}
                    </p>
                    <p className="text-sm">{user.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động'}</p>
                    <p className="text-sm">Ngày tạo: {dateFormat.format(new Date(user.createdAt))}</p>
                  </li>
                ))}
              </ul>
            </>
          )}
          <nav
            aria-label="Phân trang người dùng"
            className="mt-4 flex flex-wrap items-center justify-between gap-3 text-sm"
          >
            <button
              type="button"
              disabled={!meta?.hasPreviousPage}
              onClick={() => setFilters((current) => ({ ...current, page: current.page - 1 }))}
              className="min-h-11 border border-border px-4 py-2 text-xs uppercase tracking-widest hover:bg-secondary disabled:opacity-50"
            >
              Trang trước
            </button>
            <span>
              Trang {meta?.page} / {meta?.totalPages}
            </span>
            <button
              type="button"
              disabled={!meta?.hasNextPage}
              onClick={() => setFilters((current) => ({ ...current, page: current.page + 1 }))}
              className="min-h-11 border border-border px-4 py-2 text-xs uppercase tracking-widest hover:bg-secondary disabled:opacity-50"
            >
              Trang sau
            </button>
          </nav>
        </>
      )}
    </div>
  )
}
