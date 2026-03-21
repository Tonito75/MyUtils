import { useState } from 'react'
import { Card, CardHeader, CardMedia, CardActions, Avatar,
  IconButton, Typography, Box } from '@mui/material'
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
    <Card sx={{ mb: 2, bgcolor: 'background.paper' }}>
      <CardHeader
        avatar={
          <Avatar src={photo.avatarUrl ?? undefined} sx={{ bgcolor: 'primary.main' }}>
            {photo.username.slice(0, 2).toUpperCase()}
          </Avatar>
        }
        title={photo.username}
        subheader={
          <Box component="span" sx={{ color: 'text.secondary', fontSize: 13 }}>
            {photo.monsterEmoji} {photo.monsterName}
          </Box>
        }
      />
      <CardMedia
        component="img"
        image={photo.imageUrl}
        alt={photo.monsterName}
        sx={{ maxHeight: 480, objectFit: 'contain', bgcolor: '#111' }}
      />
      <CardActions>
        <IconButton onClick={toggleLike} color={liked ? 'error' : 'default'}>
          {liked ? <FavoriteIcon /> : <FavoriteBorderIcon />}
        </IconButton>
        <Typography variant="body2" color="text.secondary">{count}</Typography>
      </CardActions>
    </Card>
  )
}
