import { useState } from 'react'
import { Box, Button, TextField, Typography, Paper, Link, Alert } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { register } from '../api/auth'
import { useAuth } from '../contexts/AuthContext'

export default function RegisterPage() {
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const { signIn } = useAuth()
  const navigate = useNavigate()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const res = await register(username, email, password)
      signIn(res.data.token)
      navigate('/')
    } catch (err: any) {
      const msg = err.response?.data?.[0] ?? 'Erreur lors de la création du compte.'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center',
      justifyContent: 'center', bgcolor: 'background.default' }}>
      <Paper sx={{ p: 4, width: 360 }} elevation={4}>
        <Typography variant="h5" fontWeight={900} color="primary" mb={3} textAlign="center">
          Créer un compte
        </Typography>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <TextField label="Nom d'utilisateur" value={username}
            onChange={(e) => setUsername(e.target.value)} required fullWidth />
          <TextField label="Email" type="email" value={email}
            onChange={(e) => setEmail(e.target.value)} required fullWidth />
          <TextField label="Mot de passe" type="password" value={password}
            onChange={(e) => setPassword(e.target.value)} required fullWidth />
          <Button type="submit" variant="contained" fullWidth disabled={loading}>
            {loading ? 'Création...' : 'Créer mon compte'}
          </Button>
        </Box>
        <Typography variant="body2" textAlign="center" mt={2}>
          Déjà un compte ?{' '}
          <Link href="/login" underline="hover">Se connecter</Link>
        </Typography>
      </Paper>
    </Box>
  )
}
