'use client'

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, CheckCircle2, Pencil, Plus, Trash2 } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useAuth } from '@/features/culinary/contexts/AuthContext'
import { Modal } from '@/components/ui/modal'
import { ApiProblem } from '@/lib/api/problem-details'
import {
  createCategory,
  deleteCategory,
  listCategories,
  updateCategory,
  type Category,
} from '@/lib/api/content-client'

interface CategoryForm {
  name: string
  description: string
  imageUrl: string
  orderIndex: number
}

const EMPTY_FORM: CategoryForm = { name: '', description: '', imageUrl: '', orderIndex: 0 }

export function CategoryAdmin() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Category | null>(null)
  const [form, setForm] = useState<CategoryForm>(EMPTY_FORM)
  const [deleteTarget, setDeleteTarget] = useState<Category | null>(null)
  const [actionNotice, setActionNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const categories = useQuery({
    queryKey: ['categories'],
    queryFn: listCategories,
    enabled: user?.role === 'admin',
  })

  const save = useMutation({
    mutationFn: () => {
      const payload = {
        name: form.name.trim(),
        description: form.description.trim() || null,
        imageUrl: form.imageUrl.trim() || null,
        orderIndex: form.orderIndex,
      }
      return editing ? updateCategory(editing.id, payload) : createCategory(payload)
    },
    onSuccess: async () => {
      const isEditing = Boolean(editing)
      setEditing(null)
      setFormOpen(false)
      setForm(EMPTY_FORM)
      setActionError(null)
      setActionNotice(isEditing ? 'Đã cập nhật danh mục thành công.' : 'Đã thêm danh mục mới thành công.')
      await queryClient.invalidateQueries({ queryKey: ['categories'] })
    },
    onError: (error) => {
      setActionNotice(null)
      setActionError(
        error instanceof ApiProblem
          ? (error.problem.detail ?? error.problem.title)
          : 'Không thể lưu danh mục. Vui lòng thử lại.',
      )
    },
  })

  const remove = useMutation({
    mutationFn: (id: string) => deleteCategory(id),
    onSuccess: async () => {
      setDeleteTarget(null)
      setActionError(null)
      setActionNotice('Đã xóa danh mục thành công.')
      await queryClient.invalidateQueries({ queryKey: ['categories'] })
    },
    onError: (error) => {
      setActionNotice(null)
      if (error instanceof ApiProblem && error.problem.code === 'CATEGORY_DELETE_HAS_RECIPES') {
        setActionError('Xung đột dữ liệu: Danh mục này đang chứa công thức nên không thể xóa.')
      } else {
        setActionError(
          error instanceof ApiProblem
            ? (error.problem.detail ?? error.problem.title)
            : 'Không thể xóa danh mục.',
        )
      }
    },
  })

  if (user?.role !== 'admin') {
    return (
      <div role="alert" className="border border-amber-300 bg-amber-50 p-6 text-amber-800">
        Chỉ quản trị viên có thể quản lý danh mục.
      </div>
    )
  }

  const beginEdit = (category: Category) => {
    setFormOpen(true)
    setEditing(category)
    setForm({
      name: category.name,
      description: category.description ?? '',
      imageUrl: category.imageUrl ?? '',
      orderIndex: category.orderIndex,
    })
    setActionNotice(null)
    setActionError(null)
  }

  const cancelEdit = () => {
    setFormOpen(false)
    setActionError(null)
    setEditing(null)
    setForm(EMPTY_FORM)
  }

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setActionNotice(null)
    setActionError(null)
    save.mutate()
  }

  // Sort categories by orderIndex ascending, then name
  const sortedCategories = categories.data
    ? [...categories.data].sort((a, b) => a.orderIndex - b.orderIndex || a.name.localeCompare(b.name))
    : []

  return (
    <div>
      <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="font-serif text-3xl lg:text-4xl">Quản lý danh mục</h1>
          <p className="mt-1 text-muted-foreground">
            Sắp xếp và quản lý danh mục công thức trên toàn hệ thống.
          </p>
        </div>
        <button
          type="button"
          onClick={() => {
            cancelEdit()
            setFormOpen(true)
          }}
          className="inline-flex shrink-0 items-center justify-center gap-2 bg-primary px-5 py-3 text-sm uppercase tracking-widest text-primary-foreground hover:bg-primary/90"
        >
          <Plus size={16} aria-hidden="true" /> Thêm danh mục
        </button>
      </div>

      {actionNotice && (
        <div
          role="status"
          className="mb-6 flex items-center gap-3 border border-green-200 bg-green-50 p-4 text-sm text-green-800"
        >
          <CheckCircle2 size={18} className="text-green-600 shrink-0" />
          <span>{actionNotice}</span>
        </div>
      )}

      {actionError && !formOpen && !deleteTarget && (
        <div
          role="alert"
          className="mb-6 flex items-center gap-3 border border-red-200 bg-red-50 p-4 text-sm text-red-800"
        >
          <AlertCircle size={18} className="text-red-600 shrink-0" />
          <span>{actionError}</span>
        </div>
      )}

      {formOpen && (
        <Modal
          open
          title={editing ? 'Chỉnh sửa danh mục' : 'Thêm danh mục'}
          onClose={cancelEdit}
          busy={save.isPending}
          size="lg"
        >
          <form onSubmit={submit}>
            {actionError && (
              <p role="alert" className="mb-4 border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                {actionError}
              </p>
            )}
            <fieldset disabled={save.isPending}>
              <div className="grid gap-4 md:grid-cols-2">
                <label className="text-xs uppercase tracking-widest text-muted-foreground">
                  Tên danh mục <span className="text-red-500">*</span>
                  <input
                    required
                    minLength={2}
                    maxLength={100}
                    value={form.name}
                    onChange={(event) => setForm({ ...form, name: event.target.value })}
                    className="mt-2 w-full border border-border bg-background px-4 py-3 text-sm focus:border-primary focus:outline-none"
                    placeholder="VD: Món chay, Tráng miệng..."
                  />
                </label>
                <label className="text-xs uppercase tracking-widest text-muted-foreground">
                  Thứ tự hiển thị <span className="text-red-500">*</span>
                  <input
                    required
                    type="number"
                    min={0}
                    value={form.orderIndex}
                    onChange={(event) => setForm({ ...form, orderIndex: Number(event.target.value) })}
                    className="mt-2 w-full border border-border bg-background px-4 py-3 text-sm focus:border-primary focus:outline-none"
                    placeholder="0, 1, 2..."
                  />
                </label>
                <label className="text-xs uppercase tracking-widest text-muted-foreground md:col-span-2">
                  Mô tả
                  <textarea
                    maxLength={2000}
                    rows={3}
                    value={form.description}
                    onChange={(event) => setForm({ ...form, description: event.target.value })}
                    className="mt-2 w-full resize-y border border-border bg-background px-4 py-3 text-sm focus:border-primary focus:outline-none"
                    placeholder="Mô tả tóm tắt về danh mục này..."
                  />
                </label>
                <label className="text-xs uppercase tracking-widest text-muted-foreground md:col-span-2">
                  URL hình ảnh
                  <input
                    type="url"
                    maxLength={500}
                    value={form.imageUrl}
                    onChange={(event) => setForm({ ...form, imageUrl: event.target.value })}
                    className="mt-2 w-full border border-border bg-background px-4 py-3 text-sm focus:border-primary focus:outline-none"
                    placeholder="https://example.com/images/category.jpg"
                  />
                </label>
              </div>
            </fieldset>
            <div className="mt-5 flex justify-end gap-3 border-t border-border pt-4">
              <button
                type="button"
                onClick={cancelEdit}
                disabled={save.isPending}
                className="border border-border px-5 py-2.5 text-sm uppercase tracking-widest hover:bg-secondary transition-colors"
              >
                Hủy
              </button>
              <button
                disabled={save.isPending}
                className="flex items-center gap-2 bg-primary px-5 py-2.5 text-sm uppercase tracking-widest text-primary-foreground transition-opacity hover:opacity-95 disabled:opacity-60"
              >
                {editing ? <Pencil size={15} /> : <Plus size={15} />}{' '}
                {save.isPending ? 'Đang lưu…' : editing ? 'Lưu thay đổi' : 'Thêm danh mục'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {categories.isPending && (
        <div role="status" className="p-10 text-center text-muted-foreground">
          Đang tải danh mục…
        </div>
      )}

      {categories.isError && (
        <div role="alert" className="border border-red-200 bg-red-50 p-5 text-red-700">
          Không thể tải danh mục.{' '}
          <button className="underline" onClick={() => categories.refetch()}>
            Thử lại
          </button>
        </div>
      )}

      {categories.isSuccess && (
        <div className="grid gap-3 md:hidden">
          {sortedCategories.length === 0 && (
            <p className="border border-border p-6 text-sm text-muted-foreground">
              Chưa có danh mục nào. Chọn “Thêm danh mục” để bắt đầu.
            </p>
          )}
          {sortedCategories.map((category) => (
            <article key={category.id} className="border border-border bg-background p-4">
              <div className="flex items-start justify-between gap-3">
                <h2 className="font-serif text-xl">{category.name}</h2>
                <span className="text-sm text-muted-foreground">#{category.orderIndex}</span>
              </div>
              <p className="mt-2 line-clamp-2 text-sm text-muted-foreground">{category.description}</p>
              <div className="mt-4 flex items-center justify-between gap-3 border-t border-border pt-3">
                <span className="text-sm text-muted-foreground">{category.recipeCount} công thức</span>
                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={() => beginEdit(category)}
                    aria-label={`Sửa ${category.name}`}
                    className="inline-flex min-h-11 items-center gap-2 border border-border px-3 text-sm hover:bg-secondary"
                  >
                    <Pencil size={14} aria-hidden="true" /> Sửa
                  </button>
                  <button
                    type="button"
                    onClick={() => setDeleteTarget(category)}
                    aria-label={`Xóa ${category.name}`}
                    className="inline-flex min-h-11 items-center gap-2 border border-border px-3 text-sm text-red-700 hover:bg-red-50"
                  >
                    <Trash2 size={14} aria-hidden="true" /> Xóa
                  </button>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
      {categories.isSuccess && (
        <div className="hidden overflow-x-auto border border-border bg-background md:block">
          <table className="w-full text-sm">
            <thead className="border-b border-border bg-secondary/30 text-left text-xs uppercase tracking-widest text-muted-foreground">
              <tr>
                <th className="px-5 py-3 w-20">Thứ tự</th>
                <th className="px-5 py-3">Tên danh mục</th>
                <th className="px-5 py-3">Slug</th>
                <th className="px-5 py-3 text-right">Số công thức</th>
                <th className="px-5 py-3 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {sortedCategories.length === 0 ? (
                <tr>
                  <td colSpan={5} className="p-8 text-center text-muted-foreground">
                    Chưa có danh mục nào. Chọn “Thêm danh mục” để bắt đầu.
                  </td>
                </tr>
              ) : (
                sortedCategories.map((category) => (
                  <tr
                    key={category.id}
                    className="border-b border-border last:border-0 hover:bg-secondary/20 transition-colors"
                  >
                    <td className="px-5 py-4 font-mono font-medium text-muted-foreground">
                      #{category.orderIndex}
                    </td>
                    <td className="px-5 py-4 font-medium text-foreground">
                      <div>{category.name}</div>
                      {category.description && (
                        <div className="text-xs text-muted-foreground mt-0.5 line-clamp-1">
                          {category.description}
                        </div>
                      )}
                    </td>
                    <td className="px-5 py-4 font-mono text-xs text-muted-foreground">{category.slug}</td>
                    <td className="px-5 py-4 text-right">
                      {category.recipeCount > 0 ? (
                        <span className="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-medium rounded-full bg-blue-50 text-blue-700 border border-blue-200">
                          {category.recipeCount} công thức
                        </span>
                      ) : (
                        <span className="text-muted-foreground text-xs">0 công thức</span>
                      )}
                    </td>
                    <td className="px-5 py-4 text-right">
                      <div className="flex justify-end gap-2">
                        <button
                          onClick={() => beginEdit(category)}
                          aria-label={`Sửa ${category.name}`}
                          className="inline-grid h-11 w-11 place-items-center text-muted-foreground hover:text-primary transition-colors"
                          title="Chỉnh sửa danh mục"
                        >
                          <Pencil size={15} />
                        </button>
                        <button
                          className="inline-grid h-11 w-11 place-items-center text-muted-foreground hover:text-red-600 transition-colors"
                          onClick={() => setDeleteTarget(category)}
                          aria-label={`Xóa ${category.name}`}
                          title={category.recipeCount > 0 ? 'Danh mục có công thức liên kết' : 'Xóa danh mục'}
                        >
                          <Trash2 size={15} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Accessible Delete Confirmation Modal Dialog */}
      {deleteTarget && (
        <Modal
          open
          title={deleteTarget.recipeCount > 0 ? 'Không thể xóa danh mục' : 'Xác nhận xóa danh mục'}
          onClose={() => setDeleteTarget(null)}
          busy={remove.isPending}
        >
          {actionError && (
            <p role="alert" className="border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              {actionError}
            </p>
          )}
          <div id="confirm-delete-desc" className="mt-4 text-sm">
            {deleteTarget.recipeCount > 0 ? (
              <div className="border border-amber-200 bg-amber-50 p-4 text-amber-900">
                <p className="font-medium">Không thể xóa danh mục &ldquo;{deleteTarget.name}&rdquo;!</p>
                <p className="mt-1 text-xs text-amber-800 leading-relaxed">
                  Danh mục này hiện đang có <strong>{deleteTarget.recipeCount} công thức liên kết</strong>. Để
                  đảm bảo tính toàn vẹn dữ liệu, bạn cần chuyển các công thức sang danh mục khác hoặc xóa
                  chúng trước khi xóa danh mục này.
                </p>
              </div>
            ) : (
              <div className="text-muted-foreground leading-relaxed">
                <p>
                  Bạn có chắc chắn muốn xóa danh mục{' '}
                  <strong className="text-foreground">&ldquo;{deleteTarget.name}&rdquo;</strong>?
                </p>
                <p className="mt-2 text-xs text-muted-foreground">
                  Thao tác này sẽ đánh dấu xóa mềm danh mục và ẩn khỏi hệ thống công khai.
                </p>
              </div>
            )}
          </div>

          <div className="mt-6 flex justify-end gap-3">
            <button
              type="button"
              onClick={() => setDeleteTarget(null)}
              disabled={remove.isPending}
              className="border border-border px-4 py-2 text-xs uppercase tracking-widest text-foreground hover:bg-secondary transition-colors"
            >
              {deleteTarget.recipeCount > 0 ? 'Đã hiểu' : 'Hủy'}
            </button>
            {deleteTarget.recipeCount === 0 && (
              <button
                type="button"
                disabled={remove.isPending}
                onClick={() => remove.mutate(deleteTarget.id)}
                className="bg-red-600 px-4 py-2 text-xs uppercase tracking-widest text-white transition-opacity hover:opacity-90 disabled:opacity-50"
              >
                {remove.isPending ? 'Đang xóa…' : 'Xác nhận xóa'}
              </button>
            )}
          </div>
        </Modal>
      )}
    </div>
  )
}
