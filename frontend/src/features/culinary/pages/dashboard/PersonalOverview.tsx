'use client'

import { useQuery } from '@tanstack/react-query'
import {
  ArrowRight,
  BookOpen,
  Clock3,
  Eye,
  FileText,
  Pencil,
  PlusCircle,
  RefreshCw,
  UserRound,
} from 'lucide-react'
import { Link, useNavigate } from 'react-router'
import { listMyRecipes, type RecipeStatus } from '@/lib/api/content-client'
import { useAuth } from '../../contexts/AuthContext'

const STATUS_DOT: Record<RecipeStatus, string> = {
  published: 'bg-emerald-600',
  draft: 'bg-amber-500',
  archived: 'bg-border',
}

const STATUS_LABEL: Record<RecipeStatus, string> = {
  published: 'Đã xuất bản',
  draft: 'Bản nháp',
  archived: 'Đã lưu trữ',
}

export function PersonalOverview() {
  const { user } = useAuth()
  const navigate = useNavigate()

  const recentQuery = useQuery({
    queryKey: ['my-recipes', 'recent'],
    queryFn: () => listMyRecipes(undefined, 1, 5),
  })

  const publishedQuery = useQuery({
    queryKey: ['my-recipes', 'count', 'published'],
    queryFn: () => listMyRecipes('published', 1, 1),
  })

  const draftQuery = useQuery({
    queryKey: ['my-recipes', 'count', 'draft'],
    queryFn: () => listMyRecipes('draft', 1, 1),
  })

  const archivedQuery = useQuery({
    queryKey: ['my-recipes', 'count', 'archived'],
    queryFn: () => listMyRecipes('archived', 1, 1),
  })

  if (!user) {
    return (
      <div className="py-16 text-center">
        <p className="mb-4 font-serif text-2xl">Bạn cần đăng nhập để truy cập bảng điều khiển.</p>
        <button
          type="button"
          onClick={() => navigate('/login')}
          className="bg-primary px-8 py-3 text-sm uppercase tracking-widest text-primary-foreground transition-colors hover:bg-primary/90"
        >
          Đăng nhập
        </button>
      </div>
    )
  }

  const isLoading =
    recentQuery.isPending || publishedQuery.isPending || draftQuery.isPending || archivedQuery.isPending
  const isError = recentQuery.isError || publishedQuery.isError || draftQuery.isError || archivedQuery.isError

  const totalRecipes = recentQuery.data?.meta.total ?? 0
  const publishedCount = publishedQuery.data?.meta.total ?? 0
  const draftCount = draftQuery.data?.meta.total ?? 0
  const archivedCount = archivedQuery.data?.meta.total ?? 0

  const pipeline = [
    { label: 'Đã xuất bản', value: publishedCount, filter: 'published', bar: 'bg-emerald-600' },
    { label: 'Bản nháp', value: draftCount, filter: 'draft', bar: 'bg-amber-500' },
    { label: 'Đã lưu trữ', value: archivedCount, filter: 'archived', bar: 'bg-border' },
  ]
  const counted = publishedCount + draftCount + archivedCount

  const recentRecipes = recentQuery.data?.data ?? []

  return (
    <div data-personal-overview>
      {/* Welcome */}
      <div className="mb-7 flex flex-col justify-between gap-5 border-b border-border pb-7 sm:flex-row sm:items-center">
        <div>
          <h1 className="mb-2 font-serif text-3xl text-foreground lg:text-4xl">Tổng quan cá nhân</h1>
          <p className="text-sm leading-relaxed text-muted-foreground">
            Xin chào, {user.name.split(' ')[0]}. Đây là bức tranh nhanh về tiến độ viết của bạn.
          </p>
        </div>
        <Link
          to="/dashboard/recipes/new"
          className="inline-flex min-h-11 shrink-0 items-center justify-center gap-2 bg-primary px-5 py-3 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
        >
          <PlusCircle size={16} aria-hidden="true" /> Tạo công thức mới
        </Link>
      </div>

      {/* Attention */}
      {!isLoading && !isError && draftCount > 0 && (
        <Link
          to="/dashboard/recipes?status=draft"
          className="mb-8 flex items-center justify-between gap-4 border border-amber-300 bg-amber-50 px-5 py-4 text-sm text-amber-900 transition-colors hover:bg-amber-100"
        >
          <span className="flex items-center gap-3">
            <FileText size={18} strokeWidth={1.5} aria-hidden="true" />
            <span>
              Bạn có <strong>{draftCount}</strong> bản nháp chưa xuất bản.
            </span>
          </span>
          <span className="flex shrink-0 items-center gap-1.5 text-xs uppercase tracking-widest">
            Xem bản nháp <ArrowRight size={13} aria-hidden="true" />
          </span>
        </Link>
      )}

      <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_20rem]">
        {/* Recent activity */}
        <div className="order-2 min-w-0 lg:order-1">
          <div className="overflow-hidden border border-border bg-card">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border bg-background/50 px-4 py-5 sm:px-6">
              <h2 className="flex items-center gap-3 font-serif text-xl">
                <Clock3 size={17} strokeWidth={1.5} className="text-primary" aria-hidden="true" />
                Công thức gần đây
              </h2>
              <Link
                to="/dashboard/recipes"
                className="flex items-center gap-1.5 text-xs uppercase tracking-widest text-primary transition-colors hover:text-primary/80"
              >
                Quản lý tất cả <ArrowRight size={13} aria-hidden="true" />
              </Link>
            </div>

            {isError && (
              <div className="border-b border-red-200 bg-red-50 p-6 text-red-700" role="alert">
                <p>Không thể tải dữ liệu bảng điều khiển.</p>
                <button
                  type="button"
                  onClick={() => {
                    recentQuery.refetch()
                    publishedQuery.refetch()
                    draftQuery.refetch()
                    archivedQuery.refetch()
                  }}
                  className="mt-3 flex items-center gap-1.5 text-sm font-medium underline"
                >
                  <RefreshCw size={14} /> Thử lại
                </button>
              </div>
            )}

            {isLoading && (
              <div className="p-12 text-center text-muted-foreground" role="status">
                Đang tải dữ liệu tổng quan…
              </div>
            )}

            {!isLoading && !isError && recentRecipes.length === 0 && (
              <div className="px-6 py-16 text-center">
                <BookOpen className="mx-auto mb-3 text-muted-foreground" size={32} />
                <p className="mb-2 font-serif text-xl">Bạn chưa có công thức nào</p>
                <p className="text-sm text-muted-foreground">
                  Bắt đầu hành trình sáng tạo ẩm thực bằng cách tạo bản nháp đầu tiên.
                </p>
              </div>
            )}

            {!isLoading && !isError && recentRecipes.length > 0 && (
              <ol className="px-4 py-2 sm:px-6">
                {recentRecipes.map((recipe) => (
                  <li
                    key={recipe.id}
                    className="grid grid-cols-[0.75rem_minmax(0,1fr)] gap-x-4 border-b border-border py-4 last:border-0"
                  >
                    <span
                      aria-hidden="true"
                      className={`mt-1.5 size-2.5 rounded-full ${STATUS_DOT[recipe.status]}`}
                    />
                    <div className="min-w-0">
                      <Link
                        to={`/dashboard/recipes/${recipe.id}/preview`}
                        className="block break-words font-medium text-foreground hover:text-primary"
                      >
                        {recipe.title}
                      </Link>
                      <div className="mt-1 flex flex-wrap items-center gap-x-2 text-xs text-muted-foreground">
                        <span>{STATUS_LABEL[recipe.status] ?? recipe.status}</span>
                        <span aria-hidden="true">·</span>
                        <span>{recipe.category.name}</span>
                        <span aria-hidden="true">·</span>
                        <time dateTime={recipe.createdAt}>
                          {new Intl.DateTimeFormat('vi-VN').format(new Date(recipe.createdAt))}
                        </time>
                        {recipe.status === 'draft' && (
                          <Link
                            to={`/dashboard/recipes/${recipe.id}/edit`}
                            aria-label={`Chỉnh sửa ${recipe.title}`}
                            className="ml-auto inline-flex min-h-11 items-center gap-1.5 text-primary hover:underline"
                          >
                            <Pencil size={13} strokeWidth={1.5} aria-hidden="true" /> Tiếp tục viết
                          </Link>
                        )}
                      </div>
                    </div>
                  </li>
                ))}
              </ol>
            )}
          </div>
        </div>

        <aside className="order-1 min-w-0 space-y-6 lg:order-2">
          {/* Pipeline */}
          <section className="border border-border bg-card p-5" aria-label="Tình trạng công thức">
            <p className="text-xs uppercase tracking-widest text-muted-foreground">Tổng số công thức</p>
            <p className="mt-2 font-serif text-5xl leading-none tabular-nums">
              {isLoading ? '…' : totalRecipes}
            </p>
            <div className="mt-5 flex h-2 overflow-hidden bg-secondary" aria-hidden="true">
              {pipeline.map(({ label, value, bar }) => (
                <span
                  key={label}
                  className={bar}
                  style={{ width: `${counted ? (value / counted) * 100 : 0}%` }}
                />
              ))}
            </div>
            <ul className="mt-4 divide-y divide-border text-sm">
              {pipeline.map(({ label, value, filter, bar }) => (
                <li key={label}>
                  <Link
                    to={`/dashboard/recipes?status=${filter}`}
                    className="flex min-h-11 items-center justify-between gap-3 hover:text-primary"
                  >
                    <span className="flex items-center gap-2.5">
                      <span aria-hidden="true" className={`size-2.5 ${bar}`} />
                      {label}
                    </span>
                    <span className="font-serif text-lg tabular-nums">{isLoading ? '…' : value}</span>
                  </Link>
                </li>
              ))}
            </ul>
          </section>

          {/* Shortcuts */}
          <section className="border border-border bg-card p-5">
            <h2 className="mb-2 text-xs uppercase tracking-widest text-muted-foreground">Lối tắt</h2>
            <ul className="divide-y divide-border text-sm">
              {[
                { to: '/recipes', label: 'Xem trang công khai', icon: Eye },
                { to: '/profile', label: 'Hồ sơ của tôi', icon: UserRound },
              ].map(({ to, label, icon: Icon }) => (
                <li key={to}>
                  <Link to={to} className="flex min-h-11 items-center gap-3 hover:text-primary">
                    <Icon size={16} strokeWidth={1.5} aria-hidden="true" /> {label}
                  </Link>
                </li>
              ))}
            </ul>
          </section>
        </aside>
      </div>
    </div>
  )
}
