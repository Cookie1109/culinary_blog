'use client'

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, Save } from 'lucide-react'
import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiProblem } from '@/lib/api/problem-details'
import {
  createRecipe,
  getMyRecipe,
  listCategories,
  updateRecipe,
  type Nutrition,
  type RecipeDifficulty,
  type RecipeWrite,
} from '@/lib/api/content-client'

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
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const isEditing = Boolean(id)
  const [form, setForm] = useState<RecipeForm>(EMPTY_FORM)
  const [version, setVersion] = useState(1)
  const [saved, setSaved] = useState(false)
  const [clientError, setClientError] = useState<string | null>(null)
  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: listCategories })
  const recipeQuery = useQuery({
    queryKey: ['my-recipe', id],
    queryFn: () => getMyRecipe(id),
    enabled: isEditing,
  })

  useEffect(() => {
    const recipe = recipeQuery.data
    if (!recipe) return
    setForm({
      title: recipe.title,
      description: recipe.description,
      instructions: recipe.instructions ?? '',
      categoryId: recipe.category.id,
      difficulty: recipe.difficulty,
      servings: recipe.servings,
      prepTime: recipe.prepTime,
      cookTime: recipe.cookTime,
      nutrition: toFormNutrition(recipe.nutrition),
    })
    setVersion(recipe.version)
  }, [recipeQuery.data])

  useEffect(() => {
    if (!form.categoryId && categoriesQuery.data?.[0]) {
      setForm((current) => ({ ...current, categoryId: categoriesQuery.data![0].id }))
    }
  }, [categoriesQuery.data, form.categoryId])

  const save = useMutation({
    mutationFn: (payload: RecipeWrite) =>
      isEditing ? updateRecipe(id, version, payload) : createRecipe(payload),
    onSuccess: async (recipe) => {
      setVersion(recipe.version)
      setSaved(true)
      setClientError(null)
      await queryClient.invalidateQueries({ queryKey: ['my-recipes'] })
      await queryClient.invalidateQueries({ queryKey: ['my-recipe', recipe.id] })
      if (!isEditing) navigate(`/dashboard/recipes/${recipe.id}/edit`)
    },
  })

  const update = <K extends keyof RecipeForm>(key: K, value: RecipeForm[K]) => {
    setSaved(false)
    setForm((current) => ({ ...current, [key]: value }))
  }

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    if (form.title.trim().length < 5 || !form.description.trim() || !form.categoryId || form.prepTime < 1) {
      setClientError('Vui lòng điền tiêu đề, mô tả, danh mục và thời gian chuẩn bị hợp lệ.')
      return
    }
    setClientError(null)
    save.mutate(toPayload(form))
  }

  const conflict =
    save.error instanceof ApiProblem && save.error.problem.code === 'RECIPE_CONCURRENCY_CONFLICT'
  const remoteError = save.error instanceof ApiProblem ? save.error.problem.detail : null
  const inputClass =
    'w-full border border-border bg-background px-4 py-3 text-sm transition-all placeholder:text-muted-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary'
  const labelClass = 'mb-2 block text-xs uppercase tracking-widest text-muted-foreground'

  if (isEditing && recipeQuery.isPending) {
    return (
      <div role="status" className="py-16 text-center text-muted-foreground">
        Đang tải bản nháp…
      </div>
    )
  }

  if (isEditing && recipeQuery.isError) {
    return (
      <div role="alert" className="border border-red-200 bg-red-50 p-6 text-red-700">
        Không thể tải công thức này. Bạn có thể không còn quyền truy cập.
      </div>
    )
  }

  return (
    <form className="max-w-4xl" onSubmit={handleSubmit}>
      <div className="mb-8 flex items-start justify-between gap-4">
        <div>
          <Link
            to="/dashboard/recipes"
            className="mb-3 flex items-center gap-1.5 text-xs uppercase tracking-widest text-muted-foreground hover:text-primary"
          >
            <ChevronLeft size={14} /> Công thức của tôi
          </Link>
          <h1 className="font-serif text-3xl lg:text-4xl">
            {isEditing ? 'Chỉnh sửa bản nháp' : 'Tạo bản nháp mới'}
          </h1>
        </div>
        <button
          type="submit"
          disabled={save.isPending || categoriesQuery.isPending}
          className="flex items-center gap-2 bg-primary px-5 py-2.5 text-sm uppercase tracking-widest text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
        >
          <Save size={15} /> {save.isPending ? 'Đang lưu…' : 'Lưu bản nháp'}
        </button>
      </div>

      {saved && (
        <p
          role="status"
          className="mb-6 border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700"
        >
          Đã lưu bản nháp thành công.
        </p>
      )}
      {(clientError || (save.isError && !conflict)) && (
        <p role="alert" className="mb-6 border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {clientError ?? remoteError ?? 'Không thể lưu bản nháp.'}
        </p>
      )}
      {conflict && (
        <div role="alert" className="mb-6 border border-amber-300 bg-amber-50 p-4 text-sm text-amber-800">
          <p>Bản nháp đã được cập nhật ở nơi khác. Thay đổi hiện tại chưa được ghi đè.</p>
          <button type="button" className="mt-2 font-medium underline" onClick={() => recipeQuery.refetch()}>
            Tải phiên bản mới nhất
          </button>
        </div>
      )}

      <section className="space-y-6 border-b border-border pb-10" aria-labelledby="basic-heading">
        <h2 id="basic-heading" className="font-serif text-2xl">
          Thông tin cơ bản
        </h2>
        <div>
          <label htmlFor="title" className={labelClass}>
            Tên công thức
          </label>
          <input
            id="title"
            required
            minLength={5}
            maxLength={200}
            value={form.title}
            onChange={(event) => update('title', event.target.value)}
            className={inputClass}
          />
        </div>
        <div>
          <label htmlFor="description" className={labelClass}>
            Mô tả
          </label>
          <textarea
            id="description"
            required
            maxLength={2000}
            rows={4}
            value={form.description}
            onChange={(event) => update('description', event.target.value)}
            className={`${inputClass} resize-y`}
          />
        </div>
        <div>
          <label htmlFor="instructions" className={labelClass}>
            Ghi chú hướng dẫn tổng quát (không bắt buộc)
          </label>
          <textarea
            id="instructions"
            rows={3}
            value={form.instructions}
            onChange={(event) => update('instructions', event.target.value)}
            className={`${inputClass} resize-y`}
          />
        </div>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <div>
            <label htmlFor="category" className={labelClass}>
              Danh mục
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
              Khẩu phần
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
              Chuẩn bị (phút)
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
              Nấu (phút)
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

      <section className="mt-10" aria-labelledby="nutrition-heading">
        <h2 id="nutrition-heading" className="font-serif text-2xl">
          Dinh dưỡng mỗi khẩu phần
        </h2>
        <p className="mb-6 mt-1 text-sm text-muted-foreground">
          Không bắt buộc; để trống nếu chưa có dữ liệu.
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
                  onChange={(event) => update('nutrition', { ...form.nutrition, [key]: event.target.value })}
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

      <div className="mt-10 flex items-center justify-between border-t border-border pt-6">
        <button
          type="button"
          onClick={() => navigate('/dashboard/recipes')}
          className="text-sm uppercase tracking-widest text-muted-foreground hover:text-primary"
        >
          Hủy thay đổi
        </button>
        <button
          type="submit"
          disabled={save.isPending}
          className="flex items-center gap-2 bg-primary px-6 py-2.5 text-sm uppercase tracking-widest text-primary-foreground disabled:opacity-60"
        >
          <Save size={15} /> Lưu bản nháp
        </button>
      </div>
    </form>
  )
}
