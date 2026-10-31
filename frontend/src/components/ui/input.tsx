import type { InputHTMLAttributes, Ref } from 'react'

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  ref?: Ref<HTMLInputElement>
}

export function Input({
  className = '',
  id,
  label = undefined,
  error = undefined,
  ref = undefined,
  ...props
}: InputProps) {
  return (
    <div>
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
      </label>
      {error && id ? (
        <span id={`${id}-error`} className="mt-1.5 block text-sm text-red-700">
          {error}
        </span>
      ) : null}
    </div>
  )
}
