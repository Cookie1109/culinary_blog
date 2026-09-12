'use client'

import { useEffect } from 'react'
import { Button } from '@/components/ui/button'

export default function ErrorPage({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  useEffect(() => {
    console.error(error)
  }, [error])

  return (
    <main className="mx-auto grid min-h-[60vh] max-w-2xl place-items-center px-4 text-center">
      <div>
        <p className="mb-3 text-xs uppercase tracking-widest text-primary">Đã có lỗi xảy ra</p>
        <h1 className="mb-4 text-4xl">Không thể tải trang này.</h1>
        <p className="mb-8 text-muted-foreground">Mã tham chiếu: {error.digest ?? 'client-error'}</p>
        <Button onClick={reset}>Thử lại</Button>
      </div>
    </main>
  )
}
