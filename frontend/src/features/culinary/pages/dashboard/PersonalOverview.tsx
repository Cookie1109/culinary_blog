'use client'

import { useQuery } from '@tanstack/react-query'
import {
  Archive,
  ArrowRight,
  BookOpen,
  CheckCircle,
  Clock3,
  Eye,
  FileText,
  Pencil,
  PlusCircle,
  RefreshCw,
} from 'lucide-react'
import { Link, useNavigate } from 'react-router'
import { listMyRecipes, type RecipeStatus } from '@/lib/api/content-client'
import { useAuth } from '../../contexts/AuthContext'

const STATUS_BADGE: Record<RecipeStatus, string> = {
  published: 'bg-emerald-50 text-emerald-700',
  draft: 'bg-amber-50 text-amber-700',
  archived: 'bg-secondary text-muted-foreground',
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

  const stats = [
    {
      label: 'Tổng số công thức',
      value: isLoading ? '…' : String(totalRecipes),
      icon: BookOpen,
      color: 'text-foreground',
      filter: undefined,
    },
    {
      label: 'Đã xuất bản',
      value: isLoading ? '…' : String(publishedCount),
      icon: CheckCircle,
      color: 'text-green-600',
      filter: 'published',
    },
    {
      label: 'Bản nháp',
      value: isLoading ? '…' : String(draftCount),
      icon: FileText,
      color: 'text-amber-600',
      filter: 'draft',
    },
    {
      label: 'Đã lưu trữ',
      value: isLoading ? '…' : String(archivedCount),
      icon: Archive,
      color: 'text-muted-foreground',
      filter: 'archived',
    },
  ]

  const recentRecipes = recentQuery.data?.data ?? []

  return (
    <div data-personal-overview>
      {/* Welcome */}
      <div className="mb-7 flex flex-col justify-between gap-5 border-b border-border pb-7 sm:flex-row sm:items-center">
        <div>
          <h1 className="mb-2 font-serif text-3xl text-foreground lg:text-4xl">Tổng quan cá nhân</h1>
          <p className="text-sm leading-relaxed text-muted-foreground">
            Xin chào, {user.name.split(' ')[0]}. Đây là các công thức của bạn.
          </p>
        </div>
        <Link
          to="/dashboard/recipes/new"
          className="inline-flex min-h-11 shrink-0 items-center justify-center gap-2 bg-primary px-5 py-3 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
        >
          <PlusCircle size={16} aria-hidden="true" /> Tạo công thức mới
        </Link>
      </div>

      {/* Stats */}
      <div className="mb-8 grid grid-cols-2 gap-3 lg:grid-cols-4 lg:gap-5">
        {stats.map(({ label, value, icon: Icon, color, filter }) => (
          <Link
            key={label}
            to={filter ? `/dashboard/recipes?status=${filter}` : '/dashboard/recipes'}
            className="group flex min-w-0 items-center justify-between gap-3 border border-border bg-card p-4 transition-colors hover:border-primary/50 sm:p-5 lg:p-6"
          >
            <div className="min-w-0">
              <p className="mb-3 text-sm text-muted-foreground group-hover:text-foreground">{label}</p>
              <p className="font-serif text-4xl leading-none tabular-nums text-foreground">{value}</p>
            </div>
            <span
              className={`hidden size-11 shrink-0 items-center justify-center bg-background sm:flex ${color}`}
            >
              <Icon size={21} strokeWidth={1.5} aria-hidden="true" />
            </span>
          </Link>
        ))}
      </div>

      {/* Recent recipes */}
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
            Xem tất cả <ArrowRight size={13} />
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
            <p className="mb-6 text-sm text-muted-foreground">
              Bắt đầu hành trình sáng tạo ẩm thực bằng cách tạo bản nháp đầu tiên.
            </p>
          </div>
        )}

        {!isLoading && !isError && recentRecipes.length > 0 && (
          <ul className="divide-y divide-border">
            {recentRecipes.map((recipe) => (
              <li
                key={recipe.id}
                className="grid grid-cols-[3rem_minmax(0,1fr)] items-center gap-x-4 gap-y-2 px-4 py-5 transition-colors hover:bg-background/70 sm:grid-cols-[3rem_minmax(0,1fr)_auto] sm:px-6"
              >
                <span
                  aria-hidden="true"
                  className="flex size-12 shrink-0 items-center justify-center border border-border/60 bg-background font-serif text-xl text-primary"
                >
                  {Array.from(recipe.title.trim())[0]?.toLocaleUpperCase('vi-VN')}
                </span>
                <div className="min-w-0">
                  <Link
                    to={`/dashboard/recipes/${recipe.id}/preview`}
                    className="block break-words text-base font-medium text-foreground hover:text-primary"
                  >
                    {recipe.title}
                  </Link>
                  <p className="mt-1 flex flex-wrap items-center gap-x-1 text-xs leading-relaxed text-muted-foreground">
                    {recipe.category.name} ·{' '}
                    <time dateTime={recipe.createdAt}>
                      {new Intl.DateTimeFormat('vi-VN').format(new Date(recipe.createdAt))}
                    </time>
                  </p>
                </div>
                <div className="col-start-2 flex items-center justify-between gap-2 sm:col-auto sm:justify-end sm:gap-3">
                  <Link
                    to={`/dashboard/recipes/${recipe.id}/preview`}
                    aria-label={`Xem trước riêng tư ${recipe.title}`}
                    title="Xem trước"
                    className="flex size-11 items-center justify-center rounded-sm text-muted-foreground transition-colors hover:bg-secondary/60 hover:text-primary"
                  >
                    <Eye size={15} strokeWidth={1.5} aria-hidden="true" />
                  </Link>
                  <span
                    className={`inline-flex w-28 items-center justify-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-1.5 text-xs ${STATUS_BADGE[recipe.status]}`}
                  >
                    <span aria-hidden="true" className="size-1.5 rounded-full bg-current" />
                    {STATUS_LABEL[recipe.status] ?? recipe.status}
                  </span>
                  <Link
                    to={`/dashboard/recipes/${recipe.id}/edit`}
                    aria-label={`Chỉnh sửa ${recipe.title}`}
                    title="Chỉnh sửa"
                    className="flex size-11 items-center justify-center rounded-sm text-muted-foreground transition-colors hover:bg-secondary/60 hover:text-primary"
                  >
                    <Pencil size={17} strokeWidth={1.5} aria-hidden="true" />
                  </Link>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
