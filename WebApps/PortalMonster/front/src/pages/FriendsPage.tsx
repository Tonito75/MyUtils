import { useEffect, useState } from 'react'
import {
  Box, Typography, Tabs, Tab, List, ListItem, ListItemAvatar, ListItemText,
  Avatar, IconButton, TextField, InputAdornment, Button, Chip
} from '@mui/material'
import DeleteIcon from '@mui/icons-material/Delete'
import CheckIcon from '@mui/icons-material/Check'
import CloseIcon from '@mui/icons-material/Close'
import SearchIcon from '@mui/icons-material/Search'
import PersonAddIcon from '@mui/icons-material/PersonAdd'
import {
  getFriends, getFriendRequests, sendFriendRequest, acceptFriend, removeFriend,
  type FriendDto
} from '../api/friends'
import { searchUsers } from '../api/users'

export default function FriendsPage() {
  const [tab, setTab] = useState(0)
  const [friends, setFriends] = useState<FriendDto[]>([])
  const [requests, setRequests] = useState<FriendDto[]>([])
  const [searchQuery, setSearchQuery] = useState('')
  const [searchResults, setSearchResults] = useState<Array<{
    userId: string; username: string; avatarUrl: string | null; pending: boolean
  }>>([])

  useEffect(() => {
    getFriends().then((r) => setFriends(r.data))
    getFriendRequests().then((r) => setRequests(r.data))
  }, [])

  const handleSearch = async () => {
    if (!searchQuery.trim()) return
    const res = await searchUsers(searchQuery)
    setSearchResults(res.data.map((u) => ({ ...u, pending: false })))
  }

  const handleAdd = async (userId: string) => {
    await sendFriendRequest(userId)
    setSearchResults((prev) =>
      prev.map((u) => u.userId === userId ? { ...u, pending: true } : u))
  }

  const handleAccept = async (userId: string) => {
    await acceptFriend(userId)
    setRequests((prev) => prev.filter((r) => r.userId !== userId))
    const accepted = requests.find((r) => r.userId === userId)
    if (accepted) setFriends((prev) => [...prev, accepted])
  }

  const handleDecline = async (userId: string) => {
    await removeFriend(userId)
    setRequests((prev) => prev.filter((r) => r.userId !== userId))
  }

  const handleRemove = async (userId: string) => {
    await removeFriend(userId)
    setFriends((prev) => prev.filter((f) => f.userId !== userId))
  }

  const UserAvatar = ({ user }: { user: FriendDto }) => (
    <Avatar src={user.avatarUrl ?? undefined} sx={{ bgcolor: 'primary.main' }}>
      {user.username.slice(0, 2).toUpperCase()}
    </Avatar>
  )

  return (
    <Box>
      <Typography variant="h6" mb={2} fontWeight={700}>Amis</Typography>

      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
        <Tab label={`Mes amis (${friends.length})`} />
        <Tab label={`Demandes (${requests.length})`} />
        <Tab label="Rechercher" />
      </Tabs>

      {/* Tab 0: Friends list */}
      {tab === 0 && (
        <List>
          {friends.length === 0 && (
            <Typography color="text.secondary" textAlign="center" mt={2}>
              Vous n'avez pas encore d'amis.
            </Typography>
          )}
          {friends.map((f) => (
            <ListItem key={f.userId} secondaryAction={
              <IconButton edge="end" onClick={() => handleRemove(f.userId)} color="error">
                <DeleteIcon />
              </IconButton>
            }>
              <ListItemAvatar><UserAvatar user={f} /></ListItemAvatar>
              <ListItemText primary={f.username} />
            </ListItem>
          ))}
        </List>
      )}

      {/* Tab 1: Friend requests */}
      {tab === 1 && (
        <List>
          {requests.length === 0 && (
            <Typography color="text.secondary" textAlign="center" mt={2}>
              Aucune demande en attente.
            </Typography>
          )}
          {requests.map((r) => (
            <ListItem key={r.userId} secondaryAction={
              <Box sx={{ display: 'flex', gap: 1 }}>
                <IconButton onClick={() => handleAccept(r.userId)} color="success">
                  <CheckIcon />
                </IconButton>
                <IconButton onClick={() => handleDecline(r.userId)} color="error">
                  <CloseIcon />
                </IconButton>
              </Box>
            }>
              <ListItemAvatar><UserAvatar user={r} /></ListItemAvatar>
              <ListItemText primary={r.username} />
            </ListItem>
          ))}
        </List>
      )}

      {/* Tab 2: Search */}
      {tab === 2 && (
        <Box>
          <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
            <TextField
              fullWidth size="small" placeholder="Rechercher un utilisateur..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
              InputProps={{
                startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment>
              }}
            />
            <Button variant="contained" onClick={handleSearch}>Chercher</Button>
          </Box>
          <List>
            {searchResults.map((u) => (
              <ListItem key={u.userId} secondaryAction={
                u.pending ? (
                  <Chip label="En attente" size="small" disabled />
                ) : (
                  <IconButton onClick={() => handleAdd(u.userId)} color="primary">
                    <PersonAddIcon />
                  </IconButton>
                )
              }>
                <ListItemAvatar>
                  <Avatar src={u.avatarUrl ?? undefined} sx={{ bgcolor: 'primary.main' }}>
                    {u.username.slice(0, 2).toUpperCase()}
                  </Avatar>
                </ListItemAvatar>
                <ListItemText primary={u.username} />
              </ListItem>
            ))}
          </List>
        </Box>
      )}
    </Box>
  )
}
