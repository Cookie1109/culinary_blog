'use client'

import { X } from 'lucide-react'
import { useEffect, useId, useRef, type ReactNode } from 'react'

const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'

export function Modal({
  open,
  title,
  children,
  onClose,
  busy = false,
  size = 'md',
}: {
  open: boolean
  title: string
  children: ReactNode
  onClose: () => void
  busy?: boolean
  size?: 'md' | 'lg'
}) {
  const titleId = useId()
  const dialogRef = useRef<HTMLElement>(null)
  const previousActiveElement = useRef<HTMLElement | null>(null)
  const closeRef = useRef(onClose)
  const busyRef = useRef(busy)

  useEffect(() => {
    closeRef.current = onClose
    busyRef.current = busy
  }, [onClose, busy])

  useEffect(() => {
    if (!open) return undefined

    previousActiveElement.current = document.activeElement as HTMLElement | null
    const originalOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    const focusable = dialogRef.current?.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)
    if (focusable && focusable.length > 0) {
      focusable[0].focus()
    } else {
      dialogRef.current?.focus()
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault()
        event.stopPropagation()
        if (!busyRef.current) closeRef.current()
        return
      }

      if (event.key === 'Tab') {
        if (!dialogRef.current) return
        const elements = Array.from(
          dialogRef.current.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR),
        ).filter((el) => !el.hasAttribute('disabled') && !el.closest('[hidden]'))

        if (elements.length === 0) {
          event.preventDefault()
          return
        }

        const firstElement = elements[0]
        const lastElement = elements[elements.length - 1]

        if (event.shiftKey) {
          if (
            document.activeElement === firstElement ||
            !dialogRef.current.contains(document.activeElement)
          ) {
            event.preventDefault()
            lastElement.focus()
          }
        } else {
          if (document.activeElement === lastElement || !dialogRef.current.contains(document.activeElement)) {
            event.preventDefault()
            firstElement.focus()
          }
        }
      }
    }

    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      document.body.style.overflow = originalOverflow
      previousActiveElement.current?.focus()
    }
  }, [open])

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 grid place-items-center p-4" role="presentation">
      <div
        className="absolute inset-0 bg-foreground/40"
        onClick={() => {
          if (!busy) onClose()
        }}
        aria-hidden="true"
      />
      <section
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
        className={`relative z-10 min-w-0 w-full max-h-[calc(100dvh-2rem)] overflow-y-auto [overflow-wrap:anywhere] border border-border bg-card p-6 shadow-xl outline-none ${size === 'lg' ? 'max-w-2xl' : 'max-w-lg'}`}
      >
        <div className="mb-5 flex items-center justify-between gap-4">
          <h2 id={titleId} className="min-w-0 font-serif text-2xl">
            {title}
          </h2>
          <button
            type="button"
            onClick={onClose}
            disabled={busy}
            aria-label="Đóng hộp thoại"
            className="shrink-0 p-2 text-muted-foreground hover:text-primary transition-colors disabled:opacity-50"
          >
            <X size={20} aria-hidden="true" />
          </button>
        </div>
        {children}
      </section>
    </div>
  )
}
