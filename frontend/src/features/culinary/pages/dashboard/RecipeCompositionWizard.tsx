'use client'

import Image from 'next/image'
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
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useToast } from '@/components/ui/toast'
import { FieldError } from '@/components/ui/field-error'
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
import { Modal } from '@/components/ui/modal'
import { ApiProblem, apiErrorMessage } from '@/lib/api/problem-details'
import { formMessages } from '../../forms/messages'
import { setServerFieldErrors } from '../../forms/server-errors'
import { ingredientSchema, stepSchema } from '../../forms/content-schemas'
import { computeRecipeValidation, type Stage, type WizardAllSteps } from './recipe-validation'

const EMPTY_INGREDIENT: IngredientWrite = { name: '', quantity: null, unit: null, notes: null, orderIndex: 0 }
const EMPTY_STEP: StepWrite = { title: '', description: '', timerMinutes: null, imageUrl: null }

interface Props {
  recipe: Recipe
  version: number
  onChanged: (version: number) => Promise<void>
  activeStage?: Stage
  onStageChange?: (stage: Stage) => void
  onNavigateToStep?: (step: WizardAllSteps) => void
  onDirtyChange?: (dirty: boolean) => void
}

function errorMessage(error: unknown) {
  return apiErrorMessage(error, 'COMPOSITION_SAVE_FAILED')
}

function PrivateImage({ image }: { image: RecipeImage }) {
  const containerRef = useRef<HTMLDivElement>(null)
  const [shouldLoad, setShouldLoad] = useState(false)
  const [source, setSource] = useState<string | null>(null)
  const path = image.thumbnailUrl ?? image.mediumUrl ?? image.originalUrl

  useEffect(() => {
    const element = containerRef.current
    if (!element || !('IntersectionObserver' in window)) {
      setShouldLoad(true)
      return undefined
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (!entry.isIntersecting) return
        setShouldLoad(true)
        observer.disconnect()
      },
      { rootMargin: '300px' },
    )
    observer.observe(element)
    return () => observer.disconnect()
  }, [])

  useEffect(() => {
    if (!shouldLoad) return undefined

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
  }, [path, shouldLoad])

  return (
    <div ref={containerRef} className="relative h-32 w-full bg-muted">
      {source ? (
        <Image
          fill
          sizes="(min-width: 768px) 50vw, 100vw"
          unoptimized
          src={source}
          alt={image.altText ?? ''}
          loading="lazy"
          decoding="async"
          className="h-32 w-full object-cover"
        />
      ) : (
        <div className="flex h-32 items-center justify-center text-xs text-muted-foreground">
          {image.processingStatus === 'failed' ? 'Xử lý ảnh thất bại' : 'Đang xử lý ảnh…'}
        </div>
      )}
    </div>
  )
}

export function RecipeCompositionWizard({
  recipe,
  version,
  onChanged,
  activeStage = undefined,
  onStageChange = undefined,
  onNavigateToStep = undefined,
  onDirtyChange = undefined,
}: Props) {
  const { showToast } = useToast()
  const [internalStage, setInternalStage] = useState<Stage>('ingredients')
  const stage = activeStage ?? internalStage

  const setStage = (next: Stage) => {
    setInternalStage(next)
    onStageChange?.(next)
  }

  const ingredientFields = useForm<IngredientWrite>({
    resolver: zodResolver(ingredientSchema),
    defaultValues: EMPTY_INGREDIENT,
  })
  const ingredientForm = useWatch({ control: ingredientFields.control, compute: (values) => values })
  const setIngredientForm = ingredientFields.reset
  const [editingIngredient, setEditingIngredient] = useState<string | null>(null)
  const stepFields = useForm<StepWrite>({ resolver: zodResolver(stepSchema), defaultValues: EMPTY_STEP })
  const stepForm = useWatch({ control: stepFields.control, compute: (values) => values })
  const setStepForm = stepFields.reset
  const [editingStep, setEditingStep] = useState<string | null>(null)
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [imageAlt, setImageAlt] = useState('')
  const [imagePrimary, setImagePrimary] = useState(false)
  const [uploadProgress, setUploadProgress] = useState(0)
  const [clientError, setClientError] = useState<string | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<{
    id: string
    kind: 'ingredient' | 'step' | 'image'
    label: string
  } | null>(null)
  const [deleting, setDeleting] = useState(false)

  const originalIngredient = recipe.ingredients.find((item) => item.id === editingIngredient)
  const ingredientBaseline = originalIngredient
    ? {
        name: originalIngredient.name,
        quantity: originalIngredient.quantity,
        unit: originalIngredient.unit,
        notes: originalIngredient.notes,
        orderIndex: originalIngredient.orderIndex,
      }
    : EMPTY_INGREDIENT
  const originalStep = recipe.steps.find((item) => item.id === editingStep)
  const stepBaseline = originalStep
    ? {
        title: originalStep.title,
        description: originalStep.description,
        timerMinutes: originalStep.timerMinutes,
        imageUrl: originalStep.imageUrl,
        stepNumber: originalStep.stepNumber,
      }
    : EMPTY_STEP
  const dirty =
    JSON.stringify(ingredientForm) !== JSON.stringify(ingredientBaseline) ||
    JSON.stringify(stepForm) !== JSON.stringify(stepBaseline) ||
    Boolean(imageFile || imageAlt || imagePrimary)

  useEffect(() => {
    onDirtyChange?.(dirty)
  }, [dirty, onDirtyChange])

  // Confirmation state for Publish / Unpublish in preview
  const [confirmModal, setConfirmModal] = useState<'publish' | 'unpublish' | null>(null)

  const ingredientMutation = useMutation({
    mutationFn: (values: IngredientWrite) =>
      editingIngredient
        ? updateIngredient(recipe.id, editingIngredient, version, values)
        : createIngredient(recipe.id, version, { ...values, orderIndex: recipe.ingredients.length }),
    onSuccess: async (result) => {
      setIngredientForm(EMPTY_INGREDIENT)
      setEditingIngredient(null)
      await onChanged(result.meta.recipeVersion)
      showToast('Đã lưu nguyên liệu.')
    },
    onError: (error) => {
      showToast(apiErrorMessage(error, 'CONTENT_SAVE_FAILED'))
      setServerFieldErrors(error, ingredientFields.setError, {
        Name: ['name', formMessages.ingredientNameRequired],
        Quantity: ['quantity', formMessages.quantity],
        Unit: ['unit', formMessages.unitLength],
        Notes: ['notes', formMessages.notesLength],
      })
    },
  })

  const stepMutation = useMutation({
    mutationFn: (values: StepWrite) =>
      editingStep
        ? updateStep(recipe.id, editingStep, version, values)
        : createStep(recipe.id, version, values),
    onSuccess: async (result) => {
      setStepForm(EMPTY_STEP)
      setEditingStep(null)
      await onChanged(result.meta.recipeVersion)
      showToast('Đã lưu bước thực hiện.')
    },
    onError: (error) => {
      showToast(apiErrorMessage(error, 'CONTENT_SAVE_FAILED'))
      setServerFieldErrors(error, stepFields.setError, {
        Title: ['title', formMessages.stepTitleRequired],
        Description: ['description', formMessages.stepDescription],
        TimerMinutes: ['timerMinutes', formMessages.timeMinimum],
        ImageUrl: ['imageUrl', formMessages.imageUrlInvalid],
      })
    },
  })

  const imageMutation = useMutation({
    mutationFn: async () => {
      if (!imageFile) throw new Error(formMessages.uploadRequired)
      return uploadRecipeImage(recipe.id, version, imageFile, imageAlt, imagePrimary, setUploadProgress)
    },
    onSuccess: async (result) => {
      setImageFile(null)
      setImageAlt('')
      setImagePrimary(false)
      setUploadProgress(0)
      await onChanged(result.meta.recipeVersion)
      showToast('Đã tải ảnh lên.')
    },
    onError: (error) => showToast(errorMessage(error)),
  })

  const publishMutation = useMutation({
    mutationFn: () => publishRecipe(recipe.id, version),
    onSuccess: async (updated) => {
      setConfirmModal(null)
      setClientError(null)
      await onChanged(updated.version)
      showToast('Công thức đã được xuất bản thành công.')
    },
    onError: (error) => {
      showToast(apiErrorMessage(error, 'CONTENT_SAVE_FAILED'))
      setConfirmModal(null)
      if (error instanceof ApiProblem && error.problem.code === 'RECIPE_PUBLISH_INCOMPLETE') {
        setClientError(apiErrorMessage(error))
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
      showToast('Đã gỡ xuất bản công thức (chuyển về bản nháp).')
    },
    onError: (error) => {
      showToast(apiErrorMessage(error, 'CONTENT_SAVE_FAILED'))
      setConfirmModal(null)
      setClientError(errorMessage(error))
    },
  })

  const run = async (action: () => Promise<number | void>) => {
    setClientError(null)
    try {
      const nextVersion = await action()
      await onChanged(nextVersion ?? version + 1)
      showToast('Đã cập nhật nội dung công thức.')
    } catch (error) {
      setClientError(errorMessage(error))
      showToast(errorMessage(error))
    }
  }

  const moveIngredient = (item: Ingredient, direction: -1 | 1) => {
    const target = item.orderIndex + direction
    if (target < 0 || target >= recipe.ingredients.length) return
    run(async () => {
      const result = await updateIngredient(recipe.id, item.id, version, { ...item, orderIndex: target })
      return result.meta.recipeVersion
    })
  }

  const confirmDelete = async () => {
    if (!deleteTarget || deleting) return
    setDeleting(true)
    try {
      const remove = { ingredient: deleteIngredient, step: deleteStep, image: deleteRecipeImage }[
        deleteTarget.kind
      ]
      await run(() => remove(recipe.id, deleteTarget.id, version))
      setDeleteTarget(null)
    } finally {
      setDeleting(false)
    }
  }

  const moveStep = (item: RecipeStep, direction: -1 | 1) => {
    const target = item.stepNumber + direction
    if (target < 1 || target > recipe.steps.length) return
    run(async () => {
      const result = await updateStep(recipe.id, item.id, version, { ...item, stepNumber: target })
      return result.meta.recipeVersion
    })
  }

  const submitIngredient = (values: IngredientWrite) => {
    setClientError(null)
    ingredientMutation.mutate(values)
  }

  const submitStep = (values: StepWrite) => {
    setClientError(null)
    stepMutation.mutate(values)
  }

  const submitImage = (event: FormEvent) => {
    event.preventDefault()
    if (!imageFile) {
      setClientError(formMessages.uploadRequired)
      return
    }
    if (imageFile.size > 5 * 1024 * 1024) {
      setClientError(formMessages.uploadSize)
      return
    }
    if (!['image/jpeg', 'image/png', 'image/webp', 'image/avif'].includes(imageFile.type)) {
      setClientError(formMessages.uploadType)
      return
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
        <span className="text-xs text-muted-foreground">
          Bấm Thêm, Lưu hoặc Tải ảnh lên để lưu nội dung đang nhập.
        </span>
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
            <form
              noValidate
              onSubmit={ingredientFields.handleSubmit(submitIngredient)}
              className="space-y-3 border border-border p-4 bg-background"
            >
              <h3 className="font-medium text-foreground">
                {editingIngredient ? 'Sửa nguyên liệu' : 'Thêm nguyên liệu mới'}
              </h3>
              <input
                aria-label="Tên nguyên liệu"
                placeholder="Tên nguyên liệu (bắt buộc)"
                maxLength={100}
                className={inputClass}
                {...ingredientFields.register('name')}
                aria-invalid={Boolean(ingredientFields.formState.errors.name)}
                aria-describedby={
                  ingredientFields.formState.errors.name ? 'ingredient-name-error' : undefined
                }
              />
              <FieldError
                id="ingredient-name-error"
                message={ingredientFields.formState.errors.name?.message}
              />
              <div className="grid grid-cols-2 gap-3">
                <input
                  aria-label="Số lượng"
                  placeholder="Số lượng (vừa đủ nếu trống)"
                  type="number"
                  min="0.001"
                  step="0.001"
                  className={inputClass}
                  {...ingredientFields.register('quantity', {
                    setValueAs: (value: string) => (value == null || value === '' ? null : Number(value)),
                  })}
                  aria-invalid={Boolean(ingredientFields.formState.errors.quantity)}
                  aria-describedby={
                    ingredientFields.formState.errors.quantity ? 'ingredient-quantity-error' : undefined
                  }
                />
                <FieldError
                  id="ingredient-quantity-error"
                  message={ingredientFields.formState.errors.quantity?.message}
                />
                <input
                  aria-label="Đơn vị"
                  placeholder="Đơn vị (g, ml, muỗng…)"
                  maxLength={50}
                  className={inputClass}
                  {...ingredientFields.register('unit', {
                    setValueAs: (value: string) => (value == null || value === '' ? null : value),
                  })}
                  aria-invalid={Boolean(ingredientFields.formState.errors.unit)}
                  aria-describedby={
                    ingredientFields.formState.errors.unit ? 'ingredient-unit-error' : undefined
                  }
                />
                <FieldError
                  id="ingredient-unit-error"
                  message={ingredientFields.formState.errors.unit?.message}
                />
              </div>
              <input
                aria-label="Ghi chú nguyên liệu"
                placeholder="Ghi chú (tùy chọn)"
                maxLength={500}
                className={inputClass}
                {...ingredientFields.register('notes', {
                  setValueAs: (value: string) => (value == null || value === '' ? null : value),
                })}
                aria-invalid={Boolean(ingredientFields.formState.errors.notes)}
                aria-describedby={
                  ingredientFields.formState.errors.notes ? 'ingredient-notes-error' : undefined
                }
              />
              <FieldError
                id="ingredient-notes-error"
                message={ingredientFields.formState.errors.notes?.message}
              />
              <div className="flex items-center gap-2 pt-1">
                <button
                  type="submit"
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
                    onClick={() =>
                      setDeleteTarget({
                        kind: 'ingredient',
                        id: item.id,
                        label: `nguyên liệu «${item.name}»`,
                      })
                    }
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
            <form
              noValidate
              onSubmit={stepFields.handleSubmit(submitStep)}
              className="space-y-3 border border-border p-4 bg-background"
            >
              <h3 className="font-medium text-foreground">
                {editingStep ? 'Sửa bước thực hiện' : 'Thêm bước mới'}
              </h3>
              <input
                aria-label="Tiêu đề bước"
                placeholder="Tiêu đề bước (bắt buộc, ví dụ: Sơ chế rau củ)"
                maxLength={200}
                className={inputClass}
                {...stepFields.register('title')}
                aria-invalid={Boolean(stepFields.formState.errors.title)}
                aria-describedby={stepFields.formState.errors.title ? 'step-title-error' : undefined}
              />
              <FieldError id="step-title-error" message={stepFields.formState.errors.title?.message} />
              <textarea
                aria-label="Mô tả bước"
                placeholder="Mô tả chi tiết cách thực hiện…"
                maxLength={2000}
                rows={4}
                className={inputClass}
                {...stepFields.register('description')}
                aria-invalid={Boolean(stepFields.formState.errors.description)}
                aria-describedby={
                  stepFields.formState.errors.description ? 'step-description-error' : undefined
                }
              />
              <FieldError
                id="step-description-error"
                message={stepFields.formState.errors.description?.message}
              />
              <input
                aria-label="Hẹn giờ phút"
                placeholder="Hẹn giờ nấu (phút, tùy chọn)"
                type="number"
                min="0"
                className={inputClass}
                {...stepFields.register('timerMinutes', {
                  setValueAs: (value: string) => (value == null || value === '' ? null : Number(value)),
                })}
                aria-invalid={Boolean(stepFields.formState.errors.timerMinutes)}
                aria-describedby={
                  stepFields.formState.errors.timerMinutes ? 'step-timerMinutes-error' : undefined
                }
              />
              <FieldError
                id="step-timerMinutes-error"
                message={stepFields.formState.errors.timerMinutes?.message}
              />
              <div className="flex items-center gap-2 pt-1">
                <button
                  type="submit"
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
                      onClick={() =>
                        setDeleteTarget({ kind: 'step', id: item.id, label: `bước «${item.title}»` })
                      }
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
              <ImagePlus size={17} /> Tải lên hình ảnh (JPEG, PNG, WebP hoặc AVIF, tối đa 5 MB)
            </label>
            <input
              id="recipe-image"
              type="file"
              accept="image/jpeg,image/png,image/webp,image/avif"
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
              <progress aria-label="Tiến độ tải ảnh" className="w-full h-2" max={100} value={uploadProgress}>
                {uploadProgress}%
              </progress>
            )}
            <button
              type="submit"
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
                          run(async () => {
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
                        onClick={() => {
                          run(async () => {
                            const result = await updateRecipeImage(recipe.id, image.id, version, {
                              altText: image.altText,
                              isPrimary: true,
                              orderIndex: image.orderIndex,
                            })
                            return result.meta.recipeVersion
                          })
                        }}
                      >
                        <Star size={14} className={image.isPrimary ? 'fill-amber-500 text-amber-500' : ''} />
                        {image.isPrimary ? 'Ảnh chính' : 'Đặt làm ảnh chính'}
                      </button>
                      <button
                        type="button"
                        aria-label="Xóa ảnh"
                        className="text-muted-foreground hover:text-red-600"
                        onClick={() =>
                          setDeleteTarget({ kind: 'image', id: image.id, label: 'ảnh này khỏi công thức' })
                        }
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
        <Modal
          open
          title={confirmModal === 'publish' ? 'Xác nhận xuất bản công thức' : 'Xác nhận gỡ xuất bản'}
          onClose={() => setConfirmModal(null)}
          busy={publishMutation.isPending || unpublishMutation.isPending}
        >
          <div className="space-y-4">
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
        </Modal>
      )}
      {deleteTarget && (
        <Modal open title="Xác nhận xóa nội dung" onClose={() => setDeleteTarget(null)} busy={deleting}>
          <p>Bạn muốn xóa {deleteTarget.label}? Thao tác này không thể hoàn tác.</p>
          <div className="mt-6 flex justify-end gap-3">
            <button
              type="button"
              disabled={deleting}
              onClick={() => setDeleteTarget(null)}
              className="border border-border px-4 py-2"
            >
              Hủy
            </button>
            <button
              type="button"
              disabled={deleting}
              onClick={confirmDelete}
              className="bg-red-700 px-4 py-2 text-white"
            >
              {deleting ? 'Đang xóa…' : 'Xóa nội dung'}
            </button>
          </div>
        </Modal>
      )}
    </section>
  )
}
