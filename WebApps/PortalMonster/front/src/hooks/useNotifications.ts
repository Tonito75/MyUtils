import { useEffect, useState, useCallback } from 'react'
import { getFriendRequests } from '../api/friends'

export function useNotifications() {
  const [unreadCount, setUnreadCount] = useState(0)

  const fetchCount = useCallback(async () => {
    try {
      const res = await getFriendRequests()
      setUnreadCount(res.data.length)
    } catch { /* ignore */ }
  }, [])

  useEffect(() => {
    fetchCount()
    const interval = setInterval(fetchCount, 30_000)
    return () => clearInterval(interval)
  }, [fetchCount])

  const markRead = useCallback(() => setUnreadCount(0), [])

  return { unreadCount, markRead, refresh: fetchCount }
}
