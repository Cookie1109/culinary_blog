import type { ButtonHTMLAttributes, Ref } from 'react'

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger'

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  ref?: Ref<HTMLButtonElement>
}

const variants: Record<ButtonVariant, string> = {
  primary: 'bg-primary text-primary-foreground hover:bg-primary/90',
  secondary: 'border border-border bg-background text-foreground hover:bg-secondary',
  ghost: 'text-foreground hover:bg-secondary',
  danger: 'bg-red-700 text-white hover:bg-red-800',
}

export function Button({
  className = '',
  variant = 'primary',
  type = 'button',
  ref = undefined,
  ...props
}: ButtonProps) {
  return (
    <button
      ref={ref}
      type={type}
      className={`inline-flex min-h-11 items-center justify-center gap-2 px-5 py-2.5 text-sm font-medium uppercase tracking-widest transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${variants[variant]} ${className}`}
      {...props}
    />
  )
}
