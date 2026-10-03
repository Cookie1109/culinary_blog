'use client'

import { useQuery } from '@tanstack/react-query'
import {
  ArrowLeft,
  ArrowRight,
  Check,
  Copy,
  RefreshCw,
  Search,
  ShieldAlert,
  ShieldCheck,
  X,
} from 'lucide-react'
import { useEffect, useState } from 'react'
import { useAuth } from '@/features/culinary/contexts/AuthContext'
import {
  listAuditLogs,
  type AuditLogEntry,
} from '@/lib/api/content-client'

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
  })

  // Keyboard escape handler for detail modal
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && selectedEntry) {
        setSelectedEntry(null)
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [selectedEntry])

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
    <div className="max-w-6xl">
      {/* Header */}
      <div className="mb-6 flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="font-serif text-3xl text-foreground lg:text-4xl">Nhật ký kiểm toán hệ thống</h1>
          <p className="mt-1 text-muted-foreground">
            Theo dõi toàn bộ các sự kiện thay đổi dữ liệu, hoạt động xuất bản và truy vết tác nhân theo SRS FR-RCP-CORE.
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
      <div className="mb-8 border border-border bg-secondary/30 p-5">
        <div className="flex items-start gap-3">
          <ShieldAlert className="text-primary shrink-0 mt-0.5" size={20} />
          <div className="flex-1 text-sm space-y-2">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <span className="font-semibold text-foreground">
                Chính sách an toàn Telemetry &amp; Hạ tầng vận hành (P7-11)
              </span>
              <div className="flex flex-wrap gap-2 text-xs">
                <span className="inline-flex items-center gap-1 rounded bg-background px-2.5 py-1 border border-border font-mono">
                  Seq Telemetry: {telemetry?.seqConfigured ? 'Đang hoạt động' : 'Chưa bật URL'}
                </span>
                <span className="inline-flex items-center gap-1 rounded bg-background px-2.5 py-1 border border-border font-mono">
                  OpenTelemetry (OTLP): {telemetry?.otlpConfigured ? 'Đang hoạt động' : 'Chưa bật URL'}
                </span>
              </div>
            </div>
            <p className="text-xs text-muted-foreground leading-relaxed">
              Theo quy định vận hành bảo mật: Raw Seq và OTLP telemetry chỉ được truy cập thông qua kênh bảo mật nội bộ
              (VPN hoặc operational authentication/network control). Giao diện quản trị này hiển thị dữ liệu Application Audit Log
              đã được phân quyền an toàn cho Admin.
            </p>
          </div>
        </div>
      </div>

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
            placeholder="Tìm theo Correlation ID, sự kiện, người dùng, đường dẫn…"
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
        <div role="status" className="border border-border p-12 text-center text-muted-foreground bg-background">
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
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="border-b border-border bg-secondary/30 text-left text-xs uppercase tracking-widest text-muted-foreground">
                    <tr>
                      <th className="px-5 py-3 font-medium">Thời gian (UTC)</th>
                      <th className="px-5 py-3 font-medium">Mức độ</th>
                      <th className="px-5 py-3 font-medium">Sự kiện</th>
                      <th className="px-5 py-3 font-medium">Thông điệp</th>
                      <th className="px-5 py-3 font-medium">Mã tương quan (Correlation)</th>
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
                          {new Date(entry.timestamp).toISOString().replace('T', ' ').substring(0, 19)}
                        </td>
                        <td className="px-5 py-3">
                          <span
                            className={`inline-block px-2 py-0.5 text-xs font-medium rounded-full ${
                              LEVEL_BADGE[entry.level] ?? 'bg-secondary text-muted-foreground border border-border'
                            }`}
                          >
                            {entry.level}
                          </span>
                        </td>
                        <td className="px-5 py-3">
                          <span className="font-mono text-xs font-semibold text-primary">
                            {entry.eventName}
                          </span>
                        </td>
                        <td className="px-5 py-3 max-w-xs truncate text-foreground font-medium" title={entry.message}>
                          {entry.message}
                        </td>
                        <td className="px-5 py-3 font-mono text-xs text-muted-foreground">
                          {entry.correlationId ? (
                            <div className="flex items-center gap-1.5">
                              <span className="truncate max-w-[120px]" title={entry.correlationId}>
                                {entry.correlationId}
                              </span>
                              <button
                                type="button"
                                onClick={() => copyToClipboard(entry.correlationId!, entry.id)}
                                aria-label="Sao chép Correlation ID"
                                className="text-muted-foreground hover:text-foreground"
                                title="Sao chép mã tương quan"
                              >
                                {copiedId === entry.id ? (
                                  <Check size={13} className="text-green-600" />
                                ) : (
                                  <Copy size={13} />
                                )}
                              </button>
                            </div>
                          ) : (
                            <span className="text-muted-foreground/60">—</span>
                          )}
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
        <div
          role="presentation"
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
          onClick={() => setSelectedEntry(null)}
        >
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="audit-detail-title"
            className="w-full max-w-2xl max-h-[85vh] overflow-y-auto border border-border bg-background p-6 shadow-xl"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-start justify-between gap-4 border-b border-border pb-4">
              <div className="flex items-center gap-3">
                <div className="grid h-10 w-10 place-items-center rounded-full bg-primary/10 text-primary">
                  <ShieldCheck size={20} />
                </div>
                <div>
                  <h3 id="audit-detail-title" className="font-serif text-lg font-medium text-foreground">
                    Chi tiết sự kiện kiểm toán
                  </h3>
                  <p className="text-xs text-muted-foreground font-mono">
                    ID: {selectedEntry.id}
                  </p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setSelectedEntry(null)}
                aria-label="Đóng chi tiết"
                className="text-muted-foreground hover:text-foreground"
              >
                <X size={18} />
              </button>
            </div>

            <div className="mt-5 space-y-4 text-sm">
              <div className="grid grid-cols-2 gap-4 border border-border p-4 bg-secondary/10">
                <div>
                  <span className="text-xs uppercase tracking-widest text-muted-foreground">Sự kiện:</span>
                  <p className="font-mono font-semibold text-primary">{selectedEntry.eventName}</p>
                </div>
                <div>
                  <span className="text-xs uppercase tracking-widest text-muted-foreground">Mức độ:</span>
                  <p className="font-medium">{selectedEntry.level}</p>
                </div>
                <div>
                  <span className="text-xs uppercase tracking-widest text-muted-foreground">Thời gian (UTC):</span>
                  <p className="font-mono text-xs">{selectedEntry.timestamp}</p>
                </div>
                <div>
                  <span className="text-xs uppercase tracking-widest text-muted-foreground">Người thực hiện (User ID):</span>
                  <p className="font-mono text-xs truncate" title={selectedEntry.userId ?? 'Hệ thống'}>
                    {selectedEntry.userId ?? 'Hệ thống / Anonymous'}
                  </p>
                </div>
                {selectedEntry.correlationId && (
                  <div className="col-span-2">
                    <span className="text-xs uppercase tracking-widest text-muted-foreground">Correlation ID:</span>
                    <p className="font-mono text-xs text-primary">{selectedEntry.correlationId}</p>
                  </div>
                )}
                {selectedEntry.requestPath && (
                  <div className="col-span-2">
                    <span className="text-xs uppercase tracking-widest text-muted-foreground">Yêu cầu HTTP:</span>
                    <p className="font-mono text-xs">
                      {selectedEntry.requestMethod ?? 'GET'} {selectedEntry.requestPath}{' '}
                      {selectedEntry.statusCode && `(Mã: ${selectedEntry.statusCode})`}
                    </p>
                  </div>
                )}
              </div>

              <div>
                <span className="text-xs uppercase tracking-widest text-muted-foreground">Nội dung thông điệp:</span>
                <p className="mt-1 border border-border bg-background p-3 font-mono text-xs text-foreground leading-relaxed whitespace-pre-wrap">
                  {selectedEntry.message}
                </p>
              </div>

              {selectedEntry.properties && Object.keys(selectedEntry.properties).length > 0 && (
                <div>
                  <span className="text-xs uppercase tracking-widest text-muted-foreground">Thuộc tính bổ sung (Properties):</span>
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
          </div>
        </div>
      )}
    </div>
  )
}
