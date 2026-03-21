import { Outlet } from 'react-router-dom'
import { Box } from '@mui/material'
import Header from './Header'

export default function AppLayout() {
  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Header />
      <Box component="main" sx={{ maxWidth: 640, mx: 'auto', px: 2, py: 3 }}>
        <Outlet />
      </Box>
    </Box>
  )
}
