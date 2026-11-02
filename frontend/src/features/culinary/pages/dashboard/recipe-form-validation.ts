import type { IngredientWrite, RecipeWrite, StepWrite } from '@/lib/api/content-client'
import { ApiProblem } from '@/lib/api/problem-details'

export type FieldErrors = Record<string, string>

const FIELD_MESSAGES: FieldErrors = {
  title: 'Tên công thức cần từ 5 đến 200 ký tự.',
  description: 'Mô tả cần từ 1 đến 2000 ký tự.',
  instructions: 'Ghi chú hướng dẫn không quá 10000 ký tự.',
  categoryId: 'Vui lòng chọn danh mục hợp lệ.',
  difficulty: 'Vui lòng chọn độ khó hợp lệ.',
  prepTime: 'Thời gian chuẩn bị phải là số nguyên từ 1 phút.',
  cookTime: 'Thời gian nấu phải là số nguyên không âm.',
  servings: 'Khẩu phần phải là số nguyên từ 1 người.',
  nutrition: 'Giá trị dinh dưỡng phải là số không âm.',
  name: 'Tên nguyên liệu cần từ 1 đến 100 ký tự.',
  quantity: 'Số lượng phải là số lớn hơn 0 hoặc để trống.',
  unit: 'Đơn vị không quá 50 ký tự.',
  notes: 'Ghi chú nguyên liệu không quá 500 ký tự.',
  timerMinutes: 'Hẹn giờ phải là số nguyên không âm hoặc để trống.',
}

const textLength = (value: string, min: number, max: number) =>
  value.trim().length >= min && value.trim().length <= max
const integer = (value: number, min: number) => Number.isSafeInteger(value) && value >= min

export function validateRecipe(payload: RecipeWrite): FieldErrors {
  const errors: FieldErrors = {}
  if (!textLength(payload.title, 5, 200)) errors.title = FIELD_MESSAGES.title
  if (!textLength(payload.description, 1, 2000)) errors.description = FIELD_MESSAGES.description
  if ((payload.instructions?.length ?? 0) > 10000) errors.instructions = FIELD_MESSAGES.instructions
  if (!payload.categoryId) errors.categoryId = FIELD_MESSAGES.categoryId
  if (!['easy', 'medium', 'hard', 'expert'].includes(payload.difficulty))
    errors.difficulty = FIELD_MESSAGES.difficulty
  for (const key of ['prepTime', 'cookTime', 'servings'] as const) {
    if (!integer(payload[key], key === 'cookTime' ? 0 : 1)) errors[key] = FIELD_MESSAGES[key]
  }
  for (const [key, value] of Object.entries(payload.nutrition ?? {})) {
    if (value !== null && (!Number.isFinite(value) || value < 0))
      errors[`nutrition.${key}`] = FIELD_MESSAGES.nutrition
  }
  return errors
}

export function validateIngredient(payload: IngredientWrite): FieldErrors {
  const errors: FieldErrors = {}
  if (!textLength(payload.name, 1, 100)) errors.name = FIELD_MESSAGES.name
  if (payload.quantity != null && (!Number.isFinite(payload.quantity) || payload.quantity <= 0))
    errors.quantity = FIELD_MESSAGES.quantity
  if ((payload.unit?.length ?? 0) > 50) errors.unit = FIELD_MESSAGES.unit
  if ((payload.notes?.length ?? 0) > 500) errors.notes = FIELD_MESSAGES.notes
  return errors
}

export function validateStep(payload: StepWrite): FieldErrors {
  const errors: FieldErrors = {}
  if (!textLength(payload.title, 1, 200)) errors.title = 'Tiêu đề bước cần từ 1 đến 200 ký tự.'
  if (!textLength(payload.description, 1, 2000)) errors.description = 'Mô tả bước cần từ 1 đến 2000 ký tự.'
  if (payload.timerMinutes != null && !integer(payload.timerMinutes, 0))
    errors.timerMinutes = FIELD_MESSAGES.timerMinutes
  return errors
}

export function recipeFieldErrors(error: unknown, messages: FieldErrors = FIELD_MESSAGES): FieldErrors {
  if (!(error instanceof ApiProblem)) return {}
  const errors: FieldErrors = {}
  for (const path of Object.keys(error.problem.errors ?? {})) {
    const key = path
      .split('.')
      .map((part) => part.charAt(0).toLowerCase() + part.slice(1))
      .join('.')
    const message = messages[key] ?? (key.startsWith('nutrition.') ? messages.nutrition : undefined)
    if (message) errors[key] = message
  }
  return errors
}

export function recipeErrorMessage(error: unknown): string {
  const messages: Record<string, string> = {
    RECIPE_CONCURRENCY_CONFLICT:
      'Công thức đã thay đổi ở phiên khác. Hãy giữ lại nội dung và tải lại bản mới.',
    RECIPE_PUBLISH_INCOMPLETE: 'Hãy hoàn thiện thông tin, nguyên liệu và các bước trước khi xuất bản.',
    RECIPE_INVALID_TRANSITION: 'Trạng thái công thức đã thay đổi. Hãy tải lại bản mới.',
  }
  return (
    (error instanceof ApiProblem && messages[error.problem.code]) ||
    'Không thể lưu thay đổi. Hãy kiểm tra kết nối và thử lại.'
  )
}
