import { Box, Typography, IconButton } from '@mui/material'
import FavoriteIcon from '@mui/icons-material/Favorite'
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder'
import type { PhotoDto } from '../api/photos'

interface Props {
  photo: PhotoDto
  liked: boolean
  count: number
  onToggleLike: () => void
}

export default function GridPhotoCard({ photo, liked, count, onToggleLike }: Props) {
  return (
    <Box
      sx={{
        borderRadius: 2,
        overflow: 'hidden',
        bgcolor: 'background.paper',
        border: '1px solid',
        borderColor: 'divider',
        display: 'flex',
        flexDirection: 'column',
        transition: 'box-shadow 0.2s ease, transform 0.2s ease',
        '&:hover': {
          boxShadow: '0 0 0 1px #7c3aed40, 0 8px 24px rgba(124,58,237,0.15)',
          transform: 'translateY(-2px)',
        },
        '&:hover .img-overlay': { opacity: 1 },
      }}
    >
      {/* Image */}
      <Box sx={{ position: 'relative', width: '100%', aspectRatio: '1/1', overflow: 'hidden', bgcolor: '#0a0a18' }}>
        <img
          src={photo.imageUrl}
          alt={photo.monsterName}
          loading="lazy"
          style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }}
        />
        {/* Hover overlay */}
        <Box
          className="img-overlay"
          sx={{
            position: 'absolute',
            inset: 0,
            background: 'linear-gradient(135deg, rgba(124,58,237,0.5) 0%, rgba(236,72,153,0.5) 100%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: 0.5,
            opacity: 0,
            transition: 'opacity 0.18s ease',
          }}
        >
          <FavoriteIcon sx={{ fontSize: 20, color: liked ? '#ff3b5c' : 'white' }} />
          <Box component="span" sx={{ color: 'white', fontWeight: 700, fontSize: 14 }}>
            {count}
          </Box>
        </Box>
      </Box>

      {/* Footer: monster info + like */}
      <Box
        sx={{
          px: 1,
          py: 0.75,
          display: 'flex',
          alignItems: 'center',
          gap: 0.5,
          minWidth: 0,
        }}
      >
        {/* Monster badge */}
        <Box
          sx={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 0.25,
            px: 0.75,
            py: 0.25,
            borderRadius: '20px',
            background: 'var(--gradient-primary)',
            flexShrink: 0,
          }}
        >
          <Typography sx={{ fontSize: 11, lineHeight: 1 }}>{photo.monsterEmoji}</Typography>
          <Typography sx={{ fontSize: 10, fontWeight: 700, color: '#fff', lineHeight: 1, whiteSpace: 'nowrap' }}>
            {photo.monsterName}
          </Typography>
        </Box>

        <Box sx={{ flexGrow: 1 }} />

        {/* Like button */}
        <IconButton
          size="small"
          onClick={(e) => { e.stopPropagation(); onToggleLike() }}
          sx={{
            color: liked ? '#ec4899' : '#8888aa',
            p: 0.25,
            '&:hover': { color: liked ? '#f472b6' : '#f0f0f0' },
          }}
        >
          {liked
            ? <FavoriteIcon sx={{ fontSize: 16 }} />
            : <FavoriteBorderIcon sx={{ fontSize: 16 }} />
          }
        </IconButton>
        <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 600, minWidth: 16, textAlign: 'right' }}>
          {count}
        </Typography>
      </Box>
    </Box>
  )
}
