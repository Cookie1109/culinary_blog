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
import { Modal } from '@/components/ui/modal'
import { useToast } from '@/components/ui/toast'
import { ApiProblem, apiErrorMessage } from '@/lib/api/problem-details'
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
import { useAuth } from '../../contexts/AuthContext'

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
  published: 'bg-emerald-50 text-emerald-700',
  draft: 'bg-amber-50 text-amber-700',
  archived: 'bg-secondary text-muted-foreground',
}

type ConfirmActionType = 'publish' | 'unpublish' | 'archive' | 'unarchive' | 'delete'

interface ConfirmDialogState {
  type: ConfirmActionType
  recipe: Recipe
}

export function MyRecipes({ systemOverview = false }: { systemOverview?: boolean }) {
  const queryClient = useQueryClient()
  const [searchParams, setSearchParams] = useSearchParams()

  const { user } = useAuth()
  const isAdmin = user?.role === 'admin'
  const scopeParam = searchParams.get('scope')
  const scope = isAdmin && (systemOverview || scopeParam === 'all') ? 'all' : 'mine'
  const urlStatus = searchParams.get('status')
  const status: RecipeStatus | undefined =
    urlStatus === 'published' || urlStatus === 'draft' || urlStatus === 'archived' ? urlStatus : undefined
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 10

  useEffect(() => setPage(1), [scope, status])

  const [confirmDialog, setConfirmDialog] = useState<ConfirmDialogState | null>(null)
  const [actionNotice, setActionNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const { showToast } = useToast()

  useEffect(() => {
    const message = actionError ?? actionNotice
    if (message) showToast(message)
  }, [actionError, actionNotice, showToast])

  const recipesQuery = useQuery({
    queryKey: [scope === 'all' ? 'admin-recipes' : 'my-recipes', status, search.trim(), page, pageSize],
    queryFn: () =>
      scope === 'all'
        ? listAdminRecipes(status, undefined, page, pageSize, search)
        : listMyRecipes(status, page, pageSize, search),
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
      setActionError(apiErrorMessage(error, 'RECIPE_DELETE_FAILED'))
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
        setActionError(apiErrorMessage(error))
      } else {
        setActionError(apiErrorMessage(error, 'RECIPE_PUBLISH_FAILED'))
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
      setActionError(apiErrorMessage(error, 'RECIPE_UNPUBLISH_FAILED'))
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
      setActionError(apiErrorMessage(error, 'RECIPE_ARCHIVE_FAILED'))
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
      setActionError(apiErrorMessage(error, 'RECIPE_UNARCHIVE_FAILED'))
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

  const originSuffix = scope === 'all' ? '?from=system' : ''
  const Heading = systemOverview ? 'h2' : 'h1'
  const recipes = recipesQuery.data?.data ?? []
  const meta = recipesQuery.data?.meta
  const total = meta?.total ?? 0
  const totalPages = meta?.totalPages ?? 1

  useEffect(() => {
    if (recipesQuery.isSuccess && page > Math.max(1, totalPages)) setPage(Math.max(1, totalPages))
  }, [recipesQuery.isSuccess, page, totalPages])

  const handleStatusChange = (newStatus: RecipeStatus | undefined) => {
    setPage(1)
    const nextParams = new URLSearchParams(searchParams)
    if (newStatus) {
      nextParams.set('status', newStatus)
    } else {
      nextParams.delete('status')
    }
    setSearchParams(nextParams)
  }

  const totalDescription =
    scope === 'all' ? `Tổng cộng ${total} công thức trên toàn hệ thống` : `Tổng cộng ${total} công thức`
  const emptyDescription =
    scope === 'all'
      ? 'Quản lý toàn bộ công thức trên hệ thống qua quyền quản trị'
      : 'Quản lý các công thức và bản nháp của bạn'
  const emptyScopeMessage =
    scope === 'all' ? 'Chưa có công thức nào trong phạm vi này.' : 'Hãy tạo bản nháp đầu tiên của bạn.'
  const confirmationColor =
    confirmDialog?.type === 'publish' ? 'bg-green-700 hover:bg-green-800' : 'bg-primary hover:bg-primary/90'

  return (
    <div data-managed-recipes={!systemOverview || undefined}>
      {/* Header */}
      <div className="mb-7 flex flex-col justify-between gap-5 border-b border-border pb-7 sm:flex-row sm:items-center">
        <div>
          <Heading
            className={`font-serif text-foreground ${systemOverview ? 'text-2xl' : 'text-3xl lg:text-4xl'}`}
          >
            {scope === 'all' ? 'Tất cả công thức' : 'Công thức của tôi'}
          </Heading>
          <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
            {recipesQuery.data ? totalDescription : emptyDescription}
          </p>
        </div>
        {!systemOverview && (
          <Link
            to="/dashboard/recipes/new"
            className="inline-flex min-h-11 shrink-0 items-center justify-center gap-2 bg-primary px-5 py-3 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
          >
            <PlusCircle size={16} /> Tạo công thức mới
          </Link>
        )}
      </div>

      {/* Filter and Search Bar */}
      <div className="mb-5 flex flex-col gap-4 border border-border bg-card p-4 xl:flex-row xl:items-center">
        <div className="relative min-w-0 flex-1">
          <Search
            className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-foreground"
            size={16}
          />
          <input
            value={search}
            onChange={(event) => {
              setSearch(event.target.value)
              setPage(1)
            }}
            type="search"
            maxLength={100}
            aria-label="Tìm kiếm công thức trong phạm vi đang chọn"
            placeholder={
              scope === 'all' ? 'Tìm theo tiêu đề, danh mục, tác giả…' : 'Tìm theo tiêu đề, danh mục…'
            }
            className="min-h-11 w-full border border-border bg-background py-2.5 pl-10 pr-4 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div
          role="group"
          aria-label="Lọc trạng thái công thức"
          className="grid shrink-0 grid-cols-2 gap-1 border border-border bg-background p-1 sm:grid-cols-4"
        >
          {STATUS_FILTERS.map((item) => (
            <button
              type="button"
              key={item.label}
              aria-pressed={status === item.value}
              onClick={() => handleStatusChange(item.value)}
              className={`min-h-11 whitespace-nowrap px-3 py-2 text-sm font-medium transition-colors ${
                status === item.value
                  ? 'bg-primary text-primary-foreground'
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
          <p>{apiErrorMessage(recipesQuery.error, 'RECIPE_LIST_FAILED')}</p>
          <button type="button" className="mt-3 underline" onClick={() => recipesQuery.refetch()}>
            Thử lại
          </button>
        </div>
      )}

      {/* Recipe List */}
      {recipesQuery.isSuccess && (
        <div className="space-y-6">
          {recipes.length === 0 ? (
            <div className="border border-border bg-background px-4 py-16 text-center">
              <p className="mb-2 font-serif text-xl">Không tìm thấy công thức nào</p>
              <p className="text-sm text-muted-foreground">
                {search ? 'Không có công thức phù hợp với từ khóa tìm kiếm.' : emptyScopeMessage}
              </p>
              {(search || status) && (
                <button
                  type="button"
                  className="mt-4 text-sm text-primary underline"
                  onClick={() => {
                    setSearch('')
                    handleStatusChange(undefined)
                  }}
                >
                  Xóa tìm kiếm và bộ lọc
                </button>
              )}
            </div>
          ) : (
            <>
              {/* Desktop Table View */}
              <div className="hidden overflow-hidden border border-border bg-card md:block">
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead className="border-b border-border bg-background/60 text-left text-xs tracking-wide text-muted-foreground">
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
                      {recipes.map((recipe) => (
                        <tr
                          key={recipe.id}
                          className="border-b border-border transition-colors last:border-0 hover:bg-background/70"
                        >
                          <td className="px-6 py-5">
                            <div className="flex min-w-52 max-w-sm items-center gap-3">
                              <span
                                aria-hidden="true"
                                className="flex size-11 shrink-0 items-center justify-center border border-border/60 bg-background font-serif text-xl text-primary"
                              >
                                {Array.from(recipe.title.trim())[0]?.toLocaleUpperCase('vi-VN')}
                              </span>
                              <Link
                                to={`/dashboard/recipes/${recipe.id}/preview${originSuffix}`}
                                className="min-w-0 break-words font-medium text-foreground hover:text-primary"
                              >
                                {recipe.title}
                              </Link>
                            </div>
                          </td>
                          {scope === 'all' && (
                            <td className="px-6 py-4 text-xs font-medium text-muted-foreground">
                              {recipe.author?.displayName ?? 'Ẩn danh'}
                            </td>
                          )}
                          <td className="px-6 py-4 text-muted-foreground">{recipe.category.name}</td>
                          <td className="px-6 py-4">
                            <span
                              className={`inline-flex min-w-28 items-center justify-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-1.5 text-xs ${STATUS_BADGE[recipe.status]}`}
                            >
                              <span aria-hidden="true" className="size-1.5 rounded-full bg-current" />
                              {STATUS_LABELS[recipe.status]}
                            </span>
                          </td>
                          <td className="whitespace-nowrap px-6 py-4 text-right text-muted-foreground">
                            {new Intl.DateTimeFormat('vi-VN').format(new Date(recipe.createdAt))}
                          </td>
                          <td className="px-6 py-4 text-right">
                            <div className="flex items-center justify-end gap-1">
                              {/* Preview link */}
                              <Link
                                to={`/dashboard/recipes/${recipe.id}/preview${originSuffix}`}
                                aria-label={`Xem trước ${recipe.title}`}
                                title="Xem trước riêng tư"
                                className="flex size-11 items-center justify-center rounded-sm text-muted-foreground transition-colors hover:bg-secondary/60 hover:text-primary"
                              >
                                <Eye size={17} aria-hidden="true" />
                              </Link>

                              {/* Edit link - disabled/blocked for archived recipes */}
                              {recipe.status !== 'archived' && (
                                <Link
                                  to={`/dashboard/recipes/${recipe.id}/edit${originSuffix}`}
                                  aria-label={`Chỉnh sửa ${recipe.title}`}
                                  title="Chỉnh sửa công thức"
                                  className="flex size-11 items-center justify-center rounded-sm text-muted-foreground transition-colors hover:bg-secondary/60 hover:text-primary"
                                >
                                  <Pencil size={16} />
                                </Link>
                              )}

                              {/* Publish (Draft -> Published) */}
                              {recipe.status === 'draft' && (
                                <button
                                  type="button"
                                  onClick={() => {
                                    setActionError(null)
                                    setConfirmDialog({ type: 'publish', recipe })
                                  }}
                                  aria-label={`Xuất bản ${recipe.title}`}
                                  title="Xuất bản công thức"
                                  className="flex size-11 items-center justify-center rounded-sm text-muted-foreground transition-colors hover:bg-secondary/60 hover:text-green-600"
                                >
                                  <Globe size={16} />
                                </button>
                              )}

                              {/* Unpublish (Published -> Draft) */}
                              {recipe.status === 'published' && (
                                <button
                                  type="button"
                                  onClick={() => {
                                    setActionError(null)
                                    setConfirmDialog({ type: 'unpublish', recipe })
                                  }}
                                  aria-label={`Gỡ xuất bản ${recipe.title}`}
                                  title="Gỡ xuất bản"
                                  className="flex size-11 items-center justify-center rounded-sm text-muted-foreground transition-colors hover:bg-secondary/60 hover:text-amber-600"
                                >
                                  <EyeOff size={16} />
                                </button>
                              )}

                              {/* Archive (Draft / Published -> Archived) */}
                              {recipe.status !== 'archived' && (
                                <button
                                  type="button"
                                  onClick={() => {
                                    setActionError(null)
                                    setConfirmDialog({ type: 'archive', recipe })
                                  }}
                                  aria-label={`Lưu trữ ${recipe.title}`}
                                  title="Lưu trữ công thức"
                                  className="flex size-11 items-center justify-center rounded-sm text-muted-foreground transition-colors hover:bg-secondary/60 hover:text-foreground"
                                >
                                  <Archive size={16} />
                                </button>
                              )}

                              {/* Unarchive (Archived -> Draft) */}
                              {recipe.status === 'archived' && (
                                <button
                                  type="button"
                                  onClick={() => {
                                    setActionError(null)
                                    setConfirmDialog({ type: 'unarchive', recipe })
                                  }}
                                  aria-label={`Khôi phục ${recipe.title}`}
                                  title="Khôi phục về bản nháp"
                                  className="inline-flex min-h-11 items-center gap-1.5 whitespace-nowrap rounded-sm px-3 text-xs font-medium text-primary transition-colors hover:bg-primary/10"
                                >
                                  <RotateCcw size={14} /> Khôi phục
                                </button>
                              )}

                              {/* Delete button */}
                              <button
                                type="button"
                                onClick={() => {
                                  setActionError(null)
                                  setConfirmDialog({ type: 'delete', recipe })
                                }}
                                aria-label={`Xóa ${recipe.title}`}
                                title="Xóa công thức"
                                className="flex size-11 items-center justify-center rounded-sm text-muted-foreground transition-colors hover:bg-secondary/60 hover:text-red-600"
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
                {recipes.map((recipe) => (
                  <article key={recipe.id} className="space-y-4 border border-border bg-card p-4">
                    <div className="flex items-start justify-between gap-2">
                      <div className="min-w-0 flex-1">
                        <span className="text-xs uppercase tracking-widest text-muted-foreground">
                          {recipe.category.name}
                        </span>
                        <h2 className="break-words font-serif text-lg font-medium text-foreground">
                          {recipe.title}
                        </h2>
                      </div>
                      <span
                        className={`shrink-0 whitespace-nowrap rounded-full px-2.5 py-1 text-xs ${STATUS_BADGE[recipe.status]}`}
                      >
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
                        <Link
                          to={`/dashboard/recipes/${recipe.id}/preview${originSuffix}`}
                          className="inline-flex min-h-11 items-center text-sm text-muted-foreground underline hover:text-foreground"
                        >
                          Xem trước
                        </Link>
                      </div>

                      <div className="flex flex-wrap items-center gap-1">
                        {recipe.status !== 'archived' && (
                          <Link
                            to={`/dashboard/recipes/${recipe.id}/edit${originSuffix}`}
                            className="min-h-11 border border-border px-3 py-2 text-xs uppercase tracking-wider hover:bg-secondary"
                          >
                            Sửa
                          </Link>
                        )}
                        {recipe.status === 'draft' && (
                          <button
                            type="button"
                            onClick={() => {
                              setActionError(null)
                              setConfirmDialog({ type: 'publish', recipe })
                            }}
                            className="min-h-11 border border-green-600 bg-green-50 px-3 py-2 text-xs uppercase tracking-wider text-green-700 hover:bg-green-100"
                          >
                            Xuất bản
                          </button>
                        )}
                        {recipe.status === 'published' && (
                          <button
                            type="button"
                            onClick={() => {
                              setActionError(null)
                              setConfirmDialog({ type: 'unpublish', recipe })
                            }}
                            className="min-h-11 border border-amber-600 bg-amber-50 px-3 py-2 text-xs uppercase tracking-wider text-amber-700 hover:bg-amber-100"
                          >
                            Gỡ
                          </button>
                        )}
                        {recipe.status !== 'archived' && (
                          <button
                            type="button"
                            onClick={() => {
                              setActionError(null)
                              setConfirmDialog({ type: 'archive', recipe })
                            }}
                            className="min-h-11 border border-border px-3 py-2 text-xs uppercase tracking-wider hover:bg-secondary"
                          >
                            Lưu trữ
                          </button>
                        )}
                        {recipe.status === 'archived' && (
                          <button
                            type="button"
                            onClick={() => {
                              setActionError(null)
                              setConfirmDialog({ type: 'unarchive', recipe })
                            }}
                            className="min-h-11 border border-primary bg-primary px-3 py-2 text-xs uppercase tracking-wider text-primary-foreground hover:bg-primary/90"
                          >
                            Khôi phục
                          </button>
                        )}
                        <button
                          type="button"
                          onClick={() => {
                            setActionError(null)
                            setConfirmDialog({ type: 'delete', recipe })
                          }}
                          aria-label={`Xóa ${recipe.title}`}
                          className="flex size-11 items-center justify-center text-muted-foreground hover:bg-red-50 hover:text-red-600"
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
                  <div className="flex flex-wrap items-center gap-2">
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
        <Modal
          open
          title={
            {
              publish: 'Xác nhận xuất bản',
              unpublish: 'Xác nhận gỡ xuất bản',
              archive: 'Xác nhận lưu trữ',
              unarchive: 'Xác nhận khôi phục',
              delete: 'Xác nhận xóa công thức',
            }[confirmDialog.type]
          }
          onClose={() => setConfirmDialog(null)}
          busy={isPendingAction}
        >
          <div className="space-y-4">
            {actionError && (
              <p role="alert" className="border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                {actionError}
              </p>
            )}

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

            <div className="flex items-center justify-end gap-1 pt-3 border-t border-border">
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
                  confirmDialog.type === 'delete' ? 'bg-red-600 hover:bg-red-700' : confirmationColor
                }`}
              >
                {isPendingAction ? 'Đang xử lý…' : 'Xác nhận'}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}
