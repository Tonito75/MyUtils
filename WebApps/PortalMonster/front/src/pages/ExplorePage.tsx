import { useCallback, useEffect, useRef, useState } from 'react'
import {
  Box, Typography, CircularProgress, Chip, Stack,
  Drawer, IconButton, Button, Badge,
} from '@mui/material'
import FilterListIcon from '@mui/icons-material/FilterList'
import CloseIcon from '@mui/icons-material/Close'
import { getExplore, type PhotoDto } from '../api/photos'
import { getMonsters, type MonsterOption } from '../api/monsters'
import PhotoGrid from '../components/PhotoGrid'

export default function ExplorePage() {
  const [photos, setPhotos] = useState<PhotoDto[]>([])
  const [monsters, setMonsters] = useState<MonsterOption[]>([])
  const [selectedMonsters, setSelectedMonsters] = useState<number[]>([])
  const [pendingMonsters, setPendingMonsters] = useState<number[]>([])
  const [loading, setLoading] = useState(false)
  const [hasMore, setHasMore] = useState(true)
  const [filterOpen, setFilterOpen] = useState(false)
  const offset = useRef(0)
  const seed = useRef(Math.floor(Math.random() * 999983) + 1)
  const sentinel = useRef<HTMLDivElement | null>(null)
  const loadingRef = useRef(false)
  const hasMoreRef = useRef(true)

  useEffect(() => {
    getMonsters().then((res) => setMonsters(res.data))
  }, [])

  const loadMore = useCallback(async () => {
    if (loadingRef.current || !hasMoreRef.current) return
    loadingRef.current = true
    setLoading(true)
    try {
      const res = await getExplore(
        offset.current, seed.current, 20,
        selectedMonsters.length > 0 ? selectedMonsters : undefined
      )
      if (res.data.length === 0) {
        hasMoreRef.current = false
        setHasMore(false)
      } else {
        offset.current += res.data.length
        setPhotos((prev) => [...prev, ...res.data])
      }
    } finally {
      loadingRef.current = false
      setLoading(false)
    }
  }, [selectedMonsters])

  useEffect(() => {
    setPhotos([])
    setHasMore(true)
    hasMoreRef.current = true
    offset.current = 0
    seed.current = Math.floor(Math.random() * 999983) + 1
  }, [selectedMonsters])

  useEffect(() => {
    if (hasMore && photos.length === 0) loadMore()
  }, [hasMore, photos.length, loadMore])

  useEffect(() => {
    const el = sentinel.current
    if (!el) return
    const obs = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting) loadMore()
    })
    obs.observe(el)
    return () => obs.disconnect()
  }, [loadMore])

  const togglePending = (id: number) =>
    setPendingMonsters((prev) =>
      prev.includes(id) ? prev.filter((m) => m !== id) : [...prev, id])

  const openFilter = () => {
    setPendingMonsters(selectedMonsters)
    setFilterOpen(true)
  }

  const applyFilter = () => {
    setSelectedMonsters(pendingMonsters)
    setFilterOpen(false)
  }

  const resetFilter = () => {
    setPendingMonsters([])
  }

  return (
    <Box>
      {/* Header row */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2, px: { xs: 2, sm: 0 } }}>
        <Typography variant="h6" fontWeight={700} sx={{ flexGrow: 1 }}>
          Explorer
        </Typography>
        <IconButton
          onClick={openFilter}
          sx={{
            color: selectedMonsters.length > 0 ? 'primary.main' : 'text.secondary',
            border: '1px solid',
            borderColor: selectedMonsters.length > 0 ? 'primary.main' : 'divider',
            borderRadius: 2,
            px: 1.5,
            gap: 0.5,
            fontSize: 14,
            fontWeight: 600,
          }}
        >
          <Badge badgeContent={selectedMonsters.length || null} color="primary">
            <FilterListIcon sx={{ fontSize: 20 }} />
          </Badge>
          <Box component="span" sx={{ ml: 0.5, fontSize: 13, display: { xs: 'none', sm: 'inline' } }}>
            Filtres
          </Box>
        </IconButton>
      </Box>

      {/* Active filter chips (summary) */}
      {selectedMonsters.length > 0 && (
        <Stack direction="row" flexWrap="wrap" gap={0.75} mb={2} px={{ xs: 2, sm: 0 }}>
          {monsters.filter((m) => selectedMonsters.includes(m.id)).map((m) => (
            <Chip
              key={m.id}
              label={`${m.emoji} ${m.name}`}
              size="small"
              color="primary"
              onDelete={() => setSelectedMonsters((prev) => prev.filter((id) => id !== m.id))}
            />
          ))}
        </Stack>
      )}

      <PhotoGrid photos={photos} />

      <Box ref={sentinel} sx={{ py: 2, textAlign: 'center' }}>
        {loading && <CircularProgress size={24} sx={{ color: 'primary.main' }} />}
        {!hasMore && photos.length > 0 && (
          <Typography variant="body2" color="text.secondary">C'est tout !</Typography>
        )}
      </Box>

      {/* Bottom sheet filter drawer */}
      <Drawer
        anchor="bottom"
        open={filterOpen}
        onClose={() => setFilterOpen(false)}
        PaperProps={{
          sx: {
            borderTopLeftRadius: 20,
            borderTopRightRadius: 20,
            maxHeight: '60vh',
            display: 'flex',
            flexDirection: 'column',
          },
        }}
      >
        {/* Sheet header */}
        <Box sx={{ display: 'flex', alignItems: 'center', px: 2.5, pt: 2, pb: 1, flexShrink: 0 }}>
          <Typography variant="subtitle1" fontWeight={700} sx={{ flexGrow: 1 }}>
            Filtrer par monster
          </Typography>
          <IconButton size="small" onClick={() => setFilterOpen(false)} sx={{ color: 'text.secondary' }}>
            <CloseIcon />
          </IconButton>
        </Box>

        {/* Drag handle visual */}
        <Box sx={{ width: 40, height: 4, borderRadius: 2, bgcolor: '#2a2a4a', mx: 'auto', mb: 1, flexShrink: 0 }} />

        {/* Monster chips */}
        <Box sx={{ overflowY: 'auto', px: 2.5, pb: 1, flexGrow: 1 }}>
          <Stack direction="row" flexWrap="wrap" gap={1}>
            {monsters.map((m) => (
              <Chip
                key={m.id}
                label={`${m.emoji} ${m.name}`}
                onClick={() => togglePending(m.id)}
                color={pendingMonsters.includes(m.id) ? 'primary' : 'default'}
                sx={{ cursor: 'pointer' }}
              />
            ))}
          </Stack>
        </Box>

        {/* Actions */}
        <Box
          sx={{
            display: 'flex',
            gap: 1.5,
            px: 2.5,
            py: 2,
            borderTop: '1px solid',
            borderColor: 'divider',
            flexShrink: 0,
          }}
        >
          <Button
            variant="text"
            onClick={resetFilter}
            disabled={pendingMonsters.length === 0}
            sx={{ color: 'text.secondary', fontWeight: 600 }}
          >
            Réinitialiser
          </Button>
          <Button
            variant="contained"
            onClick={applyFilter}
            fullWidth
            sx={{ borderRadius: '20px', fontWeight: 700 }}
          >
            Appliquer
            {pendingMonsters.length > 0 && ` (${pendingMonsters.length})`}
          </Button>
        </Box>
      </Drawer>
    </Box>
  )
}
