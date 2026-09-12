import { Skeleton } from '@/components/ui/skeleton'

export default function Loading() {
  return (
    <main className="mx-auto max-w-7xl px-4 py-16" aria-busy="true" aria-label="Đang tải trang">
      <Skeleton className="mb-8 h-12 w-2/3" />
      <Skeleton className="h-96 w-full" />
    </main>
  )
}
