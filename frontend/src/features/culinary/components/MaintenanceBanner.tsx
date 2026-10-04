'use client'

import { useEffect, useState } from 'react'
import { publicEnvironment } from '@/lib/env'

type MaintenanceWindow = {
  enabled: boolean
  message: string | null
  startsAtUtc: string | null
  endsAtUtc: string | null
}

type MaintenanceEnvelope = {
  data: MaintenanceWindow
}

export function MaintenanceBanner() {
  const [maintenance, setMaintenance] = useState<MaintenanceWindow | null>(null)

  useEffect(() => {
    let active = true

    const load = async () => {
      try {
        const response = await fetch(`${publicEnvironment.NEXT_PUBLIC_API_BASE_URL}/operations/maintenance`, {
          cache: 'no-store',
        })
        if (!response.ok) return

        const envelope = (await response.json()) as MaintenanceEnvelope
        if (active) setMaintenance(envelope.data)
      } catch {
        // Availability alerts cover an unreachable API; the banner must not break page rendering.
      }
    }

    void load()
    const interval = window.setInterval(load, 60_000)
    return () => {
      active = false
      window.clearInterval(interval)
    }
  }, [])

  if (!maintenance?.enabled || !maintenance.message || !maintenance.startsAtUtc) return null

  const startsAt = new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'full',
    timeStyle: 'short',
  }).format(new Date(maintenance.startsAtUtc))

  return (
    <aside
      role="status"
      aria-live="polite"
      className="border-b border-amber-300 bg-amber-50 px-4 py-3 text-center text-sm text-amber-950"
    >
      <strong>Bảo trì theo kế hoạch:</strong> {maintenance.message} Bắt đầu {startsAt}.
    </aside>
  )
}
