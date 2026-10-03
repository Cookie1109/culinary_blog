'use client'

import { useQuery } from '@tanstack/react-query'
import {
  Archive,
  ArrowRight,
  BookOpen,
  CheckCircle,
  Eye,
  FileText,
  Pencil,
  PlusCircle,
  RefreshCw,
  TrendingUp,
} from 'lucide-react'
import { Link, useNavigate } from 'react-router'
import { listMyRecipes, type RecipeStatus } from '@/lib/api/content-client'
import { useAuth } from '../../contexts/AuthContext'

const STATUS_BADGE: Record<RecipeStatus, string> = {
  published: 'bg-green-50 text-green-700 border border-green-200',
  draft: 'bg-amber-50 text-amber-700 border border-amber-200',
  archived: 'bg-secondary text-muted-foreground border border-border',
}

const STATUS_LABEL: Record<RecipeStatus, string> = {
  published: 'Đã xuất bản',
  draft: 'Bản nháp',
  archived: 'Đã lưu trữ',
}

export function Dashboard() {
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
    <div>
      {/* Welcome */}
      <div className="mb-10">
        <h1 className="mb-1 font-serif text-3xl text-foreground lg:text-4xl">
          Xin chào, {user.name.split(' ')[0]}.
        </h1>
        <p className="text-muted-foreground">Dưới đây là tổng quan các công thức của bạn.</p>
      </div>

      {/* Stats */}
      <div className="mb-10 grid grid-cols-2 gap-4 lg:grid-cols-4">
        {stats.map(({ label, value, icon: Icon, color, filter }) => (
          <Link
            key={label}
            to={filter ? `/dashboard/recipes?status=${filter}` : '/dashboard/recipes'}
            className="group block border border-border bg-background p-5 transition-colors hover:border-primary/50 lg:p-6"
          >
            <div className="mb-3 flex items-start justify-between">
              <span className="text-xs uppercase tracking-widest text-muted-foreground group-hover:text-foreground">
                {label}
              </span>
              <Icon size={18} strokeWidth={1.5} className={color} />
            </div>
            <p className="font-serif text-3xl text-foreground">{value}</p>
          </Link>
        ))}
      </div>

      {/* Quick actions */}
      <div className="mb-10 flex flex-wrap gap-3">
        <Link
          to="/dashboard/recipes/new"
          className="flex items-center gap-2 bg-primary px-5 py-2.5 text-sm uppercase tracking-widest text-primary-foreground transition-colors hover:bg-primary/90"
        >
          <PlusCircle size={16} /> Tạo công thức mới
        </Link>
        <Link
          to="/dashboard/recipes"
          className="flex items-center gap-2 border border-border px-5 py-2.5 text-sm uppercase tracking-widest transition-colors hover:bg-secondary"
        >
          <BookOpen size={16} /> Quản lý công thức
        </Link>
        <Link
          to="/categories"
          className="flex items-center gap-2 border border-border px-5 py-2.5 text-sm uppercase tracking-widest transition-colors hover:bg-secondary"
        >
          <TrendingUp size={16} /> Danh mục
        </Link>
      </div>

      {/* Recent recipes */}
      <div className="border border-border bg-background">
        <div className="flex items-center justify-between border-b border-border px-6 py-4">
          <h2 className="font-serif text-xl">Công thức gần đây</h2>
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
              onClick={() => {
                void recentQuery.refetch()
                void publishedQuery.refetch()
                void draftQuery.refetch()
                void archivedQuery.refetch()
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
            <Link
              to="/dashboard/recipes/new"
              className="inline-flex items-center gap-2 bg-primary px-5 py-2.5 text-sm uppercase tracking-widest text-primary-foreground hover:bg-primary/90"
            >
              <PlusCircle size={16} /> Tạo công thức mới
            </Link>
          </div>
        )}

        {!isLoading && !isError && recentRecipes.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border">
                  <th className="px-6 py-3 text-left text-xs uppercase tracking-widest font-medium text-muted-foreground">
                    Tiêu đề
                  </th>
                  <th className="hidden px-6 py-3 text-left text-xs uppercase tracking-widest font-medium text-muted-foreground sm:table-cell">
                    Danh mục
                  </th>
                  <th className="px-6 py-3 text-left text-xs uppercase tracking-widest font-medium text-muted-foreground">
                    Trạng thái
                  </th>
                  <th className="hidden px-6 py-3 text-right text-xs uppercase tracking-widest font-medium text-muted-foreground md:table-cell">
                    Ngày tạo
                  </th>
                  <th className="px-6 py-3 text-right text-xs uppercase tracking-widest font-medium text-muted-foreground">
                    Thao tác
                  </th>
                </tr>
              </thead>
              <tbody>
                {recentRecipes.map((recipe) => (
                  <tr
                    key={recipe.id}
                    className="border-b border-border last:border-0 transition-colors hover:bg-secondary/40"
                  >
                    <td className="px-6 py-4">
                      <Link
                        to={`/dashboard/recipes/${recipe.id}/edit`}
                        className="font-medium text-foreground hover:underline"
                      >
                        {recipe.title}
                      </Link>
                    </td>
                    <td className="hidden px-6 py-4 text-muted-foreground sm:table-cell">
                      {recipe.category.name}
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-block px-2.5 py-1 text-xs rounded-none ${STATUS_BADGE[recipe.status]}`}
                      >
                        {STATUS_LABEL[recipe.status] ?? recipe.status}
                      </span>
                    </td>
                    <td className="hidden px-6 py-4 text-right text-muted-foreground md:table-cell">
                      {new Intl.DateTimeFormat('vi-VN').format(new Date(recipe.createdAt))}
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-3">
                        {recipe.status === 'published' && (
                          <Link
                            to={`/recipes/${recipe.slug}`}
                            aria-label={`Xem công thức công khai ${recipe.title}`}
                            className="text-muted-foreground hover:text-foreground"
                            title="Xem công khai"
                          >
                            <Eye size={15} />
                          </Link>
                        )}
                        <Link
                          to={`/dashboard/recipes/${recipe.id}/preview`}
                          aria-label={`Xem trước riêng tư ${recipe.title}`}
                          className="text-xs uppercase tracking-widest text-muted-foreground hover:text-foreground"
                          title="Xem trước"
                        >
                          Xem trước
                        </Link>
                        <Link
                          to={`/dashboard/recipes/${recipe.id}/edit`}
                          aria-label={`Chỉnh sửa ${recipe.title}`}
                          className="flex items-center gap-1 text-xs uppercase tracking-widest text-primary hover:text-primary/80"
                        >
                          <Pencil size={13} /> Sửa
                        </Link>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
