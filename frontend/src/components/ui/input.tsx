import { forwardRef, type InputHTMLAttributes } from 'react'

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
}

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { className = '', id, label, error, ...props },
  ref,
) {
  return (
    <label className="block" htmlFor={id}>
      {label ? (
        <span className="mb-2 block text-xs uppercase tracking-widest text-muted-foreground">{label}</span>
      ) : null}
      <input
        ref={ref}
        id={id}
        aria-invalid={Boolean(error)}
        aria-describedby={error && id ? `${id}-error` : undefined}
        className={`min-h-11 w-full border border-border bg-background px-4 py-3 text-sm placeholder:text-muted-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary ${className}`}
        {...props}
      />
      {error && id ? (
        <span id={`${id}-error`} className="mt-1.5 block text-sm text-red-700">
          {error}
        </span>
      ) : null}
    </label>
  )
})
