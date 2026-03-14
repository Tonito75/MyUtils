import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Container, Stack, Avatar, Typography, Paper,
  Button, Divider, Alert
} from '@mui/material'
import { Home, GridView, Logout } from '@mui/icons-material'
import { api } from '../api/client'
import type { CameraConfig } from '../types/camera'
import CameraCard from '../components/CameraCard'

export default function HomePage() {
  const navigate = useNavigate()
  const [cameras, setCameras] = useState<CameraConfig[]>([])
  const [pingResults, setPingResults] = useState<Record<string, boolean | null>>({})
  const [pingLoading, setPingLoading] = useState<Record<string, boolean>>({})

  useEffect(() => {
    api.getCameras().then(data => {
      setCameras(data)

      const loading: Record<string, boolean> = {}
      const results: Record<string, boolean | null> = {}
      data.forEach(c => {
        loading[c.name] = true
        results[c.name] = null
      })
      setPingLoading(loading)
      setPingResults(results)

      // Ping chaque caméra en parallèle
      data.forEach(camera => {
        api.pingCamera(camera.name).then(r => {
          setPingResults(prev => ({ ...prev, [camera.name]: r.isOnline }))
          setPingLoading(prev => ({ ...prev, [camera.name]: false }))
        }).catch(() => {
          setPingResults(prev => ({ ...prev, [camera.name]: false }))
          setPingLoading(prev => ({ ...prev, [camera.name]: false }))
        })
      })
    })
  }, [])

  const handleLogout = async () => {
    await api.logout()
    navigate('/login')
  }

  const groups = cameras.reduce<Record<string, CameraConfig[]>>((acc, cam) => {
    const key = cam.group || 'Autres'
    acc[key] = [...(acc[key] || []), cam]
    return acc
  }, {})

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Stack spacing={4} alignItems="center">
        <Stack alignItems="center" spacing={1}>
          <Stack direction="row" alignItems="center" spacing={2}>
            <Avatar sx={{ bgcolor: 'primary.main', width: 48, height: 48 }}>
              <Home />
            </Avatar>
            <Typography variant="h5" fontWeight="bold">The house of tonito !</Typography>
          </Stack>
          <Typography variant="subtitle1" color="text.secondary">Les caméras</Typography>
        </Stack>

        <Stack direction="row" spacing={2}>
          <Button variant="outlined" startIcon={<GridView />} size="small" onClick={() => navigate('/streams')}>
            Tous les flux
          </Button>
          <Button variant="outlined" color="error" startIcon={<Logout />} size="small" onClick={handleLogout}>
            Déconnexion
          </Button>
        </Stack>

        <Paper
          elevation={0}
          sx={{
            p: 3,
            width: '100%',
            background: 'rgba(255,255,255,0.05)',
            backdropFilter: 'blur(10px)',
            border: '1px solid rgba(255,255,255,0.1)',
          }}
        >
          <Stack spacing={3}>
            <Typography variant="body2" color="text.secondary" align="center">
              Système de surveillance de renards, pigeons, pies, chats et autres trucs très utiles.
            </Typography>

            {Object.entries(groups).map(([group, cams]) => (
              <Stack key={group} spacing={2}>
                <Stack direction="row" alignItems="center" spacing={2}>
                  <Divider sx={{ flex: 1 }} />
                  <Typography variant="subtitle2" color="text.secondary">{group}</Typography>
                  <Divider sx={{ flex: 1 }} />
                </Stack>
                {cams.map(cam => (
                  <CameraCard
                    key={cam.name}
                    camera={cam}
                    isOnline={pingResults[cam.name] ?? null}
                    isLoading={pingLoading[cam.name] ?? false}
                    onHistory={name => navigate(`/camerahistory/${name.toLowerCase()}`)}
                  />
                ))}
              </Stack>
            ))}

            <Alert severity="info" variant="outlined">
              <Typography variant="body2">Le chargement du flux peut prendre quelques secondes</Typography>
            </Alert>
          </Stack>
        </Paper>

        <Typography variant="caption" color="text.secondary">© Tonito</Typography>
      </Stack>
    </Container>
  )
}
