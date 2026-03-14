import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Container, Stack, Avatar, Typography, Paper,
  TextField, Button, Alert, InputAdornment
} from '@mui/material'
import { Home, Lock, Login } from '@mui/icons-material'
import { api } from '../api/client'

export default function LoginPage() {
  const [password, setPassword] = useState('')
  const [error, setError] = useState(false)
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError(false)

    try {
      const res = await api.login(password)
      if (res.ok) {
        navigate('/')
      } else {
        setError(true)
      }
    } catch {
      setError(true)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Container maxWidth="xs" sx={{ py: 8 }}>
      <Stack spacing={4} alignItems="center">
        <Stack alignItems="center" spacing={1}>
          <Stack direction="row" alignItems="center" spacing={2}>
            <Avatar sx={{ bgcolor: 'primary.main', width: 48, height: 48 }}>
              <Home />
            </Avatar>
            <Typography variant="h5" fontWeight="bold">The house of tonito !</Typography>
          </Stack>
          <Typography variant="subtitle1" color="text.secondary">Connexion requise</Typography>
        </Stack>

        <Paper
          elevation={0}
          sx={{
            p: 4,
            width: '100%',
            background: 'rgba(255,255,255,0.05)',
            backdropFilter: 'blur(10px)',
            border: '1px solid rgba(255,255,255,0.1)',
          }}
        >
          <Stack spacing={3}>
            <Stack alignItems="center" spacing={1}>
              <Typography variant="h5" color="primary">Authentification</Typography>
              <Typography variant="body2" color="text.secondary" align="center">
                Veuillez entrer vos identifiants pour accéder aux caméras.
              </Typography>
            </Stack>

            {error && (
              <Alert severity="error">Identifiants incorrects. Veuillez réessayer.</Alert>
            )}

            <form onSubmit={handleSubmit}>
              <Stack spacing={3}>
                <TextField
                  label="Mot de passe"
                  type="password"
                  variant="outlined"
                  fullWidth
                  required
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  autoComplete="current-password"
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Lock color="action" />
                      </InputAdornment>
                    ),
                  }}
                />
                <Button
                  type="submit"
                  variant="contained"
                  size="large"
                  fullWidth
                  disabled={loading}
                  startIcon={<Login />}
                >
                  Se connecter
                </Button>
              </Stack>
            </form>
          </Stack>
        </Paper>

        <Typography variant="caption" color="text.secondary">© Tonito</Typography>
      </Stack>
    </Container>
  )
}
