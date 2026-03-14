import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { CircularProgress, Box } from '@mui/material'
import { api } from './api/client'
import LoginPage from './pages/LoginPage'
import HomePage from './pages/HomePage'
import CameraHistoryPage from './pages/CameraHistoryPage'
import AllStreamsPage from './pages/AllStreamsPage'

function RequireAuth({ children }: { children: React.ReactNode }) {
  const [auth, setAuth] = useState<boolean | null>(null)

  useEffect(() => {
    api.me().then(r => setAuth(r.isAuthenticated)).catch(() => setAuth(false))
  }, [])

  if (auth === null)
    return (
      <Box display="flex" justifyContent="center" alignItems="center" height="100vh">
        <CircularProgress />
      </Box>
    )

  return auth ? <>{children}</> : <Navigate to="/login" replace />
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<RequireAuth><HomePage /></RequireAuth>} />
        <Route path="/camerahistory/:name" element={<RequireAuth><CameraHistoryPage /></RequireAuth>} />
        <Route path="/streams" element={<RequireAuth><AllStreamsPage /></RequireAuth>} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
