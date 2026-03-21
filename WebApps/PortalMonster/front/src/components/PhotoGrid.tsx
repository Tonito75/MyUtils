import { Box, Dialog, IconButton, Typography, Avatar } from '@mui/material'
import { useState } from 'react'
import CloseIcon from '@mui/icons-material/Close'
import FavoriteIcon from '@mui/icons-material/Favorite'
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder'
import type { PhotoDto } from '../api/photos'
import { likePhoto, unlikePhoto } from '../api/photos'
import GridPhotoCard from './GridPhotoCard'

interface Props { photos: PhotoDto[] }

export default function PhotoGrid({ photos }: Props) {
  const [likeState, setLikeState] = useState<Record<number, { liked: boolean; count: number }>>(
    () => Object.fromEntries(photos.map((p) => [p.id, { liked: p.likedByMe, count: p.likesCount }]))
  )
  const [openPhoto, setOpenPhoto] = useState<PhotoDto | null>(null)

  const mergedState = (photo: PhotoDto) =>
    likeState[photo.id] ?? { liked: photo.likedByMe, count: photo.likesCount }

  const toggleLike = async (photo: PhotoDto) => {
    const current = mergedState(photo)
    try {
      if (current.liked) {
        const res = await unlikePhoto(photo.id)
        setLikeState((s) => ({ ...s, [photo.id]: { liked: false, count: res.data.likesCount } }))
      } else {
        const res = await likePhoto(photo.id)
        setLikeState((s) => ({ ...s, [photo.id]: { liked: true, count: res.data.likesCount } }))
      }
    } catch { /* ignore */ }
  }

  const dialogState = openPhoto ? mergedState(openPhoto) : null

  return (
    <>
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: 'repeat(2, 1fr)', sm: 'repeat(3, 1fr)' },
          gap: '12px',
        }}
      >
        {photos.map((photo) => {
          const state = mergedState(photo)
          return (
            <GridPhotoCard
              key={photo.id}
              photo={photo}
              liked={state.liked}
              count={state.count}
              onToggleLike={() => toggleLike(photo)}
              onOpen={() => setOpenPhoto(photo)}
            />
          )
        })}
      </Box>

      {/* Photo detail dialog */}
      <Dialog
        open={!!openPhoto}
        onClose={() => setOpenPhoto(null)}
        maxWidth="sm"
        fullWidth
        PaperProps={{ sx: { bgcolor: 'background.paper', border: '1px solid', borderColor: 'divider', borderRadius: 3, overflow: 'hidden' } }}
      >
        {openPhoto && dialogState && (
          <>
            {/* Header */}
            <Box sx={{ display: 'flex', alignItems: 'center', px: 2, py: 1.5, gap: 1.5 }}>
              <Box sx={{ p: '2px', borderRadius: '50%', background: 'var(--gradient-primary)', flexShrink: 0 }}>
                <Avatar
                  src={openPhoto.avatarUrl ?? undefined}
                  sx={{ width: 36, height: 36, bgcolor: 'background.paper', fontSize: 13, fontWeight: 700, border: '2px solid', borderColor: 'background.paper' }}
                >
                  {openPhoto.username?.slice(0, 2).toUpperCase()}
                </Avatar>
              </Box>
              <Box sx={{ flexGrow: 1, minWidth: 0 }}>
                <Typography variant="body2" fontWeight={700} noWrap>{openPhoto.username}</Typography>
                <Box sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.25, px: 0.75, py: 0.125, borderRadius: '20px', bgcolor: 'rgba(0,0,0,0.35)', backdropFilter: 'blur(6px)', border: '1px solid rgba(255,255,255,0.1)', mt: 0.25 }}>
                  <Typography sx={{ fontSize: 11, lineHeight: 1 }}>{openPhoto.monsterEmoji}</Typography>
                  <Typography sx={{ fontSize: 10, fontWeight: 600, color: 'rgba(255,255,255,0.85)', lineHeight: 1 }}>{openPhoto.monsterName}</Typography>
                </Box>
              </Box>
              <IconButton size="small" onClick={() => setOpenPhoto(null)} sx={{ color: 'text.secondary' }}>
                <CloseIcon />
              </IconButton>
            </Box>

            {/* Image */}
            <Box sx={{ width: '100%', lineHeight: 0 }}>
              <img
                src={openPhoto.imageUrl}
                alt={openPhoto.monsterName}
                style={{ width: '100%', maxHeight: '70vh', objectFit: 'contain', display: 'block', background: '#0a0a18' }}
              />
            </Box>

            {/* Footer */}
            <Box sx={{ px: 1, py: 0.5, display: 'flex', alignItems: 'center', gap: 0.5, borderTop: '1px solid', borderColor: 'divider' }}>
              <IconButton
                size="small"
                onClick={() => toggleLike(openPhoto)}
                sx={{ color: dialogState.liked ? '#ec4899' : '#8888aa', '&:hover': { color: dialogState.liked ? '#f472b6' : '#f0f0f0' } }}
              >
                {dialogState.liked ? <FavoriteIcon sx={{ fontSize: 22 }} /> : <FavoriteBorderIcon sx={{ fontSize: 22 }} />}
              </IconButton>
              <Typography variant="body2" sx={{ color: 'text.secondary', fontSize: 13, fontWeight: 600 }}>
                {dialogState.count > 0 ? `${dialogState.count} j'aime` : 'Aucun like'}
              </Typography>
            </Box>
          </>
        )}
      </Dialog>
    </>
  )
}
