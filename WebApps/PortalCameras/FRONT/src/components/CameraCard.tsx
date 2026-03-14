import { Stack, Paper, Typography, Button, CircularProgress } from '@mui/material'
import { Videocam, Circle, PlayCircle, History } from '@mui/icons-material'
import type { CameraConfig } from '../types/camera'

interface Props {
  camera: CameraConfig
  isOnline: boolean | null
  isLoading: boolean
  onHistory: (name: string) => void
}

export default function CameraCard({ camera, isOnline, isLoading, onHistory }: Props) {
  const statusColor = isLoading
    ? 'warning'
    : isOnline === true
    ? 'success'
    : isOnline === false
    ? 'error'
    : 'disabled'

  return (
    <Paper
      elevation={0}
      sx={{
        p: 2,
        background: 'rgba(255,255,255,0.05)',
        border: '1px solid rgba(255,255,255,0.08)',
        borderRadius: 2,
      }}
    >
      <Stack direction="row" alignItems="center" spacing={2}>
        <Videocam color="primary" />
        <Stack flex={1}>
          <Stack direction="row" alignItems="center" spacing={1}>
            <Typography variant="h6">{camera.name}</Typography>
            {isLoading ? (
              <CircularProgress size={12} color="warning" />
            ) : (
              <Circle sx={{ fontSize: 12 }} color={statusColor} />
            )}
          </Stack>
        </Stack>
        <Stack spacing={0}>
          <Button
            size="small"
            color="secondary"
            startIcon={<PlayCircle />}
            href={camera.url}
            target="_blank"
            rel="noopener noreferrer"
          >
            Flux live
          </Button>
          <Button
            size="small"
            color="primary"
            startIcon={<History />}
            onClick={() => onHistory(camera.name)}
          >
            Historique
          </Button>
        </Stack>
      </Stack>
    </Paper>
  )
}
