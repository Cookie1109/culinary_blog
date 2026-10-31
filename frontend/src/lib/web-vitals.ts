export const vitalNames = ['LCP', 'CLS', 'INP'] as const
export const vitalRoutes = [
  '/',
  '/recipes',
  '/recipes/[slug]',
  '/categories',
  '/categories/[slug]',
  '/search',
  '/login',
  '/register',
  '/auth/login',
  '/auth/register',
  '/forgot-password',
  '/reset-password',
  '/profile',
  '/dashboard',
  '/dashboard/system',
  '/dashboard/categories',
  '/dashboard/audit',
  '/dashboard/recipes',
  '/dashboard/recipes/new',
  '/dashboard/recipes/[id]/edit',
  '/dashboard/recipes/[id]/preview',
  'other',
] as const

export type WebVitalSample = {
  name: (typeof vitalNames)[number]
  id: string
  value: number
  route: (typeof vitalRoutes)[number]
  device: 'mobile' | 'desktop'
  navigationType: string
}

export function webVitalRoute(pathname: string): WebVitalSample['route'] {
  // Match browser URLs only; server rewrite targets and unknown paths stay masked.
  if (
    /^\/(?:(?:auth\/)?(?:login|register)|(?:forgot|reset)-password|profile|recipes|categories|search|dashboard(?:\/(?:system|categories|audit|recipes(?:\/new)?))?)?$/.test(
      pathname,
    )
  )
    return pathname as WebVitalSample['route']
  const publicDetail = /^\/(recipes|categories)\/[^/]+$/.exec(pathname)
  if (publicDetail) return `/${publicDetail[1]}/[slug]` as WebVitalSample['route']
  if (/^\/dashboard\/recipes\/[^/]+\/(edit|preview)$/.test(pathname))
    return pathname.replace(/\/[^/]+\/(edit|preview)$/, '/[id]/$1') as WebVitalSample['route']
  return 'other'
}

export function parseWebVital(value: unknown): WebVitalSample | null {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return null
  const data = value as Record<string, unknown>
  if (
    Object.keys(data).some(
      (key) => !['name', 'id', 'value', 'route', 'device', 'navigationType'].includes(key),
    )
  )
    return null
  if (
    !(vitalNames as readonly unknown[]).includes(data.name) ||
    typeof data.id !== 'string' ||
    !/^[a-zA-Z0-9._-]{1,128}$/.test(data.id) ||
    typeof data.value !== 'number' ||
    !Number.isFinite(data.value) ||
    data.value < 0 ||
    !(vitalRoutes as readonly unknown[]).includes(data.route) ||
    !['mobile', 'desktop'].includes(data.device as string) ||
    !['navigate', 'reload', 'back-forward', 'back-forward-cache', 'prerender', 'restore'].includes(
      data.navigationType as string,
    )
  )
    return null
  return data as WebVitalSample
}
