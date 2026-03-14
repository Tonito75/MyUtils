import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Container, Stack, Avatar, Typography, Paper, Button, Box
} from '@mui/material'
import { GridView, ArrowBack, Videocam } from '@mui/icons-material'
import { api } from '../api/client'
import type { CameraConfig } from '../types/camera'

export default function AllStreamsPage() {
  const navigate = useNavigate()
  const [cameras, setCameras] = useState<CameraConfig[]>([])

  useEffect(() => {
    api.getCameras().then(setCameras)
  }, [])

  return (
    <Container maxWidth="xl" sx={{ py: 8 }}>
      <Stack spacing={4} alignItems="center">
        <Stack alignItems="center" spacing={1}>
          <Stack direction="row" alignItems="center" spacing={2}>
            <Avatar sx={{ bgcolor: 'primary.main', width: 48, height: 48 }}>
              <GridView />
            </Avatar>
            <Typography variant="h5" fontWeight="bold">Tous les flux</Typography>
          </Stack>
        </Stack>

        <Button
          variant="outlined"
          startIcon={<ArrowBack />}
          size="small"
          onClick={() => navigate('/')}
        >
          Retour
        </Button>

        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(400px, 1fr))',
            gap: 2,
            width: '100%',
          }}
        >
          {cameras.map(camera => (
            <Paper
              key={camera.name}
              elevation={0}
              sx={{
                p: 2,
                background: 'rgba(255,255,255,0.05)',
                border: '1px solid rgba(255,255,255,0.1)',
                overflow: 'hidden',
              }}
            >
              <Stack spacing={1}>
                <Stack direction="row" alignItems="center" spacing={1}>
                  <Videocam fontSize="small" />
                  <Typography variant="subtitle1" fontWeight="bold">{camera.name}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {camera.group || 'Autres'}
                  </Typography>
                </Stack>
                <Box
                  component="iframe"
                  src={`${camera.url}_low`}
                  allowFullScreen
                  sx={{
                    width: '100%',
                    aspectRatio: '16/9',
                    border: 'none',
                    borderRadius: 1,
                    background: 'rgba(0,0,0,0.3)',
                  }}
                />
              </Stack>
            </Paper>
          ))}
        </Box>

        <Typography variant="caption" color="text.secondary">© Tonito</Typography>
      </Stack>
    </Container>
  )
}
