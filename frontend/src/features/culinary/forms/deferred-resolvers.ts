import { zodResolver } from '@hookform/resolvers/zod'
import { loginSchema, registerSchema, passwordFormSchema, profileSchema } from './auth-schemas'
import { categorySchema, publicRecipeFiltersSchema, recipeSchema } from './content-schemas'

// Load validation when RHF first validates, keeping Zod out of the initial route JS.
export const profileResolver = zodResolver(profileSchema)
export const loginResolver = zodResolver(loginSchema)
export const registerResolver = zodResolver(registerSchema)
export const categoryResolver = zodResolver(categorySchema)
export const publicRecipeFiltersResolver = zodResolver(publicRecipeFiltersSchema)
export const recipeResolver = zodResolver(recipeSchema)
export const passwordResolver = (mode: 'change' | 'forgot' | 'reset') => zodResolver(passwordFormSchema(mode))
