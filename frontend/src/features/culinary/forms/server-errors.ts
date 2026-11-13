import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import { ApiProblem } from '@/lib/api/problem-details'

// Only known field names cross this boundary; server messages may contain internal details.
export function setServerFieldErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  fields: Record<string, [Path<T>, string]>,
) {
  if (!(error instanceof ApiProblem) || error.problem.status >= 500) return
  const errors = Object.keys(error.problem.errors ?? {}).map((key) => key.toLowerCase())
  for (const [property, [field, message]] of Object.entries(fields)) {
    if (errors.includes(property.toLowerCase())) {
      setError(field, { type: 'server', message })
    }
  }
}
