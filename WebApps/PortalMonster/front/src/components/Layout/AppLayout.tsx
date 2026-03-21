import { Outlet, useLocation } from 'react-router-dom'
import { Box } from '@mui/material'
import Header from './Header'
import BottomNav from './BottomNav'

const WIDE_ROUTES = ['/explore']

export default function AppLayout() {
  const location = useLocation()
  const isWide = WIDE_ROUTES.some((r) => location.pathname.startsWith(r))

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Header />
      <Box
        component="main"
        sx={{
          maxWidth: isWide ? 900 : 480,
          mx: 'auto',
          px: { xs: 0, sm: 2 },
          py: 2,
          pb: { xs: '72px', sm: 2 },
        }}
      >
        <Outlet />
      </Box>
      <BottomNav />
    </Box>
  )
}
