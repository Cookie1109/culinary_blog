'use client'

import { useEffect } from 'react'
import { onCLS, onINP, onLCP, type Metric } from 'web-vitals'
import { webVitalRoute } from '@/lib/web-vitals'

export function WebVitals() {
  useEffect(() => {
    // Web Vitals describe a document visit, including its later client navigations.
    const entryRoute = webVitalRoute(window.location.pathname)
    const device = /Mobi|Android/i.test(navigator.userAgent) ? 'mobile' : 'desktop'
    const report = (metric: Metric) => {
      const body = JSON.stringify({
        name: metric.name,
        id: metric.id,
        value: metric.value,
        route: entryRoute,
        device,
        navigationType: metric.navigationType,
      })
      if (navigator.sendBeacon) navigator.sendBeacon('/api/web-vitals', body)
    }
    onCLS(report)
    onINP(report)
    onLCP(report)
  }, [])
  return null
}
