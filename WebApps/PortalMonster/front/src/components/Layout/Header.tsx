import { useState } from 'react'
import { AppBar, Toolbar, IconButton, Badge, Avatar, Box, Button, Tooltip } from '@mui/material'
import NotificationsIcon from '@mui/icons-material/Notifications'
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline'
import { useNavigate, NavLink } from 'react-router-dom'
import { useAuth } from '../../contexts/AuthContext'
import NotificationPopover from '../NotificationPopover'
import { useNotifications } from '../../hooks/useNotifications'

export default function Header() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [bellAnchor, setBellAnchor] = useState<HTMLElement | null>(null)
  const { unreadCount, markRead } = useNotifications()

  const handleBellClick = (e: React.MouseEvent<HTMLElement>) => {
    markRead()
    setBellAnchor(e.currentTarget)
  }

  const navStyle = { color: 'inherit', textDecoration: 'none' }
  const activeStyle = { color: '#00c853', fontWeight: 700 }

  return (
    <AppBar position="sticky" sx={{ bgcolor: 'background.paper' }} elevation={1}>
      <Toolbar sx={{ gap: 2 }}>
        <Box sx={{ fontWeight: 900, fontSize: 20, color: 'primary.main', mr: 2, cursor: 'pointer' }}
          onClick={() => navigate('/')}>
          Monster Hub
        </Box>

        <NavLink to="/" style={({ isActive }) => isActive ? { ...navStyle, ...activeStyle } : navStyle}>
          <Button color="inherit">Mon fil</Button>
        </NavLink>
        <NavLink to="/explore" style={({ isActive }) => isActive ? { ...navStyle, ...activeStyle } : navStyle}>
          <Button color="inherit">Explorer</Button>
        </NavLink>
        <NavLink to="/upload" style={({ isActive }) => isActive ? { ...navStyle, ...activeStyle } : navStyle}>
          <IconButton color={window.location.pathname === '/upload' ? 'primary' : 'inherit'}>
            <AddCircleOutlineIcon />
          </IconButton>
        </NavLink>

        <Box sx={{ flexGrow: 1 }} />

        <IconButton color="inherit" onClick={handleBellClick}>
          <Badge badgeContent={unreadCount} color="error">
            <NotificationsIcon />
          </Badge>
        </IconButton>

        <Tooltip title={user?.username}>
          <Avatar
            sx={{ cursor: 'pointer', width: 32, height: 32, bgcolor: 'primary.main' }}
            onClick={() => navigate('/profile')}
          >
            {user?.username.slice(0, 2).toUpperCase()}
          </Avatar>
        </Tooltip>
      </Toolbar>

      <NotificationPopover anchorEl={bellAnchor} onClose={() => setBellAnchor(null)} />
    </AppBar>
  )
}
