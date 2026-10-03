'use client'

import { useEffect, useState } from 'react'

export function NetworkStatusBanner() {
  const [isOnline, setIsOnline] = useState(true)

  useEffect(() => {
    const updateStatus = () => setIsOnline(navigator.onLine)
    updateStatus()
    window.addEventListener('online', updateStatus)
    window.addEventListener('offline', updateStatus)
    return () => {
      window.removeEventListener('online', updateStatus)
      window.removeEventListener('offline', updateStatus)
    }
  }, [])

  if (isOnline) return null

  return (
    <div
      role="status"
      aria-live="polite"
      className="bg-foreground px-4 py-2 text-center text-sm text-background"
    >
      Bạn đang ngoại tuyến. Nội dung mới sẽ tải lại khi kết nối mạng được khôi phục.
    </div>
  )
}
