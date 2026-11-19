'use client'

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertCircle,
  BookOpen,
  CheckCircle2,
  FolderOpen,
  ImageOff,
  Layers,
  Pencil,
  Plus,
  Search,
  Trash2,
} from 'lucide-react'
import Image from 'next/image'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { FieldError } from '@/components/ui/field-error'
import { useToast } from '@/components/ui/toast'
import { useAuth } from '@/features/culinary/contexts/AuthContext'
import { Modal } from '@/components/ui/modal'
import { publicMediaUrl } from '@/lib/media-url'
import { ApiProblem, apiErrorMessage } from '@/lib/api/problem-details'
import {
  createCategory,
  deleteCategory,
  listCategories,
  updateCategory,
  type Category,
} from '@/lib/api/content-client'
import { CategoryImageField } from './CategoryImageField'
import { formMessages } from '../../forms/messages'
import { setServerFieldErrors } from '../../forms/server-errors'
import type { CategoryValues } from '../../forms/content-schemas'

const EMPTY_FORM: CategoryValues = { name: '', description: '', imageUrl: '', orderIndex: 0 }

function CategoryThumb({ category }: { category: Category }) {
  const [failed, setFailed] = useState(false)
  if (!category.imageUrl || failed) {
    return (
      <div className="grid h-full w-full place-items-center text-muted-foreground" aria-hidden="true">
        <ImageOff size={18} strokeWidth={1.5} />
      </div>
    )
  }
  return (
    <Image
      src={publicMediaUrl(category.imageUrl)}
      alt=""
      fill
      sizes="80px"
      loading="lazy"
      onError={() => setFailed(true)}
      className="object-cover"
    />
  )
}

export function CategoryAdmin() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Category | null>(null)
  const {
    register,
    handleSubmit,
    reset: resetForm,
    setError: setFieldError,
    setValue,
    control,
    formState: { errors },
  } = useForm<CategoryValues>({
    resolver: async (...args) => (await import('../../forms/deferred-resolvers')).categoryResolver(...args),
    defaultValues: EMPTY_FORM,
  })
  const imageUrl = useWatch({ control, name: 'imageUrl' })
  const { showToast } = useToast()
  const [deleteTarget, setDeleteTarget] = useState<Category | null>(null)
  const [actionNotice, setActionNotice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [query, setQuery] = useState('')
  const [imageUploading, setImageUploading] = useState(false)

  const categories = useQuery({
    queryKey: ['categories'],
    queryFn: listCategories,
    enabled: user?.role === 'admin',
  })

  const save = useMutation({
    mutationFn: (form: CategoryValues) => {
      const payload = {
        name: form.name.trim(),
        description: form.description.trim() || null,
        imageUrl: form.imageUrl.trim() || null,
        orderIndex: editing
          ? editing.orderIndex
          : Math.max(
              -1,
              ...(queryClient.getQueryData<Category[]>(['categories']) ?? []).map((c) => c.orderIndex),
            ) + 1,
      }
      return editing ? updateCategory(editing.id, payload) : createCategory(payload)
    },
    onMutate: async (form) => {
      if (!editing) return undefined
      await queryClient.cancelQueries({ queryKey: ['categories'] })
      const previous = queryClient.getQueryData<Category[]>(['categories'])
      queryClient.setQueryData<Category[]>(['categories'], (current) =>
        current?.map((category) => (category.id === editing.id ? { ...category, ...form } : category)),
      )
      return { previous }
    },
    onSuccess: async () => {
      const isEditing = Boolean(editing)
      setEditing(null)
      setFormOpen(false)
      resetForm(EMPTY_FORM)
      setActionError(null)
      const message = isEditing ? 'Đã cập nhật danh mục thành công.' : 'Đã thêm danh mục mới thành công.'
      setActionNotice(message)
      showToast(message)
      await queryClient.invalidateQueries({ queryKey: ['categories'] })
    },
    onError: (error, _form, context) => {
      showToast(apiErrorMessage(error, 'CATEGORY_SAVE_FAILED'))
      if (context?.previous) queryClient.setQueryData(['categories'], context.previous)
      setServerFieldErrors(error, setFieldError, {
        Name: ['name', formMessages.categoryName],
        Description: ['description', formMessages.descriptionLength],
        ImageUrl: ['imageUrl', formMessages.imageUrlInvalid],
        OrderIndex: ['orderIndex', formMessages.orderMinimum],
      })
      if (
        error instanceof ApiProblem &&
        ['CATEGORY_NAME_EXISTS', 'CATEGORY_SLUG_EXISTS'].includes(error.problem.code)
      )
        setFieldError('name', { type: 'server', message: apiErrorMessage(error) })
      setActionNotice(null)
      setActionError(apiErrorMessage(error, 'CATEGORY_SAVE_FAILED'))
    },
  })

  const remove = useMutation({
    mutationFn: (id: string) => deleteCategory(id),
    onSuccess: async () => {
      setDeleteTarget(null)
      setActionError(null)
      setActionNotice('Đã xóa danh mục thành công.')
      showToast('Đã xóa danh mục thành công.')
      await queryClient.invalidateQueries({ queryKey: ['categories'] })
    },
    onError: (error) => {
      showToast(apiErrorMessage(error, 'CATEGORY_DELETE_FAILED'))
      setActionNotice(null)
      if (error instanceof ApiProblem && error.problem.code === 'CATEGORY_DELETE_HAS_RECIPES') {
        setActionError(apiErrorMessage(error))
      } else {
        setActionError(apiErrorMessage(error, 'CATEGORY_DELETE_FAILED'))
      }
    },
  })

  const saveLabel = editing ? 'Lưu thay đổi' : 'Thêm danh mục'

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
    resetForm({
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
    resetForm(EMPTY_FORM)
  }

  const submit = (values: CategoryValues) => {
    setActionNotice(null)
    setActionError(null)
    save.mutate(values)
  }

  const sortedCategories = categories.data
    ? [...categories.data].sort((a, b) => a.name.localeCompare(b.name, 'vi'))
    : []
  const totalRecipes = sortedCategories.reduce((sum, c) => sum + c.recipeCount, 0)
  const emptyCount = sortedCategories.filter((c) => c.recipeCount === 0).length
  const needle = query.trim().toLowerCase()
  const visibleCategories = needle
    ? sortedCategories.filter((c) => c.name.toLowerCase().includes(needle))
    : sortedCategories

  return (
    <div>
      <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="font-serif text-3xl lg:text-4xl">Quản lý danh mục</h1>
          <p className="mt-1 text-muted-foreground">
            Quản lý danh mục công thức. Trang công khai hiển thị danh mục theo tên A–Z.
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
          <form noValidate onSubmit={handleSubmit(submit)}>
            {actionError && (
              <p role="alert" className="mb-4 border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                {actionError}
              </p>
            )}
            <fieldset disabled={save.isPending}>
              <div className="grid gap-5">
                <label className="text-xs uppercase tracking-widest text-muted-foreground">
                  Tên danh mục <span className="text-red-500">*</span>
                  <input
                    required
                    minLength={2}
                    maxLength={50}
                    {...register('name')}
                    aria-invalid={Boolean(errors.name)}
                    aria-describedby={errors.name ? 'category-name-error' : undefined}
                    className="mt-2 w-full border border-border bg-background px-4 py-3 text-sm focus:border-primary focus:outline-none"
                    placeholder="VD: Món chay, Tráng miệng..."
                  />
                  <FieldError id="category-name-error" message={errors.name?.message} />
                </label>
                <label className="text-xs uppercase tracking-widest text-muted-foreground">
                  Mô tả
                  <textarea
                    maxLength={2000}
                    rows={3}
                    {...register('description')}
                    aria-invalid={Boolean(errors.description)}
                    aria-describedby={errors.description ? 'category-description-error' : undefined}
                    className="mt-2 w-full resize-y border border-border bg-background px-4 py-3 text-sm focus:border-primary focus:outline-none"
                    placeholder="Mô tả tóm tắt về danh mục này..."
                  />
                  <FieldError id="category-description-error" message={errors.description?.message} />
                </label>
                <div>
                  <CategoryImageField
                    value={imageUrl}
                    onChange={(url) => setValue('imageUrl', url, { shouldDirty: true, shouldValidate: true })}
                    onBusyChange={setImageUploading}
                    disabled={save.isPending}
                  />
                  <FieldError id="category-imageUrl-error" message={errors.imageUrl?.message} />
                </div>
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
                type="submit"
                disabled={save.isPending || imageUploading}
                className="flex items-center gap-2 bg-primary px-5 py-2.5 text-sm uppercase tracking-widest text-primary-foreground transition-opacity hover:opacity-95 disabled:opacity-60"
              >
                {editing ? <Pencil size={15} /> : <Plus size={15} />}{' '}
                {save.isPending ? 'Đang lưu…' : saveLabel}
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
          <button type="button" className="underline" onClick={() => categories.refetch()}>
            Thử lại
          </button>
        </div>
      )}

      {categories.isSuccess && (
        <>
          <div className="mb-4 flex flex-col gap-4 border border-border bg-card p-4 lg:flex-row lg:items-center lg:justify-between">
            <dl className="flex flex-wrap gap-x-8 gap-y-3">
              {[
                { label: 'Danh mục', value: sortedCategories.length, icon: Layers },
                { label: 'Công thức', value: totalRecipes, icon: BookOpen },
                { label: 'Danh mục trống', value: emptyCount, icon: FolderOpen },
              ].map(({ label, value, icon: Icon }) => (
                <div key={label} className="flex items-center gap-3">
                  <Icon size={18} strokeWidth={1.5} className="text-primary" aria-hidden="true" />
                  <dd className="font-serif text-2xl leading-none tabular-nums">{value}</dd>
                  <dt className="text-xs uppercase tracking-widest text-muted-foreground">{label}</dt>
                </div>
              ))}
            </dl>
            <div className="relative w-full lg:max-w-xs">
              <Search
                size={16}
                aria-hidden="true"
                className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground"
              />
              <input
                type="search"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                aria-label="Tìm danh mục"
                placeholder="Tìm theo tên danh mục…"
                className="min-h-11 w-full border border-border bg-background py-2 pl-9 pr-3 text-sm focus:border-primary focus:outline-none"
              />
            </div>
          </div>

          {visibleCategories.length === 0 ? (
            <p className="border border-border bg-background p-8 text-center text-sm text-muted-foreground">
              {sortedCategories.length === 0
                ? 'Chưa có danh mục nào. Chọn “Thêm danh mục” để bắt đầu.'
                : 'Không tìm thấy danh mục phù hợp.'}
            </p>
          ) : (
            <>
              <div className="hidden overflow-x-auto border border-border bg-background md:block">
                <table className="w-full text-left text-sm">
                  <caption className="sr-only">Danh sách danh mục công thức</caption>
                  <thead className="border-b border-border bg-secondary/30 text-xs uppercase tracking-widest text-muted-foreground">
                    <tr>
                      <th scope="col" className="px-5 py-3 font-medium">
                        Danh mục
                      </th>
                      <th scope="col" className="px-5 py-3 text-right font-medium">
                        Số công thức
                      </th>
                      <th scope="col" className="px-5 py-3 text-right font-medium">
                        Thao tác
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {visibleCategories.map((category) => (
                      <tr
                        key={category.id}
                        className="border-b border-border transition-colors last:border-0 hover:bg-secondary/20"
                      >
                        <td className="px-5 py-3">
                          <div className="flex items-center gap-4">
                            <div className="relative h-12 w-16 shrink-0 overflow-hidden border border-border bg-secondary/40">
                              <CategoryThumb category={category} />
                            </div>
                            <div className="min-w-0">
                              <div className="font-medium text-foreground">{category.name}</div>
                              <div className="mt-0.5 line-clamp-1 text-xs text-muted-foreground">
                                {category.description || 'Chưa có mô tả.'}
                              </div>
                            </div>
                          </div>
                        </td>
                        <td className="px-5 py-3 text-right">
                          <span
                            className={`inline-block border px-2 py-1 text-xs ${
                              category.recipeCount > 0
                                ? 'border-primary/30 bg-primary/5 text-primary'
                                : 'border-border text-muted-foreground'
                            }`}
                          >
                            {category.recipeCount} công thức
                          </span>
                        </td>
                        <td className="px-5 py-3 text-right">
                          <div className="flex justify-end gap-1">
                            <button
                              type="button"
                              onClick={() => beginEdit(category)}
                              aria-label={`Sửa ${category.name}`}
                              title="Chỉnh sửa danh mục"
                              className="inline-grid h-11 w-11 place-items-center text-muted-foreground transition-colors hover:text-primary"
                            >
                              <Pencil size={15} aria-hidden="true" />
                            </button>
                            <button
                              type="button"
                              onClick={() => setDeleteTarget(category)}
                              aria-label={`Xóa ${category.name}`}
                              title={
                                category.recipeCount > 0 ? 'Danh mục có công thức liên kết' : 'Xóa danh mục'
                              }
                              className="inline-grid h-11 w-11 place-items-center text-muted-foreground transition-colors hover:text-red-600"
                            >
                              <Trash2 size={15} aria-hidden="true" />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <ul className="grid gap-3 md:hidden" aria-label="Danh sách danh mục công thức">
                {visibleCategories.map((category) => (
                  <li key={category.id} className="border border-border bg-background p-4">
                    <div className="flex gap-3">
                      <div className="relative h-14 w-20 shrink-0 overflow-hidden border border-border bg-secondary/40">
                        <CategoryThumb category={category} />
                      </div>
                      <div className="min-w-0 flex-1">
                        <h2 className="font-serif text-xl">{category.name}</h2>
                        <p className="mt-0.5 line-clamp-2 text-sm text-muted-foreground">
                          {category.description || 'Chưa có mô tả.'}
                        </p>
                      </div>
                    </div>
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
                  </li>
                ))}
              </ul>
            </>
          )}
        </>
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
