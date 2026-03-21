import { useCallback, useEffect, useRef, useState } from 'react'
import { Box, Typography, CircularProgress, Chip, Stack } from '@mui/material'
import { getExplore, type PhotoDto } from '../api/photos'
import { getMonsters, type MonsterOption } from '../api/monsters'
import PhotoGrid from '../components/PhotoGrid'

export default function ExplorePage() {
  const [photos, setPhotos] = useState<PhotoDto[]>([])
  const [monsters, setMonsters] = useState<MonsterOption[]>([])
  const [selectedMonsters, setSelectedMonsters] = useState<number[]>([])
  const [loading, setLoading] = useState(false)
  const [hasMore, setHasMore] = useState(true)
  const offset = useRef(0)
  const seed = useRef(Math.floor(Math.random() * 999983) + 1)
  const sentinel = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    getMonsters().then((res) => setMonsters(res.data))
  }, [])

  const loadMore = useCallback(async () => {
    if (loading || !hasMore) return
    setLoading(true)
    try {
      const res = await getExplore(offset.current, seed.current, 20,
        selectedMonsters.length > 0 ? selectedMonsters : undefined)
      if (res.data.length === 0) {
        setHasMore(false)
      } else {
        offset.current += res.data.length
        setPhotos((prev) => [...prev, ...res.data])
      }
    } finally {
      setLoading(false)
    }
  }, [loading, hasMore, selectedMonsters])

  // Reset on filter change
  useEffect(() => {
    setPhotos([])
    setHasMore(true)
    offset.current = 0
    seed.current = Math.floor(Math.random() * 999983) + 1
  }, [selectedMonsters])

  // Trigger first load when hasMore resets
  useEffect(() => {
    if (hasMore && photos.length === 0) loadMore()
  }, [hasMore, photos.length])

  // IntersectionObserver
  useEffect(() => {
    const el = sentinel.current
    if (!el) return
    const obs = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting) loadMore()
    })
    obs.observe(el)
    return () => obs.disconnect()
  }, [loadMore])

  const toggleMonster = (id: number) =>
    setSelectedMonsters((prev) =>
      prev.includes(id) ? prev.filter((m) => m !== id) : [...prev, id])

  return (
    <Box>
      <Typography variant="h6" mb={2} fontWeight={700}>Explorer</Typography>
      <Stack direction="row" flexWrap="wrap" gap={1} mb={2}>
        {monsters.map((m) => (
          <Chip key={m.id} label={`${m.emoji} ${m.name}`} size="small"
            color={selectedMonsters.includes(m.id) ? 'primary' : 'default'}
            onClick={() => toggleMonster(m.id)} />
        ))}
      </Stack>
      <PhotoGrid photos={photos} />
      <Box ref={sentinel} sx={{ py: 2, textAlign: 'center' }}>
        {loading && <CircularProgress size={24} />}
        {!hasMore && photos.length > 0 && (
          <Typography variant="body2" color="text.secondary">C'est tout !</Typography>
        )}
      </Box>
    </Box>
  )
}
