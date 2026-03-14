import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Container, Stack, Avatar, Typography, Paper,
  Button, CircularProgress, Alert, Switch, Box
} from '@mui/material'
import { History, ArrowBack } from '@mui/icons-material'
import { api } from '../api/client'
import type { CameraImage } from '../types/camera'

const MAX_IMAGES = 10

export default function CameraHistoryPage() {
  const { name } = useParams<{ name: string }>()
  const navigate = useNavigate()
  const [images, setImages] = useState<CameraImage[]>([])
  const [loading, setLoading] = useState(true)
  const [useAI, setUseAI] = useState(false)

  const load = async (ai: boolean) => {
    setLoading(true)
    setImages([])
    try {
      const data = await api.getCameraHistory(name!, ai)
      setImages(data)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load(false) }, [name])

  const handleAIToggle = (checked: boolean) => {
    setUseAI(checked)
    load(checked)
  }

  return (
    <Container maxWidth="lg" sx={{ py: 8 }}>
      <Stack spacing={4} alignItems="center">
        <Stack alignItems="center" spacing={1}>
          <Stack direction="row" alignItems="center" spacing={2}>
            <Avatar sx={{ bgcolor: 'primary.main', width: 48, height: 48 }}>
              <History />
            </Avatar>
            <Typography variant="h5" fontWeight="bold">Historique - {name}</Typography>
          </Stack>
          <Typography variant="subtitle1" color="text.secondary">
            Les {MAX_IMAGES} dernières captures
          </Typography>
        </Stack>

        <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ width: '100%' }}>
          <Button
            variant="outlined"
            startIcon={<ArrowBack />}
            size="small"
            onClick={() => navigate('/')}
            sx={{ minWidth: 180 }}
          >
            Retour
          </Button>
          <Stack direction="row" alignItems="center" spacing={1}>
            <Typography variant="caption" color="text.secondary">
              Détection intelligente (expérimental)
            </Typography>
            <Switch
              size="small"
              checked={useAI}
              onChange={e => handleAIToggle(e.target.checked)}
              color="secondary"
            />
          </Stack>
        </Stack>

        <Paper
          elevation={0}
          sx={{
            p: 3,
            width: '100%',
            background: 'rgba(255,255,255,0.05)',
            border: '1px solid rgba(255,255,255,0.1)',
          }}
        >
          {loading && (
            <Stack alignItems="center" spacing={2} py={8}>
              <CircularProgress color="primary" />
              <Typography variant="body2" color="text.secondary">Chargement des images...</Typography>
            </Stack>
          )}

          {!loading && images.length === 0 && (
            <Alert severity="info" variant="outlined">
              Aucune image trouvée pour cette caméra.
            </Alert>
          )}

          {!loading && images.length > 0 && (
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: {
                  xs: '1fr',
                  sm: 'repeat(2, 1fr)',
                  md: 'repeat(3, 1fr)',
                  lg: 'repeat(auto-fill, minmax(400px, 1fr))',
                },
                gap: 2,
              }}
            >
              {images.map((img) => (
                <Paper
                  key={img.url}
                  elevation={2}
                  sx={{
                    p: 2,
                    background: 'rgba(255,255,255,0.05)',
                    border: '1px solid rgba(255,255,255,0.08)',
                  }}
                >
                  <Stack spacing={1} alignItems="center">
                    <Typography variant="caption" color="text.secondary">
                      {new Date(img.date).toLocaleString('fr-FR')} — {img.timeAgo}
                    </Typography>
                    <Box
                      component="a"
                      href={img.url}
                      target="_blank"
                      rel="noopener noreferrer"
                      sx={{ display: 'block', width: '100%' }}
                    >
                      <Box
                        component="img"
                        src={img.url}
                        alt="Capture"
                        sx={{
                          width: '100%',
                          height: 300,
                          objectFit: 'cover',
                          borderRadius: 1,
                          transition: 'transform 0.2s',
                          '&:hover': { transform: 'scale(1.02)' },
                        }}
                      />
                    </Box>
                  </Stack>
                </Paper>
              ))}
            </Box>
          )}
        </Paper>

        <Typography variant="caption" color="text.secondary">
          {images.length} image(s) trouvée(s)
        </Typography>
      </Stack>
    </Container>
  )
}
