'use client'

import Image from 'next/image'
import { useMutation } from '@tanstack/react-query'
import { arrayMove } from '@dnd-kit/sortable'
import { ArrowDown, ArrowLeft, ArrowRight, ArrowUp, ImagePlus, Plus, Save, Star, Trash2 } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
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
import { apiErrorMessage } from '@/lib/api/problem-details'
import { getTimerInput, TIMER_UNIT_SECONDS, type TimerUnit } from '@/lib/recipe-timer'
import { formMessages } from '../../forms/messages'
import { setServerFieldErrors } from '../../forms/server-errors'
import { ingredientSchema, stepSchema } from '../../forms/content-schemas'
import type { Stage, WizardAllSteps } from './recipe-validation'
import { RecipeDetail } from '../RecipeDetail'
import { RecipeStepList } from './RecipeStepList'

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
  const [timerUnit, setTimerUnit] = useState<TimerUnit>('minutes')
  const imageInputRef = useRef<HTMLInputElement>(null)
  const imageUploadPendingRef = useRef(false)
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [draggingImage, setDraggingImage] = useState(false)
  const [uploadProgress, setUploadProgress] = useState(0)
  const [clientError, setClientError] = useState<string | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<{
    id: string
    kind: 'ingredient' | 'step' | 'image'
    label: string
  } | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [stepOrder, setStepOrder] = useState<{ source: RecipeStep[]; steps: RecipeStep[] } | null>(null)
  const reorderPendingRef = useRef(false)
  const orderedSteps = stepOrder?.source === recipe.steps ? stepOrder.steps : recipe.steps

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
  const originalTimer = getTimerInput(originalStep?.timerMinutes ?? null)
  const stepBaseline = originalStep
    ? {
        title: originalStep.title,
        description: originalStep.description,
        timerMinutes: originalTimer.value,
        imageUrl: originalStep.imageUrl,
      }
    : EMPTY_STEP
  const dirty =
    JSON.stringify(ingredientForm) !== JSON.stringify(ingredientBaseline) ||
    JSON.stringify(stepForm) !== JSON.stringify(stepBaseline) ||
    (stepForm.timerMinutes !== null && timerUnit !== originalTimer.unit) ||
    Boolean(imageFile)

  useEffect(() => {
    onDirtyChange?.(dirty)
  }, [dirty, onDirtyChange])

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
      setTimerUnit('minutes')
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

  const reorderStepMutation = useMutation({
    mutationFn: ({ step, position }: { step: RecipeStep; position: number }) =>
      updateStep(recipe.id, step.id, version, { ...step, stepNumber: position }),
    onMutate: ({ step, position }) => {
      reorderPendingRef.current = true
      setClientError(null)
      const previousOrder = stepOrder
      const from = orderedSteps.findIndex((item) => item.id === step.id)
      setStepOrder({
        source: recipe.steps,
        steps: arrayMove(orderedSteps, from, position - 1).map((item, index) => ({
          ...item,
          stepNumber: index + 1,
        })),
      })
      return previousOrder
    },
    onSuccess: async (result) => {
      await onChanged(result.meta.recipeVersion)
    },
    onError: (error, _variables, previousOrder) => {
      setStepOrder(previousOrder ?? null)
      setClientError(errorMessage(error))
      showToast(errorMessage(error))
    },
    onSettled: () => {
      reorderPendingRef.current = false
    },
  })

  const imageMutation = useMutation({
    mutationFn: (file: File) =>
      uploadRecipeImage(
        recipe.id,
        version,
        file,
        recipe.title.slice(0, 200),
        !recipe.images.some((image) => image.isPrimary),
        setUploadProgress,
      ),
    onSuccess: async (result) => {
      await onChanged(result.meta.recipeVersion)
      showToast('Đã tải ảnh lên.')
    },
    onError: (error) => showToast(apiErrorMessage(error, 'FILE_UPLOAD_FAILED')),
    onSettled: () => {
      imageUploadPendingRef.current = false
      setImageFile(null)
      setUploadProgress(0)
    },
  })

  const run = async (action: () => Promise<number | void>) => {
    setClientError(null)
    try {
      const nextVersion = await action()
      await onChanged(nextVersion ?? version + 1)
      showToast('Đã cập nhật nội dung công thức.')
      return true
    } catch (error) {
      setClientError(errorMessage(error))
      showToast(errorMessage(error))
      return false
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
      const success = await run(() => remove(recipe.id, deleteTarget.id, version))
      if (success) setDeleteTarget(null)
    } finally {
      setDeleting(false)
    }
  }

  const moveStep = (step: RecipeStep, position: number) => {
    if (
      reorderPendingRef.current ||
      stepMutation.isPending ||
      deleting ||
      position === step.stepNumber ||
      position < 1 ||
      position > orderedSteps.length
    )
      return
    reorderPendingRef.current = true
    reorderStepMutation.mutate({ step, position })
  }

  const submitIngredient = (values: IngredientWrite) => {
    setClientError(null)
    ingredientMutation.mutate(values)
  }

  const submitStep = (values: StepWrite) => {
    setClientError(null)
    stepMutation.mutate({
      title: values.title,
      description: values.description,
      imageUrl: values.imageUrl,
      timerMinutes:
        values.timerMinutes === null ? null : (values.timerMinutes * TIMER_UNIT_SECONDS[timerUnit]) / 60,
    })
  }

  const submitImage = (file: File | undefined) => {
    if (!file || imageUploadPendingRef.current || deleting) return
    imageMutation.reset()
    if (file.size > 5 * 1024 * 1024) {
      setClientError(formMessages.uploadSize)
      return
    }
    if (!['image/jpeg', 'image/png', 'image/webp', 'image/avif'].includes(file.type)) {
      setClientError(formMessages.uploadType)
      return
    }
    setClientError(null)
    imageUploadPendingRef.current = true
    setImageFile(file)
    setUploadProgress(0)
    imageMutation.mutate(file)
  }

  const inputClass =
    'w-full border border-border bg-background px-3 py-2 text-sm focus:border-primary focus:outline-none'
  return (
    <section
      aria-labelledby="composition-heading"
      className={stage !== 'preview' ? 'xl:flex xl:h-full xl:min-h-0 xl:flex-col' : undefined}
    >
      <h2 id="composition-heading" className="sr-only">
        Nội dung công thức
      </h2>

      {/* Errors */}
      {clientError && (
        <p role="alert" className="mt-5 shrink-0 border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {clientError}
        </p>
      )}
      {(ingredientMutation.isError || stepMutation.isError || imageMutation.isError) && (
        <p role="alert" className="mt-5 shrink-0 border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {imageMutation.isError
            ? apiErrorMessage(imageMutation.error, 'FILE_UPLOAD_FAILED')
            : errorMessage(ingredientMutation.error ?? stepMutation.error)}
        </p>
      )}

      {/* Stage: Ingredients */}
      {stage === 'ingredients' && (
        <div className="flex min-h-0 flex-col gap-6 xl:flex-1">
          <div className="grid items-start gap-6 xl:min-h-0 xl:flex-1 xl:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)] xl:grid-rows-[minmax(0,1fr)]">
            <form
              noValidate
              onSubmit={ingredientFields.handleSubmit(submitIngredient)}
              className="min-w-0 space-y-3 border border-border p-4 bg-background xl:max-h-full xl:overflow-y-auto xl:overscroll-contain"
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
                <div className="min-w-0">
                  <label
                    htmlFor="ingredient-quantity"
                    className="mb-1.5 block text-xs font-medium text-foreground"
                  >
                    Số lượng
                  </label>
                  <input
                    id="ingredient-quantity"
                    aria-label="Số lượng"
                    placeholder="Vừa đủ"
                    type="number"
                    inputMode="decimal"
                    min="0.001"
                    step="0.001"
                    onKeyDown={(event) => {
                      if (!event.ctrlKey && !event.metaKey && ['e', 'E'].includes(event.key)) {
                        event.preventDefault()
                      }
                    }}
                    onPaste={(event) => {
                      if (/[eE]/.test(event.clipboardData.getData('text'))) event.preventDefault()
                    }}
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
                </div>
                <div className="min-w-0">
                  <label
                    htmlFor="ingredient-unit"
                    className="mb-1.5 block text-xs font-medium text-foreground"
                  >
                    Đơn vị
                  </label>
                  <input
                    id="ingredient-unit"
                    aria-label="Đơn vị"
                    placeholder="g, ml, muỗng…"
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

            <div className="min-w-0 xl:flex xl:h-full xl:min-h-0 xl:flex-col">
              <div className="mb-3 flex shrink-0 items-center justify-between">
                <h4 className="text-xs uppercase tracking-widest text-muted-foreground font-medium">
                  Danh sách nguyên liệu ({recipe.ingredients.length})
                </h4>
              </div>
              <div
                role="list"
                aria-label="Danh sách nguyên liệu"
                className="max-h-[min(50dvh,28rem)] space-y-2 overflow-y-auto overscroll-contain pb-3 pr-2 [scrollbar-gutter:stable] xl:max-h-none xl:min-h-0 xl:flex-1"
              >
                {recipe.ingredients.length === 0 && (
                  <div className="border border-dashed border-border p-8 text-center text-sm text-muted-foreground">
                    Chưa có nguyên liệu nào. Hãy thêm ít nhất 1 nguyên liệu để đủ điều kiện xuất bản.
                  </div>
                )}
                {recipe.ingredients.map((item) => (
                  <article
                    key={item.id}
                    role="listitem"
                    className="flex items-center gap-3 border border-border bg-background p-3"
                  >
                    <div className="min-w-0 flex-1 wrap-anywhere">
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
          </div>

          {/* Stepper Navigation */}
          <div className="flex shrink-0 items-center justify-between border-t border-border pt-4">
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
        <div className="flex min-h-0 flex-col gap-6 xl:flex-1">
          <div className="grid items-start gap-6 xl:min-h-0 xl:flex-1 xl:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)] xl:grid-rows-[minmax(0,1fr)]">
            <form
              noValidate
              onSubmit={stepFields.handleSubmit(submitStep)}
              className="min-w-0 border border-border p-4 bg-background xl:max-h-full xl:overflow-y-auto xl:overscroll-contain"
            >
              <fieldset
                disabled={stepMutation.isPending || reorderStepMutation.isPending || deleting}
                className="min-w-0 space-y-3"
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
                <div className="grid grid-cols-[minmax(0,1fr)_6rem] gap-3">
                  <input
                    aria-label="Thời gian hẹn giờ"
                    placeholder="Hẹn giờ nấu (tùy chọn)"
                    type="number"
                    inputMode="numeric"
                    min="0"
                    step="1"
                    onKeyDown={(event) => {
                      if (
                        !event.ctrlKey &&
                        !event.metaKey &&
                        ['e', 'E', '+', '-', '.', ','].includes(event.key)
                      ) {
                        event.preventDefault()
                      }
                    }}
                    onPaste={(event) => {
                      if (!/^\d+$/.test(event.clipboardData.getData('text'))) event.preventDefault()
                    }}
                    className={inputClass}
                    {...stepFields.register('timerMinutes', {
                      setValueAs: (value: string) => (value == null || value === '' ? null : Number(value)),
                    })}
                    aria-invalid={Boolean(stepFields.formState.errors.timerMinutes)}
                    aria-describedby={
                      stepFields.formState.errors.timerMinutes ? 'step-timerMinutes-error' : undefined
                    }
                  />
                  <select
                    aria-label="Đơn vị hẹn giờ"
                    value={timerUnit}
                    onChange={(event) => setTimerUnit(event.target.value as TimerUnit)}
                    className={inputClass}
                  >
                    <option value="hours">Giờ</option>
                    <option value="minutes">Phút</option>
                    <option value="seconds">Giây</option>
                  </select>
                </div>
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
                        setTimerUnit('minutes')
                      }}
                      className="border border-border px-3 py-2 text-xs uppercase tracking-wider text-muted-foreground hover:bg-secondary"
                    >
                      Hủy sửa
                    </button>
                  )}
                </div>
              </fieldset>
            </form>

            <RecipeStepList
              steps={orderedSteps}
              disabled={stepMutation.isPending || reorderStepMutation.isPending || deleting}
              saving={reorderStepMutation.isPending}
              onMove={moveStep}
              onEdit={(item) => {
                setEditingStep(item.id)
                const timer = getTimerInput(item.timerMinutes)
                setTimerUnit(timer.unit)
                setStepForm({
                  title: item.title,
                  description: item.description,
                  timerMinutes: timer.value,
                  imageUrl: item.imageUrl,
                })
              }}
              onDelete={(item) =>
                setDeleteTarget({ kind: 'step', id: item.id, label: `bước «${item.title}»` })
              }
            />
          </div>

          {/* Stepper Navigation */}
          <div className="flex shrink-0 items-center justify-between border-t border-border pt-4">
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
        <div className="flex min-h-0 flex-col gap-6 xl:flex-1">
          <div className="shrink-0 space-y-3">
            <input
              id="recipe-image"
              ref={imageInputRef}
              type="file"
              aria-label="Chọn ảnh từ máy tính"
              accept="image/jpeg,image/png,image/webp,image/avif"
              disabled={imageMutation.isPending || deleting}
              onChange={(event) => {
                const input = event.currentTarget
                const file = input.files?.[0]
                input.value = ''
                submitImage(file)
              }}
              className="hidden"
            />
            <button
              type="button"
              disabled={imageMutation.isPending || deleting}
              onClick={() => imageInputRef.current?.click()}
              onDragOver={(event) => {
                event.preventDefault()
                const transfer = event.dataTransfer
                transfer.dropEffect = imageMutation.isPending || deleting ? 'none' : 'copy'
                if (!imageMutation.isPending && !deleting) setDraggingImage(true)
              }}
              onDragLeave={() => setDraggingImage(false)}
              onDrop={(event) => {
                event.preventDefault()
                setDraggingImage(false)
                if (imageUploadPendingRef.current || deleting) return
                if (event.dataTransfer.files.length > 1) {
                  setClientError('Vui lòng chọn hoặc kéo thả một ảnh mỗi lần.')
                  return
                }
                submitImage(event.dataTransfer.files[0])
              }}
              className={`w-full border border-dashed p-5 text-center transition-colors disabled:cursor-wait disabled:opacity-60 ${
                draggingImage
                  ? 'border-primary bg-primary/5'
                  : 'border-border bg-background hover:border-primary hover:bg-primary/5'
              }`}
            >
              <span className="pointer-events-none flex flex-col items-center gap-2">
                <ImagePlus size={24} className="text-primary" aria-hidden="true" />
                <span className="text-sm font-medium">
                  {imageMutation.isPending
                    ? 'Đang tải ảnh lên…'
                    : 'Kéo thả một ảnh vào đây hoặc chọn ảnh từ máy'}
                </span>
                <span className="text-xs text-muted-foreground">JPEG, PNG, WebP hoặc AVIF, tối đa 5 MB</span>
              </span>
            </button>
            {imageMutation.isPending && (
              <div className="space-y-1">
                <p role="status" className="truncate text-xs text-muted-foreground">
                  {imageFile?.name} · {uploadProgress}%
                </p>
                <progress
                  aria-label="Tiến độ tải ảnh"
                  className="h-2 w-full"
                  max={100}
                  value={uploadProgress}
                >
                  {uploadProgress}%
                </progress>
              </div>
            )}
          </div>

          <div className="min-w-0 xl:flex xl:min-h-0 xl:flex-1 xl:flex-col">
            <h4 className="mb-3 shrink-0 text-xs font-medium uppercase tracking-widest text-muted-foreground">
              Thư viện ảnh của công thức ({recipe.images.length})
            </h4>
            <div
              role="region"
              aria-label="Thư viện ảnh của công thức"
              className="max-h-[min(50dvh,28rem)] overflow-y-auto overscroll-contain pb-3 pr-2 [scrollbar-gutter:stable] xl:max-h-none xl:min-h-0 xl:flex-1"
            >
              {recipe.images.length === 0 ? (
                <p className="border border-dashed border-border p-6 text-center text-sm text-muted-foreground">
                  Chưa có ảnh nào được tải lên.
                </p>
              ) : (
                <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                  {recipe.images.map((image) => (
                    <article key={image.id} className="border border-border bg-background p-3 space-y-2">
                      <PrivateImage image={image} />
                      <div className="flex items-center justify-between pt-1">
                        <button
                          type="button"
                          disabled={image.isPrimary || imageMutation.isPending || deleting}
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
                          <Star
                            size={14}
                            className={image.isPrimary ? 'fill-amber-500 text-amber-500' : ''}
                          />
                          {image.isPrimary ? 'Ảnh chính' : 'Đặt làm ảnh chính'}
                        </button>
                        <button
                          type="button"
                          aria-label="Xóa ảnh"
                          disabled={imageMutation.isPending || deleting}
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
          </div>

          {/* Stepper Navigation */}
          <div className="flex shrink-0 items-center justify-between border-t border-border pt-4">
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

      {/* Stage: Recipe Preview */}
      {stage === 'preview' && (
        <div className="min-w-0 space-y-6 wrap-anywhere">
          <RecipeDetail recipe={recipe} privatePreview />
          <div className="flex items-center justify-between border-t border-border pt-4">
            <button
              type="button"
              onClick={() => setStage('images')}
              className="flex items-center gap-1.5 text-xs uppercase tracking-wider text-muted-foreground hover:text-foreground"
            >
              <ArrowLeft size={14} /> Quay lại: Hình ảnh
            </button>
          </div>
        </div>
      )}

      {deleteTarget && (
        <Modal open title="Xác nhận xóa nội dung" onClose={() => setDeleteTarget(null)} busy={deleting}>
          <p>Bạn muốn xóa {deleteTarget.label}? Thao tác này không thể hoàn tác.</p>
          {clientError && (
            <p role="alert" className="text-sm text-red-700">
              {clientError}
            </p>
          )}
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
