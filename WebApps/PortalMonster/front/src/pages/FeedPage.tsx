import { useCallback } from 'react'
import { Typography, CircularProgress, Box } from '@mui/material'
import { getFeed, type PhotoDto } from '../api/photos'
import PhotoCard from '../components/PhotoCard'
import { useInfiniteScroll } from '../hooks/useInfiniteScroll'

export default function FeedPage() {
  const fetcher = useCallback(async (cursor?: number) => {
    const res = await getFeed(cursor, 10)
    return res.data
  }, [])

  const getNextCursor = useCallback((items: PhotoDto[]) =>
    items.length > 0 ? items[items.length - 1].id : undefined, [])

  const { items, loading, hasMore, sentinelRef } = useInfiniteScroll({ fetcher, getNextCursor })

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: '2px' }}>
      {items.map((photo) => <PhotoCard key={photo.id} photo={photo} />)}
      {items.length === 0 && !loading && (
        <Typography color="text.secondary" textAlign="center" mt={6} fontSize={14}>
          Aucune photo pour le moment. Ajoutez des amis pour voir leur fil !
        </Typography>
      )}
      <Box ref={sentinelRef} sx={{ py: 3, textAlign: 'center' }}>
        {loading && <CircularProgress size={24} />}
        {!hasMore && items.length > 0 && (
          <Typography variant="body2" color="text.secondary">C'est tout !</Typography>
        )}
      </Box>
    </Box>
  )
}
