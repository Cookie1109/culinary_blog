import * as z from 'zod/mini'
import { _default as withDefault } from 'zod/mini'

export const problemDetailsSchema = z.object({
  type: z.optional(z.string()),
  title: z.string(),
  status: z.int(),
  detail: z.optional(z.string()),
  instance: z.optional(z.string()),
  code: withDefault(z.string(), 'HTTP_ERROR'),
  traceId: z.optional(z.string()),
  errors: z.optional(z.record(z.string(), z.array(z.string()))),
})

export type ProblemDetails = z.infer<typeof problemDetailsSchema>
