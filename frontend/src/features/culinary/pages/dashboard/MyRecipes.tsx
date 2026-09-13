'use client'

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Eye, Pencil, PlusCircle, Search, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { ApiProblem } from '@/lib/api/problem-details'
import { deleteRecipe, listMyRecipes, type RecipeStatus } from '@/lib/api/content-client'

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
  published: 'bg-green-50 text-green-700 border-green-200',
  draft: 'bg-amber-50 text-amber-700 border-amber-200',
  archived: 'bg-secondary text-muted-foreground border-border',
}

export function MyRecipes() {
  const queryClient = useQueryClient()
  const [status, setStatus] = useState<RecipeStatus | undefined>()
  const [search, setSearch] = useState('')
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null)
  const recipesQuery = useQuery({
    queryKey: ['my-recipes', status],
    queryFn: () => listMyRecipes(status),
  })
  const removeRecipe = useMutation({
    mutationFn: ({ id, version }: { id: string; version: number }) => deleteRecipe(id, version),
    onSuccess: async () => {
      setConfirmDelete(null)
      await queryClient.invalidateQueries({ queryKey: ['my-recipes'] })
    },
  })

  const recipes = recipesQuery.data?.data ?? []
  const normalizedSearch = search.trim().toLocaleLowerCase('vi')
  const filtered = recipes.filter(
    (recipe) =>
      !normalizedSearch ||
      recipe.title.toLocaleLowerCase('vi').includes(normalizedSearch) ||
      recipe.category.name.toLocaleLowerCase('vi').includes(normalizedSearch),
  )
  const error = recipesQuery.error instanceof ApiProblem ? recipesQuery.error.problem.detail : null

  return (
    <div>
      <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="font-serif text-3xl text-foreground lg:text-4xl">Công thức của tôi</h1>
          <p className="mt-1 text-muted-foreground">
            {recipesQuery.data
              ? `Tổng cộng ${recipesQuery.data.meta.total} công thức`
              : 'Quản lý bản nháp của bạn'}
          </p>
        </div>
        <Link
          to="/dashboard/recipes/new"
          className="flex shrink-0 items-center gap-2 bg-primary px-5 py-3 text-sm uppercase tracking-widest text-primary-foreground transition-colors hover:bg-primary/90"
        >
          <PlusCircle size={16} /> Tạo công thức mới
        </Link>
      </div>

      <div className="mb-6 flex flex-col gap-3 sm:flex-row">
        <div className="relative max-w-sm flex-1">
          <Search
            className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-foreground"
            size={16}
          />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Tìm kiếm công thức…"
            className="w-full border border-border bg-background py-2.5 pl-10 pr-4 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div className="flex border border-border bg-background">
          {STATUS_FILTERS.map((item) => (
            <button
              key={item.label}
              onClick={() => setStatus(item.value)}
              className={`px-4 py-2.5 text-xs font-medium uppercase tracking-widest transition-colors ${status === item.value ? 'bg-foreground text-background' : 'text-muted-foreground hover:bg-secondary'}`}
            >
              {item.label}
            </button>
          ))}
        </div>
      </div>

      {recipesQuery.isPending && (
        <div className="border border-border p-10 text-center text-muted-foreground" role="status">
          Đang tải công thức…
        </div>
      )}
      {recipesQuery.isError && (
        <div className="border border-red-200 bg-red-50 p-6 text-red-700" role="alert">
          <p>{error ?? 'Không thể tải danh sách công thức.'}</p>
          <button className="mt-3 underline" onClick={() => recipesQuery.refetch()}>
            Thử lại
          </button>
        </div>
      )}
      {removeRecipe.error && (
        <p className="mb-4 border border-red-200 bg-red-50 p-3 text-sm text-red-700" role="alert">
          Không thể xóa công thức. Hãy tải lại dữ liệu và thử lại.
        </p>
      )}

      {recipesQuery.isSuccess && (
        <div className="overflow-hidden border border-border bg-background">
          {filtered.length === 0 ? (
            <div className="px-4 py-16 text-center">
              <p className="mb-2 font-serif text-xl">Không tìm thấy công thức nào</p>
              <p className="text-sm text-muted-foreground">Hãy đổi bộ lọc hoặc tạo bản nháp đầu tiên.</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="border-b border-border bg-secondary/30 text-left text-xs uppercase tracking-widest text-muted-foreground">
                  <tr>
                    <th className="px-6 py-3">Tiêu đề</th>
                    <th className="hidden px-6 py-3 sm:table-cell">Danh mục</th>
                    <th className="px-6 py-3">Trạng thái</th>
                    <th className="hidden px-6 py-3 text-right md:table-cell">Ngày tạo</th>
                    <th className="px-6 py-3">
                      <span className="sr-only">Thao tác</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map((recipe) => (
                    <tr
                      key={recipe.id}
                      className="border-b border-border last:border-0 hover:bg-secondary/30"
                    >
                      <td className="px-6 py-4 font-medium">{recipe.title}</td>
                      <td className="hidden px-6 py-4 text-muted-foreground sm:table-cell">
                        {recipe.category.name}
                      </td>
                      <td className="px-6 py-4">
                        <span className={`border px-2.5 py-1 text-xs ${STATUS_BADGE[recipe.status]}`}>
                          {STATUS_LABELS[recipe.status]}
                        </span>
                      </td>
                      <td className="hidden px-6 py-4 text-right text-muted-foreground md:table-cell">
                        {new Intl.DateTimeFormat('vi-VN').format(new Date(recipe.createdAt))}
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex items-center justify-end gap-2">
                          {recipe.status === 'published' && (
                            <Link to={`/recipes/${recipe.slug}`} aria-label="Xem công thức">
                              <Eye size={15} />
                            </Link>
                          )}
                          <Link to={`/dashboard/recipes/${recipe.id}/edit`} aria-label="Chỉnh sửa công thức">
                            <Pencil size={15} />
                          </Link>
                          {confirmDelete === recipe.id ? (
                            <span className="flex gap-2 text-xs">
                              <button
                                disabled={removeRecipe.isPending}
                                className="font-medium text-red-600"
                                onClick={() =>
                                  removeRecipe.mutate({ id: recipe.id, version: recipe.version })
                                }
                              >
                                Xóa
                              </button>
                              <button onClick={() => setConfirmDelete(null)}>Hủy</button>
                            </span>
                          ) : (
                            <button
                              onClick={() => setConfirmDelete(recipe.id)}
                              aria-label="Xóa công thức"
                              className="text-muted-foreground hover:text-red-600"
                            >
                              <Trash2 size={15} />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
