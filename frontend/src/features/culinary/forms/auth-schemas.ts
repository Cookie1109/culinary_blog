import { z } from 'zod'
import { formMessages } from './messages'

const email = z
  .string()
  .trim()
  .min(1, formMessages.emailRequired)
  .max(256, formMessages.emailLength)
  .pipe(z.email({ pattern: z.regexes.html5Email, error: formMessages.emailInvalid }))
const displayName = z.string().trim().min(2, formMessages.displayName).max(100, formMessages.displayName)
const passwordMessage = formMessages.passwordStrength
const newPassword = z
  .string()
  .min(8, passwordMessage)
  .max(128, passwordMessage)
  .regex(/[a-z]/, passwordMessage)
  .regex(/[A-Z]/, passwordMessage)
  .regex(/[0-9]/, passwordMessage)
  .regex(/[^a-zA-Z0-9]/, passwordMessage)

export const optionalImageUrl = z
  .string()
  .trim()
  .max(500, formMessages.imageUrlLength)
  .refine((value) => {
    if (!value) return true
    try {
      const url = new URL(value)
      return url.protocol === 'http:' || url.protocol === 'https:'
    } catch {
      return false
    }
  }, formMessages.imageUrlInvalid)

export const loginSchema = z.object({
  email,
  password: z.string().min(1, formMessages.passwordRequired).max(128, formMessages.passwordLength),
})

export const registerSchema = z
  .object({
    name: displayName,
    email,
    password: newPassword,
    confirm: z.string().min(1, formMessages.passwordConfirmation),
  })
  .refine((data) => data.password === data.confirm, {
    path: ['confirm'],
    message: formMessages.passwordConfirmation,
  })

export const profileSchema = z.object({
  name: displayName,
  bio: z.string().max(2000, formMessages.bioLength),
  avatar: optionalImageUrl,
})

export function passwordFormSchema(mode: 'change' | 'forgot' | 'reset') {
  return z
    .object({
      email: mode === 'forgot' ? email : z.string(),
      currentPassword: mode === 'change' ? loginSchema.shape.password : z.string(),
      newPassword: mode === 'forgot' ? z.string() : newPassword,
      confirmPassword: z.string(),
    })
    .refine((data) => mode === 'forgot' || data.newPassword === data.confirmPassword, {
      path: ['confirmPassword'],
      message: formMessages.passwordConfirmation,
    })
}

export type LoginValues = z.infer<typeof loginSchema>
export type RegisterValues = z.infer<typeof registerSchema>
export type ProfileValues = z.infer<typeof profileSchema>
export type PasswordValues = z.infer<ReturnType<typeof passwordFormSchema>>
