'use client'

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import dynamic from 'next/dynamic'
import {
  AlertCircle,
  Archive,
  ArrowRight,
  CheckCircle2,
  ChevronDown,
  ChevronLeft,
  ChevronUp,
  Copy,
  Eye,
  EyeOff,
  Globe,
  RotateCcw,
  Save,
  Trash2,
} from 'lucide-react'
import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router'
import { Modal } from '@/components/ui/modal'
import { useUnsavedChanges } from '@/components/navigation-guard'
import { useAuth } from '../../contexts/AuthContext'
import { ApiProblem } from '@/lib/api/problem-details'
import { computeRecipeValidation, type Stage, type WizardAllSteps } from './recipe-validation'
import {
  archiveRecipe,
  createRecipe,
  deleteRecipe,
  getMyRecipe,
  getAdminRecipe,
  listCategories,
  publishRecipe,
  unarchiveRecipe,
  unpublishRecipe,
  updateRecipe,
  type Nutrition,
  type RecipeDifficulty,
  type RecipeStatus,
  type RecipeWrite,
} from '@/lib/api/content-client'

const RecipeCompositionWizard = dynamic(
  () => import('./RecipeCompositionWizard').then((module) => module.RecipeCompositionWizard),
  {
    loading: () => (
      <div
        className="mt-6 border border-border bg-card p-8 text-center text-sm text-muted-foreground"
        role="status"
      >
        Đang tải trình soạn công thức…
      </div>
    ),
  },
)

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

type NutritionField = keyof Nutrition
type NutritionForm = Record<NutritionField, string>

interface RecipeForm {
  title: string
  description: string
  instructions: string
  categoryId: string
  difficulty: RecipeDifficulty
  servings: number
  prepTime: number
  cookTime: number
  nutrition: NutritionForm
}

const EMPTY_NUTRITION: NutritionForm = {
  calories: '',
  protein: '',
  carbohydrates: '',
  fat: '',
  fiber: '',
  sodium: '',
}

const EMPTY_FORM: RecipeForm = {
  title: '',
  description: '',
  instructions: '',
  categoryId: '',
  difficulty: 'medium',
  servings: 4,
  prepTime: 15,
  cookTime: 30,
  nutrition: EMPTY_NUTRITION,
}

const NUTRITION_FIELDS: { key: NutritionField; label: string; unit: string }[] = [
  { key: 'calories', label: 'Calo', unit: 'kcal' },
  { key: 'protein', label: 'Chất đạm', unit: 'g' },
  { key: 'carbohydrates', label: 'Tinh bột', unit: 'g' },
  { key: 'fat', label: 'Chất béo', unit: 'g' },
  { key: 'fiber', label: 'Chất xơ', unit: 'g' },
  { key: 'sodium', label: 'Natri', unit: 'mg' },
]

function toFormNutrition(nutrition: Nutrition | null): NutritionForm {
  return Object.fromEntries(
    NUTRITION_FIELDS.map(({ key }) => [key, nutrition?.[key]?.toString() ?? '']),
  ) as unknown as NutritionForm
}

function toPayload(form: RecipeForm): RecipeWrite {
  const nutrition = Object.fromEntries(
    NUTRITION_FIELDS.map(({ key }) => [key, form.nutrition[key] === '' ? null : Number(form.nutrition[key])]),
  ) as unknown as Nutrition
  return {
    title: form.title.trim(),
    description: form.description.trim(),
    instructions: form.instructions.trim() || null,
    categoryId: form.categoryId,
    prepTime: form.prepTime,
    cookTime: form.cookTime,
    servings: form.servings,
    difficulty: form.difficulty,
    nutrition: Object.values(nutrition).every((value) => value === null) ? null : nutrition,
  }
}

export function RecipeEditor() {
  const { id } = useParams<{ id: string }>()
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const fromSystem = user?.role === 'admin' && searchParams.get('from') === 'system'
  const returnTo = fromSystem ? '/dashboard/system' : '/dashboard/recipes'
  const originSuffix = fromSystem ? '?from=system' : ''
  const isEditing = Boolean(id)

  const urlStep = searchParams.get('step') as WizardAllSteps | null
  const [activeStep, setActiveStep] = useState<WizardAllSteps>(
    urlStep && ['basic', 'ingredients', 'steps', 'images', 'preview'].includes(urlStep) ? urlStep : 'basic',
  )

  const [form, setForm] = useState<RecipeForm>(EMPTY_FORM)
  const initialSnapshotRef = useRef<string>(JSON.stringify(EMPTY_FORM))
  const [version, setVersion] = useState(1)
  const [saved, setSaved] = useState(false)
  const [lastSavedAt, setLastSavedAt] = useState<string | null>(null)
  const [clientError, setClientError] = useState<string | null>(null)
  const [publishNotice, setPublishNotice] = useState<string | null>(null)
  const [publishError, setPublishError] = useState<string | null>(null)

  // Expandable validation checklist at top
  const [showValidationBanner, setShowValidationBanner] = useState(true)

  // Modals
  const [confirmModal, setConfirmModal] = useState<
    'publish' | 'unpublish' | 'archive' | 'unarchive' | 'delete' | null
  >(null)
  const [navGuardTarget, setNavGuardTarget] = useState<string | WizardAllSteps | null>(null)
  const [conflictModalOpen, setConflictModalOpen] = useState(false)
  const [copiedField, setCopiedField] = useState<string | null>(null)
  const [compositionDirty, setCompositionDirty] = useState(false)
  const basicDirtyRef = useRef(false)
  const loadedRecipeIdRef = useRef<string | null>(null)
  const saveBeforeLeavingRef = useRef(false)

  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: listCategories })
  const recipeQuery = useQuery({
    queryKey: ['my-recipe', id, user?.role],
    queryFn: () => (user?.role === 'admin' ? getAdminRecipe(id!) : getMyRecipe(id!)),
    enabled: isEditing && Boolean(user),
  })

  // Sync loaded recipe into form
  useEffect(() => {
    const recipe = recipeQuery.data
    if (!recipe) return
    setVersion(recipe.version)
    if (loadedRecipeIdRef.current === recipe.id && basicDirtyRef.current) return
    const loadedForm: RecipeForm = {
      title: recipe.title,
      description: recipe.description,
      instructions: recipe.instructions ?? '',
      categoryId: recipe.category.id,
      difficulty: recipe.difficulty,
      servings: recipe.servings,
      prepTime: recipe.prepTime,
      cookTime: recipe.cookTime,
      nutrition: toFormNutrition(recipe.nutrition),
    }
    setForm(loadedForm)
    initialSnapshotRef.current = JSON.stringify(loadedForm)
    loadedRecipeIdRef.current = recipe.id
  }, [recipeQuery.data])

  // Select default category
  useEffect(() => {
    if (!isEditing && !form.categoryId && categoriesQuery.data?.[0]) {
      const categoryId = categoriesQuery.data[0].id
      const initial = JSON.parse(initialSnapshotRef.current) as RecipeForm
      initialSnapshotRef.current = JSON.stringify({ ...initial, categoryId })
      setForm((current) => ({ ...current, categoryId }))
    }
  }, [categoriesQuery.data, form.categoryId, isEditing])

  // Calculate isDirty
  const isDirty = JSON.stringify(form) !== initialSnapshotRef.current
  basicDirtyRef.current = isDirty
  const hasUnsavedChanges = isDirty || compositionDirty
  const onLeave = useCallback((target: string) => setNavGuardTarget(target), [])
  const allowNavigation = useUnsavedChanges(hasUnsavedChanges, onLeave)

  const save = useMutation({
    mutationFn: (payload: RecipeWrite) =>
      isEditing ? updateRecipe(id!, version, payload) : createRecipe(payload),
    onSuccess: async (recipe) => {
      setVersion(recipe.version)
      setSaved(true)
      setLastSavedAt(
        new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' }).format(
          new Date(),
        ),
      )
      initialSnapshotRef.current = JSON.stringify(form)
      setClientError(null)
      setPublishNotice(null)
      setPublishError(null)
      setConflictModalOpen(false)
      await queryClient.invalidateQueries({ queryKey: ['my-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['admin-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['my-recipe', recipe.id] })
      if (!isEditing && !saveBeforeLeavingRef.current) {
        allowNavigation()
        navigate(`/dashboard/recipes/${recipe.id}/edit?step=ingredients`)
      }
    },
    onError: (error) => {
      if (error instanceof ApiProblem && error.problem.code === 'RECIPE_CONCURRENCY_CONFLICT') {
        setConflictModalOpen(true)
      }
    },
  })

  const publishMutation = useMutation({
    mutationFn: () => publishRecipe(id!, version),
    onSuccess: async (recipe) => {
      setConfirmModal(null)
      setVersion(recipe.version)
      setPublishNotice('Công thức đã được xuất bản thành công.')
      setPublishError(null)
      setSaved(false)
      setClientError(null)
      await queryClient.invalidateQueries({ queryKey: ['my-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['admin-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['my-recipe', id] })
    },
    onError: (error) => {
      setConfirmModal(null)
      setPublishNotice(null)
      if (error instanceof ApiProblem && error.problem.code === 'RECIPE_PUBLISH_INCOMPLETE') {
        setPublishError('Công thức cần ít nhất 1 nguyên liệu và 1 bước thực hiện để xuất bản.')
      } else {
        setPublishError(
          error instanceof ApiProblem
            ? (error.problem.detail ?? error.problem.title)
            : 'Không thể xuất bản công thức.',
        )
      }
    },
  })

  const unpublishMutation = useMutation({
    mutationFn: () => unpublishRecipe(id!, version),
    onSuccess: async (recipe) => {
      setConfirmModal(null)
      setVersion(recipe.version)
      setPublishNotice('Đã gỡ xuất bản công thức (chuyển về bản nháp).')
      setPublishError(null)
      setSaved(false)
      setClientError(null)
      await queryClient.invalidateQueries({ queryKey: ['my-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['admin-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['my-recipe', id] })
    },
    onError: (error) => {
      setConfirmModal(null)
      setPublishNotice(null)
      setPublishError(
        error instanceof ApiProblem
          ? (error.problem.detail ?? error.problem.title)
          : 'Không thể gỡ xuất bản công thức.',
      )
    },
  })

  const archiveMutation = useMutation({
    mutationFn: () => archiveRecipe(id!, version),
    onSuccess: async (recipe) => {
      setConfirmModal(null)
      setVersion(recipe.version)
      setPublishNotice('Công thức đã được đưa vào kho lưu trữ.')
      setPublishError(null)
      await queryClient.invalidateQueries({ queryKey: ['my-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['admin-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['my-recipe', id] })
    },
    onError: (error) => {
      setConfirmModal(null)
      setPublishError(
        error instanceof ApiProblem
          ? (error.problem.detail ?? error.problem.title)
          : 'Không thể lưu trữ công thức.',
      )
    },
  })

  const unarchiveMutation = useMutation({
    mutationFn: () => unarchiveRecipe(id!, version),
    onSuccess: async (recipe) => {
      setConfirmModal(null)
      setVersion(recipe.version)
      setPublishNotice('Đã khôi phục công thức về bản nháp.')
      setPublishError(null)
      await queryClient.invalidateQueries({ queryKey: ['my-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['admin-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['my-recipe', id] })
    },
    onError: (error) => {
      setConfirmModal(null)
      setPublishError(
        error instanceof ApiProblem
          ? (error.problem.detail ?? error.problem.title)
          : 'Không thể khôi phục công thức.',
      )
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => deleteRecipe(id!, version),
    onSuccess: async () => {
      setConfirmModal(null)
      await queryClient.invalidateQueries({ queryKey: ['my-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['admin-recipes'] })
      navigate(returnTo)
    },
    onError: (error) => {
      setConfirmModal(null)
      setClientError(
        error instanceof ApiProblem
          ? (error.problem.detail ?? error.problem.title)
          : 'Không thể xóa công thức.',
      )
    },
  })

  const update = <K extends keyof RecipeForm>(key: K, value: RecipeForm[K]) => {
    setSaved(false)
    setForm((current) => ({ ...current, [key]: value }))
  }

  const saveBasic = async () => {
    if (save.isPending) return false
    if (form.title.trim().length < 5 || !form.description.trim() || !form.categoryId || form.prepTime < 1) {
      setClientError('Vui lòng điền tiêu đề (ít nhất 5 ký tự), mô tả, danh mục và thời gian chuẩn bị hợp lệ.')
      return false
    }
    setClientError(null)
    try {
      await save.mutateAsync(toPayload(form))
      return true
    } catch {
      return false
    }
  }

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    void saveBasic()
  }

  const handleCompositionChanged = async (nextVersion: number) => {
    setVersion(nextVersion)
    setPublishNotice(null)
    setPublishError(null)
    await recipeQuery.refetch()
  }

  // Navigation guard helper
  const requestNavigation = (target: string | WizardAllSteps) => {
    if (hasUnsavedChanges && target.startsWith('/')) {
      setNavGuardTarget(target)
    } else {
      executeNavigation(target)
    }
  }

  const executeNavigation = (target: string | WizardAllSteps) => {
    setNavGuardTarget(null)
    if (target.startsWith('/')) {
      allowNavigation()
      navigate(target)
    } else {
      const targetStep = target as WizardAllSteps
      setActiveStep(targetStep)
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        next.set('step', targetStep)
        return next
      })
      window.scrollTo({ top: 0, behavior: 'smooth' })
    }
  }

  // Deep link handler (P7-05)
  const handleDeepLink = (target: WizardAllSteps, fieldId?: string) => {
    requestNavigation(target)
    if (target === 'basic' && fieldId) {
      setTimeout(() => {
        const el = document.getElementById(fieldId)
        if (el) {
          el.focus()
          el.scrollIntoView({ behavior: 'smooth', block: 'center' })
        }
      }, 100)
    }
  }

  const copyToClipboard = async (text: string, fieldName: string) => {
    try {
      await navigator.clipboard.writeText(text)
      setCopiedField(fieldName)
      setTimeout(() => setCopiedField(null), 2500)
    } catch {
      // Fallback
    }
  }

  const conflict =
    save.error instanceof ApiProblem && save.error.problem.code === 'RECIPE_CONCURRENCY_CONFLICT'
  const remoteError = save.error instanceof ApiProblem ? save.error.problem.detail : null
  const isPendingAction =
    publishMutation.isPending ||
    unpublishMutation.isPending ||
    archiveMutation.isPending ||
    unarchiveMutation.isPending ||
    deleteMutation.isPending

  const inputClass =
    'w-full border border-border bg-background px-4 py-3 text-sm transition-all placeholder:text-muted-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary'
  const labelClass = 'mb-2 block text-xs uppercase tracking-widest text-muted-foreground font-medium'

  const wizardSteps: { id: WizardAllSteps; label: string; desc: string }[] = [
    { id: 'basic', label: '1. Thông tin cơ bản', desc: 'Tiêu đề, danh mục, thời gian' },
    { id: 'ingredients', label: '2. Nguyên liệu', desc: 'Định lượng và gia vị' },
    { id: 'steps', label: '3. Các bước', desc: 'Hướng dẫn và hẹn giờ' },
    { id: 'images', label: '4. Hình ảnh', desc: 'Ảnh đại diện và các ảnh phụ' },
    { id: 'preview', label: '5. Xem trước & Xuất bản', desc: 'Kiểm tra và công khai' },
  ]

  const validationChecks = recipeQuery.data ? computeRecipeValidation(recipeQuery.data) : []
  const passedChecksCount = validationChecks.filter((c) => c.isValid).length

  if (isEditing && recipeQuery.isPending) {
    return (
      <div role="status" className="py-16 text-center text-muted-foreground">
        Đang tải thông tin công thức…
      </div>
    )
  }

  if (isEditing && recipeQuery.isError) {
    return (
      <div role="alert" className="border border-red-200 bg-red-50 p-6 text-red-700">
        Không thể tải công thức này. Bạn có thể không còn quyền truy cập hoặc công thức đã bị xóa.
      </div>
    )
  }

  return (
    <div className="max-w-4xl mx-auto">
      {/* Header */}
      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <button
            type="button"
            onClick={() => requestNavigation(returnTo)}
            className="mb-3 flex items-center gap-1.5 text-xs uppercase tracking-widest text-muted-foreground hover:text-primary"
          >
            <ChevronLeft size={14} /> {fromSystem ? 'Tổng quan hệ thống' : 'Công thức của tôi'}
          </button>
          <div className="flex flex-wrap items-center gap-3">
            <h1 className="font-serif text-3xl lg:text-4xl text-foreground">
              {isEditing ? 'Chỉnh sửa công thức' : 'Tạo bản nháp mới'}
            </h1>
            {isEditing && recipeQuery.data && (
              <span
                className={`border px-2.5 py-1 text-xs font-medium uppercase tracking-wider ${
                  STATUS_BADGE[recipeQuery.data.status]
                }`}
              >
                {STATUS_LABELS[recipeQuery.data.status]}
              </span>
            )}
          </div>
          {isEditing && recipeQuery.data && (
            <div className="mt-2 flex flex-wrap gap-4">
              <Link
                to={`/dashboard/recipes/${recipeQuery.data.id}/preview${originSuffix}`}
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-1.5 text-xs text-muted-foreground underline hover:text-foreground"
              >
                <Eye size={13} /> Xem trước riêng tư
              </Link>
              {recipeQuery.data.status === 'published' && (
                <Link
                  to={`/recipes/${recipeQuery.data.slug}`}
                  target="_blank"
                  rel="noreferrer"
                  className="inline-flex items-center gap-1.5 text-xs text-muted-foreground underline hover:text-foreground"
                >
                  <Eye size={13} /> Xem bài viết công khai
                </Link>
              )}
            </div>
          )}
        </div>

        {/* Global Actions */}
        <div className="flex flex-wrap items-center gap-2">
          {isEditing && recipeQuery.data && (
            <>
              {recipeQuery.data.status === 'draft' && (
                <button
                  type="button"
                  onClick={() => setConfirmModal('publish')}
                  disabled={publishMutation.isPending || save.isPending || hasUnsavedChanges}
                  className="flex items-center gap-1.5 border border-green-600 bg-green-50 px-3.5 py-2 text-xs uppercase tracking-widest text-green-700 hover:bg-green-100 disabled:opacity-60"
                >
                  <Globe size={14} /> Xuất bản
                </button>
              )}
              {recipeQuery.data.status === 'published' && (
                <button
                  type="button"
                  onClick={() => setConfirmModal('unpublish')}
                  disabled={unpublishMutation.isPending || save.isPending}
                  className="flex items-center gap-1.5 border border-amber-600 bg-amber-50 px-3.5 py-2 text-xs uppercase tracking-widest text-amber-700 hover:bg-amber-100 disabled:opacity-60"
                >
                  <EyeOff size={14} /> Gỡ
                </button>
              )}
              {recipeQuery.data.status !== 'archived' && (
                <button
                  type="button"
                  onClick={() => setConfirmModal('archive')}
                  disabled={archiveMutation.isPending || save.isPending}
                  className="flex items-center gap-1.5 border border-border px-3.5 py-2 text-xs uppercase tracking-widest text-muted-foreground hover:bg-secondary disabled:opacity-60"
                  title="Lưu trữ công thức"
                >
                  <Archive size={14} /> Lưu trữ
                </button>
              )}
              {recipeQuery.data.status === 'archived' && (
                <button
                  type="button"
                  onClick={() => setConfirmModal('unarchive')}
                  disabled={unarchiveMutation.isPending}
                  className="flex items-center gap-1.5 border border-primary bg-primary px-3.5 py-2 text-xs uppercase tracking-widest text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
                >
                  <RotateCcw size={14} /> Khôi phục
                </button>
              )}
              <button
                type="button"
                onClick={() => setConfirmModal('delete')}
                aria-label="Xóa công thức"
                className="p-2 border border-border text-muted-foreground hover:text-red-600 hover:border-red-300 transition-colors"
                title="Xóa công thức"
              >
                <Trash2 size={15} />
              </button>
            </>
          )}

          {activeStep === 'basic' && (
            <button
              type="button"
              onClick={handleSubmit}
              disabled={save.isPending || categoriesQuery.isPending}
              className="flex items-center gap-2 bg-primary px-5 py-2 text-xs uppercase tracking-widest text-primary-foreground hover:bg-primary/90 disabled:opacity-60 font-medium"
            >
              <Save size={14} /> {save.isPending ? 'Đang lưu…' : 'Lưu bản nháp'}
            </button>
          )}
        </div>
      </div>

      {/* P7-03 Save State & Status Notifications */}
      <div className="mb-6 space-y-2">
        {hasUnsavedChanges && (
          <div className="flex items-center gap-2 px-3 py-2 bg-amber-50 border border-amber-200 text-amber-800 text-xs">
            <span className="w-2 h-2 rounded-full bg-amber-500 animate-pulse" />
            Có thay đổi chưa lưu. Lưu thông tin cơ bản hoặc bấm Thêm/Lưu ở bước tương ứng để cập nhật.
          </div>
        )}

        {saved && !hasUnsavedChanges && (
          <div
            role="status"
            className="flex items-center justify-between border border-green-200 bg-green-50 px-4 py-2 text-xs text-green-700"
          >
            <span>Đã lưu bản nháp thành công vào máy chủ.</span>
            {lastSavedAt && <span className="text-muted-foreground">{lastSavedAt}</span>}
          </div>
        )}

        {publishNotice && (
          <p role="status" className="border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">
            {publishNotice}
          </p>
        )}

        {publishError && (
          <p role="alert" className="border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {publishError}
          </p>
        )}

        {(clientError || (save.isError && !conflict)) && (
          <p role="alert" className="border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {clientError ?? remoteError ?? 'Không thể lưu bản nháp.'}
          </p>
        )}
      </div>

      {/* P7-03 Unified 5-Step Wizard Navigation Bar */}
      <div className="mb-8">
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-5" aria-label="Các bước của Wizard">
          {wizardSteps.map((step) => {
            const isActive = activeStep === step.id
            const isDisabled = !isEditing && step.id !== 'basic'

            return (
              <button
                key={step.id}
                type="button"
                disabled={isDisabled}
                onClick={() => requestNavigation(step.id)}
                className={`p-3 text-left border transition-all ${
                  isActive
                    ? 'border-primary bg-primary text-primary-foreground'
                    : isDisabled
                      ? 'border-border bg-secondary/30 text-muted-foreground/50 cursor-not-allowed'
                      : 'border-border bg-background hover:bg-secondary text-foreground'
                }`}
              >
                <div className="text-xs font-semibold uppercase tracking-wider">{step.label}</div>
                <div
                  className={`text-[11px] mt-0.5 line-clamp-1 ${
                    isActive ? 'text-primary-foreground/80' : 'text-muted-foreground'
                  }`}
                >
                  {step.desc}
                </div>
              </button>
            )
          })}
        </div>
        {!isEditing && (
          <p className="mt-2 text-xs text-muted-foreground">
            * Bước 1 (Thông tin cơ bản) là bắt buộc. Sau khi lưu bản nháp đầu tiên, các bước nguyên liệu,
            hướng dẫn và hình ảnh sẽ được mở khóa.
          </p>
        )}
      </div>

      {/* P7-05 Validation Summary Banner with Deep Linking (when editing) */}
      {isEditing && recipeQuery.data && (
        <div className="mb-8 border border-border bg-secondary/15 p-4">
          <div
            className="flex items-center justify-between cursor-pointer select-none"
            onClick={() => setShowValidationBanner(!showValidationBanner)}
          >
            <div className="flex items-center gap-2">
              <span className="font-medium text-sm text-foreground">
                Tổng hợp điều kiện xuất bản ({passedChecksCount}/{validationChecks.length} tiêu chuẩn đạt)
              </span>
              <span
                className={`text-[11px] px-2 py-0.5 border ${
                  passedChecksCount === validationChecks.length
                    ? 'border-green-300 bg-green-50 text-green-700'
                    : 'border-amber-300 bg-amber-50 text-amber-700'
                }`}
              >
                {passedChecksCount === validationChecks.length ? 'Sẵn sàng xuất bản' : 'Chưa hoàn tất'}
              </span>
            </div>
            <button
              type="button"
              className="text-xs text-muted-foreground hover:text-foreground flex items-center gap-1"
              aria-label={showValidationBanner ? 'Thu gọn điều kiện' : 'Mở rộng điều kiện'}
            >
              {showValidationBanner ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
            </button>
          </div>

          {showValidationBanner && (
            <div className="mt-3 pt-3 border-t border-border grid gap-2 sm:grid-cols-2 text-xs">
              {validationChecks.map((item) => (
                <div
                  key={item.id}
                  className="flex items-center justify-between p-2 border border-border bg-background rounded"
                >
                  <div className="flex items-center gap-2 min-w-0">
                    {item.isValid ? (
                      <CheckCircle2 size={14} className="text-green-600 shrink-0" />
                    ) : (
                      <AlertCircle size={14} className="text-amber-600 shrink-0" />
                    )}
                    <span className="truncate text-foreground font-medium">{item.label}</span>
                  </div>
                  {!item.isValid && (
                    <button
                      type="button"
                      onClick={() => handleDeepLink(item.step, item.id)}
                      className="ml-2 text-primary hover:underline uppercase tracking-wider text-[11px] shrink-0 font-medium"
                    >
                      Đến bước {item.stepLabel.split('.')[0]} →
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Step 1: Basic Information Form */}
      {activeStep === 'basic' && (
        <form onSubmit={handleSubmit}>
          <fieldset disabled={save.isPending} className="min-w-0">
            <section className="space-y-6 border-b border-border pb-10" aria-labelledby="basic-heading">
              <h2 id="basic-heading" className="font-serif text-2xl text-foreground">
                Thông tin cơ bản của món ăn
              </h2>

              <div>
                <label htmlFor="title" className={labelClass}>
                  Tên công thức <span className="text-red-600">*</span>
                </label>
                <input
                  id="title"
                  required
                  minLength={5}
                  maxLength={200}
                  placeholder="Ví dụ: Bánh Mì Men Tự Nhiên Sourdough"
                  value={form.title}
                  onChange={(event) => update('title', event.target.value)}
                  className={inputClass}
                />
                <span className="text-[11px] text-muted-foreground mt-1 block">Tối thiểu 5 ký tự.</span>
              </div>

              <div>
                <label htmlFor="description" className={labelClass}>
                  Mô tả ngắn <span className="text-red-600">*</span>
                </label>
                <textarea
                  id="description"
                  required
                  maxLength={2000}
                  rows={4}
                  placeholder="Giới thiệu về hương vị, nguồn gốc hoặc cảm hứng của món ăn…"
                  value={form.description}
                  onChange={(event) => update('description', event.target.value)}
                  className={`${inputClass} resize-y`}
                />
              </div>

              <div>
                <label htmlFor="instructions" className={labelClass}>
                  Ghi chú hướng dẫn chung (không bắt buộc)
                </label>
                <textarea
                  id="instructions"
                  rows={3}
                  placeholder="Mẹo nhỏ, lưu ý về nhiệt độ lò hoặc biến tấu nguyên liệu…"
                  value={form.instructions}
                  onChange={(event) => update('instructions', event.target.value)}
                  className={`${inputClass} resize-y`}
                />
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                <div>
                  <label htmlFor="category" className={labelClass}>
                    Danh mục <span className="text-red-600">*</span>
                  </label>
                  <select
                    id="category"
                    required
                    value={form.categoryId}
                    onChange={(event) => update('categoryId', event.target.value)}
                    className={inputClass}
                    disabled={categoriesQuery.isPending}
                  >
                    <option value="">Chọn danh mục</option>
                    {categoriesQuery.data?.map((category) => (
                      <option key={category.id} value={category.id}>
                        {category.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label htmlFor="difficulty" className={labelClass}>
                    Độ khó
                  </label>
                  <select
                    id="difficulty"
                    value={form.difficulty}
                    onChange={(event) => update('difficulty', event.target.value as RecipeDifficulty)}
                    className={inputClass}
                  >
                    <option value="easy">Dễ</option>
                    <option value="medium">Trung bình</option>
                    <option value="hard">Khó</option>
                    <option value="expert">Chuyên gia</option>
                  </select>
                </div>

                <div>
                  <label htmlFor="servings" className={labelClass}>
                    Khẩu phần (người) <span className="text-red-600">*</span>
                  </label>
                  <input
                    id="servings"
                    type="number"
                    min={1}
                    required
                    value={form.servings}
                    onChange={(event) => update('servings', Number(event.target.value))}
                    className={inputClass}
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label htmlFor="prepTime" className={labelClass}>
                    Chuẩn bị (phút) <span className="text-red-600">*</span>
                  </label>
                  <input
                    id="prepTime"
                    type="number"
                    min={1}
                    required
                    value={form.prepTime}
                    onChange={(event) => update('prepTime', Number(event.target.value))}
                    className={inputClass}
                  />
                </div>

                <div>
                  <label htmlFor="cookTime" className={labelClass}>
                    Nấu / nướng (phút) <span className="text-red-600">*</span>
                  </label>
                  <input
                    id="cookTime"
                    type="number"
                    min={0}
                    required
                    value={form.cookTime}
                    onChange={(event) => update('cookTime', Number(event.target.value))}
                    className={inputClass}
                  />
                </div>
              </div>
            </section>

            {/* Nutrition Section */}
            <section className="mt-10" aria-labelledby="nutrition-heading">
              <h2 id="nutrition-heading" className="font-serif text-2xl text-foreground">
                Dinh dưỡng mỗi khẩu phần
              </h2>
              <p className="mb-6 mt-1 text-sm text-muted-foreground">
                Không bắt buộc; để trống nếu chưa có thông số dinh dưỡng chính xác.
              </p>
              <div className="grid grid-cols-2 gap-5 sm:grid-cols-3">
                {NUTRITION_FIELDS.map(({ key, label, unit }) => (
                  <div key={key}>
                    <label htmlFor={key} className={labelClass}>
                      {label}
                    </label>
                    <div className="relative">
                      <input
                        id={key}
                        type="number"
                        min={0}
                        step="0.01"
                        value={form.nutrition[key]}
                        onChange={(event) =>
                          update('nutrition', { ...form.nutrition, [key]: event.target.value })
                        }
                        className={`${inputClass} pr-14`}
                      />
                      <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-muted-foreground">
                        {unit}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            </section>

            {/* Stepper Navigation / Save Button */}
            <div className="mt-10 flex items-center justify-between border-t border-border pt-6">
              <button
                type="button"
                onClick={() => requestNavigation(returnTo)}
                className="text-sm uppercase tracking-widest text-muted-foreground hover:text-primary"
              >
                Quay lại danh sách
              </button>
              <div className="flex items-center gap-3">
                <button
                  type="submit"
                  disabled={save.isPending}
                  className="flex items-center gap-2 bg-primary px-6 py-2.5 text-sm uppercase tracking-widest text-primary-foreground disabled:opacity-60 hover:bg-primary/90 font-medium"
                >
                  <Save size={15} /> {save.isPending ? 'Đang lưu…' : 'Lưu bản nháp'}
                </button>
                {isEditing && (
                  <button
                    type="button"
                    onClick={() => requestNavigation('ingredients')}
                    className="flex items-center gap-1.5 border border-border px-5 py-2.5 text-sm uppercase tracking-widest hover:bg-secondary"
                  >
                    Bước tiếp theo <ArrowRight size={14} />
                  </button>
                )}
              </div>
            </div>
          </fieldset>
        </form>
      )}

      {/* Steps 2-5: Composition Wizard */}
      {isEditing && recipeQuery.data && (
        <div hidden={activeStep === 'basic'}>
          <RecipeCompositionWizard
            recipe={recipeQuery.data}
            version={version}
            activeStage={activeStep === 'basic' ? 'ingredients' : (activeStep as Stage)}
            onDirtyChange={setCompositionDirty}
            onStageChange={(newStage) => {
              setActiveStep(newStage)
              setSearchParams((current) => {
                const next = new URLSearchParams(current)
                next.set('step', newStage)
                return next
              })
            }}
            onNavigateToStep={(step) => requestNavigation(step)}
            onChanged={handleCompositionChanged}
          />
        </div>
      )}

      {/* P7-03 In-App Navigation Guard Modal */}
      {navGuardTarget && !conflictModalOpen && (
        <Modal
          open
          title={'Có thay đổi chưa lưu'}
          onClose={() => setNavGuardTarget(null)}
          busy={save.isPending}
        >
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground leading-relaxed">
              Bạn có nội dung chưa lưu. Hãy lưu thông tin cơ bản và bấm thêm/lưu nguyên liệu, bước thực hiện
              hoặc ảnh trước khi rời trang.
            </p>
            {(clientError || save.isError) && (
              <p role="alert" className="text-sm text-red-700">
                {clientError ?? remoteError ?? 'Không thể lưu bản nháp. Nội dung của bạn vẫn được giữ lại.'}
              </p>
            )}
            <div className="flex flex-col-reverse sm:flex-row sm:items-center sm:justify-end gap-2 pt-3 border-t border-border">
              <button
                type="button"
                onClick={() => setNavGuardTarget(null)}
                disabled={save.isPending}
                className="border border-border px-4 py-2 text-xs uppercase tracking-wider text-muted-foreground hover:bg-secondary"
              >
                Ở lại tiếp tục sửa
              </button>
              <button
                type="button"
                onClick={() => executeNavigation(navGuardTarget)}
                disabled={save.isPending}
                className="border border-red-300 text-red-700 bg-red-50 hover:bg-red-100 px-4 py-2 text-xs uppercase tracking-wider"
              >
                Rời đi không lưu
              </button>
              {!compositionDirty && (
                <button
                  type="button"
                  disabled={save.isPending}
                  onClick={async () => {
                    saveBeforeLeavingRef.current = true
                    const target = navGuardTarget
                    const success = await saveBasic()
                    saveBeforeLeavingRef.current = false
                    if (success) executeNavigation(target)
                  }}
                  className="bg-primary text-primary-foreground hover:bg-primary/90 px-4 py-2 text-xs uppercase tracking-wider"
                >
                  {save.isPending ? 'Đang lưu…' : 'Lưu và chuyển tiếp'}
                </button>
              )}
            </div>
          </div>
        </Modal>
      )}

      {/* P7-06 Concurrency Conflict Modal (Stale Version UX) */}
      {conflictModalOpen && (
        <Modal open title="Xung đột phiên bản dữ liệu" onClose={() => setConflictModalOpen(false)}>
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground leading-relaxed">
              Công thức này đã được chỉnh sửa từ một phiên làm việc khác (hoặc trên thiết bị khác). Phiên bản
              hiện tại trên máy chủ đã thay đổi, khiến việc ghi đè trực tiếp bị chặn để tránh mất dữ liệu.
            </p>

            <div className="border border-border bg-secondary/30 p-3 space-y-2 text-xs">
              <div className="font-medium text-foreground">Nội dung bạn đang chỉnh sửa tại máy này:</div>
              <div>
                <strong>Tiêu đề:</strong> {form.title}
              </div>
              <div className="line-clamp-2">
                <strong>Mô tả:</strong> {form.description}
              </div>
              <button
                type="button"
                onClick={() =>
                  copyToClipboard(
                    `Tiêu đề: ${form.title}\n\nMô tả:\n${form.description}\n\nHướng dẫn:\n${form.instructions}`,
                    'conflict',
                  )
                }
                className="inline-flex items-center gap-1.5 font-medium text-primary underline pt-1"
              >
                <Copy size={13} />{' '}
                {copiedField === 'conflict'
                  ? 'Đã sao chép vào clipboard!'
                  : 'Sao chép nội dung cục bộ để lưu tạm'}
              </button>
            </div>

            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-end gap-2 pt-3 border-t border-border">
              <button
                type="button"
                onClick={() => {
                  setConflictModalOpen(false)
                  if (recipeQuery.data) {
                    const loadedForm: RecipeForm = {
                      title: recipeQuery.data.title,
                      description: recipeQuery.data.description,
                      instructions: recipeQuery.data.instructions ?? '',
                      categoryId: recipeQuery.data.category.id,
                      difficulty: recipeQuery.data.difficulty,
                      servings: recipeQuery.data.servings,
                      prepTime: recipeQuery.data.prepTime,
                      cookTime: recipeQuery.data.cookTime,
                      nutrition: toFormNutrition(recipeQuery.data.nutrition),
                    }
                    setForm(loadedForm)
                    initialSnapshotRef.current = JSON.stringify(loadedForm)
                    setVersion(recipeQuery.data.version)
                  }
                }}
                className="border border-border px-4 py-2 text-xs uppercase tracking-wider text-muted-foreground hover:bg-secondary"
              >
                Hủy thay đổi cục bộ
              </button>
              <button
                type="button"
                onClick={async () => {
                  const refetched = await recipeQuery.refetch()
                  if (refetched.data) {
                    const loaded = refetched.data
                    setVersion(loaded.version)
                    const loadedForm: RecipeForm = {
                      title: loaded.title,
                      description: loaded.description,
                      instructions: loaded.instructions ?? '',
                      categoryId: loaded.category.id,
                      difficulty: loaded.difficulty,
                      servings: loaded.servings,
                      prepTime: loaded.prepTime,
                      cookTime: loaded.cookTime,
                      nutrition: toFormNutrition(loaded.nutrition),
                    }
                    setForm(loadedForm)
                    initialSnapshotRef.current = JSON.stringify(loadedForm)
                    setConflictModalOpen(false)
                  }
                }}
                className="bg-primary text-primary-foreground hover:bg-primary/90 px-4 py-2 text-xs uppercase tracking-wider"
              >
                Tải lại phiên bản mới nhất
              </button>
            </div>
          </div>
        </Modal>
      )}

      {/* P7-04 Action Confirmation Modal */}
      {confirmModal && (
        <Modal
          open
          title={
            {
              publish: 'Xác nhận xuất bản công thức',
              unpublish: 'Xác nhận gỡ xuất bản',
              archive: 'Xác nhận lưu trữ',
              unarchive: 'Xác nhận khôi phục',
              delete: 'Xác nhận xóa công thức',
            }[confirmModal]
          }
          onClose={() => setConfirmModal(null)}
          busy={isPendingAction}
        >
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground leading-relaxed">
              {confirmModal === 'publish' && (
                <>
                  Công thức <strong>«{form.title || 'này'}»</strong> sẽ được hiển thị công khai trên website
                  cho toàn bộ độc giả truy cập.
                </>
              )}
              {confirmModal === 'unpublish' && (
                <>
                  Công thức <strong>«{form.title || 'này'}»</strong> sẽ được gỡ khỏi trang công khai và chuyển
                  về bản nháp riêng tư.
                </>
              )}
              {confirmModal === 'archive' && (
                <>
                  Công thức <strong>«{form.title || 'này'}»</strong> sẽ được chuyển vào kho lưu trữ và ẩn khỏi
                  chế độ công khai.
                </>
              )}
              {confirmModal === 'unarchive' && (
                <>
                  Công thức <strong>«{form.title || 'này'}»</strong> sẽ được khôi phục về trạng thái bản nháp
                  để bạn tiếp tục chỉnh sửa.
                </>
              )}
              {confirmModal === 'delete' && (
                <>
                  Bạn có chắc chắn muốn xóa công thức <strong>«{form.title || 'này'}»</strong>? Hành động này
                  sẽ xóa toàn bộ dữ liệu khỏi hệ thống.
                </>
              )}
            </p>

            <div className="flex items-center justify-end gap-3 pt-3 border-t border-border">
              <button
                type="button"
                onClick={() => setConfirmModal(null)}
                disabled={isPendingAction}
                className="border border-border px-4 py-2 text-xs uppercase tracking-wider text-muted-foreground hover:bg-secondary"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                disabled={isPendingAction}
                onClick={() => {
                  if (confirmModal === 'publish') publishMutation.mutate()
                  else if (confirmModal === 'unpublish') unpublishMutation.mutate()
                  else if (confirmModal === 'archive') archiveMutation.mutate()
                  else if (confirmModal === 'unarchive') unarchiveMutation.mutate()
                  else if (confirmModal === 'delete') deleteMutation.mutate()
                }}
                className={`px-5 py-2 text-xs uppercase tracking-wider text-white ${
                  confirmModal === 'delete'
                    ? 'bg-red-600 hover:bg-red-700'
                    : confirmModal === 'publish'
                      ? 'bg-green-700 hover:bg-green-800'
                      : 'bg-primary hover:bg-primary/90'
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
