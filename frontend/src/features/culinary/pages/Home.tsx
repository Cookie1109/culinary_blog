'use client'

import { useQuery } from '@tanstack/react-query'
import { ArrowRight } from 'lucide-react'
import { Link } from 'react-router'
import { RecipeCard } from '@/features/culinary/components/RecipeCard'
import { listPublishedRecipes } from '@/lib/api/content-client'

export function Home() {
  const recipesQuery = useQuery({
    queryKey: ['public-recipes', 'featured'],
    queryFn: () => listPublishedRecipes(1, 3),
  })
  const recipes = recipesQuery.data?.data ?? []
  const featured = recipes[0]

  return (
    <div className="pb-24">
      <section className="relative bg-secondary/30">
        <div className="relative mx-auto max-w-7xl px-4 py-24 sm:px-6 lg:px-8 lg:py-32">
          <div className="max-w-3xl">
            <span className="mb-4 block text-sm font-medium uppercase tracking-widest text-primary">
              Công thức mới nhất
            </span>
            <h1 className="mb-6 font-serif text-5xl leading-[1.1] text-foreground lg:text-7xl">
              {featured?.title ?? 'Góc bếp Culinary Blog'}
            </h1>
            <p className="mb-10 max-w-2xl text-xl leading-relaxed text-muted-foreground">
              {featured?.description ??
                'Các công thức đã xuất bản sẽ xuất hiện tại đây. Hãy quay lại khi cộng đồng chia sẻ món mới.'}
            </p>
            {featured && (
              <Link
                to={`/recipes/${featured.slug}`}
                className="inline-flex items-center gap-2 bg-primary px-8 py-4 text-sm font-medium uppercase tracking-widest text-primary-foreground transition-colors hover:bg-primary/90"
              >
                Khám phá công thức
                <ArrowRight size={18} aria-hidden="true" />
              </Link>
            )}
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-7xl px-4 pt-24 sm:px-6 lg:px-8" aria-labelledby="featured-title">
        <div className="mb-12 flex items-end justify-between border-b border-border pb-6">
          <h2 id="featured-title" className="font-serif text-3xl text-foreground">
            Bộ sưu tập nổi bật
          </h2>
          <Link
            to="/recipes"
            className="text-sm font-medium uppercase tracking-wide text-primary transition-colors hover:text-foreground"
          >
            Xem tất cả
          </Link>
        </div>

        {recipesQuery.isPending ? (
          <p role="status" className="py-16 text-center text-muted-foreground">
            Đang tải công thức…
          </p>
        ) : recipesQuery.isError ? (
          <p role="alert" className="py-16 text-center text-red-600">
            Không thể tải công thức. Vui lòng thử lại sau.
          </p>
        ) : recipes.length === 0 ? (
          <p className="py-16 text-center text-muted-foreground">Chưa có công thức nào được xuất bản.</p>
        ) : (
          <div className="grid grid-cols-1 gap-x-8 gap-y-16 md:grid-cols-3">
            {recipes.map((recipe) => (
              <RecipeCard key={recipe.id} recipe={recipe} />
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
