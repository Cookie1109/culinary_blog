import type { ProblemDetails } from './problem-details-schema'

export type { ProblemDetails } from './problem-details-schema'

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
    const { problemDetailsSchema } = await import('./problem-details-schema')
    const parsed = problemDetailsSchema.safeParse(await response.json())
    return new ApiProblem(parsed.success ? parsed.data : fallback)
  } catch {
    return new ApiProblem(fallback)
  }
}
