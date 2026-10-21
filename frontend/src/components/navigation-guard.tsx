'use client'

import { createContext, useContext, useEffect, useMemo, useRef, type ReactNode } from 'react'

type LeaveHandler = (target: string) => void
type GuardRegistration = { onLeave: LeaveHandler }
const GuardContext = createContext<{
  register: (guard: GuardRegistration | null) => void
} | null>(null)

export function NavigationGuardProvider({ children }: { children: ReactNode }) {
  const guardRef = useRef<GuardRegistration | null>(null)
  const context = useMemo(
    () => ({
      register: (guard: GuardRegistration | null) => {
        guardRef.current = guard
      },
    }),
    [],
  )

  useEffect(() => {
    const originalPush = window.history.pushState
    const originalReplace = window.history.replaceState
    let index = window.history.state?.culinaryIndex ?? 0
    let restoring = false
    originalReplace.call(window.history, { ...window.history.state, culinaryIndex: index }, '')

    const push: History['pushState'] = function (data, unused, url) {
      originalPush.call(window.history, { ...data, culinaryIndex: index + 1 }, unused, url)
      index += 1
    }
    const replace: History['replaceState'] = function (data, unused, url) {
      originalReplace.call(window.history, { ...data, culinaryIndex: index }, unused, url)
    }
    window.history.pushState = push
    window.history.replaceState = replace

    const handleClick = (event: MouseEvent) => {
      if (
        !guardRef.current ||
        event.defaultPrevented ||
        event.button !== 0 ||
        event.ctrlKey ||
        event.metaKey ||
        event.shiftKey ||
        event.altKey
      )
        return
      const link = event.target instanceof Element ? event.target.closest<HTMLAnchorElement>('a[href]') : null
      if (!link || link.hasAttribute('download') || (link.target && link.target !== '_self')) return
      const target = new URL(link.href, window.location.href)
      if (
        target.origin !== window.location.origin ||
        (target.pathname === window.location.pathname && target.search === window.location.search)
      )
        return
      event.preventDefault()
      event.stopPropagation()
      guardRef.current.onLeave(`${target.pathname}${target.search}${target.hash}`)
    }
    const handlePopState = (event: PopStateEvent) => {
      const previous = index
      index = event.state?.culinaryIndex ?? previous - 1
      if (restoring) {
        restoring = false
        event.stopImmediatePropagation()
        return
      }
      if (!guardRef.current) return
      event.stopImmediatePropagation()
      const target = `${window.location.pathname}${window.location.search}${window.location.hash}`
      restoring = true
      window.history.go(previous - index)
      guardRef.current.onLeave(target)
    }
    const handleBeforeUnload = (event: BeforeUnloadEvent) => {
      if (!guardRef.current) return
      event.preventDefault()
      event.returnValue = ''
    }
    document.addEventListener('click', handleClick, true)
    window.addEventListener('popstate', handlePopState, true)
    window.addEventListener('beforeunload', handleBeforeUnload)
    return () => {
      if (window.history.pushState === push) window.history.pushState = originalPush
      if (window.history.replaceState === replace) window.history.replaceState = originalReplace
      document.removeEventListener('click', handleClick, true)
      window.removeEventListener('popstate', handlePopState, true)
      window.removeEventListener('beforeunload', handleBeforeUnload)
    }
  }, [])

  return <GuardContext.Provider value={context}>{children}</GuardContext.Provider>
}

export function useUnsavedChanges(dirty: boolean, onLeave: LeaveHandler) {
  const context = useContext(GuardContext)
  if (!context) throw new Error('NavigationGuardProvider is required')
  useEffect(() => {
    context.register(dirty ? { onLeave } : null)
    return () => context.register(null)
  }, [context, dirty, onLeave])
  return () => context.register(null)
}
