import { Clock } from 'lucide-react'
import Image from 'next/image'
import { Link } from 'react-router'
import type { Recipe } from '@/lib/api/content-client'
import { publicMediaUrl } from '@/lib/media-url'

const IMAGE_PLACEHOLDER =
  'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="16" height="16"%3E%3Crect width="16" height="16" fill="%23eee9e2"/%3E%3C/svg%3E'

export function RecipeCard({ recipe }: { recipe: Recipe }) {
  return (
    <article className="group">
      <Link
        to={`/recipes/${recipe.slug}`}
        className="relative mb-4 block aspect-square overflow-hidden bg-secondary"
      >
        {recipe.primaryImageUrl ? (
          <Image
            src={publicMediaUrl(recipe.primaryImageUrl)}
            alt={recipe.title}
            fill
            sizes="(max-width: 767px) 100vw, (max-width: 1279px) 50vw, 33vw"
            placeholder="blur"
            blurDataURL={IMAGE_PLACEHOLDER}
            className="object-cover transition-transform duration-700 ease-out group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full items-center justify-center px-6 text-center font-serif text-xl text-muted-foreground">
            {recipe.title}
          </div>
        )}
      </Link>
      <div className="mb-2 flex items-center justify-between gap-3">
        <span className="text-xs uppercase tracking-widest text-muted-foreground">
          {recipe.category.name}
        </span>
        <span className="flex items-center gap-1.5 text-sm text-muted-foreground">
          <Clock size={14} aria-hidden="true" />
          {recipe.prepTime + recipe.cookTime} phút
        </span>
      </div>
      <h2 className="font-serif text-2xl transition-colors group-hover:text-primary">
        <Link to={`/recipes/${recipe.slug}`}>{recipe.title}</Link>
      </h2>
      <p className="mt-2 line-clamp-2 text-sm leading-relaxed text-muted-foreground">{recipe.description}</p>
    </article>
  )
}
