'use client'

import { useMutation } from '@tanstack/react-query'
import {
  AlertCircle,
  ArrowDown,
  ArrowLeft,
  ArrowRight,
  ArrowUp,
  CheckCircle2,
  EyeOff,
  Globe,
  ImagePlus,
  Plus,
  Save,
  Star,
  Trash2,
} from 'lucide-react'
import { useEffect, useState, type FormEvent } from 'react'
import { authenticatedBlobUrl } from '@/lib/api/auth-client'
import {
  createIngredient,
  createStep,
  deleteIngredient,
  deleteRecipeImage,
  deleteStep,
  publishRecipe,
  unpublishRecipe,
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

export type Stage = 'ingredients' | 'steps' | 'images' | 'preview'
export type WizardAllSteps = 'basic' | Stage

const EMPTY_INGREDIENT: IngredientWrite = { name: '', quantity: null, unit: null, notes: null, orderIndex: 0 }
const EMPTY_STEP: StepWrite = { title: '', description: '', timerMinutes: null, imageUrl: null }

export interface ValidationItem {
  id: string
  step: WizardAllSteps
  stepLabel: string
  label: string
  isValid: boolean
  message: string
}

export function computeRecipeValidation(recipe: Recipe): ValidationItem[] {
  return [
    {
      id: 'title',
      step: 'basic',
      stepLabel: '1. Thông tin cơ bản',
      label: 'Tên công thức',
      isValid: (recipe.title?.trim().length ?? 0) >= 5,
      message: 'Cần có ít nhất 5 ký tự.',
    },
    {
      id: 'description',
      step: 'basic',
      stepLabel: '1. Thông tin cơ bản',
      label: 'Mô tả',
      isValid: (recipe.description?.trim().length ?? 0) > 0,
      message: 'Vui lòng nhập mô tả cho công thức.',
    },
    {
      id: 'category',
      step: 'basic',
      stepLabel: '1. Thông tin cơ bản',
      label: 'Danh mục',
      isValid: Boolean(recipe.category?.id),
      message: 'Vui lòng chọn danh mục.',
    },
    {
      id: 'prepTime',
      step: 'basic',
      stepLabel: '1. Thông tin cơ bản',
      label: 'Thời gian chuẩn bị',
      isValid: recipe.prepTime >= 1,
      message: 'Thời gian chuẩn bị phải ít nhất 1 phút.',
    },
    {
      id: 'ingredients',
      step: 'ingredients',
      stepLabel: '2. Nguyên liệu',
      label: 'Nguyên liệu',
      isValid: recipe.ingredients.length >= 1,
      message: `Cần ít nhất 1 nguyên liệu (hiện có: ${recipe.ingredients.length}).`,
    },
    {
      id: 'steps',
      step: 'steps',
      stepLabel: '3. Các bước thực hiện',
      label: 'Các bước thực hiện',
      isValid: recipe.steps.length >= 1,
      message: `Cần ít nhất 1 bước thực hiện (hiện có: ${recipe.steps.length}).`,
    },
    {
      id: 'images',
      step: 'images',
      stepLabel: '4. Hình ảnh',
      label: 'Hình ảnh công thức',
      isValid: recipe.images.length >= 1,
      message: `Khuyến nghị ít nhất 1 hình ảnh đại diện (hiện có: ${recipe.images.length}).`,
    },
  ]
}

interface Props {
  recipe: Recipe
  version: number
  onChanged: (version: number) => Promise<void>
  activeStage?: Stage
  onStageChange?: (stage: Stage) => void
  onNavigateToStep?: (step: WizardAllSteps) => void
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

export function RecipeCompositionWizard({
  recipe,
  version,
  onChanged,
  activeStage,
  onStageChange,
  onNavigateToStep,
}: Props) {
  const [internalStage, setInternalStage] = useState<Stage>('ingredients')
  const stage = activeStage ?? internalStage

  const setStage = (next: Stage) => {
    setInternalStage(next)
    onStageChange?.(next)
  }

  const [ingredientForm, setIngredientForm] = useState<IngredientWrite>(EMPTY_INGREDIENT)
  const [editingIngredient, setEditingIngredient] = useState<string | null>(null)
  const [stepForm, setStepForm] = useState<StepWrite>(EMPTY_STEP)
  const [editingStep, setEditingStep] = useState<string | null>(null)
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [imageAlt, setImageAlt] = useState('')
  const [imagePrimary, setImagePrimary] = useState(false)
  const [uploadProgress, setUploadProgress] = useState(0)
  const [clientError, setClientError] = useState<string | null>(null)

  // Confirmation state for Publish / Unpublish in preview
  const [confirmModal, setConfirmModal] = useState<'publish' | 'unpublish' | null>(null)

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

  const publishMutation = useMutation({
    mutationFn: () => publishRecipe(recipe.id, version),
    onSuccess: async (updated) => {
      setConfirmModal(null)
      setClientError(null)
      await onChanged(updated.version)
    },
    onError: (error) => {
      setConfirmModal(null)
      if (error instanceof ApiProblem && error.problem.code === 'RECIPE_PUBLISH_INCOMPLETE') {
        setClientError('Công thức cần ít nhất 1 nguyên liệu và 1 bước thực hiện để xuất bản.')
      } else {
        setClientError(errorMessage(error))
      }
    },
  })

  const unpublishMutation = useMutation({
    mutationFn: () => unpublishRecipe(recipe.id, version),
    onSuccess: async (updated) => {
      setConfirmModal(null)
      setClientError(null)
      await onChanged(updated.version)
    },
    onError: (error) => {
      setConfirmModal(null)
      setClientError(errorMessage(error))
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

  const handleStepJump = (target: WizardAllSteps) => {
    if (target === 'basic') {
      onNavigateToStep?.('basic')
    } else {
      setStage(target)
      onNavigateToStep?.(target)
    }
  }

  const validationChecks = computeRecipeValidation(recipe)
  const isPublishReady = validationChecks.filter((c) => c.id !== 'images').every((c) => c.isValid)

  const inputClass =
    'w-full border border-border bg-background px-3 py-2 text-sm focus:border-primary focus:outline-none'
  const stages: { id: Stage; label: string }[] = [
    { id: 'ingredients', label: '2. Nguyên liệu' },
    { id: 'steps', label: '3. Các bước' },
    { id: 'images', label: '4. Hình ảnh' },
    { id: 'preview', label: '5. Xem trước' },
  ]

  return (
    <section className="mt-8 border-t border-border pt-6" aria-labelledby="composition-heading">
      <div className="flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between">
        <h2 id="composition-heading" className="font-serif text-2xl">
          Hoàn thiện nội dung công thức
        </h2>
        <span className="text-xs text-muted-foreground">Mỗi thay đổi được lưu ngay vào máy chủ</span>
      </div>

      {/* Stage Tabs */}
      <nav className="mt-4 grid grid-cols-2 gap-2 sm:grid-cols-4" aria-label="Các bước hoàn thiện công thức">
        {stages.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setStage(item.id)}
            aria-current={stage === item.id ? 'step' : undefined}
            className={`border px-3 py-2.5 text-xs uppercase tracking-wider transition-colors ${
              stage === item.id
                ? 'border-primary bg-primary text-primary-foreground font-medium'
                : 'border-border bg-background text-muted-foreground hover:bg-secondary'
            }`}
          >
            {item.label}
          </button>
        ))}
      </nav>

      {/* Errors */}
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

      {/* Stage: Ingredients */}
      {stage === 'ingredients' && (
        <div className="mt-6 space-y-6">
          <div className="grid gap-6 lg:grid-cols-[1fr_1.4fr]">
            <form onSubmit={submitIngredient} className="space-y-3 border border-border p-4 bg-background">
              <h3 className="font-medium text-foreground">
                {editingIngredient ? 'Sửa nguyên liệu' : 'Thêm nguyên liệu mới'}
              </h3>
              <input
                aria-label="Tên nguyên liệu"
                placeholder="Tên nguyên liệu (bắt buộc)"
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
                  placeholder="Đơn vị (g, ml, muỗng…)"
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
                placeholder="Ghi chú (tùy chọn)"
                maxLength={500}
                className={inputClass}
                value={ingredientForm.notes ?? ''}
                onChange={(event) =>
                  setIngredientForm({ ...ingredientForm, notes: event.target.value || null })
                }
              />
              <div className="flex items-center gap-2 pt-1">
                <button
                  disabled={ingredientMutation.isPending}
                  className="flex items-center gap-2 bg-primary px-4 py-2 text-sm text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
                >
                  <Plus size={14} /> {editingIngredient ? 'Lưu nguyên liệu' : 'Thêm nguyên liệu'}
                </button>
                {editingIngredient && (
                  <button
                    type="button"
                    onClick={() => {
                      setEditingIngredient(null)
                      setIngredientForm(EMPTY_INGREDIENT)
                    }}
                    className="border border-border px-3 py-2 text-xs uppercase tracking-wider text-muted-foreground hover:bg-secondary"
                  >
                    Hủy sửa
                  </button>
                )}
              </div>
            </form>

            <div className="space-y-2">
              <div className="flex items-center justify-between pb-1">
                <h4 className="text-xs uppercase tracking-widest text-muted-foreground font-medium">
                  Danh sách nguyên liệu ({recipe.ingredients.length})
                </h4>
              </div>
              {recipe.ingredients.length === 0 && (
                <div className="border border-dashed border-border p-8 text-center text-sm text-muted-foreground">
                  Chưa có nguyên liệu nào. Hãy thêm ít nhất 1 nguyên liệu để đủ điều kiện xuất bản.
                </div>
              )}
              {recipe.ingredients.map((item) => (
                <article
                  key={item.id}
                  className="flex items-center gap-3 border border-border bg-background p-3"
                >
                  <div className="min-w-0 flex-1">
                    <strong className="block truncate text-sm text-foreground">{item.name}</strong>
                    <span className="text-xs text-muted-foreground">
                      {item.quantity ?? 'Vừa đủ'} {item.unit ?? ''}
                      {item.notes ? ` · ${item.notes}` : ''}
                    </span>
                  </div>
                  <button
                    type="button"
                    aria-label="Đưa nguyên liệu lên"
                    onClick={() => moveIngredient(item, -1)}
                    className="p-1 text-muted-foreground hover:text-foreground"
                  >
                    <ArrowUp size={15} />
                  </button>
                  <button
                    type="button"
                    aria-label="Đưa nguyên liệu xuống"
                    onClick={() => moveIngredient(item, 1)}
                    className="p-1 text-muted-foreground hover:text-foreground"
                  >
                    <ArrowDown size={15} />
                  </button>
                  <button
                    type="button"
                    className="text-xs underline text-muted-foreground hover:text-primary px-1"
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
                    className="p-1 text-muted-foreground hover:text-red-600"
                    onClick={() => {
                      if (window.confirm(`Xóa nguyên liệu «${item.name}»?`))
                        void run(() => deleteIngredient(recipe.id, item.id, version))
                    }}
                  >
                    <Trash2 size={15} />
                  </button>
                </article>
              ))}
            </div>
          </div>

          {/* Stepper Navigation */}
          <div className="flex items-center justify-between border-t border-border pt-4">
            <button
              type="button"
              onClick={() => onNavigateToStep?.('basic')}
              className="flex items-center gap-1.5 text-xs uppercase tracking-wider text-muted-foreground hover:text-foreground"
            >
              <ArrowLeft size={14} /> Quay lại: Thông tin cơ bản
            </button>
            <button
              type="button"
              onClick={() => setStage('steps')}
              className="flex items-center gap-1.5 bg-primary px-4 py-2 text-xs uppercase tracking-wider text-primary-foreground hover:bg-primary/90"
            >
              Tiếp tục: Các bước <ArrowRight size={14} />
            </button>
          </div>
        </div>
      )}

      {/* Stage: Steps */}
      {stage === 'steps' && (
        <div className="mt-6 space-y-6">
          <div className="grid gap-6 lg:grid-cols-[1fr_1.4fr]">
            <form onSubmit={submitStep} className="space-y-3 border border-border p-4 bg-background">
              <h3 className="font-medium text-foreground">
                {editingStep ? 'Sửa bước thực hiện' : 'Thêm bước mới'}
              </h3>
              <input
                aria-label="Tiêu đề bước"
                placeholder="Tiêu đề bước (bắt buộc, ví dụ: Sơ chế rau củ)"
                maxLength={200}
                className={inputClass}
                value={stepForm.title}
                onChange={(event) => setStepForm({ ...stepForm, title: event.target.value })}
              />
              <textarea
                aria-label="Mô tả bước"
                placeholder="Mô tả chi tiết cách thực hiện…"
                maxLength={2000}
                rows={4}
                className={inputClass}
                value={stepForm.description}
                onChange={(event) => setStepForm({ ...stepForm, description: event.target.value })}
              />
              <input
                aria-label="Hẹn giờ phút"
                placeholder="Hẹn giờ nấu (phút, tùy chọn)"
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
              <div className="flex items-center gap-2 pt-1">
                <button
                  disabled={stepMutation.isPending}
                  className="flex items-center gap-2 bg-primary px-4 py-2 text-sm text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
                >
                  <Save size={14} /> {editingStep ? 'Lưu bước' : 'Thêm bước'}
                </button>
                {editingStep && (
                  <button
                    type="button"
                    onClick={() => {
                      setEditingStep(null)
                      setStepForm(EMPTY_STEP)
                    }}
                    className="border border-border px-3 py-2 text-xs uppercase tracking-wider text-muted-foreground hover:bg-secondary"
                  >
                    Hủy sửa
                  </button>
                )}
              </div>
            </form>

            <ol className="space-y-3">
              <div className="flex items-center justify-between pb-1">
                <h4 className="text-xs uppercase tracking-widest text-muted-foreground font-medium">
                  Danh sách bước ({recipe.steps.length})
                </h4>
              </div>
              {recipe.steps.length === 0 && (
                <li className="border border-dashed border-border p-8 text-center text-sm text-muted-foreground">
                  Chưa có bước thực hiện nào. Hãy thêm ít nhất 1 bước để đủ điều kiện xuất bản.
                </li>
              )}
              {recipe.steps.map((item) => (
                <li key={item.id} className="flex gap-3 border border-border bg-background p-4">
                  <span className="font-serif text-2xl text-primary">{item.stepNumber}</span>
                  <div className="min-w-0 flex-1">
                    <strong className="block text-sm text-foreground">{item.title}</strong>
                    <p className="mt-1 text-sm text-muted-foreground">{item.description}</p>
                    {item.timerMinutes && (
                      <span className="mt-1.5 inline-block text-xs text-amber-700 bg-amber-50 border border-amber-200 px-2 py-0.5">
                        ⏱ {item.timerMinutes} phút
                      </span>
                    )}
                  </div>
                  <div className="flex flex-col items-center gap-1">
                    <button
                      type="button"
                      aria-label="Đưa bước lên"
                      onClick={() => moveStep(item, -1)}
                      className="p-1 text-muted-foreground hover:text-foreground"
                    >
                      <ArrowUp size={15} />
                    </button>
                    <button
                      type="button"
                      aria-label="Đưa bước xuống"
                      onClick={() => moveStep(item, 1)}
                      className="p-1 text-muted-foreground hover:text-foreground"
                    >
                      <ArrowDown size={15} />
                    </button>
                    <button
                      type="button"
                      className="text-xs underline text-muted-foreground hover:text-primary py-1"
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
                      className="p-1 text-muted-foreground hover:text-red-600"
                      onClick={() => {
                        if (window.confirm(`Xóa bước «${item.title}»?`))
                          void run(() => deleteStep(recipe.id, item.id, version))
                      }}
                    >
                      <Trash2 size={15} />
                    </button>
                  </div>
                </li>
              ))}
            </ol>
          </div>

          {/* Stepper Navigation */}
          <div className="flex items-center justify-between border-t border-border pt-4">
            <button
              type="button"
              onClick={() => setStage('ingredients')}
              className="flex items-center gap-1.5 text-xs uppercase tracking-wider text-muted-foreground hover:text-foreground"
            >
              <ArrowLeft size={14} /> Quay lại: Nguyên liệu
            </button>
            <button
              type="button"
              onClick={() => setStage('images')}
              className="flex items-center gap-1.5 bg-primary px-4 py-2 text-xs uppercase tracking-wider text-primary-foreground hover:bg-primary/90"
            >
              Tiếp tục: Hình ảnh <ArrowRight size={14} />
            </button>
          </div>
        </div>
      )}

      {/* Stage: Images */}
      {stage === 'images' && (
        <div className="mt-6 space-y-6">
          <form
            onSubmit={submitImage}
            className="space-y-3 border border-dashed border-border p-5 bg-background"
          >
            <label className="flex items-center gap-2 text-sm font-medium" htmlFor="recipe-image">
              <ImagePlus size={17} /> Tải lên hình ảnh (JPEG, PNG hoặc WebP, tối đa 5 MB)
            </label>
            <input
              id="recipe-image"
              type="file"
              accept="image/jpeg,image/png,image/webp"
              onChange={(event) => setImageFile(event.target.files?.[0] ?? null)}
              className="block w-full text-sm text-muted-foreground file:mr-4 file:py-2 file:px-4 file:border-0 file:text-xs file:uppercase file:tracking-wider file:bg-primary file:text-primary-foreground hover:file:bg-primary/90"
            />
            <input
              aria-label="Mô tả ảnh"
              placeholder="Alt text mô tả ảnh (khuyến nghị cho SEO và trợ năng)"
              maxLength={200}
              className={inputClass}
              value={imageAlt}
              onChange={(event) => setImageAlt(event.target.value)}
            />
            <label className="flex items-center gap-2 text-sm text-foreground">
              <input
                type="checkbox"
                checked={imagePrimary}
                onChange={(event) => setImagePrimary(event.target.checked)}
              />{' '}
              Đặt làm ảnh đại diện chính của công thức
            </label>
            {imageMutation.isPending && (
              <progress className="w-full h-2" max={100} value={uploadProgress}>
                {uploadProgress}%
              </progress>
            )}
            <button
              disabled={imageMutation.isPending}
              className="bg-primary px-5 py-2.5 text-xs uppercase tracking-wider text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
            >
              {imageMutation.isPending ? `Đang tải ${uploadProgress}%…` : 'Tải ảnh lên'}
            </button>
          </form>

          <div>
            <h4 className="text-xs uppercase tracking-widest text-muted-foreground font-medium mb-3">
              Thư viện ảnh của công thức ({recipe.images.length})
            </h4>
            {recipe.images.length === 0 ? (
              <p className="border border-dashed border-border p-6 text-center text-sm text-muted-foreground">
                Chưa có ảnh nào được tải lên.
              </p>
            ) : (
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {recipe.images.map((image) => (
                  <article key={image.id} className="border border-border bg-background p-3 space-y-2">
                    <PrivateImage image={image} />
                    <input
                      aria-label="Alt text"
                      placeholder="Alt text mô tả ảnh"
                      className={`${inputClass} text-xs`}
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
                    <div className="flex items-center justify-between pt-1">
                      <button
                        type="button"
                        disabled={image.isPrimary}
                        className={`flex items-center gap-1 text-xs ${
                          image.isPrimary
                            ? 'font-medium text-amber-700'
                            : 'text-muted-foreground hover:text-foreground'
                        }`}
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
                        <Star size={14} className={image.isPrimary ? 'fill-amber-500 text-amber-500' : ''} />
                        {image.isPrimary ? 'Ảnh chính' : 'Đặt làm ảnh chính'}
                      </button>
                      <button
                        type="button"
                        aria-label="Xóa ảnh"
                        className="text-muted-foreground hover:text-red-600"
                        onClick={() => {
                          if (window.confirm('Xóa ảnh này khỏi công thức?'))
                            void run(() => deleteRecipeImage(recipe.id, image.id, version))
                        }}
                      >
                        <Trash2 size={15} />
                      </button>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </div>

          {/* Stepper Navigation */}
          <div className="flex items-center justify-between border-t border-border pt-4">
            <button
              type="button"
              onClick={() => setStage('steps')}
              className="flex items-center gap-1.5 text-xs uppercase tracking-wider text-muted-foreground hover:text-foreground"
            >
              <ArrowLeft size={14} /> Quay lại: Các bước
            </button>
            <button
              type="button"
              onClick={() => setStage('preview')}
              className="flex items-center gap-1.5 bg-primary px-4 py-2 text-xs uppercase tracking-wider text-primary-foreground hover:bg-primary/90"
            >
              Tiếp tục: Xem trước & Xuất bản <ArrowRight size={14} />
            </button>
          </div>
        </div>
      )}

      {/* Stage: Preview and Publish Validation (P7-04 & P7-05) */}
      {stage === 'preview' && (
        <article className="mt-6 space-y-6">
          {/* Header & Publication Action */}
          <div className="border border-border bg-background p-6">
            <div className="flex flex-col gap-4 border-b border-border pb-6 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <div className="flex items-center gap-2">
                  <span className="text-xs uppercase tracking-widest text-primary font-medium">
                    Bản xem trước
                  </span>
                  <span
                    className={`border px-2 py-0.5 text-xs font-medium ${
                      recipe.status === 'published'
                        ? 'border-green-200 bg-green-50 text-green-700'
                        : 'border-amber-200 bg-amber-50 text-amber-700'
                    }`}
                  >
                    {recipe.status === 'published' ? 'Đã xuất bản' : 'Bản nháp'}
                  </span>
                </div>
                <h3 className="mt-2 font-serif text-3xl">{recipe.title}</h3>
              </div>
              <div className="flex flex-wrap items-center gap-3">
                {recipe.status === 'draft' ? (
                  <button
                    type="button"
                    disabled={publishMutation.isPending || !isPublishReady}
                    onClick={() => setConfirmModal('publish')}
                    className="flex items-center gap-2 bg-green-700 px-5 py-2.5 text-sm uppercase tracking-widest text-white transition-colors hover:bg-green-800 disabled:opacity-50"
                  >
                    <Globe size={15} />
                    {publishMutation.isPending ? 'Đang xuất bản…' : 'Xuất bản công thức'}
                  </button>
                ) : (
                  <button
                    type="button"
                    disabled={unpublishMutation.isPending}
                    onClick={() => setConfirmModal('unpublish')}
                    className="flex items-center gap-2 border border-amber-600 bg-amber-50 px-5 py-2.5 text-sm uppercase tracking-widest text-amber-800 transition-colors hover:bg-amber-100 disabled:opacity-50"
                  >
                    <EyeOff size={15} />
                    {unpublishMutation.isPending ? 'Đang gỡ…' : 'Gỡ xuất bản'}
                  </button>
                )}
              </div>
            </div>

            {/* P7-05 Validation Checklist Summary with Deep Links */}
            <div className="mt-6 border border-border bg-secondary/20 p-4">
              <div className="flex items-center justify-between mb-3">
                <h4 className="font-serif text-lg text-foreground">Tổng hợp điều kiện xuất bản</h4>
                <span
                  className={`text-xs font-medium px-2 py-0.5 border ${
                    isPublishReady
                      ? 'border-green-200 bg-green-50 text-green-700'
                      : 'border-amber-200 bg-amber-50 text-amber-700'
                  }`}
                >
                  {validationChecks.filter((c) => c.isValid).length} / {validationChecks.length} tiêu chuẩn
                  đạt
                </span>
              </div>

              <ul className="space-y-2 text-sm">
                {validationChecks.map((item) => (
                  <li
                    key={item.id}
                    className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-1 p-2 rounded bg-background border border-border"
                  >
                    <div className="flex items-center gap-2">
                      {item.isValid ? (
                        <CheckCircle2 size={16} className="text-green-600 shrink-0" />
                      ) : (
                        <AlertCircle size={16} className="text-amber-600 shrink-0" />
                      )}
                      <span className={item.isValid ? 'text-foreground' : 'text-amber-800 font-medium'}>
                        {item.label}: {item.isValid ? 'Đã đạt' : item.message}
                      </span>
                    </div>

                    {!item.isValid && (
                      <button
                        type="button"
                        onClick={() => handleStepJump(item.step)}
                        className="text-xs uppercase tracking-wider font-medium text-primary hover:underline self-end sm:self-auto"
                      >
                        Chuyển đến: {item.stepLabel} →
                      </button>
                    )}
                  </li>
                ))}
              </ul>

              {isPublishReady ? (
                <p className="mt-3 text-xs text-green-700 flex items-center gap-1.5 font-medium">
                  <CheckCircle2 size={14} /> Công thức đã đáp ứng đầy đủ điều kiện để xuất bản công khai.
                </p>
              ) : (
                <p className="mt-3 text-xs text-amber-700 flex items-center gap-1.5">
                  <AlertCircle size={14} /> Vui lòng hoàn thành các tiêu chuẩn chưa đạt ở trên trước khi xuất
                  bản.
                </p>
              )}
            </div>

            {/* Recipe Content Preview */}
            <div className="mt-6 space-y-6">
              <div>
                <h4 className="font-serif text-lg font-medium text-foreground">Mô tả</h4>
                <p className="mt-1 text-sm text-muted-foreground leading-relaxed">{recipe.description}</p>
              </div>

              <div className="grid gap-6 md:grid-cols-2">
                <div>
                  <h4 className="font-medium text-foreground">Nguyên liệu ({recipe.ingredients.length})</h4>
                  <ul className="mt-2 list-disc space-y-1 pl-5 text-sm">
                    {recipe.ingredients.map((item) => (
                      <li key={item.id}>
                        {item.quantity ?? 'Vừa đủ'} {item.unit} {item.name}
                        {item.notes ? ` (${item.notes})` : ''}
                      </li>
                    ))}
                  </ul>
                </div>
                <div>
                  <h4 className="font-medium text-foreground">Các bước thực hiện ({recipe.steps.length})</h4>
                  <ol className="mt-2 list-decimal space-y-2 pl-5 text-sm">
                    {recipe.steps.map((item) => (
                      <li key={item.id}>
                        <strong>{item.title}:</strong> {item.description}
                        {item.timerMinutes && (
                          <span className="ml-2 text-xs text-amber-700 font-medium">
                            (⏱ {item.timerMinutes} phút)
                          </span>
                        )}
                      </li>
                    ))}
                  </ol>
                </div>
              </div>
            </div>
          </div>

          {/* Stepper Navigation */}
          <div className="flex items-center justify-between border-t border-border pt-4">
            <button
              type="button"
              onClick={() => setStage('images')}
              className="flex items-center gap-1.5 text-xs uppercase tracking-wider text-muted-foreground hover:text-foreground"
            >
              <ArrowLeft size={14} /> Quay lại: Hình ảnh
            </button>
          </div>
        </article>
      )}

      {/* P7-04 Publication Confirmation Modal */}
      {confirmModal && (
        <div
          role="dialog"
          aria-modal="true"
          aria-labelledby="wizard-confirm-title"
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
        >
          <div className="w-full max-w-md border border-border bg-background p-6 shadow-xl space-y-4">
            <h3 id="wizard-confirm-title" className="font-serif text-2xl text-foreground">
              {confirmModal === 'publish' ? 'Xác nhận xuất bản công thức' : 'Xác nhận gỡ xuất bản'}
            </h3>
            <p className="text-sm text-muted-foreground leading-relaxed">
              {confirmModal === 'publish' ? (
                <>
                  Công thức <strong>«{recipe.title}»</strong> sẽ được hiển thị công khai trên website cho toàn
                  bộ người đọc truy cập. Bạn có chắc chắn muốn xuất bản ngay bây giờ?
                </>
              ) : (
                <>
                  Công thức <strong>«{recipe.title}»</strong> sẽ bị gỡ khỏi trang công khai và chuyển về trạng
                  thái bản nháp riêng tư.
                </>
              )}
            </p>
            <div className="flex items-center justify-end gap-3 pt-3 border-t border-border">
              <button
                type="button"
                disabled={publishMutation.isPending || unpublishMutation.isPending}
                onClick={() => setConfirmModal(null)}
                className="border border-border px-4 py-2 text-xs uppercase tracking-wider text-muted-foreground hover:bg-secondary"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                disabled={publishMutation.isPending || unpublishMutation.isPending}
                onClick={() => {
                  if (confirmModal === 'publish') publishMutation.mutate()
                  else if (confirmModal === 'unpublish') unpublishMutation.mutate()
                }}
                className={`px-5 py-2 text-xs uppercase tracking-wider text-white ${
                  confirmModal === 'publish'
                    ? 'bg-green-700 hover:bg-green-800'
                    : 'bg-amber-700 hover:bg-amber-800'
                }`}
              >
                {publishMutation.isPending || unpublishMutation.isPending ? 'Đang xử lý…' : 'Xác nhận'}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}
