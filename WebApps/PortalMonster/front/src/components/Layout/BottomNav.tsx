import { BottomNavigation, BottomNavigationAction, Box } from '@mui/material'
import HomeIcon from '@mui/icons-material/Home'
import ExploreIcon from '@mui/icons-material/Explore'
import PeopleIcon from '@mui/icons-material/People'
import PersonIcon from '@mui/icons-material/Person'
import { useLocation, useNavigate } from 'react-router-dom'

const NAV_ITEMS = [
  { label: 'Fil', icon: <HomeIcon />, path: '/' },
  { label: 'Explorer', icon: <ExploreIcon />, path: '/explore' },
  { label: 'Amis', icon: <PeopleIcon />, path: '/friends' },
  { label: 'Profil', icon: <PersonIcon />, path: '/profile' },
]

export default function BottomNav() {
  const location = useLocation()
  const navigate = useNavigate()

  const currentValue = NAV_ITEMS.findIndex((item) =>
    item.path === '/'
      ? location.pathname === '/'
      : location.pathname.startsWith(item.path)
  )

  return (
    <Box
      sx={{
        position: 'fixed',
        bottom: 0,
        left: 0,
        right: 0,
        zIndex: (theme) => theme.zIndex.appBar,
        display: { xs: 'block', sm: 'none' },
      }}
    >
      <BottomNavigation
        value={currentValue === -1 ? null : currentValue}
        onChange={(_, newValue) => navigate(NAV_ITEMS[newValue].path)}
      >
        {NAV_ITEMS.map((item) => (
          <BottomNavigationAction
            key={item.path}
            label={item.label}
            icon={item.icon}
            showLabel
          />
        ))}
      </BottomNavigation>
    </Box>
  )
}
