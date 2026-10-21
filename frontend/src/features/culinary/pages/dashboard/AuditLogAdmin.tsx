'use client'

import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, ArrowRight, Check, Copy, RefreshCw, Search, ShieldAlert } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { Modal } from '@/components/ui/modal'
import { useAuth } from '@/features/culinary/contexts/AuthContext'
import { listAuditLogs, type AuditLogEntry } from '@/lib/api/content-client'

const LEVEL_FILTERS = [
  { value: 'all', label: 'Tất cả' },
  { value: 'Information', label: 'Thông tin' },
  { value: 'Warning', label: 'Cảnh báo' },
  { value: 'Error', label: 'Lỗi' },
]

const LEVEL_BADGE: Record<string, string> = {
  Information: 'bg-blue-50 text-blue-700 border border-blue-200',
  Warning: 'bg-amber-50 text-amber-700 border border-amber-200',
  Error: 'bg-red-50 text-red-700 border border-red-200',
}

const ACTION_LABELS: Record<string, string> = {
  created: 'Tạo mới',
  updated: 'Cập nhật',
  deleted: 'Xóa',
  published: 'Xuất bản',
  unpublished: 'Gỡ xuất bản',
  archived: 'Lưu trữ',
  unarchived: 'Khôi phục',
}
const RESOURCE_LABELS: Record<string, string> = {
  recipe: 'công thức',
  category: 'danh mục',
  ingredients: 'nguyên liệu',
  steps: 'bước nấu',
  images: 'hình ảnh',
}

function eventLabel(entry: AuditLogEntry) {
  const parts = entry.eventName.split('.')
  const action = ACTION_LABELS[parts.at(-1) ?? '']
  return action
    ? `${action} ${RESOURCE_LABELS[parts.length > 2 ? parts[1] : parts[0]] ?? ''}`
    : entry.eventName
}

function actorLabel(entry: AuditLogEntry) {
  return (
    entry.properties?.UserName ||
    entry.userId ||
    (entry.properties?.Source === 'snapshot' ? 'Không ghi nhận' : 'Hệ thống')
  )
}

function targetLabel(entry: AuditLogEntry) {
  return (
    entry.properties?.EntityName ||
    entry.properties?.Title ||
    entry.properties?.Name ||
    entry.properties?.EntityId ||
    entry.requestPath ||
    '—'
  )
}

function formatTime(timestamp: string) {
  return new Intl.DateTimeFormat('vi-VN', {
    timeZone: 'Asia/Bangkok',
    dateStyle: 'short',
    timeStyle: 'medium',
    hour12: false,
  }).format(new Date(timestamp))
}

export function AuditLogAdmin() {
  const { user } = useAuth()
  const [level, setLevel] = useState('all')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 15

  const [selectedEntry, setSelectedEntry] = useState<AuditLogEntry | null>(null)
  const [copiedId, setCopiedId] = useState<string | null>(null)

  const logsQuery = useQuery({
    queryKey: ['admin-audit-logs', level, search, page, pageSize],
    queryFn: () => listAuditLogs({ level, search: search.trim() || undefined, page, pageSize }),
    enabled: user?.role === 'admin',
    staleTime: 0,
  })

  if (user?.role !== 'admin') {
    return (
      <div role="alert" className="border border-amber-300 bg-amber-50 p-6 text-amber-800">
        Chỉ quản trị viên có thể truy cập nhật ký kiểm toán.
      </div>
    )
  }

  const copyToClipboard = async (text: string, id: string) => {
    try {
      await navigator.clipboard.writeText(text)
      setCopiedId(id)
      setTimeout(() => setCopiedId(null), 2000)
    } catch {
      // Fallback ignore
    }
  }

  const logs = logsQuery.data?.data ?? []
  const meta = logsQuery.data?.meta
  const telemetry = logsQuery.data?.telemetry
  const total = meta?.total ?? 0
  const totalPages = meta?.totalPages ?? 1

  return (
    <div>
      <Link
        to="/dashboard/system"
        className="mb-5 inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-primary"
      >
        <ArrowLeft size={16} aria-hidden="true" /> Tổng quan hệ thống
      </Link>
      {/* Header */}
      <div className="mb-6 flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="font-serif text-3xl text-foreground lg:text-4xl">Lịch sử hệ thống</h1>
          <p className="mt-1 text-muted-foreground">
            Theo dõi các thay đổi dữ liệu, hoạt động xuất bản và người thực hiện.
          </p>
        </div>
        <button
          type="button"
          onClick={() => logsQuery.refetch()}
          disabled={logsQuery.isFetching}
          className="flex shrink-0 items-center gap-2 border border-border bg-background px-4 py-2.5 text-xs uppercase tracking-widest text-foreground hover:bg-secondary transition-colors disabled:opacity-50"
        >
          <RefreshCw size={14} className={logsQuery.isFetching ? 'animate-spin' : ''} />
          Làm mới
        </button>
      </div>

      {/* Operational Telemetry Notice Banner */}
      <details className="mb-6 border border-border bg-secondary/30 p-4">
        <summary className="cursor-pointer text-sm font-medium">Trạng thái giám sát hệ thống</summary>
        <div className="flex items-start gap-3">
          <ShieldAlert className="text-primary shrink-0 mt-0.5" size={20} />
          <div className="flex-1 text-sm space-y-2">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <span className="font-semibold text-foreground">Trạng thái giám sát hệ thống</span>
              <div className="flex flex-wrap gap-2 text-xs">
                <span className="inline-flex items-center gap-1 rounded bg-background px-2.5 py-1 border border-border font-mono">
                  Nhật ký Seq: {telemetry?.seqConfigured ? 'Đang hoạt động' : 'Chưa cấu hình'}
                </span>
                <span className="inline-flex items-center gap-1 rounded bg-background px-2.5 py-1 border border-border font-mono">
                  Giám sát OpenTelemetry: {telemetry?.otlpConfigured ? 'Đang hoạt động' : 'Chưa cấu hình'}
                </span>
              </div>
            </div>
            <p className="text-xs text-muted-foreground leading-relaxed">
              Dữ liệu giám sát chi tiết chỉ truy cập được qua mạng nội bộ hoặc kết nối có xác thực. Trang này
              hiển thị nhật ký hoạt động dành cho quản trị viên.
            </p>
          </div>
        </div>
      </details>

      {/* Filter and Search Bar */}
      <div className="mb-6 flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="relative max-w-md flex-1">
          <Search
            className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-muted-foreground"
            size={16}
          />
          <input
            value={search}
            onChange={(event) => {
              setSearch(event.target.value)
              setPage(1)
            }}
            aria-label="Tìm lịch sử theo người thực hiện, nội dung hoặc sự kiện"
            placeholder="Tìm theo người thực hiện, nội dung, sự kiện…"
            className="w-full border border-border bg-background py-2.5 pl-10 pr-4 text-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          />
        </div>
        <div className="flex flex-wrap border border-border bg-background">
          {LEVEL_FILTERS.map((item) => (
            <button
              key={item.value}
              onClick={() => {
                setLevel(item.value)
                setPage(1)
              }}
              className={`px-4 py-2 text-xs font-medium uppercase tracking-widest transition-colors ${
                level === item.value
                  ? 'bg-foreground text-background'
                  : 'text-muted-foreground hover:bg-secondary'
              }`}
            >
              {item.label}
            </button>
          ))}
        </div>
      </div>

      {/* Logs Table */}
      {logsQuery.isPending && (
        <div
          role="status"
          className="border border-border p-12 text-center text-muted-foreground bg-background"
        >
          Đang tải nhật ký kiểm toán…
        </div>
      )}

      {logsQuery.isError && (
        <div role="alert" className="border border-red-200 bg-red-50 p-6 text-red-700">
          Không thể tải nhật ký kiểm toán.{' '}
          <button className="underline font-medium" onClick={() => logsQuery.refetch()}>
            Thử lại
          </button>
        </div>
      )}

      {logsQuery.isSuccess && (
        <>
          {logs.length === 0 ? (
            <div className="border border-border p-12 text-center text-muted-foreground bg-background">
              Không tìm thấy nhật ký kiểm toán nào phù hợp với bộ lọc hiện tại.
            </div>
          ) : (
            <div className="overflow-hidden border border-border bg-background">
              <div className="hidden overflow-x-auto md:block">
                <table className="w-full text-sm">
                  <thead className="border-b border-border bg-secondary/30 text-left text-xs uppercase tracking-widest text-muted-foreground">
                    <tr>
                      <th className="px-5 py-3 font-medium">Thời gian (UTC+7)</th>
                      <th className="px-5 py-3 font-medium">Người thực hiện</th>
                      <th className="px-5 py-3 font-medium">Thao tác</th>
                      <th className="px-5 py-3 font-medium">Nội dung</th>
                      <th className="px-5 py-3 font-medium">Mức độ</th>
                      <th className="px-5 py-3 text-right font-medium">Chi tiết</th>
                    </tr>
                  </thead>
                  <tbody>
                    {logs.map((entry) => (
                      <tr
                        key={entry.id}
                        className="border-b border-border last:border-0 hover:bg-secondary/20 transition-colors"
                      >
                        <td className="px-5 py-3 font-mono text-xs text-muted-foreground whitespace-nowrap">
                          {formatTime(entry.timestamp)}
                        </td>
                        <td className="px-5 py-3" title={entry.userId ?? undefined}>
                          {actorLabel(entry)}
                        </td>
                        <td className="px-5 py-3 whitespace-nowrap">{eventLabel(entry)}</td>
                        <td className="max-w-xs px-5 py-3 break-words">
                          {targetLabel(entry)}
                          {entry.properties?.Source === 'snapshot' && (
                            <span className="mt-1 block text-xs text-muted-foreground">
                              Tổng hợp từ dữ liệu hiện có
                            </span>
                          )}
                        </td>
                        <td className="px-5 py-3">
                          <span
                            className={`inline-block px-2 py-0.5 text-xs font-medium rounded-full ${
                              LEVEL_BADGE[entry.level] ??
                              'bg-secondary text-muted-foreground border border-border'
                            }`}
                          >
                            {LEVEL_FILTERS.find((item) => item.value === entry.level)?.label ?? entry.level}
                          </span>
                        </td>
                        <td className="px-5 py-3 text-right">
                          <button
                            type="button"
                            onClick={() => setSelectedEntry(entry)}
                            className="border border-border px-3 py-1 text-xs uppercase tracking-wider hover:bg-secondary transition-colors"
                          >
                            Xem
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="divide-y divide-border md:hidden">
                {logs.map((entry) => (
                  <article key={entry.id} className="p-4">
                    <div className="flex items-start justify-between gap-3">
                      <p className="font-medium">{eventLabel(entry)}</p>
                      <span
                        className={`shrink-0 border px-2 py-1 text-xs ${LEVEL_BADGE[entry.level] ?? 'border-border text-muted-foreground'}`}
                      >
                        {LEVEL_FILTERS.find((item) => item.value === entry.level)?.label ?? entry.level}
                      </span>
                    </div>
                    <p className="mt-2 break-words text-sm">{targetLabel(entry)}</p>
                    <p className="mt-2 break-words text-sm text-muted-foreground">{actorLabel(entry)}</p>
                    {entry.properties?.Source === 'snapshot' && (
                      <p className="mt-1 text-xs text-muted-foreground">Tổng hợp từ dữ liệu hiện có</p>
                    )}
                    <div className="mt-3 flex items-center justify-between gap-3">
                      <time dateTime={entry.timestamp} className="text-xs text-muted-foreground">
                        {formatTime(entry.timestamp)} (UTC+7)
                      </time>
                      <button
                        type="button"
                        onClick={() => setSelectedEntry(entry)}
                        className="min-h-11 shrink-0 border border-border px-3 text-sm hover:bg-secondary"
                      >
                        Chi tiết
                      </button>
                    </div>
                  </article>
                ))}
              </div>

              {/* Pagination */}
              {totalPages > 1 && (
                <div className="flex items-center justify-between border-t border-border px-6 py-4">
                  <p className="text-xs text-muted-foreground">
                    Trang {page} / {totalPages} (Tổng số {total} bản ghi)
                  </p>
                  <div className="flex items-center gap-2">
                    <button
                      disabled={page <= 1}
                      onClick={() => setPage((p) => Math.max(1, p - 1))}
                      className="flex items-center gap-1 border border-border px-3 py-1.5 text-xs font-medium uppercase tracking-wider disabled:opacity-40"
                    >
                      <ArrowLeft size={14} /> Trước
                    </button>
                    <button
                      disabled={page >= totalPages}
                      onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                      className="flex items-center gap-1 border border-border px-3 py-1.5 text-xs font-medium uppercase tracking-wider disabled:opacity-40"
                    >
                      Sau <ArrowRight size={14} />
                    </button>
                  </div>
                </div>
              )}
            </div>
          )}
        </>
      )}

      {/* Detail Modal Dialog */}
      {selectedEntry && (
        <Modal open title="Chi tiết sự kiện kiểm toán" onClose={() => setSelectedEntry(null)} size="lg">
          <p className="break-all text-xs text-muted-foreground font-mono">ID: {selectedEntry.id}</p>
          <p className="mt-3 text-sm">Nội dung: {targetLabel(selectedEntry)}</p>
          <div className="mt-5 space-y-4 text-sm">
            <div className="grid min-w-0 grid-cols-1 gap-4 sm:grid-cols-2 border border-border p-4 bg-secondary/10">
              <div>
                <span className="text-xs uppercase tracking-widest text-muted-foreground">Sự kiện:</span>
                <p className="font-semibold text-primary">{eventLabel(selectedEntry)}</p>
              </div>
              <div>
                <span className="text-xs uppercase tracking-widest text-muted-foreground">Mức độ:</span>
                <p className="font-medium">{selectedEntry.level}</p>
              </div>
              <div>
                <span className="text-xs uppercase tracking-widest text-muted-foreground">
                  Thời gian (UTC+7):
                </span>
                <p className="font-mono text-xs">{formatTime(selectedEntry.timestamp)}</p>
              </div>
              <div>
                <span className="text-xs uppercase tracking-widest text-muted-foreground">
                  Người thực hiện:
                </span>
                <p className="font-mono text-xs truncate" title={selectedEntry.userId ?? 'Hệ thống'}>
                  {actorLabel(selectedEntry)}
                </p>
              </div>
              {selectedEntry.correlationId && (
                <div className="min-w-0 sm:col-span-2">
                  <span className="text-xs uppercase tracking-widest text-muted-foreground">
                    Correlation ID:
                  </span>
                  <div className="flex items-center gap-2">
                    <p className="break-all font-mono text-xs text-primary">{selectedEntry.correlationId}</p>
                    <button
                      type="button"
                      onClick={() => copyToClipboard(selectedEntry.correlationId!, selectedEntry.id)}
                      aria-label="Sao chép Correlation ID"
                    >
                      {copiedId === selectedEntry.id ? <Check size={14} /> : <Copy size={14} />}
                    </button>
                  </div>
                </div>
              )}
              {selectedEntry.requestPath && (
                <div className="min-w-0 sm:col-span-2">
                  <span className="text-xs uppercase tracking-widest text-muted-foreground">
                    Yêu cầu HTTP:
                  </span>
                  <p className="font-mono text-xs">
                    {selectedEntry.requestMethod ?? 'GET'} {selectedEntry.requestPath}{' '}
                    {selectedEntry.statusCode && `(Mã: ${selectedEntry.statusCode})`}
                  </p>
                </div>
              )}
            </div>

            <div>
              <span className="text-xs uppercase tracking-widest text-muted-foreground">
                Nội dung thông điệp:
              </span>
              <p className="mt-1 border border-border bg-background p-3 font-mono text-xs text-foreground leading-relaxed whitespace-pre-wrap">
                {selectedEntry.message}
              </p>
            </div>

            {selectedEntry.properties && Object.keys(selectedEntry.properties).length > 0 && (
              <div>
                <span className="text-xs uppercase tracking-widest text-muted-foreground">
                  Thuộc tính bổ sung (Properties):
                </span>
                <pre className="mt-1 max-h-48 overflow-y-auto border border-border bg-secondary/30 p-3 font-mono text-xs leading-relaxed">
                  {JSON.stringify(selectedEntry.properties, null, 2)}
                </pre>
              </div>
            )}
          </div>

          <div className="mt-6 flex justify-end">
            <button
              type="button"
              onClick={() => setSelectedEntry(null)}
              className="border border-border px-5 py-2 text-xs uppercase tracking-widest text-foreground hover:bg-secondary transition-colors"
            >
              Đóng
            </button>
          </div>
        </Modal>
      )}
    </div>
  )
}
