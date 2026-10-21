'use client'

import { useQuery } from '@tanstack/react-query'
import { Archive, BookOpen, CheckCircle, FileText, History } from 'lucide-react'
import { Link } from 'react-router'
import { listAdminRecipes, type RecipeStatus } from '@/lib/api/content-client'
import { useAuth } from '../../contexts/AuthContext'
import { MyRecipes } from './MyRecipes'

const STATUSES: { status?: RecipeStatus; label: string; icon: typeof BookOpen; color: string }[] = [
  { label: 'Tổng số công thức', icon: BookOpen, color: 'text-foreground' },
  { status: 'published', label: 'Đã xuất bản', icon: CheckCircle, color: 'text-green-600' },
  { status: 'draft', label: 'Bản nháp', icon: FileText, color: 'text-amber-600' },
  { status: 'archived', label: 'Đã lưu trữ', icon: Archive, color: 'text-muted-foreground' },
]

export function SystemOverview() {
  const { user } = useAuth()
  if (user?.role !== 'admin') {
    return <div role="alert">Chỉ quản trị viên có thể truy cập tổng quan hệ thống.</div>
  }
  return <SystemContent />
}

function SystemStat({ status, label, icon: Icon, color }: (typeof STATUSES)[number]) {
  const query = useQuery({
    queryKey: ['admin-recipes', 'count', status],
    queryFn: () => listAdminRecipes(status, undefined, 1, 1),
  })
  return (
    <div className="border border-border bg-background p-5 lg:p-6">
      <div className="mb-3 flex items-start justify-between gap-2">
        <span className="text-xs uppercase tracking-widest text-muted-foreground">{label}</span>
        <Icon size={18} strokeWidth={1.5} className={color} aria-hidden="true" />
      </div>
      <p className="font-serif text-3xl text-foreground">
        {query.isPending ? '…' : query.isError ? '—' : query.data.meta.total}
      </p>
      {query.isError && <span className="text-xs text-red-700">Không thể tải thống kê</span>}
    </div>
  )
}

function SystemContent() {
  return (
    <div>
      <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="font-serif text-3xl text-foreground lg:text-4xl">Tổng quan hệ thống</h1>
          <p className="mt-1 text-muted-foreground">Theo dõi và quản lý công thức của toàn bộ tác giả.</p>
        </div>
        <div className="flex flex-wrap gap-3">
          <Link
            to="/dashboard/audit"
            className="flex items-center gap-2 border border-border px-4 py-2.5 text-sm hover:bg-secondary"
          >
            <History size={16} aria-hidden="true" /> Lịch sử
          </Link>
        </div>
      </div>
      <div
        className="mb-10 grid grid-cols-2 gap-4 lg:grid-cols-4"
        aria-label="Thống kê công thức toàn hệ thống"
      >
        {STATUSES.map((stat) => (
          <SystemStat key={stat.label} {...stat} />
        ))}
      </div>
      <MyRecipes systemOverview />
    </div>
  )
}
