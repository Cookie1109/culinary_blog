import { z } from 'zod'

const publicEnvironmentSchema = z.object({
  NEXT_PUBLIC_API_BASE_URL: z
    .string()
    .refine(
      (value) => value.startsWith('/') || URL.canParse(value),
      'Must be an absolute URL or root-relative path',
    )
    .default('/api/v1'),
})

export const publicEnvironment = publicEnvironmentSchema.parse({
  NEXT_PUBLIC_API_BASE_URL: process.env.NEXT_PUBLIC_API_BASE_URL,
})
