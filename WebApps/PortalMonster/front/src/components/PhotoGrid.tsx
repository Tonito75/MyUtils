import { ImageList, ImageListItem, Box } from '@mui/material'
import type { PhotoDto } from '../api/photos'
import FavoriteIcon from '@mui/icons-material/Favorite'
import { useState } from 'react'
import { likePhoto, unlikePhoto } from '../api/photos'

interface Props { photos: PhotoDto[] }

export default function PhotoGrid({ photos }: Props) {
  const [likeState, setLikeState] = useState<Record<number, { liked: boolean; count: number }>>(
    () => Object.fromEntries(photos.map((p) => [p.id, { liked: p.likedByMe, count: p.likesCount }]))
  )

  const toggleLike = async (photo: PhotoDto) => {
    const current = likeState[photo.id]
    try {
      if (current?.liked) {
        const res = await unlikePhoto(photo.id)
        setLikeState((s) => ({ ...s, [photo.id]: { liked: false, count: res.data.likesCount } }))
      } else {
        const res = await likePhoto(photo.id)
        setLikeState((s) => ({ ...s, [photo.id]: { liked: true, count: res.data.likesCount } }))
      }
    } catch { /* ignore */ }
  }

  return (
    <ImageList variant="masonry" cols={3} gap={4}>
      {photos.map((photo) => {
        const state = likeState[photo.id] ?? { liked: photo.likedByMe, count: photo.likesCount }
        return (
          <ImageListItem key={photo.id} sx={{ position: 'relative', cursor: 'pointer' }}>
            <img src={photo.imageUrl} alt={photo.monsterName} loading="lazy"
              style={{ borderRadius: 4 }} />
            <Box onClick={() => toggleLike(photo)}
              sx={{ position: 'absolute', bottom: 4, right: 4, display: 'flex',
                alignItems: 'center', gap: 0.5, bgcolor: 'rgba(0,0,0,0.6)',
                borderRadius: 1, px: 0.75, py: 0.25 }}>
              <FavoriteIcon sx={{ fontSize: 14, color: state.liked ? 'error.main' : 'white' }} />
              <Box component="span" sx={{ color: 'white', fontSize: 12 }}>{state.count}</Box>
            </Box>
          </ImageListItem>
        )
      })}
    </ImageList>
  )
}
