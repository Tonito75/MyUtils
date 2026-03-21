import { useCallback, useEffect, useRef, useState } from 'react'
import {
  Box, Typography, Avatar, Button, Dialog, DialogTitle, DialogContent,
  DialogActions, TextField, Alert, Stack, CircularProgress, IconButton
} from '@mui/material'
import EditIcon from '@mui/icons-material/Edit'
import { getMe, updateProfile, uploadAvatar, getUserPhotos, type UserProfile } from '../api/users'
import { useAuth } from '../contexts/AuthContext'
import PhotoGrid from '../components/PhotoGrid'
import type { PhotoDto } from '../api/photos'

export default function ProfilePage() {
  const { user } = useAuth()
  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [editOpen, setEditOpen] = useState(false)
  const [currentPassword, setCurrentPassword] = useState('')
  const [newEmail, setNewEmail] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [editError, setEditError] = useState('')
  const [editSaving, setEditSaving] = useState(false)
  const [photos, setPhotos] = useState<PhotoDto[]>([])
  const [hasMore, setHasMore] = useState(true)
  const [loading, setLoading] = useState(false)
  const cursor = useRef<number | undefined>(undefined)
  const sentinel = useRef<HTMLDivElement | null>(null)
  const avatarInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    getMe().then((r) => {
      setProfile(r.data)
      setNewEmail(r.data.email)
    })
  }, [])

  const loadPhotos = useCallback(async () => {
    if (!user || loading || !hasMore) return
    setLoading(true)
    try {
      const res = await getUserPhotos(user.id, cursor.current, 12)
      if (res.data.length === 0) {
        setHasMore(false)
      } else {
        cursor.current = res.data[res.data.length - 1].id
        setPhotos((prev) => [...prev, ...res.data])
      }
    } finally {
      setLoading(false)
    }
  }, [user, loading, hasMore])

  useEffect(() => {
    loadPhotos()
  }, [])

  useEffect(() => {
    const el = sentinel.current
    if (!el) return
    const obs = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting) loadPhotos()
    })
    obs.observe(el)
    return () => obs.disconnect()
  }, [loadPhotos])

  const handleSaveProfile = async () => {
    setEditError('')
    setEditSaving(true)
    try {
      await updateProfile({
        currentPassword,
        newEmail: newEmail !== profile?.email ? newEmail : undefined,
        newPassword: newPassword || undefined
      })
      setEditOpen(false)
      const r = await getMe()
      setProfile(r.data)
    } catch (err: any) {
      setEditError(err.response?.data?.error ?? 'Erreur lors de la mise à jour.')
    } finally {
      setEditSaving(false)
    }
  }

  const handleAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0]
    if (!f) return
    const res = await uploadAvatar(f)
    setProfile((p) => p ? { ...p, avatarUrl: res.data.avatarUrl } : p)
  }

  return (
    <Box>
      {/* Profile header */}
      <Stack direction="row" spacing={2} alignItems="center" mb={3}>
        <Box sx={{ position: 'relative', flexShrink: 0 }}>
          {/* Gradient ring around avatar */}
          <Box
            sx={{ p: '3px', borderRadius: '50%', background: 'var(--gradient-primary)', cursor: 'pointer' }}
            onClick={() => avatarInputRef.current?.click()}
          >
            <Avatar
              src={profile?.avatarUrl ?? undefined}
              sx={{ width: 72, height: 72, bgcolor: 'background.paper', fontSize: 28, fontWeight: 700, border: '3px solid', borderColor: 'background.paper' }}
            >
              {user?.username?.slice(0, 2).toUpperCase()}
            </Avatar>
          </Box>
          <input ref={avatarInputRef} type="file" accept="image/*"
            style={{ display: 'none' }} onChange={handleAvatarChange} />
        </Box>
        <Box sx={{ flexGrow: 1 }}>
          <Typography
            variant="h6"
            fontWeight={800}
            sx={{
              background: 'var(--gradient-primary)',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
              backgroundClip: 'text',
            }}
          >
            {user?.username}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {profile?.photoCount ?? 0} photos · {profile?.friendCount ?? 0} amis
          </Typography>
        </Box>
        <IconButton onClick={() => setEditOpen(true)}><EditIcon /></IconButton>
      </Stack>

      {/* Photos grid */}
      <PhotoGrid photos={photos} />
      {photos.length === 0 && !loading && (
        <Typography color="text.secondary" textAlign="center" mt={4}>
          Aucune photo publiée.
        </Typography>
      )}
      <Box ref={sentinel} sx={{ py: 2, textAlign: 'center' }}>
        {loading && <CircularProgress size={24} />}
      </Box>

      {/* Edit profile dialog */}
      <Dialog open={editOpen} onClose={() => setEditOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Modifier mon profil</DialogTitle>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 2 }}>
          {editError && <Alert severity="error">{editError}</Alert>}
          <TextField label="Mot de passe actuel" type="password" fullWidth required
            value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} />
          <TextField label="Nouvel email" type="email" fullWidth
            value={newEmail} onChange={(e) => setNewEmail(e.target.value)} />
          <TextField label="Nouveau mot de passe (optionnel)" type="password" fullWidth
            value={newPassword} onChange={(e) => setNewPassword(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditOpen(false)}>Annuler</Button>
          <Button variant="contained" onClick={handleSaveProfile} disabled={editSaving}>
            {editSaving ? 'Enregistrement...' : 'Enregistrer'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
