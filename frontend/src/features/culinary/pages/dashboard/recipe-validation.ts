import type { Recipe } from '@/lib/api/content-client'

export type Stage = 'ingredients' | 'steps' | 'images' | 'preview'
export type WizardAllSteps = 'basic' | Stage

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
