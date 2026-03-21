import { useState } from 'react'
import { AppBar, Toolbar, IconButton, Badge, Avatar, Box, Tooltip, Button } from '@mui/material'
import NotificationsIcon from '@mui/icons-material/Notifications'
import AddIcon from '@mui/icons-material/Add'
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

  return (
    <AppBar position="sticky" elevation={0}>
      <Toolbar sx={{ gap: 0, px: { xs: 2, sm: 3 } }}>
        {/* Logo */}
        <Box
          onClick={() => navigate('/')}
          sx={{
            fontWeight: 900,
            fontSize: 20,
            letterSpacing: '-0.5px',
            background: 'var(--gradient-primary)',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            backgroundClip: 'text',
            cursor: 'pointer',
            mr: 4,
            userSelect: 'none',
          }}
        >
          Monster Hub
        </Box>

        {/* Nav links — desktop only */}
        <Box sx={{ display: { xs: 'none', sm: 'flex' }, alignItems: 'center', gap: 0.5 }}>
          {[
            { to: '/', label: 'Fil' },
            { to: '/explore', label: 'Explorer' },
            { to: '/friends', label: 'Amis' },
          ].map(({ to, label }) => (
            <NavLink key={to} to={to} style={{ textDecoration: 'none' }} end={to === '/'}>
              {({ isActive }) => (
                <Box
                  sx={{
                    px: 1.5,
                    py: 0.75,
                    fontSize: 14,
                    fontWeight: isActive ? 600 : 400,
                    color: isActive ? 'transparent' : '#8888aa',
                    borderBottom: isActive
                      ? '2px solid #7c3aed'
                      : '2px solid transparent',
                    background: isActive ? 'var(--gradient-primary)' : 'none',
                    WebkitBackgroundClip: isActive ? 'text' : 'unset',
                    WebkitTextFillColor: isActive ? 'transparent' : 'unset',
                    backgroundClip: isActive ? 'text' : 'unset',
                    transition: 'color 0.15s, border-color 0.15s',
                    cursor: 'pointer',
                    '&:hover': { color: '#bbb', WebkitTextFillColor: 'unset', background: 'none', WebkitBackgroundClip: 'unset', backgroundClip: 'unset' },
                  }}
                >
                  {label}
                </Box>
              )}
            </NavLink>
          ))}
        </Box>

        <Box sx={{ flexGrow: 1 }} />

        {/* Actions */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          {/* Publish button */}
          <Button
            variant="contained"
            size="small"
            startIcon={<AddIcon sx={{ fontSize: '17px !important' }} />}
            onClick={() => navigate('/upload')}
            sx={{ borderRadius: '20px', fontSize: 13, px: 1.5, py: 0.5, minWidth: 0 }}
          >
            Publier
          </Button>

          {/* Bell */}
          <IconButton
            onClick={handleBellClick}
            size="small"
            sx={{ color: '#8888aa', '&:hover': { color: '#f0f0f0' } }}
          >
            <Badge badgeContent={unreadCount} color="error">
              <NotificationsIcon sx={{ fontSize: 22 }} />
            </Badge>
          </IconButton>

          {/* Avatar */}
          <Tooltip title={user?.username ?? ''}>
            <Avatar
              sx={{
                width: 32,
                height: 32,
                background: 'var(--gradient-primary)',
                fontSize: 11,
                fontWeight: 700,
                cursor: 'pointer',
              }}
              onClick={() => navigate('/profile')}
            >
              {user?.username?.slice(0, 2).toUpperCase()}
            </Avatar>
          </Tooltip>
        </Box>
      </Toolbar>

      <NotificationPopover anchorEl={bellAnchor} onClose={() => setBellAnchor(null)} />
    </AppBar>
  )
}
