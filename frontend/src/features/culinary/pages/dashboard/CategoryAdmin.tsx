'use client'

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Pencil, Plus, Trash2, X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useAuth } from '@/features/culinary/contexts/AuthContext'
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
  const [editing, setEditing] = useState<Category | null>(null)
  const [form, setForm] = useState<CategoryForm>(EMPTY_FORM)
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null)
  const categories = useQuery({ queryKey: ['categories'], queryFn: listCategories })
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
      setEditing(null)
      setForm(EMPTY_FORM)
      await queryClient.invalidateQueries({ queryKey: ['categories'] })
    },
  })
  const remove = useMutation({
    mutationFn: deleteCategory,
    onSuccess: async () => {
      setConfirmDelete(null)
      await queryClient.invalidateQueries({ queryKey: ['categories'] })
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
    setEditing(category)
    setForm({
      name: category.name,
      description: category.description ?? '',
      imageUrl: category.imageUrl ?? '',
      orderIndex: category.orderIndex,
    })
  }
  const cancelEdit = () => {
    setEditing(null)
    setForm(EMPTY_FORM)
  }
  const submit = (event: FormEvent) => {
    event.preventDefault()
    save.mutate()
  }
  const mutationError = save.error ?? remove.error
  const problem = mutationError instanceof ApiProblem ? mutationError.problem : null

  return (
    <div className="max-w-5xl">
      <div className="mb-8">
        <h1 className="font-serif text-3xl lg:text-4xl">Quản lý danh mục</h1>
        <p className="mt-1 text-muted-foreground">Slug được giữ ổn định khi đổi tên danh mục.</p>
      </div>

      <form onSubmit={submit} className="mb-8 border border-border bg-background p-6">
        <div className="mb-5 flex items-center justify-between">
          <h2 className="font-serif text-xl">{editing ? 'Chỉnh sửa danh mục' : 'Thêm danh mục'}</h2>
          {editing && (
            <button type="button" onClick={cancelEdit} aria-label="Hủy chỉnh sửa">
              <X size={18} />
            </button>
          )}
        </div>
        <div className="grid gap-4 md:grid-cols-2">
          <label className="text-xs uppercase tracking-widest text-muted-foreground">
            Tên
            <input
              required
              minLength={2}
              maxLength={100}
              value={form.name}
              onChange={(event) => setForm({ ...form, name: event.target.value })}
              className="mt-2 w-full border border-border bg-background px-4 py-3 text-sm focus:border-primary focus:outline-none"
            />
          </label>
          <label className="text-xs uppercase tracking-widest text-muted-foreground">
            Thứ tự
            <input
              required
              type="number"
              min={0}
              value={form.orderIndex}
              onChange={(event) => setForm({ ...form, orderIndex: Number(event.target.value) })}
              className="mt-2 w-full border border-border bg-background px-4 py-3 text-sm focus:border-primary focus:outline-none"
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
            />
          </label>
        </div>
        <button
          disabled={save.isPending}
          className="mt-5 flex items-center gap-2 bg-primary px-5 py-2.5 text-sm uppercase tracking-widest text-primary-foreground disabled:opacity-60"
        >
          {editing ? <Pencil size={15} /> : <Plus size={15} />}{' '}
          {save.isPending ? 'Đang lưu…' : editing ? 'Lưu thay đổi' : 'Thêm danh mục'}
        </button>
      </form>

      {problem && (
        <p role="alert" className="mb-5 border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          {problem.detail ?? 'Không thể thực hiện thao tác.'}
        </p>
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
        <div className="overflow-x-auto border border-border bg-background">
          <table className="w-full text-sm">
            <thead className="border-b border-border bg-secondary/30 text-left text-xs uppercase tracking-widest text-muted-foreground">
              <tr>
                <th className="px-5 py-3">Tên</th>
                <th className="px-5 py-3">Slug</th>
                <th className="px-5 py-3 text-right">Công thức đã xuất bản</th>
                <th className="px-5 py-3">
                  <span className="sr-only">Thao tác</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {categories.data.map((category) => (
                <tr key={category.id} className="border-b border-border last:border-0">
                  <td className="px-5 py-4 font-medium">{category.name}</td>
                  <td className="px-5 py-4 text-muted-foreground">{category.slug}</td>
                  <td className="px-5 py-4 text-right">{category.recipeCount}</td>
                  <td className="px-5 py-4">
                    <div className="flex justify-end gap-3">
                      <button onClick={() => beginEdit(category)} aria-label={`Sửa ${category.name}`}>
                        <Pencil size={15} />
                      </button>
                      {confirmDelete === category.id ? (
                        <span className="flex gap-2 text-xs">
                          <button
                            className="font-medium text-red-600"
                            disabled={remove.isPending}
                            onClick={() => remove.mutate(category.id)}
                          >
                            Xác nhận
                          </button>
                          <button onClick={() => setConfirmDelete(null)}>Hủy</button>
                        </span>
                      ) : (
                        <button
                          className="text-muted-foreground hover:text-red-600"
                          onClick={() => setConfirmDelete(category.id)}
                          aria-label={`Xóa ${category.name}`}
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
  )
}
