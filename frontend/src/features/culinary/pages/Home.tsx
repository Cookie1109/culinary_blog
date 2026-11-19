'use client'

import { useEffect, useRef, useState } from 'react'
import { ArrowRight, ChevronLeft, ChevronRight, LoaderCircle } from 'lucide-react'
import Image from 'next/image'
import { Link } from 'react-router'
import { CategoryCard } from '@/features/culinary/components/CategoryCard'
import { RecipeCard } from '@/features/culinary/components/RecipeCard'
import { listPublishedRecipes, type Category, type PageEnvelope, type Recipe } from '@/lib/api/content-client'
import heroImage from '@/assets/home-hero.jpg'

function scrollCarousel(element: HTMLDivElement | null, direction: -1 | 1) {
  if (!element) return

  const maxScroll = element.scrollWidth - element.clientWidth
  const atStart = element.scrollLeft <= 4
  const atEnd = element.scrollLeft >= maxScroll - 4
  let left = element.scrollLeft + direction * element.clientWidth
  if (direction === 1 && atEnd) left = 0
  else if (direction === -1 && atStart) left = maxScroll

  element.scrollTo({ left, behavior: 'smooth' })
}

export function Home({
  recipePage,
  categories,
}: {
  recipePage: PageEnvelope<Recipe>
  categories: Category[]
}) {
  const [recipes, setRecipes] = useState(recipePage.data)
  const [loadedPage, setLoadedPage] = useState(recipePage.meta.page)
  const [totalPages, setTotalPages] = useState(recipePage.meta.totalPages)
  const [loadingMoreRecipes, setLoadingMoreRecipes] = useState(false)
  const [collectionError, setCollectionError] = useState(false)
  const collectionRef = useRef<HTMLDivElement>(null)
  const categoriesRef = useRef<HTMLDivElement>(null)
  const loadingMoreRecipesRef = useRef(false)
  const contentGeneration = useRef(0)

  useEffect(() => {
    contentGeneration.current += 1
    setRecipes(recipePage.data)
    setLoadedPage(recipePage.meta.page)
    setTotalPages(recipePage.meta.totalPages)
    setCollectionError(false)
  }, [recipePage])

  const showNextCollectionRecipes = async () => {
    const carousel = collectionRef.current
    if (!carousel) return

    const atEnd = carousel.scrollLeft >= carousel.scrollWidth - carousel.clientWidth - 4
    if (!atEnd || loadedPage >= totalPages) {
      scrollCarousel(carousel, 1)
      return
    }

    if (loadingMoreRecipesRef.current) return
    loadingMoreRecipesRef.current = true
    setLoadingMoreRecipes(true)
    setCollectionError(false)

    const generation = contentGeneration.current
    try {
      const nextPage = await listPublishedRecipes(loadedPage + 1, recipePage.meta.pageSize)
      if (generation !== contentGeneration.current) return
      setRecipes((current) => {
        const knownIds = new Set(current.map((recipe) => recipe.id))
        return [...current, ...nextPage.data.filter((recipe) => !knownIds.has(recipe.id))]
      })
      setLoadedPage(nextPage.meta.page)
      setTotalPages(nextPage.meta.totalPages)
      window.requestAnimationFrame(() => {
        window.requestAnimationFrame(() => scrollCarousel(collectionRef.current, 1))
      })
    } catch {
      setCollectionError(true)
    } finally {
      loadingMoreRecipesRef.current = false
      setLoadingMoreRecipes(false)
    }
  }

  return (
    <div className="pb-24">
      <section
        aria-labelledby="home-hero-title"
        className="mx-auto grid min-h-[calc(100svh-81px)] max-w-7xl items-center gap-12 px-6 pb-16 pt-12 sm:px-8 lg:grid-cols-2 lg:gap-20 lg:pb-24 xl:grid-cols-[minmax(0,1fr)_580px] xl:pl-12 xl:pr-4"
      >
        <div>
          <p className="mb-7 flex items-center gap-3 text-xs uppercase tracking-[0.25em] text-primary">
            <span aria-hidden="true" className="h-px w-10 bg-primary" />
            Tạp chí ẩm thực
          </p>
          <h1
            id="home-hero-title"
            className="mb-7 break-words font-serif text-5xl font-semibold leading-[1.02] tracking-tight text-foreground sm:text-6xl xl:text-[76px]"
          >
            Kho <span className="text-primary">công thức</span>
            <br className="hidden xl:block" /> nấu ăn cho mọi gian bếp
          </h1>
          <p className="mb-8 max-w-xl text-base leading-relaxed text-muted-foreground sm:text-lg">
            Nơi chia sẻ công thức đã thử nghiệm, cẩm nang theo mùa và câu chuyện bếp núc, dành cho những người
            yêu quá trình nấu nướng không kém món ăn.
          </p>
          <Link
            to="/recipes"
            className="inline-flex min-h-12 items-center justify-center gap-3 bg-primary px-8 py-4 text-xs font-semibold uppercase tracking-wider text-primary-foreground transition-colors hover:bg-primary/90"
          >
            Khám phá công thức <ArrowRight size={16} aria-hidden="true" />
          </Link>
        </div>
        <div className="relative mr-4 mt-4 lg:mr-0">
          <div
            aria-hidden="true"
            className="absolute inset-0 -translate-y-4 translate-x-4 border border-primary/40"
          />
          <div className="relative aspect-[29/30]">
            <Image
              src={heroImage}
              alt="Bàn ăn với bánh mì, trứng, tách gốm và đĩa xanh trên nền gỗ"
              fill
              priority
              loading="eager"
              quality={90}
              sizes="(min-width: 1280px) 580px, (min-width: 1024px) 45vw, 100vw"
              placeholder="blur"
              className="object-cover"
            />
          </div>
          <div className="absolute -left-3 bottom-5 max-w-[260px] bg-background px-5 py-6 shadow-xl sm:-left-6 sm:bottom-8">
            <p className="mb-2 text-xs uppercase tracking-widest text-primary">Cộng đồng ẩm thực</p>
            <p className="font-serif text-xl leading-snug text-foreground">
              Chia sẻ, lưu giữ và khám phá công thức mỗi ngày.
            </p>
          </div>
        </div>
      </section>

      {/* Featured Recipes Collection */}
      <section className="mx-auto max-w-7xl px-4 pt-20 sm:px-6 lg:px-8" aria-labelledby="featured-title">
        <div className="mb-10 flex items-end justify-between gap-4 border-b border-border pb-6">
          <h2 id="featured-title" className="font-serif text-2xl text-foreground sm:text-3xl">
            Bộ sưu tập nổi bật
          </h2>
          <Link
            to="/recipes"
            aria-label="Xem tất cả công thức nổi bật"
            className="shrink-0 text-sm font-medium uppercase tracking-wide text-primary transition-colors hover:text-foreground"
          >
            Xem tất cả
          </Link>
        </div>

        {recipes.length === 0 ? (
          <p className="py-16 text-center text-muted-foreground">Chưa có công thức nào được xuất bản.</p>
        ) : (
          <div className="relative px-10 sm:px-14">
            <div className="pointer-events-none absolute left-10 right-10 top-0 z-10 aspect-square sm:left-14 sm:right-14 md:aspect-[3/1]">
              <button
                type="button"
                onClick={() => scrollCarousel(collectionRef.current, -1)}
                aria-label="Xem các công thức nổi bật trước"
                aria-controls="featured-recipes-carousel"
                className="pointer-events-auto absolute -left-10 top-1/2 flex h-12 w-10 -translate-y-1/2 cursor-pointer items-center justify-center text-foreground/60 transition-all duration-200 hover:text-primary active:scale-95 sm:-left-14"
              >
                <ChevronLeft size={36} strokeWidth={1.5} aria-hidden="true" />
              </button>

              <button
                type="button"
                onClick={() => {
                  showNextCollectionRecipes()
                }}
                aria-label="Xem thêm công thức nổi bật"
                aria-controls="featured-recipes-carousel"
                aria-busy={loadingMoreRecipes}
                className="pointer-events-auto absolute -right-10 top-1/2 flex h-12 w-10 -translate-y-1/2 cursor-pointer items-center justify-center text-foreground/60 transition-all duration-200 hover:text-primary active:scale-95 sm:-right-14"
              >
                {loadingMoreRecipes ? (
                  <LoaderCircle size={28} className="animate-spin text-primary" aria-hidden="true" />
                ) : (
                  <ChevronRight size={36} strokeWidth={1.5} aria-hidden="true" />
                )}
              </button>
            </div>

            <div
              ref={collectionRef}
              id="featured-recipes-carousel"
              className="flex snap-x snap-mandatory gap-8 overflow-x-auto scroll-smooth px-1 pb-4 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden [&>div>article]:[content-visibility:visible]"
            >
              {recipes.map((recipe) => (
                <div
                  key={recipe.id}
                  className="min-w-0 shrink-0 basis-full snap-start md:basis-[calc((100%-4rem)/3)]"
                >
                  <RecipeCard recipe={recipe} headingLevel="h3" />
                </div>
              ))}
            </div>
            {collectionError && (
              <p className="mt-3 text-center text-sm text-primary" role="alert">
                Không thể tải thêm công thức. Vui lòng thử lại.
              </p>
            )}
          </div>
        )}
      </section>

      {/* Featured Categories */}
      <section className="mx-auto max-w-7xl px-4 pt-20 sm:px-6 lg:px-8" aria-labelledby="categories-title">
        <div className="mb-10 flex items-end justify-between gap-4 border-b border-border pb-6">
          <div>
            <p className="mb-2 text-sm uppercase tracking-widest text-primary">Khám phá theo chủ đề</p>
            <h2 id="categories-title" className="font-serif text-2xl text-foreground sm:text-3xl">
              Danh mục nổi bật
            </h2>
          </div>
          <Link
            to="/categories"
            aria-label="Xem tất cả danh mục nổi bật"
            className="shrink-0 text-sm font-medium uppercase tracking-wide text-primary transition-colors hover:text-foreground"
          >
            Xem tất cả
          </Link>
        </div>

        {categories.length === 0 ? (
          <p className="py-12 text-center text-muted-foreground">Chưa có danh mục nào.</p>
        ) : (
          <div className="relative px-10 sm:px-14">
            <div className="pointer-events-none absolute left-10 right-10 top-0 z-10 aspect-[4/3] sm:left-14 sm:right-14 sm:aspect-[8/3] lg:aspect-[16/3]">
              <button
                type="button"
                onClick={() => scrollCarousel(categoriesRef.current, -1)}
                aria-label="Xem các danh mục trước"
                aria-controls="featured-categories-carousel"
                className="pointer-events-auto absolute -left-10 top-1/2 flex h-12 w-10 -translate-y-1/2 cursor-pointer items-center justify-center text-foreground/60 transition-all duration-200 hover:text-primary active:scale-95 sm:-left-14"
              >
                <ChevronLeft size={36} strokeWidth={1.5} aria-hidden="true" />
              </button>

              <button
                type="button"
                onClick={() => scrollCarousel(categoriesRef.current, 1)}
                aria-label="Xem thêm danh mục"
                aria-controls="featured-categories-carousel"
                className="pointer-events-auto absolute -right-10 top-1/2 flex h-12 w-10 -translate-y-1/2 cursor-pointer items-center justify-center text-foreground/60 transition-all duration-200 hover:text-primary active:scale-95 sm:-right-14"
              >
                <ChevronRight size={36} strokeWidth={1.5} aria-hidden="true" />
              </button>
            </div>

            <div
              ref={categoriesRef}
              id="featured-categories-carousel"
              className="flex snap-x snap-mandatory gap-5 overflow-x-auto scroll-smooth px-1 pb-4 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden [&>div>article]:[content-visibility:visible]"
            >
              {categories.map((category) => (
                <div
                  key={category.id}
                  className="min-w-0 shrink-0 basis-full snap-start sm:basis-[calc((100%-1.25rem)/2)] lg:basis-[calc((100%-3.75rem)/4)]"
                >
                  <CategoryCard category={category} headingLevel="h3" />
                </div>
              ))}
            </div>
          </div>
        )}
      </section>
    </div>
  )
}
