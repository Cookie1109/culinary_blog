import { z } from 'zod'
import { formMessages } from './messages'
import { optionalImageUrl } from './auth-schemas'

export const categorySchema = z.object({
  name: z
    .string()
    .trim()
    .min(2, formMessages.categoryName)
    .max(50, formMessages.categoryName)
    .refine((value) => !/<[^>]*>/.test(value), formMessages.categoryHtml),
  description: z.string().max(2000, formMessages.descriptionLength),
  imageUrl: optionalImageUrl,
  orderIndex: z
    .number({ error: formMessages.number })
    .int(formMessages.orderInteger)
    .min(0, formMessages.orderMinimum),
})

const nutritionValue = z
  .string()
  .refine(
    (value) => value === '' || (Number.isFinite(Number(value)) && Number(value) >= 0),
    formMessages.nutrition,
  )

export const recipeSchema = z.object({
  title: z.string().trim().min(5, formMessages.recipeTitle).max(200, formMessages.recipeTitle),
  description: z
    .string()
    .trim()
    .min(1, formMessages.recipeDescription)
    .max(2000, formMessages.descriptionLength),
  instructions: z.string(),
  categoryId: z.string().min(1, formMessages.categoryRequired),
  difficulty: z.enum(['easy', 'medium', 'hard', 'expert'], { error: formMessages.difficulty }),
  servings: z
    .number({ error: formMessages.number })
    .int(formMessages.servingsInteger)
    .positive(formMessages.servingsMinimum),
  prepTime: z
    .number({ error: formMessages.number })
    .int(formMessages.timeInteger)
    .positive(formMessages.prepTime),
  cookTime: z
    .number({ error: formMessages.number })
    .int(formMessages.timeInteger)
    .min(0, formMessages.cookTime),
  nutrition: z.object({
    calories: nutritionValue,
    protein: nutritionValue,
    carbohydrates: nutritionValue,
    fat: nutritionValue,
    fiber: nutritionValue,
    sodium: nutritionValue,
  }),
})

export const ingredientSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, formMessages.ingredientNameRequired)
    .max(100, formMessages.ingredientNameLength),
  quantity: z.number({ error: formMessages.number }).positive(formMessages.quantity).nullable(),
  unit: z.string().max(50, formMessages.unitLength).nullable(),
  notes: z.string().max(500, formMessages.notesLength).nullable(),
  orderIndex: z
    .number({ error: formMessages.number })
    .int(formMessages.orderInteger)
    .min(0, formMessages.orderMinimum),
})

export const stepSchema = z.object({
  title: z.string().trim().min(1, formMessages.stepTitleRequired).max(200, formMessages.stepTitleLength),
  description: z
    .string()
    .trim()
    .min(1, formMessages.stepDescription)
    .max(2000, formMessages.descriptionLength),
  timerMinutes: z
    .number({ error: formMessages.number })
    .int(formMessages.timeInteger)
    .min(0, formMessages.timeMinimum)
    .nullable(),
  imageUrl: optionalImageUrl.nullable(),
  stepNumber: z
    .number({ error: formMessages.number })
    .int(formMessages.stepNumber)
    .positive(formMessages.stepNumber)
    .optional(),
})

export type CategoryValues = z.infer<typeof categorySchema>

function integerFilter(minimum: number) {
  return z
    .string()
    .trim()
    .refine(
      (value) =>
        value === '' ||
        (/^\d+$/.test(value) &&
          Number.isSafeInteger(Number(value)) &&
          Number(value) >= minimum &&
          Number(value) <= 2147483647),
      formMessages.integerFilter(minimum),
    )
}

export const publicRecipeFiltersSchema = z
  .object({
    maxCookTime: integerFilter(0),
    minServings: integerFilter(1),
    minPrepTime: integerFilter(1),
    maxPrepTime: integerFilter(1),
  })
  .refine(
    (values) =>
      !values.minPrepTime || !values.maxPrepTime || Number(values.maxPrepTime) >= Number(values.minPrepTime),
    {
      path: ['maxPrepTime'],
      message: formMessages.prepRange,
    },
  )

export type PublicRecipeFilterValues = z.infer<typeof publicRecipeFiltersSchema>
