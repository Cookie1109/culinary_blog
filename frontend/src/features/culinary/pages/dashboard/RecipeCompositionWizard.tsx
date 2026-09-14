'use client'

import { useMutation } from '@tanstack/react-query'
import { ArrowDown, ArrowUp, ImagePlus, Plus, Save, Star, Trash2 } from 'lucide-react'
import { useEffect, useState, type FormEvent } from 'react'
import { authenticatedBlobUrl } from '@/lib/api/auth-client'
import {
  createIngredient,
  createStep,
  deleteIngredient,
  deleteRecipeImage,
  deleteStep,
  updateIngredient,
  updateRecipeImage,
  updateStep,
  uploadRecipeImage,
  type Ingredient,
  type IngredientWrite,
  type Recipe,
  type RecipeImage,
  type RecipeStep,
  type StepWrite,
} from '@/lib/api/content-client'
import { ApiProblem } from '@/lib/api/problem-details'

type Stage = 'ingredients' | 'steps' | 'images' | 'preview'

const EMPTY_INGREDIENT: IngredientWrite = { name: '', quantity: null, unit: null, notes: null, orderIndex: 0 }
const EMPTY_STEP: StepWrite = { title: '', description: '', timerMinutes: null, imageUrl: null }

interface Props {
  recipe: Recipe
  version: number
  onChanged: (version: number) => Promise<void>
}

function errorMessage(error: unknown) {
  return error instanceof ApiProblem
    ? (error.problem.detail ?? error.problem.title)
    : 'Không thể lưu thay đổi.'
}

function PrivateImage({ image }: { image: RecipeImage }) {
  const [source, setSource] = useState<string | null>(null)
  const path = image.thumbnailUrl ?? image.mediumUrl ?? image.originalUrl

  useEffect(() => {
    let active = true
    let objectUrl: string | null = null
    authenticatedBlobUrl(path)
      .then((url) => {
        objectUrl = url
        if (active) setSource(url)
      })
      .catch(() => setSource(null))
    return () => {
      active = false
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [path])

  return source ? (
    <img src={source} alt={image.altText ?? ''} className="h-32 w-full object-cover" />
  ) : (
    <div className="flex h-32 items-center justify-center bg-muted text-xs text-muted-foreground">
      {image.processingStatus === 'failed' ? 'Xử lý ảnh thất bại' : 'Đang xử lý ảnh…'}
    </div>
  )
}

export function RecipeCompositionWizard({ recipe, version, onChanged }: Props) {
  const [stage, setStage] = useState<Stage>('ingredients')
  const [ingredientForm, setIngredientForm] = useState<IngredientWrite>(EMPTY_INGREDIENT)
  const [editingIngredient, setEditingIngredient] = useState<string | null>(null)
  const [stepForm, setStepForm] = useState<StepWrite>(EMPTY_STEP)
  const [editingStep, setEditingStep] = useState<string | null>(null)
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [imageAlt, setImageAlt] = useState('')
  const [imagePrimary, setImagePrimary] = useState(false)
  const [uploadProgress, setUploadProgress] = useState(0)
  const [clientError, setClientError] = useState<string | null>(null)

  const ingredientMutation = useMutation({
    mutationFn: () =>
      editingIngredient
        ? updateIngredient(recipe.id, editingIngredient, version, ingredientForm)
        : createIngredient(recipe.id, version, { ...ingredientForm, orderIndex: recipe.ingredients.length }),
    onSuccess: async (result) => {
      setIngredientForm(EMPTY_INGREDIENT)
      setEditingIngredient(null)
      await onChanged(result.meta.recipeVersion)
    },
  })

  const stepMutation = useMutation({
    mutationFn: () =>
      editingStep
        ? updateStep(recipe.id, editingStep, version, stepForm)
        : createStep(recipe.id, version, stepForm),
    onSuccess: async (result) => {
      setStepForm(EMPTY_STEP)
      setEditingStep(null)
      await onChanged(result.meta.recipeVersion)
    },
  })

  const imageMutation = useMutation({
    mutationFn: async () => {
      if (!imageFile) throw new Error('Vui lòng chọn ảnh.')
      return uploadRecipeImage(recipe.id, version, imageFile, imageAlt, imagePrimary, setUploadProgress)
    },
    onSuccess: async (result) => {
      setImageFile(null)
      setImageAlt('')
      setImagePrimary(false)
      setUploadProgress(0)
      await onChanged(result.meta.recipeVersion)
    },
  })

  const run = async (action: () => Promise<number | void>) => {
    setClientError(null)
    try {
      const nextVersion = await action()
      await onChanged(nextVersion ?? version + 1)
    } catch (error) {
      setClientError(errorMessage(error))
    }
  }

  const moveIngredient = (item: Ingredient, direction: -1 | 1) => {
    const target = item.orderIndex + direction
    if (target < 0 || target >= recipe.ingredients.length) return
    void run(async () => {
      const result = await updateIngredient(recipe.id, item.id, version, { ...item, orderIndex: target })
      return result.meta.recipeVersion
    })
  }

  const moveStep = (item: RecipeStep, direction: -1 | 1) => {
    const target = item.stepNumber + direction
    if (target < 1 || target > recipe.steps.length) return
    void run(async () => {
      const result = await updateStep(recipe.id, item.id, version, { ...item, stepNumber: target })
      return result.meta.recipeVersion
    })
  }

  const submitIngredient = (event: FormEvent) => {
    event.preventDefault()
    if (!ingredientForm.name.trim()) return setClientError('Tên nguyên liệu là bắt buộc.')
    setClientError(null)
    ingredientMutation.mutate()
  }

  const submitStep = (event: FormEvent) => {
    event.preventDefault()
    if (!stepForm.title.trim() || !stepForm.description.trim()) {
      return setClientError('Tiêu đề và mô tả bước là bắt buộc.')
    }
    setClientError(null)
    stepMutation.mutate()
  }

  const submitImage = (event: FormEvent) => {
    event.preventDefault()
    if (!imageFile) return setClientError('Vui lòng chọn ảnh.')
    if (imageFile.size > 5 * 1024 * 1024) return setClientError('Ảnh không được vượt quá 5 MB.')
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(imageFile.type)) {
      return setClientError('Chỉ chấp nhận JPEG, PNG hoặc WebP.')
    }
    setClientError(null)
    imageMutation.mutate()
  }

  const inputClass =
    'w-full border border-border bg-background px-3 py-2 text-sm focus:border-primary focus:outline-none'
  const stages: { id: Stage; label: string }[] = [
    { id: 'ingredients', label: '2. Nguyên liệu' },
    { id: 'steps', label: '3. Các bước' },
    { id: 'images', label: '4. Hình ảnh' },
    { id: 'preview', label: '5. Xem trước' },
  ]

  return (
    <section className="mt-10 border-t border-border pt-8" aria-labelledby="composition-heading">
      <h2 id="composition-heading" className="font-serif text-2xl">
        Hoàn thiện công thức
      </h2>
      <p className="mt-1 text-sm text-muted-foreground">
        Mỗi thay đổi được lưu ngay; tải lại trang không làm mất phần đã hoàn thành.
      </p>

      <nav className="mt-6 grid grid-cols-2 gap-2 sm:grid-cols-4" aria-label="Các bước hoàn thiện công thức">
        {stages.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setStage(item.id)}
            aria-current={stage === item.id ? 'step' : undefined}
            className={`border px-3 py-2 text-xs uppercase tracking-wider ${stage === item.id ? 'border-primary bg-primary text-primary-foreground' : 'border-border'}`}
          >
            {item.label}
          </button>
        ))}
      </nav>

      {clientError && (
        <p role="alert" className="mt-5 border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {clientError}
        </p>
      )}
      {(ingredientMutation.isError || stepMutation.isError || imageMutation.isError) && (
        <p role="alert" className="mt-5 border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {errorMessage(ingredientMutation.error ?? stepMutation.error ?? imageMutation.error)}
        </p>
      )}

      {stage === 'ingredients' && (
        <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_1.4fr]">
          <form onSubmit={submitIngredient} className="space-y-3 border border-border p-4">
            <h3 className="font-medium">{editingIngredient ? 'Sửa nguyên liệu' : 'Thêm nguyên liệu'}</h3>
            <input
              aria-label="Tên nguyên liệu"
              placeholder="Tên nguyên liệu"
              maxLength={200}
              className={inputClass}
              value={ingredientForm.name}
              onChange={(event) => setIngredientForm({ ...ingredientForm, name: event.target.value })}
            />
            <div className="grid grid-cols-2 gap-3">
              <input
                aria-label="Số lượng"
                placeholder="Số lượng (vừa đủ nếu trống)"
                type="number"
                min="0.001"
                step="0.001"
                className={inputClass}
                value={ingredientForm.quantity ?? ''}
                onChange={(event) =>
                  setIngredientForm({
                    ...ingredientForm,
                    quantity: event.target.value ? Number(event.target.value) : null,
                  })
                }
              />
              <input
                aria-label="Đơn vị"
                placeholder="Đơn vị"
                maxLength={50}
                className={inputClass}
                value={ingredientForm.unit ?? ''}
                onChange={(event) =>
                  setIngredientForm({ ...ingredientForm, unit: event.target.value || null })
                }
              />
            </div>
            <input
              aria-label="Ghi chú nguyên liệu"
              placeholder="Ghi chú"
              maxLength={500}
              className={inputClass}
              value={ingredientForm.notes ?? ''}
              onChange={(event) =>
                setIngredientForm({ ...ingredientForm, notes: event.target.value || null })
              }
            />
            <button
              disabled={ingredientMutation.isPending}
              className="flex items-center gap-2 bg-primary px-4 py-2 text-sm text-primary-foreground disabled:opacity-60"
            >
              <Plus size={14} /> {editingIngredient ? 'Lưu nguyên liệu' : 'Thêm nguyên liệu'}
            </button>
          </form>
          <div className="space-y-2">
            {recipe.ingredients.length === 0 && (
              <p className="p-5 text-sm text-muted-foreground">Chưa có nguyên liệu.</p>
            )}
            {recipe.ingredients.map((item) => (
              <article key={item.id} className="flex items-center gap-3 border border-border p-3">
                <div className="min-w-0 flex-1">
                  <strong className="block truncate text-sm">{item.name}</strong>
                  <span className="text-xs text-muted-foreground">
                    {item.quantity ?? 'Vừa đủ'} {item.unit ?? ''}
                  </span>
                </div>
                <button
                  type="button"
                  aria-label="Đưa nguyên liệu lên"
                  onClick={() => moveIngredient(item, -1)}
                >
                  <ArrowUp size={15} />
                </button>
                <button
                  type="button"
                  aria-label="Đưa nguyên liệu xuống"
                  onClick={() => moveIngredient(item, 1)}
                >
                  <ArrowDown size={15} />
                </button>
                <button
                  type="button"
                  className="text-xs underline"
                  onClick={() => {
                    setEditingIngredient(item.id)
                    setIngredientForm({
                      name: item.name,
                      quantity: item.quantity,
                      unit: item.unit,
                      notes: item.notes,
                      orderIndex: item.orderIndex,
                    })
                  }}
                >
                  Sửa
                </button>
                <button
                  type="button"
                  aria-label="Xóa nguyên liệu"
                  className="text-red-600"
                  onClick={() => {
                    if (window.confirm('Xóa nguyên liệu này?'))
                      void run(() => deleteIngredient(recipe.id, item.id, version))
                  }}
                >
                  <Trash2 size={15} />
                </button>
              </article>
            ))}
          </div>
        </div>
      )}

      {stage === 'steps' && (
        <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_1.4fr]">
          <form onSubmit={submitStep} className="space-y-3 border border-border p-4">
            <h3 className="font-medium">{editingStep ? 'Sửa bước' : 'Thêm bước cuối'}</h3>
            <input
              aria-label="Tiêu đề bước"
              placeholder="Tiêu đề bước"
              maxLength={200}
              className={inputClass}
              value={stepForm.title}
              onChange={(event) => setStepForm({ ...stepForm, title: event.target.value })}
            />
            <textarea
              aria-label="Mô tả bước"
              placeholder="Mô tả chi tiết"
              maxLength={2000}
              rows={4}
              className={inputClass}
              value={stepForm.description}
              onChange={(event) => setStepForm({ ...stepForm, description: event.target.value })}
            />
            <input
              aria-label="Hẹn giờ phút"
              placeholder="Hẹn giờ (phút)"
              type="number"
              min="0"
              className={inputClass}
              value={stepForm.timerMinutes ?? ''}
              onChange={(event) =>
                setStepForm({
                  ...stepForm,
                  timerMinutes: event.target.value ? Number(event.target.value) : null,
                })
              }
            />
            <button
              disabled={stepMutation.isPending}
              className="flex items-center gap-2 bg-primary px-4 py-2 text-sm text-primary-foreground disabled:opacity-60"
            >
              <Save size={14} /> Lưu bước
            </button>
          </form>
          <ol className="space-y-3">
            {recipe.steps.length === 0 && (
              <li className="p-5 text-sm text-muted-foreground">Chưa có bước thực hiện.</li>
            )}
            {recipe.steps.map((item) => (
              <li key={item.id} className="flex gap-3 border border-border p-4">
                <span className="font-serif text-2xl text-primary">{item.stepNumber}</span>
                <div className="min-w-0 flex-1">
                  <strong className="block text-sm">{item.title}</strong>
                  <p className="mt-1 text-sm text-muted-foreground">{item.description}</p>
                </div>
                <button type="button" aria-label="Đưa bước lên" onClick={() => moveStep(item, -1)}>
                  <ArrowUp size={15} />
                </button>
                <button type="button" aria-label="Đưa bước xuống" onClick={() => moveStep(item, 1)}>
                  <ArrowDown size={15} />
                </button>
                <button
                  type="button"
                  className="text-xs underline"
                  onClick={() => {
                    setEditingStep(item.id)
                    setStepForm({
                      title: item.title,
                      description: item.description,
                      timerMinutes: item.timerMinutes,
                      imageUrl: item.imageUrl,
                      stepNumber: item.stepNumber,
                    })
                  }}
                >
                  Sửa
                </button>
                <button
                  type="button"
                  aria-label="Xóa bước"
                  className="text-red-600"
                  onClick={() => {
                    if (window.confirm('Xóa bước này?'))
                      void run(() => deleteStep(recipe.id, item.id, version))
                  }}
                >
                  <Trash2 size={15} />
                </button>
              </li>
            ))}
          </ol>
        </div>
      )}

      {stage === 'images' && (
        <div className="mt-6">
          <form onSubmit={submitImage} className="space-y-3 border border-dashed border-border p-5">
            <label className="flex items-center gap-2 text-sm font-medium" htmlFor="recipe-image">
              <ImagePlus size={17} /> Ảnh JPEG, PNG hoặc WebP (tối đa 5 MB)
            </label>
            <input
              id="recipe-image"
              type="file"
              accept="image/jpeg,image/png,image/webp"
              onChange={(event) => setImageFile(event.target.files?.[0] ?? null)}
            />
            <input
              aria-label="Mô tả ảnh"
              placeholder="Alt text mô tả ảnh"
              maxLength={200}
              className={inputClass}
              value={imageAlt}
              onChange={(event) => setImageAlt(event.target.value)}
            />
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={imagePrimary}
                onChange={(event) => setImagePrimary(event.target.checked)}
              />{' '}
              Đặt làm ảnh chính
            </label>
            {imageMutation.isPending && (
              <progress className="w-full" max={100} value={uploadProgress}>
                {uploadProgress}%
              </progress>
            )}
            <button
              disabled={imageMutation.isPending}
              className="bg-primary px-4 py-2 text-sm text-primary-foreground disabled:opacity-60"
            >
              {imageMutation.isPending ? `Đang tải ${uploadProgress}%` : 'Tải ảnh lên'}
            </button>
          </form>
          <div className="mt-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {recipe.images.map((image) => (
              <article key={image.id} className="border border-border p-3">
                <PrivateImage image={image} />
                <input
                  aria-label="Alt text"
                  className={`${inputClass} mt-3`}
                  defaultValue={image.altText ?? ''}
                  onBlur={(event) => {
                    if (event.target.value !== (image.altText ?? ''))
                      void run(async () => {
                        const result = await updateRecipeImage(recipe.id, image.id, version, {
                          altText: event.target.value || null,
                          isPrimary: image.isPrimary,
                          orderIndex: image.orderIndex,
                        })
                        return result.meta.recipeVersion
                      })
                  }}
                />
                <div className="mt-3 flex items-center justify-between">
                  <button
                    type="button"
                    disabled={image.isPrimary}
                    className="flex items-center gap-1 text-xs disabled:text-primary"
                    onClick={() =>
                      void run(async () => {
                        const result = await updateRecipeImage(recipe.id, image.id, version, {
                          altText: image.altText,
                          isPrimary: true,
                          orderIndex: image.orderIndex,
                        })
                        return result.meta.recipeVersion
                      })
                    }
                  >
                    <Star size={14} /> {image.isPrimary ? 'Ảnh chính' : 'Đặt ảnh chính'}
                  </button>
                  <button
                    type="button"
                    aria-label="Xóa ảnh"
                    className="text-red-600"
                    onClick={() => {
                      if (window.confirm('Xóa ảnh này?'))
                        void run(() => deleteRecipeImage(recipe.id, image.id, version))
                    }}
                  >
                    <Trash2 size={15} />
                  </button>
                </div>
              </article>
            ))}
          </div>
        </div>
      )}

      {stage === 'preview' && (
        <article className="mt-6 border border-border p-6">
          <p className="text-xs uppercase tracking-widest text-primary">Bản xem trước</p>
          <h3 className="mt-2 font-serif text-3xl">{recipe.title}</h3>
          <p className="mt-3 text-muted-foreground">{recipe.description}</p>
          <div className="mt-6 grid gap-6 md:grid-cols-2">
            <div>
              <h4 className="font-medium">Nguyên liệu</h4>
              <ul className="mt-2 list-disc space-y-1 pl-5 text-sm">
                {recipe.ingredients.map((item) => (
                  <li key={item.id}>
                    {item.quantity ?? 'Vừa đủ'} {item.unit} {item.name}
                  </li>
                ))}
              </ul>
            </div>
            <div>
              <h4 className="font-medium">Cách làm</h4>
              <ol className="mt-2 list-decimal space-y-2 pl-5 text-sm">
                {recipe.steps.map((item) => (
                  <li key={item.id}>
                    <strong>{item.title}:</strong> {item.description}
                  </li>
                ))}
              </ol>
            </div>
          </div>
        </article>
      )}
    </section>
  )
}
