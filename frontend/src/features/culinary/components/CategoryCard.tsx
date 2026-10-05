import { BookOpen } from 'lucide-react'
import Image from 'next/image'
import { Link } from 'react-router'
import type { Category } from '@/lib/api/content-client'
import { publicMediaUrl } from '@/lib/media-url'

const IMAGE_PLACEHOLDER =
  'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="16" height="12"%3E%3Crect width="16" height="12" fill="%23eee9e2"/%3E%3C/svg%3E'

export function CategoryCard({
  category,
  headingLevel = 'h2',
}: {
  category: Category
  headingLevel?: 'h2' | 'h3'
}) {
  const Heading = headingLevel

  return (
    <article className="group flex h-full flex-col [content-visibility:auto] [contain-intrinsic-size:auto_28rem]">
      <Link
        to={`/categories/${category.slug}`}
        aria-hidden="true"
        tabIndex={-1}
        className="relative mb-4 block aspect-[4/3] w-full overflow-hidden bg-secondary"
      >
        {category.imageUrl ? (
          <Image
            src={publicMediaUrl(category.imageUrl)}
            alt={category.name}
            fill
            loading="lazy"
            quality={90}
            sizes="(max-width: 639px) 100vw, (max-width: 1023px) 50vw, 25vw"
            placeholder="blur"
            blurDataURL={IMAGE_PLACEHOLDER}
            className="object-cover transition-transform duration-500 ease-out group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full items-center justify-center bg-secondary px-4 text-center font-serif text-lg text-muted-foreground">
            {category.name}
          </div>
        )}
      </Link>
      <Heading className="mb-2 font-serif text-xl font-bold tracking-tight text-foreground transition-colors group-hover:text-primary sm:text-2xl">
        <Link
          to={`/categories/${category.slug}`}
          aria-label={`Danh mục ${category.name}, có ${category.recipeCount} công thức`}
        >
          {category.name}
        </Link>
      </Heading>
      <p className="mb-3 line-clamp-2 flex-1 text-sm leading-relaxed text-muted-foreground">
        {category.description ?? 'Khám phá các món ăn hấp dẫn trong danh mục này.'}
      </p>
      <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
        <BookOpen size={14} aria-hidden="true" />
        <span>{category.recipeCount} công thức</span>
      </div>
    </article>
  )
}
