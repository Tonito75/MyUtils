import { Box } from '@mui/material'
import { useState } from 'react'
import type { PhotoDto } from '../api/photos'
import { likePhoto, unlikePhoto } from '../api/photos'
import GridPhotoCard from './GridPhotoCard'

interface Props { photos: PhotoDto[] }

export default function PhotoGrid({ photos }: Props) {
  const [likeState, setLikeState] = useState<Record<number, { liked: boolean; count: number }>>(
    () => Object.fromEntries(photos.map((p) => [p.id, { liked: p.likedByMe, count: p.likesCount }]))
  )

  // Sync new photos into likeState without overwriting existing
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

  return (
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
          />
        )
      })}
    </Box>
  )
}
