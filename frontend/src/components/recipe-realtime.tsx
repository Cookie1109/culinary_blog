'use client'

import { useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { usePathname, useRouter } from 'next/navigation'

const listKeys = new Set(['my-recipes', 'admin-recipes', 'categories', 'admin-categories'])

export function RecipeRealtime() {
  const queryClient = useQueryClient()
  const router = useRouter()
  const pathname = usePathname()

  useEffect(() => {
    const events = new EventSource('/api/recipe-events')
    let closed = false
    let timer: ReturnType<typeof setTimeout> | undefined
    const refresh = () => {
      clearTimeout(timer)
      timer = setTimeout(async () => {
        await queryClient.invalidateQueries({ predicate: (query) => listKeys.has(String(query.queryKey[0])) })
        if (closed) return
        const path = window.location.pathname
        // Editor drafts remain under their existing dirty-state/concurrency handling.
        if (
          !/^\/(profile|login|register|auth|forgot-password|reset-password)(\/|$)/.test(path) &&
          !/^\/dashboard\/recipes\/(new|[^/]+\/edit)$/.test(path)
        ) {
          router.refresh()
        }
      }, 200)
    }
    events.addEventListener('connected', refresh)
    events.addEventListener('changed', refresh)
    return () => {
      closed = true
      clearTimeout(timer)
      events.close()
    }
  }, [queryClient, router, pathname])

  return null
}
