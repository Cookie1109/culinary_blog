'use client'

import NextLink from 'next/link'
import {
  useParams as useNextParams,
  usePathname,
  useRouter,
  useSearchParams as useNextSearchParams,
} from 'next/navigation'
import { createContext, useCallback, useContext, useMemo, type ComponentProps, type ReactNode } from 'react'

type LinkProps = Omit<ComponentProps<typeof NextLink>, 'href'> & {
  to: ComponentProps<typeof NextLink>['href']
}

export function Link({ to, ...props }: LinkProps) {
  return <NextLink href={to} {...props} />
}

type NavLinkState = { isActive: boolean }
type NavLinkProps = Omit<LinkProps, 'className'> & {
  end?: boolean
  className?: string | ((state: NavLinkState) => string)
}

export function NavLink({ to, end = false, className, ...props }: NavLinkProps) {
  const pathname = usePathname()
  const searchParams = useNextSearchParams()
  const targetUrl = typeof to === 'string' ? to : (to.pathname?.toString() ?? '/')
  const [target, query = ''] = targetUrl.split('?')
  const queryMatches = [...new URLSearchParams(query)].every(
    ([key, value]) => searchParams.get(key) === value,
  )
  const isActive =
    queryMatches && (end ? pathname === target : pathname === target || pathname.startsWith(`${target}/`))
  const resolvedClassName = typeof className === 'function' ? className({ isActive }) : className

  return <NextLink href={to} className={resolvedClassName} {...props} />
}

export function useNavigate() {
  const router = useRouter()
  return useCallback((to: string) => router.push(to), [router])
}

type SearchParamsInitializer = URLSearchParams | Record<string, string>
type SearchParamsSetter = (
  next: SearchParamsInitializer | ((current: URLSearchParams) => SearchParamsInitializer),
  options?: { replace?: boolean; scroll?: boolean },
) => void

export function useSearchParams(): [URLSearchParams, SearchParamsSetter] {
  const current = useNextSearchParams()
  const pathname = usePathname()
  const router = useRouter()
  const snapshot = useMemo(() => new URLSearchParams(current.toString()), [current])

  const setSearchParams = useCallback<SearchParamsSetter>(
    (nextValue, options) => {
      const currentValue = new URLSearchParams(current.toString())
      const resolved = typeof nextValue === 'function' ? nextValue(currentValue) : nextValue
      const next = resolved instanceof URLSearchParams ? resolved : new URLSearchParams(resolved)
      const query = next.toString()
      const href = query ? `${pathname}?${query}` : pathname
      if (options?.replace === false) router.push(href, { scroll: options.scroll ?? true })
      else router.replace(href, { scroll: options?.scroll ?? true })
    },
    [current, pathname, router],
  )

  return [snapshot, setSearchParams]
}

export function useParams<T extends Record<string, string | string[]> = Record<string, string>>() {
  return useNextParams<T>()
}

const OutletContext = createContext<ReactNode>(null)

export function LegacyOutletProvider({ children, outlet }: { children: ReactNode; outlet: ReactNode }) {
  return <OutletContext.Provider value={outlet}>{children}</OutletContext.Provider>
}

export function Outlet() {
  return useContext(OutletContext)
}
