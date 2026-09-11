import { z } from 'zod'

export const problemDetailsSchema = z.object({
  type: z.string().optional(),
  title: z.string(),
  status: z.number().int(),
  detail: z.string().optional(),
  instance: z.string().optional(),
  code: z.string().default('HTTP_ERROR'),
  traceId: z.string().optional(),
  errors: z.record(z.string(), z.array(z.string())).optional(),
})

export type ProblemDetails = z.infer<typeof problemDetailsSchema>

export class ApiProblem extends Error {
  constructor(public readonly problem: ProblemDetails) {
    super(problem.detail ?? problem.title)
    this.name = 'ApiProblem'
  }
}

export async function readProblemDetails(response: Response): Promise<ApiProblem> {
  const fallback: ProblemDetails = {
    title: response.statusText || 'Request failed',
    status: response.status,
    code: 'HTTP_ERROR',
  }

  try {
    const parsed = problemDetailsSchema.safeParse(await response.json())
    return new ApiProblem(parsed.success ? parsed.data : fallback)
  } catch {
    return new ApiProblem(fallback)
  }
}
