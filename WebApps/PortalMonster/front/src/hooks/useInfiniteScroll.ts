import { useRef, useCallback, useState } from 'react'

interface UseInfiniteScrollOptions<T, C> {
  fetcher: (cursor: C | undefined) => Promise<T[]>
  getNextCursor: (items: T[]) => C | undefined
}

export function useInfiniteScroll<T, C = T>({ fetcher, getNextCursor }: UseInfiniteScrollOptions<T, C>) {
  const [items, setItems] = useState<T[]>([])
  const [loading, setLoading] = useState(false)
  const [hasMore, setHasMore] = useState(true)
  const cursor = useRef<C | undefined>(undefined)
  const observer = useRef<IntersectionObserver | null>(null)

  const loadMore = useCallback(async () => {
    if (loading || !hasMore) return
    setLoading(true)
    try {
      const newItems = await fetcher(cursor.current)
      if (newItems.length === 0) {
        setHasMore(false)
      } else {
        cursor.current = getNextCursor(newItems)
        setItems((prev) => [...prev, ...newItems])
        if (!cursor.current) setHasMore(false)
      }
    } finally {
      setLoading(false)
    }
  }, [fetcher, getNextCursor, loading, hasMore])

  const sentinelRef = useCallback((node: HTMLDivElement | null) => {
    if (observer.current) observer.current.disconnect()
    if (!node) return
    observer.current = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting) loadMore()
    })
    observer.current.observe(node)
  }, [loadMore])

  return { items, loading, hasMore, sentinelRef, reset: () => {
    setItems([]); setHasMore(true); cursor.current = undefined
  }}
}
