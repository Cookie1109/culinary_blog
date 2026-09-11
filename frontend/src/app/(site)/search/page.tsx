'use client'

import { Suspense } from 'react'
import { Search } from '@/features/culinary/pages/Search'
import { Skeleton } from '@/components/ui/skeleton'

export default function SearchPage() {
  return (
    <Suspense fallback={<Skeleton className="mx-auto my-16 h-96 max-w-7xl" />}>
      <Search />
    </Suspense>
  )
}
