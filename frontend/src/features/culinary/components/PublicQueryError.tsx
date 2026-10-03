'use client'

export function PublicQueryError({ message, onRetry }: { message: string; onRetry: () => void }) {
  return (
    <div role="alert" className="py-20 text-center">
      <p className="mb-4 text-red-700">{message}</p>
      <button
        type="button"
        onClick={onRetry}
        className="border border-foreground px-5 py-2 text-sm font-medium transition-colors hover:bg-foreground hover:text-background"
      >
        Thử lại
      </button>
    </div>
  )
}
