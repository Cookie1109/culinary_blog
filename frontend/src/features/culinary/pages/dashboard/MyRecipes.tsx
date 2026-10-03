'use client'

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Archive,
  ArrowLeft,
  ArrowRight,
  Eye,
  EyeOff,
  Globe,
  Pencil,
  PlusCircle,
  RotateCcw,
  Search,
  Trash2,
} from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { useAuth } from '../../contexts/AuthContext'
import { ApiProblem } from '@/lib/api/problem-details'
import {
  archiveRecipe,
  deleteRecipe,
  listAdminRecipes,
  listMyRecipes,
  publishRecipe,
  unarchiveRecipe,
  unpublishRecipe,
  type Recipe,
  type RecipeStatus,
} from '@/lib/api/content-client'

const STATUS_FILTERS: { value: RecipeStatus | undefined; label: string }[] = [
  { value: undefined, label: 'Tất cả' },
  { value: 'published', label: 'Đã xuất bản' },
  { value: 'draft', label: 'Bản nháp' },
  { value: 'archived', label: 'Đã lưu trữ' },
]

const STATUS_LABELS: Record<RecipeStatus, string> = {
  draft: 'Bản nháp',
  published: 'Đã xuất bản',
  archived: 'Đã lưu trữ',
}

const STATUS_BADGE: Record<RecipeStatus, string> = {
  published: 'bg-green-50 text-green-700 border border-green-200',
  draft: 'bg-amber-50 text-amber-700 border border-amber-200',
  archived: 'bg-secondary text-muted-foreground border border-border',
}

type ConfirmActionType = 'publish' | 'unpublish' | 'archive' | 'unarchive' | 'delete'

interface ConfirmDialogState {
  type: ConfirmActionType
  recipe: Recipe
}

export function MyRecipes() {
  const queryClient = useQueryClient()
  const [searchParams, setSearchParams] = useSearchParams()

  const { user } = useAuth()
  const isAdmin = user?.role === 'admin'
  const scopeParam = searchParams.get('scope')
  const [scope, setScope] = useState<'mine' | 'all'>(isAdmin && scopeParam === 'all' ? 'all' : 'mine')

  const initialStatus = searchParams.get('status') as RecipeStatus | null
  const [status, setStatus] = useState<RecipeStatus | undefined>(
    initialStatus === 'published' || initialStatus === 'draft' || initialStatus === 'archived'
      ? initialStatus
      : undefined,
  )
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 10

  const [confirmDialog, setConfirmDialog] = useState<ConfirmDialogState | null>(null)
  const [actionNotice, setActionNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  // Sync scope and status if URL query changes
  useEffect(() => {
    if (isAdmin && scopeParam === 'all') {
      setScope('all')
    } else if (scopeParam === 'mine') {
      setScope('mine')
    }
  }, [isAdmin, scopeParam])

  useEffect(() => {
    const urlStatus = searchParams.get('status') as RecipeStatus | null
    if (urlStatus === 'published' || urlStatus === 'draft' || urlStatus === 'archived') {
      setStatus(urlStatus)
    }
  }, [searchParams])

  const recipesQuery = useQuery({
    queryKey: [scope === 'all' ? 'admin-recipes' : 'my-recipes', status, page, pageSize],
    queryFn: () =>
      scope === 'all'
        ? listAdminRecipes(status, undefined, page, pageSize)
        : listMyRecipes(status, page, pageSize),
  })

  const removeRecipe = useMutation({
    mutationFn: ({ id, version }: { id: string; version: number }) => deleteRecipe(id, version),
    onSuccess: async () => {
      setConfirmDialog(null)
      setActionError(null)
      setActionNotice('Đã xóa công thức thành công.')
      await queryClient.invalidateQueries({ queryKey: ['my-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['admin-recipes'] })
    },
    onError: (error) => {
      setActionNotice(null)
      setActionError(
        error instanceof ApiProblem
          ? (error.problem.detail ?? error.problem.title)
          : 'Không thể xóa công thức.',
      )
    },
  })

  const publishMutation = useMutation({
    mutationFn: ({ id, version }: { id: string; version: number }) => publishRecipe(id, version),
    onSuccess: async () => {
      setConfirmDialog(null)
      setActionError(null)
      setActionNotice('Công thức đã được xuất bản thành công.')
      await queryClient.invalidateQueries({ queryKey: ['my-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['admin-recipes'] })
    },
    onError: (error) => {
      setActionNotice(null)
      if (error instanceof ApiProblem && error.problem.code === 'RECIPE_PUBLISH_INCOMPLETE') {
        setActionError('Công thức cần ít nhất 1 nguyên liệu và 1 bước thực hiện để xuất bản.')
      } else {
        setActionError(
          error instanceof ApiProblem
            ? (error.problem.detail ?? error.problem.title)
            : 'Không thể xuất bản công thức.',
        )
      }
    },
  })

  const unpublishMutation = useMutation({
    mutationFn: ({ id, version }: { id: string; version: number }) => unpublishRecipe(id, version),
    onSuccess: async () => {
      setConfirmDialog(null)
      setActionError(null)
      setActionNotice('Đã gỡ xuất bản công thức (chuyển về bản nháp).')
      await queryClient.invalidateQueries({ queryKey: ['my-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['admin-recipes'] })
    },
    onError: (error) => {
      setActionNotice(null)
      setActionError(
        error instanceof ApiProblem
          ? (error.problem.detail ?? error.problem.title)
          : 'Không thể gỡ xuất bản công thức.',
      )
    },
  })

  const archiveMutation = useMutation({
    mutationFn: ({ id, version }: { id: string; version: number }) => archiveRecipe(id, version),
    onSuccess: async () => {
      setConfirmDialog(null)
      setActionError(null)
      setActionNotice('Đã đưa công thức vào kho lưu trữ.')
      await queryClient.invalidateQueries({ queryKey: ['my-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['admin-recipes'] })
    },
    onError: (error) => {
      setActionNotice(null)
      setActionError(
        error instanceof ApiProblem
          ? (error.problem.detail ?? error.problem.title)
          : 'Không thể lưu trữ công thức.',
      )
    },
  })

  const unarchiveMutation = useMutation({
    mutationFn: ({ id, version }: { id: string; version: number }) => unarchiveRecipe(id, version),
    onSuccess: async () => {
      setConfirmDialog(null)
      setActionError(null)
      setActionNotice('Đã khôi phục công thức về trạng thái bản nháp.')
      await queryClient.invalidateQueries({ queryKey: ['my-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['admin-recipes'] })
    },
    onError: (error) => {
      setActionNotice(null)
      setActionError(
        error instanceof ApiProblem
          ? (error.problem.detail ?? error.problem.title)
          : 'Không thể khôi phục công thức.',
      )
    },
  })

  const isPendingAction =
    removeRecipe.isPending ||
    publishMutation.isPending ||
    unpublishMutation.isPending ||
    archiveMutation.isPending ||
    unarchiveMutation.isPending

  const handleConfirmAction = () => {
    if (!confirmDialog) return
    const { type, recipe } = confirmDialog
    if (type === 'delete') {
      removeRecipe.mutate({ id: recipe.id, version: recipe.version })
    } else if (type === 'publish') {
      publishMutation.mutate({ id: recipe.id, version: recipe.version })
    } else if (type === 'unpublish') {
      unpublishMutation.mutate({ id: recipe.id, version: recipe.version })
    } else if (type === 'archive') {
      archiveMutation.mutate({ id: recipe.id, version: recipe.version })
    } else if (type === 'unarchive') {
      unarchiveMutation.mutate({ id: recipe.id, version: recipe.version })
    }
  }

  const recipes = recipesQuery.data?.data ?? []
  const meta = recipesQuery.data?.meta
  const total = meta?.total ?? 0
  const totalPages = meta?.totalPages ?? 1

  const normalizedSearch = search.trim().toLocaleLowerCase('vi')
  const filtered = recipes.filter(
    (recipe) =>
      !normalizedSearch ||
      recipe.title.toLocaleLowerCase('vi').includes(normalizedSearch) ||
      recipe.category.name.toLocaleLowerCase('vi').includes(normalizedSearch) ||
      (scope === 'all' && recipe.author?.displayName?.toLocaleLowerCase('vi').includes(normalizedSearch)),
  )

  const handleScopeChange = (newScope: 'mine' | 'all') => {
    setScope(newScope)
    setPage(1)
    const nextParams = new URLSearchParams(searchParams)
    if (newScope === 'all') {
      nextParams.set('scope', 'all')
    } else {
      nextParams.delete('scope')
    }
    setSearchParams(nextParams)
  }

  const handleStatusChange = (newStatus: RecipeStatus | undefined) => {
    setStatus(newStatus)
    setPage(1)
    const nextParams = new URLSearchParams(searchParams)
    if (newStatus) {
      nextParams.set('status', newStatus)
    } else {
      nextParams.delete('status')
    }
    setSearchParams(nextParams)
  }

  return (
    <div>
      {/* Admin Scope Toggle Tabs */}
      {isAdmin && (
        <div
          className="mb-6 flex border-b border-border"
          role="tablist"
          aria-label="Phạm vi quản lý công thức"
        >
          <button
            type="button"
            role="tab"
            aria-selected={scope === 'mine'}
            onClick={() => handleScopeChange('mine')}
            className={`flex items-center gap-2 border-b-2 px-6 py-3 text-sm font-medium transition-colors ${
              scope === 'mine'
                ? 'border-primary text-primary font-semibold'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <span>Công thức của tôi</span>
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={scope === 'all'}
            onClick={() => handleScopeChange('all')}
            className={`flex items-center gap-2 border-b-2 px-6 py-3 text-sm font-medium transition-colors ${
              scope === 'all'
                ? 'border-primary text-primary font-semibold'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <span>Tất cả công thức (Chế độ Quản trị)</span>
            <span className="rounded-full bg-primary/10 px-2 py-0.5 text-xs text-primary font-mono">
              /admin/recipes
            </span>
          </button>
        </div>
      )}

      {/* Header */}
      <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="font-serif text-3xl text-foreground lg:text-4xl">
            {scope === 'all' ? 'Tất cả công thức (Quản trị hệ thống)' : 'Công thức của tôi'}
          </h1>
          <p className="mt-1 text-muted-foreground">
            {scope === 'all'
              ? recipesQuery.data
                ? `Tổng cộng ${total} công thức trên toàn hệ thống (dùng API /admin/recipes)`
                : 'Quản lý toàn bộ công thức trên hệ thống qua quyền quản trị'
              : recipesQuery.data
                ? `Tổng cộng ${total} công thức`
                : 'Quản lý các công thức và bản nháp của bạn'}
          </p>
        </div>
        <Link
          to="/dashboard/recipes/new"
          className="flex shrink-0 items-center gap-2 bg-primary px-5 py-3 text-sm uppercase tracking-widest text-primary-foreground transition-colors hover:bg-primary/90"
        >
          <PlusCircle size={16} /> Tạo công thức mới
        </Link>
      </div>

      {/* Filter and Search Bar */}
      <div className="mb-6 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="relative max-w-sm flex-1">
          <Search
            className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-foreground"
            size={16}
          />
          <input
            value={search}
            onChange={(event) => {
              setSearch(event.target.value)
            }}
            placeholder="Tìm kiếm theo tiêu đề, danh mục…"
            className="w-full border border-border bg-background py-2.5 pl-10 pr-4 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div className="flex flex-wrap border border-border bg-background">
          {STATUS_FILTERS.map((item) => (
            <button
              key={item.label}
              onClick={() => handleStatusChange(item.value)}
              className={`px-4 py-2.5 text-xs font-medium uppercase tracking-widest transition-colors ${
                status === item.value
                  ? 'bg-foreground text-background'
                  : 'text-muted-foreground hover:bg-secondary'
              }`}
            >
              {item.label}
            </button>
          ))}
        </div>
      </div>

      {/* Notifications */}
      {actionNotice && (
        <p className="mb-4 border border-green-200 bg-green-50 p-3 text-sm text-green-700" role="status">
          {actionNotice}
        </p>
      )}
      {actionError && (
        <p className="mb-4 border border-red-200 bg-red-50 p-3 text-sm text-red-700" role="alert">
          {actionError}
        </p>
      )}

      {/* Loading & Error States */}
      {recipesQuery.isPending && (
        <div className="border border-border p-12 text-center text-muted-foreground" role="status">
          Đang tải danh sách công thức…
        </div>
      )}
      {recipesQuery.isError && (
        <div className="border border-red-200 bg-red-50 p-6 text-red-700" role="alert">
          <p>
            {recipesQuery.error instanceof ApiProblem
              ? recipesQuery.error.problem.detail
              : 'Không thể tải danh sách công thức.'}
          </p>
          <button className="mt-3 underline" onClick={() => recipesQuery.refetch()}>
            Thử lại
          </button>
        </div>
      )}

      {/* Recipe List */}
      {recipesQuery.isSuccess && (
        <div className="space-y-6">
          {filtered.length === 0 ? (
            <div className="border border-border bg-background px-4 py-16 text-center">
              <p className="mb-2 font-serif text-xl">Không tìm thấy công thức nào</p>
              <p className="text-sm text-muted-foreground">
                {search
                  ? 'Không có công thức phù hợp với từ khóa tìm kiếm.'
                  : 'Hãy tạo bản nháp đầu tiên của bạn.'}
              </p>
            </div>
          ) : (
            <>
              {/* Desktop Table View */}
              <div className="hidden md:block overflow-hidden border border-border bg-background">
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead className="border-b border-border bg-secondary/30 text-left text-xs uppercase tracking-widest text-muted-foreground">
                      <tr>
                        <th className="px-6 py-3 font-medium">Tiêu đề</th>
                        {scope === 'all' && <th className="px-6 py-3 font-medium">Tác giả</th>}
                        <th className="px-6 py-3 font-medium">Danh mục</th>
                        <th className="px-6 py-3 font-medium">Trạng thái</th>
                        <th className="px-6 py-3 text-right font-medium">Ngày tạo</th>
                        <th className="px-6 py-3 text-right font-medium">Thao tác</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filtered.map((recipe) => (
                        <tr
                          key={recipe.id}
                          className="border-b border-border last:border-0 hover:bg-secondary/30 transition-colors"
                        >
                          <td className="px-6 py-4">
                            <Link
                              to={`/dashboard/recipes/${recipe.id}/edit`}
                              className="font-medium text-foreground hover:underline"
                            >
                              {recipe.title}
                            </Link>
                          </td>
                          {scope === 'all' && (
                            <td className="px-6 py-4 text-xs font-medium text-muted-foreground">
                              {recipe.author?.displayName ?? 'Ẩn danh'}
                            </td>
                          )}
                          <td className="px-6 py-4 text-muted-foreground">{recipe.category.name}</td>
                          <td className="px-6 py-4">
                            <span className={`px-2.5 py-1 text-xs ${STATUS_BADGE[recipe.status]}`}>
                              {STATUS_LABELS[recipe.status]}
                            </span>
                          </td>
                          <td className="px-6 py-4 text-right text-muted-foreground">
                            {new Intl.DateTimeFormat('vi-VN').format(new Date(recipe.createdAt))}
                          </td>
                          <td className="px-6 py-4 text-right">
                            <div className="flex items-center justify-end gap-3">
                              {/* Public view if published */}
                              {recipe.status === 'published' && (
                                <Link
                                  to={`/recipes/${recipe.slug}`}
                                  aria-label={`Xem công thức công khai ${recipe.title}`}
                                  title="Xem công khai"
                                  className="text-muted-foreground hover:text-foreground"
                                >
                                  <Eye size={16} />
                                </Link>
                              )}

                              {/* Preview link */}
                              <Link
                                to={`/dashboard/recipes/${recipe.id}/preview`}
                                aria-label={`Xem trước ${recipe.title}`}
                                title="Xem trước riêng tư"
                                className="text-xs uppercase tracking-wider text-muted-foreground hover:text-foreground"
                              >
                                Xem trước
                              </Link>

                              {/* Edit link - disabled/blocked for archived recipes */}
                              {recipe.status !== 'archived' && (
                                <Link
                                  to={`/dashboard/recipes/${recipe.id}/edit`}
                                  aria-label={`Chỉnh sửa ${recipe.title}`}
                                  title="Chỉnh sửa công thức"
                                  className="text-muted-foreground hover:text-primary"
                                >
                                  <Pencil size={16} />
                                </Link>
                              )}

                              {/* Publish (Draft -> Published) */}
                              {recipe.status === 'draft' && (
                                <button
                                  type="button"
                                  onClick={() => setConfirmDialog({ type: 'publish', recipe })}
                                  aria-label={`Xuất bản ${recipe.title}`}
                                  title="Xuất bản công thức"
                                  className="text-muted-foreground hover:text-green-600"
                                >
                                  <Globe size={16} />
                                </button>
                              )}

                              {/* Unpublish (Published -> Draft) */}
                              {recipe.status === 'published' && (
                                <button
                                  type="button"
                                  onClick={() => setConfirmDialog({ type: 'unpublish', recipe })}
                                  aria-label={`Gỡ xuất bản ${recipe.title}`}
                                  title="Gỡ xuất bản"
                                  className="text-muted-foreground hover:text-amber-600"
                                >
                                  <EyeOff size={16} />
                                </button>
                              )}

                              {/* Archive (Draft / Published -> Archived) */}
                              {recipe.status !== 'archived' && (
                                <button
                                  type="button"
                                  onClick={() => setConfirmDialog({ type: 'archive', recipe })}
                                  aria-label={`Lưu trữ ${recipe.title}`}
                                  title="Lưu trữ công thức"
                                  className="text-muted-foreground hover:text-foreground"
                                >
                                  <Archive size={16} />
                                </button>
                              )}

                              {/* Unarchive (Archived -> Draft) */}
                              {recipe.status === 'archived' && (
                                <button
                                  type="button"
                                  onClick={() => setConfirmDialog({ type: 'unarchive', recipe })}
                                  aria-label={`Khôi phục ${recipe.title}`}
                                  title="Khôi phục về bản nháp"
                                  className="flex items-center gap-1 text-xs text-primary hover:underline"
                                >
                                  <RotateCcw size={14} /> Khôi phục
                                </button>
                              )}

                              {/* Delete button */}
                              <button
                                type="button"
                                onClick={() => setConfirmDialog({ type: 'delete', recipe })}
                                aria-label={`Xóa ${recipe.title}`}
                                title="Xóa công thức"
                                className="text-muted-foreground hover:text-red-600"
                              >
                                <Trash2 size={16} />
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              {/* Mobile Card View */}
              <div className="grid gap-4 md:hidden">
                {filtered.map((recipe) => (
                  <article key={recipe.id} className="border border-border bg-background p-4 space-y-3">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <span className="text-xs uppercase tracking-widest text-muted-foreground">
                          {recipe.category.name}
                        </span>
                        <h2 className="font-serif text-lg font-medium text-foreground">{recipe.title}</h2>
                      </div>
                      <span className={`px-2 py-0.5 text-xs ${STATUS_BADGE[recipe.status]}`}>
                        {STATUS_LABELS[recipe.status]}
                      </span>
                    </div>

                    <div className="text-xs text-muted-foreground space-y-1">
                      {scope === 'all' && (
                        <div>
                          Tác giả:{' '}
                          <span className="font-medium text-foreground">
                            {recipe.author?.displayName ?? 'Ẩn danh'}
                          </span>
                        </div>
                      )}
                      <div>
                        Ngày tạo: {new Intl.DateTimeFormat('vi-VN').format(new Date(recipe.createdAt))}
                      </div>
                    </div>

                    <div className="flex flex-wrap items-center justify-between gap-2 border-t border-border pt-3">
                      <div className="flex items-center gap-3">
                        {recipe.status === 'published' && (
                          <Link
                            to={`/recipes/${recipe.slug}`}
                            className="text-xs underline text-muted-foreground hover:text-foreground"
                          >
                            Xem bài viết
                          </Link>
                        )}
                        <Link
                          to={`/dashboard/recipes/${recipe.id}/preview`}
                          className="text-xs underline text-muted-foreground hover:text-foreground"
                        >
                          Xem trước
                        </Link>
                      </div>

                      <div className="flex items-center gap-2">
                        {recipe.status !== 'archived' && (
                          <Link
                            to={`/dashboard/recipes/${recipe.id}/edit`}
                            className="border border-border px-3 py-1.5 text-xs uppercase tracking-wider hover:bg-secondary"
                          >
                            Sửa
                          </Link>
                        )}
                        {recipe.status === 'draft' && (
                          <button
                            type="button"
                            onClick={() => setConfirmDialog({ type: 'publish', recipe })}
                            className="border border-green-600 bg-green-50 px-3 py-1.5 text-xs uppercase tracking-wider text-green-700 hover:bg-green-100"
                          >
                            Xuất bản
                          </button>
                        )}
                        {recipe.status === 'published' && (
                          <button
                            type="button"
                            onClick={() => setConfirmDialog({ type: 'unpublish', recipe })}
                            className="border border-amber-600 bg-amber-50 px-3 py-1.5 text-xs uppercase tracking-wider text-amber-700 hover:bg-amber-100"
                          >
                            Gỡ
                          </button>
                        )}
                        {recipe.status === 'archived' && (
                          <button
                            type="button"
                            onClick={() => setConfirmDialog({ type: 'unarchive', recipe })}
                            className="border border-primary bg-primary px-3 py-1.5 text-xs uppercase tracking-wider text-primary-foreground hover:bg-primary/90"
                          >
                            Khôi phục
                          </button>
                        )}
                        <button
                          type="button"
                          onClick={() => setConfirmDialog({ type: 'delete', recipe })}
                          aria-label={`Xóa ${recipe.title}`}
                          className="p-1.5 text-muted-foreground hover:text-red-600"
                        >
                          <Trash2 size={16} />
                        </button>
                      </div>
                    </div>
                  </article>
                ))}
              </div>

              {/* Pagination Controls */}
              {totalPages > 1 && (
                <div className="flex flex-col items-center justify-between gap-4 border-t border-border pt-6 sm:flex-row">
                  <p className="text-xs text-muted-foreground">
                    Hiển thị {(page - 1) * pageSize + 1} - {Math.min(page * pageSize, total)} trong tổng số{' '}
                    {total} công thức
                  </p>
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      disabled={page <= 1}
                      onClick={() => setPage((current) => Math.max(1, current - 1))}
                      className="flex items-center gap-1 border border-border bg-background px-3 py-1.5 text-xs uppercase tracking-wider hover:bg-secondary disabled:opacity-40 disabled:hover:bg-background"
                    >
                      <ArrowLeft size={14} /> Trước
                    </button>
                    <span className="px-2 text-xs font-medium text-foreground">
                      Trang {page} / {totalPages}
                    </span>
                    <button
                      type="button"
                      disabled={page >= totalPages}
                      onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
                      className="flex items-center gap-1 border border-border bg-background px-3 py-1.5 text-xs uppercase tracking-wider hover:bg-secondary disabled:opacity-40 disabled:hover:bg-background"
                    >
                      Sau <ArrowRight size={14} />
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      )}

      {/* P7-04 Action Confirmation Modal */}
      {confirmDialog && (
        <div
          role="dialog"
          aria-modal="true"
          aria-labelledby="confirm-modal-title"
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
        >
          <div className="w-full max-w-md border border-border bg-background p-6 shadow-xl space-y-4">
            <h2 id="confirm-modal-title" className="font-serif text-2xl text-foreground">
              {confirmDialog.type === 'publish' && 'Xác nhận xuất bản'}
              {confirmDialog.type === 'unpublish' && 'Xác nhận gỡ xuất bản'}
              {confirmDialog.type === 'archive' && 'Xác nhận lưu trữ'}
              {confirmDialog.type === 'unarchive' && 'Xác nhận khôi phục'}
              {confirmDialog.type === 'delete' && 'Xác nhận xóa công thức'}
            </h2>

            <p className="text-sm text-muted-foreground leading-relaxed">
              {confirmDialog.type === 'publish' && (
                <>
                  Công thức <strong>«{confirmDialog.recipe.title}»</strong> sẽ được hiển thị công khai trên
                  trang web cho tất cả độc giả truy cập.
                </>
              )}
              {confirmDialog.type === 'unpublish' && (
                <>
                  Công thức <strong>«{confirmDialog.recipe.title}»</strong> sẽ được gỡ khỏi chế độ công khai
                  và chuyển về bản nháp.
                </>
              )}
              {confirmDialog.type === 'archive' && (
                <>
                  Công thức <strong>«{confirmDialog.recipe.title}»</strong> sẽ được chuyển vào kho lưu trữ và
                  không hiển thị công khai.
                </>
              )}
              {confirmDialog.type === 'unarchive' && (
                <>
                  Công thức <strong>«{confirmDialog.recipe.title}»</strong> sẽ được khôi phục về trạng thái
                  bản nháp để bạn có thể chỉnh sửa tiếp.
                </>
              )}
              {confirmDialog.type === 'delete' && (
                <>
                  Bạn có chắc chắn muốn xóa công thức <strong>«{confirmDialog.recipe.title}»</strong>? Thao
                  tác này không thể hoàn tác trực tiếp trên giao diện.
                </>
              )}
            </p>

            <div className="flex items-center justify-end gap-3 pt-3 border-t border-border">
              <button
                type="button"
                disabled={isPendingAction}
                onClick={() => setConfirmDialog(null)}
                className="border border-border px-4 py-2 text-xs uppercase tracking-wider text-muted-foreground hover:bg-secondary disabled:opacity-50"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                disabled={isPendingAction}
                onClick={handleConfirmAction}
                className={`px-5 py-2 text-xs uppercase tracking-wider text-white disabled:opacity-50 ${
                  confirmDialog.type === 'delete'
                    ? 'bg-red-600 hover:bg-red-700'
                    : confirmDialog.type === 'publish'
                      ? 'bg-green-700 hover:bg-green-800'
                      : 'bg-primary hover:bg-primary/90'
                }`}
              >
                {isPendingAction ? 'Đang xử lý…' : 'Xác nhận'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
