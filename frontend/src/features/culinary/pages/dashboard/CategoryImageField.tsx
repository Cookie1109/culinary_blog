'use client'

import { ImagePlus, Loader2, RefreshCw, Trash2 } from 'lucide-react'
import Image from 'next/image'
import { useId, useRef, useState, type DragEvent } from 'react'
import { apiErrorMessage } from '@/lib/api/problem-details'
import { uploadCategoryImage } from '@/lib/api/content-client'
import { publicMediaUrl } from '@/lib/media-url'

const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/avif']
const MAX_BYTES = 5 * 1024 * 1024

export function CategoryImageField({
  value,
  onChange,
  onBusyChange,
  disabled = false,
}: {
  value: string
  onChange: (url: string) => void
  onBusyChange: (busy: boolean) => void
  disabled?: boolean
}) {
  const inputId = useId()
  const inputRef = useRef<HTMLInputElement>(null)
  const [dragging, setDragging] = useState(false)
  const [progress, setProgress] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const uploading = progress !== null
  const inert = disabled || uploading
  const previewable = value.startsWith('/')

  const upload = async (file: File | undefined) => {
    if (!file) return
    if (!ACCEPTED_TYPES.includes(file.type)) {
      setError('Chỉ hỗ trợ ảnh JPEG, PNG, WebP hoặc AVIF.')
      return
    }
    if (file.size > MAX_BYTES) {
      setError('Ảnh không được vượt quá 5 MB.')
      return
    }
    setError(null)
    setProgress(0)
    onBusyChange(true)
    try {
      onChange(await uploadCategoryImage(file, setProgress))
    } catch (failure) {
      setError(apiErrorMessage(failure, 'FILE_UPLOAD_FAILED'))
    } finally {
      setProgress(null)
      onBusyChange(false)
      if (inputRef.current) inputRef.current.value = ''
    }
  }

  const onDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault()
    setDragging(false)
    if (!inert) upload(event.dataTransfer.files[0])
  }

  return (
    <div>
      <label htmlFor={inputId} className="text-xs uppercase tracking-widest text-muted-foreground">
        Ảnh danh mục <span className="normal-case tracking-normal">(không bắt buộc)</span>
      </label>
      <input
        ref={inputRef}
        id={inputId}
        type="file"
        accept={ACCEPTED_TYPES.join(',')}
        disabled={inert}
        onChange={(event) => upload(event.target.files?.[0])}
        className="sr-only"
      />

      {value && !uploading ? (
        <div className="mt-2 flex items-center gap-4 border border-border bg-background p-3">
          <div className="relative h-20 w-28 shrink-0 overflow-hidden border border-border bg-secondary/40">
            {previewable && (
              <Image
                src={publicMediaUrl(value)}
                alt="Ảnh danh mục"
                fill
                unoptimized
                className="object-cover"
              />
            )}
          </div>
          <div className="min-w-0 flex-1 text-sm text-muted-foreground">
            {previewable ? 'Đã chọn ảnh.' : 'Ảnh từ liên kết ngoài.'}
          </div>
          <div className="flex shrink-0 gap-2">
            <button
              type="button"
              disabled={inert}
              onClick={() => inputRef.current?.click()}
              className="inline-flex min-h-11 items-center gap-2 border border-border px-3 text-xs uppercase tracking-widest hover:bg-secondary disabled:opacity-50"
            >
              <RefreshCw size={14} aria-hidden="true" /> Đổi ảnh
            </button>
            <button
              type="button"
              disabled={inert}
              onClick={() => onChange('')}
              aria-label="Xóa ảnh danh mục"
              className="inline-grid h-11 w-11 place-items-center border border-border text-muted-foreground hover:bg-red-50 hover:text-red-600 disabled:opacity-50"
            >
              <Trash2 size={15} aria-hidden="true" />
            </button>
          </div>
        </div>
      ) : (
        <div
          onDragOver={(event) => {
            event.preventDefault()
            if (!inert) setDragging(true)
          }}
          onDragLeave={() => setDragging(false)}
          onDrop={onDrop}
          className={`mt-2 border border-dashed px-4 py-8 text-center transition-colors ${
            dragging ? 'border-primary bg-primary/5' : 'border-border bg-background'
          }`}
        >
          {uploading ? (
            <div role="status" className="mx-auto max-w-xs">
              <Loader2 size={22} className="mx-auto mb-2 animate-spin text-primary" aria-hidden="true" />
              <p className="text-sm">Đang tải ảnh lên… {progress}%</p>
              <div className="mt-3 h-1.5 bg-secondary" aria-hidden="true">
                <div className="h-full bg-primary transition-[width]" style={{ width: `${progress}%` }} />
              </div>
            </div>
          ) : (
            <>
              <ImagePlus
                size={24}
                strokeWidth={1.5}
                className="mx-auto mb-2 text-muted-foreground"
                aria-hidden="true"
              />
              <p className="text-sm">
                Kéo thả ảnh vào đây hoặc{' '}
                <button
                  type="button"
                  disabled={inert}
                  onClick={() => inputRef.current?.click()}
                  className="text-primary underline underline-offset-2 disabled:opacity-50"
                >
                  chọn từ máy
                </button>
              </p>
              <p className="mt-1 text-xs text-muted-foreground">JPEG, PNG, WebP hoặc AVIF · tối đa 5 MB</p>
            </>
          )}
        </div>
      )}

      {error && (
        <p role="alert" className="mt-2 text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  )
}
