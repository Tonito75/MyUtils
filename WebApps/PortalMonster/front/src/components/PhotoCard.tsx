import { useState } from 'react'
import { Box, Avatar, Typography, IconButton } from '@mui/material'
import FavoriteIcon from '@mui/icons-material/Favorite'
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder'
import type { PhotoDto } from '../api/photos'
import { likePhoto, unlikePhoto } from '../api/photos'

interface Props { photo: PhotoDto }

export default function PhotoCard({ photo }: Props) {
  const [liked, setLiked] = useState(photo.likedByMe)
  const [count, setCount] = useState(photo.likesCount)

  const toggleLike = async () => {
    try {
      if (liked) {
        const res = await unlikePhoto(photo.id)
        setLiked(false)
        setCount(res.data.likesCount)
      } else {
        const res = await likePhoto(photo.id)
        setLiked(true)
        setCount(res.data.likesCount)
      }
    } catch { /* ignore */ }
  }

  return (
    <Box
      sx={{
        mb: '12px',
        bgcolor: 'background.paper',
        border: '1px solid',
        borderColor: 'divider',
        borderRadius: 2,
        overflow: 'hidden',
        transition: 'box-shadow 0.2s ease, transform 0.2s ease',
        '&:hover': {
          boxShadow: '0 0 0 1px #7c3aed40, 0 8px 24px rgba(124,58,237,0.15)',
          transform: 'translateY(-2px)',
        },
      }}
    >
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', px: 1.5, py: 1.25, gap: 1.25 }}>
        {/* Avatar with gradient ring */}
        <Box
          sx={{
            p: '2px',
            borderRadius: '50%',
            background: 'var(--gradient-primary)',
            flexShrink: 0,
          }}
        >
          <Avatar
            src={photo.avatarUrl ?? undefined}
            sx={{
              width: 36,
              height: 36,
              bgcolor: 'background.paper',
              fontSize: 13,
              fontWeight: 700,
              border: '2px solid',
              borderColor: 'background.paper',
            }}
          >
            {photo.username?.slice(0, 2).toUpperCase()}
          </Avatar>
        </Box>

        <Box sx={{ flexGrow: 1, minWidth: 0 }}>
          <Typography variant="body2" fontWeight={700} noWrap sx={{ color: 'text.primary', lineHeight: 1.3 }}>
            {photo.username}
          </Typography>
          {/* Monster pill */}
          <Box
            sx={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: 0.25,
              px: 0.75,
              py: 0.125,
              borderRadius: '20px',
              bgcolor: 'rgba(0,0,0,0.35)',
              backdropFilter: 'blur(6px)',
              border: '1px solid rgba(255,255,255,0.1)',
              mt: 0.25,
            }}
          >
            <Typography sx={{ fontSize: 11, lineHeight: 1 }}>{photo.monsterEmoji}</Typography>
            <Typography sx={{ fontSize: 10, fontWeight: 600, color: 'rgba(255,255,255,0.85)', lineHeight: 1 }}>
              {photo.monsterName}
            </Typography>
          </Box>
        </Box>
      </Box>

      {/* Image */}
      <Box sx={{ width: '100%', aspectRatio: '4/5', overflow: 'hidden', bgcolor: '#0a0a18' }}>
        <img
          src={photo.imageUrl}
          alt={photo.monsterName}
          style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }}
        />
      </Box>

      {/* Footer */}
      <Box sx={{ px: 0.5, py: 0.25, display: 'flex', alignItems: 'center', gap: 0.5, borderTop: '1px solid', borderColor: 'divider' }}>
        <IconButton
          onClick={toggleLike}
          size="small"
          sx={{ color: liked ? '#ec4899' : '#8888aa', '&:hover': { color: liked ? '#f472b6' : '#f0f0f0' } }}
        >
          {liked ? <FavoriteIcon sx={{ fontSize: 22 }} /> : <FavoriteBorderIcon sx={{ fontSize: 22 }} />}
        </IconButton>
        <Typography variant="body2" sx={{ color: 'text.secondary', fontSize: 13, fontWeight: 600 }}>
          {count > 0 ? `${count} j'aime` : 'Aucun like'}
        </Typography>
      </Box>
    </Box>
  )
}
