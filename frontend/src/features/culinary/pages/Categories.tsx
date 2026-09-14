'use client'

import { useQuery } from '@tanstack/react-query'
import { BookOpen } from 'lucide-react'
import { Link } from 'react-router'
import { listCategories } from '@/lib/api/content-client'

export function Categories() {
  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: listCategories })

  return (
    <div className="mx-auto max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
      <header className="mb-12 border-b border-border pb-8">
        <h1 className="mb-2 font-serif text-4xl text-foreground lg:text-5xl">Danh mục món ăn</h1>
        <p className="text-muted-foreground">Khám phá các công thức theo chủ đề ẩm thực.</p>
      </header>

      {categoriesQuery.isPending ? (
        <p role="status" className="py-20 text-center text-muted-foreground">
          Đang tải danh mục…
        </p>
      ) : categoriesQuery.isError ? (
        <p role="alert" className="py-20 text-center text-red-600">
          Không thể tải danh mục.
        </p>
      ) : categoriesQuery.data.length === 0 ? (
        <p className="py-20 text-center text-muted-foreground">Chưa có danh mục nào.</p>
      ) : (
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {categoriesQuery.data.map((category) => (
            <article key={category.id} className="group flex flex-col border border-border p-6">
              <h2 className="mb-2 font-serif text-xl text-foreground transition-colors group-hover:text-primary">
                <Link to={`/recipes?category=${category.slug}`}>{category.name}</Link>
              </h2>
              <p className="mb-4 flex-1 text-sm leading-relaxed text-muted-foreground">
                {category.description ?? 'Chưa có mô tả.'}
              </p>
              <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                <BookOpen size={13} aria-hidden="true" />
                <span>{category.recipeCount} công thức</span>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  )
}
