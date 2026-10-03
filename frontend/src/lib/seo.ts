import type { Metadata } from 'next'
import type { Recipe } from '@/lib/api/content-client'
import { publicMediaUrl } from '@/lib/media-url'

export const SITE_NAME = 'Culinary Blog'
export const SITE_DESCRIPTION = 'Công thức nấu ăn được tuyển chọn và chia sẻ bởi cộng đồng.'
export const SITE_URL = new URL(process.env.SITE_URL ?? 'http://localhost:8080')
export const DEFAULT_SOCIAL_IMAGE = new URL('/opengraph-image', SITE_URL).toString()

interface PageMetadataOptions {
  title: string
  description: string
  path: string
  image?: string | null
  imageAlt?: string
  index?: boolean
  follow?: boolean
  absoluteTitle?: boolean
  type?: 'website' | 'article'
  publishedTime?: string | null
  author?: string
}

export function absoluteUrl(path: string) {
  return new URL(path, SITE_URL).toString()
}

export function publicImageUrl(source?: string | null) {
  return source ? absoluteUrl(publicMediaUrl(source)) : DEFAULT_SOCIAL_IMAGE
}

export function summarize(value: string, maximumLength = 160) {
  const normalized = value.replace(/\s+/g, ' ').trim()
  if (normalized.length <= maximumLength) return normalized

  return `${normalized.slice(0, maximumLength - 1).trimEnd()}…`
}

export function createPageMetadata(options: PageMetadataOptions): Metadata {
  const canonical = absoluteUrl(options.path)
  const image = publicImageUrl(options.image)
  const title = options.absoluteTitle ? { absolute: options.title } : options.title
  const index = options.index ?? true
  const follow = options.follow ?? index
  const sharedOpenGraph = {
    title: options.title,
    description: options.description,
    url: canonical,
    siteName: SITE_NAME,
    locale: 'vi_VN',
    images: [{ url: image, width: 1200, height: 630, alt: options.imageAlt ?? options.title }],
  }
  const openGraph: Metadata['openGraph'] =
    options.type === 'article'
      ? {
          ...sharedOpenGraph,
          type: 'article',
          publishedTime: options.publishedTime ?? undefined,
          authors: options.author ? [options.author] : undefined,
        }
      : { ...sharedOpenGraph, type: 'website' }

  return {
    title,
    description: options.description,
    alternates: { canonical },
    robots: { index, follow },
    openGraph,
    twitter: {
      card: 'summary_large_image',
      title: options.title,
      description: options.description,
      images: [{ url: image, alt: options.imageAlt ?? options.title }],
    },
  }
}

function duration(minutes: number) {
  return `PT${minutes}M`
}

function ingredientText(recipe: Recipe) {
  return [...recipe.ingredients]
    .sort((left, right) => left.orderIndex - right.orderIndex)
    .map((ingredient) =>
      [ingredient.quantity, ingredient.unit, ingredient.name, ingredient.notes]
        .filter((value) => value !== null && value !== '')
        .join(' '),
    )
}

function nutrition(recipe: Recipe) {
  if (!recipe.nutrition) return undefined

  const values = {
    '@type': 'NutritionInformation',
    calories: recipe.nutrition.calories == null ? undefined : `${recipe.nutrition.calories} kcal`,
    proteinContent: recipe.nutrition.protein == null ? undefined : `${recipe.nutrition.protein} g`,
    carbohydrateContent:
      recipe.nutrition.carbohydrates == null ? undefined : `${recipe.nutrition.carbohydrates} g`,
    fatContent: recipe.nutrition.fat == null ? undefined : `${recipe.nutrition.fat} g`,
    fiberContent: recipe.nutrition.fiber == null ? undefined : `${recipe.nutrition.fiber} g`,
    sodiumContent: recipe.nutrition.sodium == null ? undefined : `${recipe.nutrition.sodium} mg`,
  }

  return Object.values(values)
    .slice(1)
    .some((value) => value !== undefined)
    ? values
    : undefined
}

export function recipeImages(recipe: Recipe) {
  const candidates = [
    recipe.primaryImageUrl,
    ...recipe.images
      .filter((image) => image.processingStatus === 'ready')
      .sort(
        (left, right) =>
          Number(right.isPrimary) - Number(left.isPrimary) || left.orderIndex - right.orderIndex,
      )
      .map((image) => image.mediumUrl ?? image.originalUrl),
  ]
    .filter((source): source is string => Boolean(source))
    .map(publicImageUrl)

  return candidates.length > 0 ? [...new Set(candidates)] : [DEFAULT_SOCIAL_IMAGE]
}

export function createRecipeJsonLd(recipe: Recipe) {
  const canonical = absoluteUrl(`/recipes/${encodeURIComponent(recipe.slug)}`)

  return {
    '@context': 'https://schema.org',
    '@type': 'Recipe',
    '@id': `${canonical}#recipe`,
    mainEntityOfPage: canonical,
    name: recipe.title,
    description: summarize(recipe.description),
    image: recipeImages(recipe),
    author: { '@type': 'Person', name: recipe.author.displayName },
    datePublished: recipe.publishedAt ?? undefined,
    prepTime: duration(recipe.prepTime),
    cookTime: duration(recipe.cookTime),
    totalTime: duration(recipe.prepTime + recipe.cookTime),
    recipeYield: `${recipe.servings} khẩu phần`,
    recipeCategory: recipe.category.name,
    recipeIngredient: ingredientText(recipe),
    recipeInstructions: [...recipe.steps]
      .sort((left, right) => left.stepNumber - right.stepNumber)
      .map((step) => ({
        '@type': 'HowToStep',
        position: step.stepNumber,
        name: step.title,
        text: step.description,
        image: step.imageUrl ? publicImageUrl(step.imageUrl) : undefined,
      })),
    nutrition: nutrition(recipe),
    inLanguage: 'vi-VN',
  }
}

export function serializeJsonLd(value: unknown) {
  return JSON.stringify(value).replace(/</g, '\\u003c')
}
