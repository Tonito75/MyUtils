# Monster Hub — Frontend + Docker Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the full Monster Hub React frontend (MUI, React Router, Axios, infinite scroll) and Docker Compose production setup.

**Architecture:** React + Vite SPA with React Router v6. MUI v6 for all UI components. Axios with a JWT interceptor for API calls. Infinite scroll via IntersectionObserver. Auth state managed in a React Context. Each page is a self-contained component; shared logic lives in hooks and API modules.

**Tech Stack:** React 18, Vite, TypeScript, MUI v6, React Router v6, Axios, Docker (nginx), Docker Compose, Caddy

**Spec:** `docs/superpowers/specs/2026-03-21-monster-hub-design.md`
**Backend Plan:** `docs/superpowers/plans/2026-03-21-monster-hub-backend.md`

**Prerequisites:** Backend API must be running on `http://localhost:5000` for dev.

---

## File Map

```
front/
├── index.html
├── vite.config.ts
├── tsconfig.json
├── package.json
├── Dockerfile
│
└── src/
    ├── main.tsx
    ├── App.tsx
    │
    ├── api/
    │   ├── client.ts               ← Axios instance + JWT interceptor
    │   ├── auth.ts                 ← register, login
    │   ├── users.ts                ← me, updateProfile, uploadAvatar, search, userPhotos
    │   ├── photos.ts               ← feed, explore, analyze, publish, delete, like, unlike
    │   ├── friends.ts              ← list, requests, send, accept, delete
    │   ├── notifications.ts        ← list (mark all read)
    │   └── monsters.ts             ← list
    │
    ├── contexts/
    │   └── AuthContext.tsx         ← currentUser, token, login(), logout()
    │
    ├── hooks/
    │   ├── useInfiniteScroll.ts    ← IntersectionObserver-based pagination
    │   └── useNotifications.ts    ← poll + unread count
    │
    ├── components/
    │   ├── Layout/
    │   │   ├── AppLayout.tsx       ← Header + Outlet
    │   │   └── Header.tsx          ← nav + notifications bell + avatar
    │   ├── PhotoCard.tsx           ← single photo card (avatar, image, like)
    │   ├── PhotoGrid.tsx           ← masonry grid of PhotoCards (explore)
    │   ├── NotificationPopover.tsx ← bell dropdown with friend requests
    │   └── ProtectedRoute.tsx      ← redirects to /login if not authed
    │
    └── pages/
        ├── LoginPage.tsx
        ├── RegisterPage.tsx
        ├── FeedPage.tsx
        ├── ExplorePage.tsx
        ├── UploadPage.tsx
        ├── FriendsPage.tsx
        └── ProfilePage.tsx

(root of repo)
├── docker-compose.yml
└── Caddyfile
```

---

## Task 1: Project scaffold

**Files:**
- Create: `front/` (Vite project)
- Create: `front/src/main.tsx`
- Create: `front/src/App.tsx`

- [ ] **Step 1: Scaffold Vite + React + TypeScript project**

```bash
cd WebApps/PortalMonster
npm create vite@latest front -- --template react-ts
cd front
npm install
```

- [ ] **Step 2: Install dependencies**

```bash
npm install @mui/material @emotion/react @emotion/styled @mui/icons-material
npm install react-router-dom axios
npm install @mui/lab
```

- [ ] **Step 3: Replace `front/src/main.tsx`**

```tsx
import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.tsx'
import { BrowserRouter } from 'react-router-dom'
import { CssBaseline, ThemeProvider, createTheme } from '@mui/material'

const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: { main: '#00c853' },
    background: { default: '#0a0a0a', paper: '#1a1a1a' },
  },
  typography: { fontFamily: '"Inter", "Roboto", sans-serif' },
})

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <App />
      </ThemeProvider>
    </BrowserRouter>
  </React.StrictMode>,
)
```

- [ ] **Step 4: Run dev server to verify it starts**

```bash
npm run dev
```
Expected: Vite starts on `http://localhost:5173`.

- [ ] **Step 5: Commit**

```bash
git add front/
git commit -m "chore(front): scaffold React + Vite + MUI project"
```

---

## Task 2: API client + Auth context

**Files:**
- Create: `front/src/api/client.ts`
- Create: `front/src/api/auth.ts`
- Create: `front/src/contexts/AuthContext.tsx`
- Create: `front/src/components/ProtectedRoute.tsx`

- [ ] **Step 1: Create `front/src/api/client.ts`**

```ts
import axios from 'axios'

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5000',
})

client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

client.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('token')
      window.location.href = '/login'
    }
    return Promise.reject(err)
  }
)

export default client
```

- [ ] **Step 2: Create `front/src/api/auth.ts`**

```ts
import client from './client'

export const register = (username: string, email: string, password: string) =>
  client.post<{ token: string }>('/api/auth/register', { username, email, password })

export const login = (username: string, password: string) =>
  client.post<{ token: string }>('/api/auth/login', { username, password })
```

- [ ] **Step 3: Create `front/src/contexts/AuthContext.tsx`**

```tsx
import React, { createContext, useContext, useState, useEffect } from 'react'
import { jwtDecode } from 'jwt-decode' // npm install jwt-decode

interface JwtPayload { sub: string; unique_name: string; exp: number }
interface AuthUser { id: string; username: string }

interface AuthContextType {
  user: AuthUser | null
  token: string | null
  signIn: (token: string) => void
  signOut: () => void
}

const AuthContext = createContext<AuthContextType>(null!)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setToken] = useState<string | null>(localStorage.getItem('token'))
  const [user, setUser] = useState<AuthUser | null>(null)

  useEffect(() => {
    if (token) {
      try {
        const payload = jwtDecode<JwtPayload>(token)
        setUser({ id: payload.sub, username: payload.unique_name })
      } catch {
        setToken(null)
      }
    }
  }, [token])

  const signIn = (t: string) => {
    localStorage.setItem('token', t)
    setToken(t)
  }

  const signOut = () => {
    localStorage.removeItem('token')
    setToken(null)
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, token, signIn, signOut }}>
      {children}
    </AuthContext.Provider>
  )
}

export const useAuth = () => useContext(AuthContext)
```

- [ ] **Step 4: Install jwt-decode**

```bash
npm install jwt-decode
```

- [ ] **Step 5: Create `front/src/components/ProtectedRoute.tsx`**

```tsx
import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'

export default function ProtectedRoute() {
  const { token } = useAuth()
  return token ? <Outlet /> : <Navigate to="/login" replace />
}
```

- [ ] **Step 6: Commit**

```bash
git add front/src/api/ front/src/contexts/ front/src/components/ProtectedRoute.tsx
git commit -m "feat(front): add Axios client, auth API, AuthContext, ProtectedRoute"
```

---

## Task 3: API modules

**Files:**
- Create: `front/src/api/users.ts`
- Create: `front/src/api/photos.ts`
- Create: `front/src/api/friends.ts`
- Create: `front/src/api/notifications.ts`
- Create: `front/src/api/monsters.ts`

- [ ] **Step 1: Create `front/src/api/users.ts`**

```ts
import client from './client'
import type { PhotoDto } from './photos'

export interface UserProfile {
  id: string; username: string; email: string
  avatarUrl: string | null; photoCount: number; friendCount: number
}

export const getMe = () => client.get<UserProfile>('/api/users/me')

export const updateProfile = (data: {
  currentPassword: string; newEmail?: string; newPassword?: string
}) => client.put('/api/users/me', data)

export const uploadAvatar = (file: File) => {
  const form = new FormData()
  form.append('file', file)
  return client.put<{ avatarUrl: string }>('/api/users/me/avatar', form)
}

export const searchUsers = (q: string) =>
  client.get<Array<{ userId: string; username: string; avatarUrl: string | null }>>(
    '/api/users/search', { params: { q } })

export const getUserPhotos = (id: string, cursor?: number, limit = 10) =>
  client.get<PhotoDto[]>(`/api/users/${id}/photos`, { params: { cursor, limit } })
```

- [ ] **Step 2: Create `front/src/api/photos.ts`**

```ts
import client from './client'

export interface PhotoDto {
  id: number; imageUrl: string; userId: string; username: string
  avatarUrl: string | null; createdAt: string; monsterId: number
  monsterName: string; monsterEmoji: string; likesCount: number; likedByMe: boolean
}

export interface AnalyzeResult {
  monsterId: number; monsterName: string; emoji: string
}

export const getFeed = (cursor?: number, limit = 10) =>
  client.get<PhotoDto[]>('/api/photos/feed', { params: { cursor, limit } })

export const getExplore = (offset: number, seed: number, limit = 20, monsterIds?: number[]) =>
  client.get<PhotoDto[]>('/api/photos/explore', {
    params: { offset, limit, seed, monsterIds: monsterIds?.join(',') }
  })

export const analyzePhoto = (file: File) => {
  const form = new FormData()
  form.append('file', file)
  return client.post<AnalyzeResult>('/api/photos/analyze', form)
}

export const publishPhoto = (file: File, monsterId: number) => {
  const form = new FormData()
  form.append('file', file)
  form.append('monsterId', String(monsterId))
  return client.post<{ photoId: number; imageUrl: string }>('/api/photos', form)
}

export const deletePhoto = (id: number) => client.delete(`/api/photos/${id}`)

export const likePhoto = (id: number) =>
  client.post<{ likesCount: number }>(`/api/photos/${id}/like`)

export const unlikePhoto = (id: number) =>
  client.delete<{ likesCount: number }>(`/api/photos/${id}/like`)
```

- [ ] **Step 3: Create `front/src/api/friends.ts`**

```ts
import client from './client'

export interface FriendDto {
  userId: string; username: string; avatarUrl: string | null
}

export const getFriends = () => client.get<FriendDto[]>('/api/friends')
export const getFriendRequests = () => client.get<FriendDto[]>('/api/friends/requests')
export const sendFriendRequest = (userId: string) => client.post(`/api/friends/${userId}`)
export const acceptFriend = (userId: string) => client.put(`/api/friends/${userId}/accept`)
export const removeFriend = (userId: string) => client.delete(`/api/friends/${userId}`)
```

- [ ] **Step 4: Create `front/src/api/notifications.ts`**

```ts
import client from './client'

export interface NotificationDto {
  id: number; type: string; relatedUserId: string
  relatedUsername: string; relatedAvatarUrl: string | null; createdAt: string
}

export const getNotifications = () =>
  client.get<NotificationDto[]>('/api/notifications')
```

- [ ] **Step 5: Create `front/src/api/monsters.ts`**

```ts
import client from './client'

export interface MonsterOption { id: number; name: string; emoji: string }

export const getMonsters = () => client.get<MonsterOption[]>('/api/monsters')
```

- [ ] **Step 6: Commit**

```bash
git add front/src/api/
git commit -m "feat(front): add all API modules"
```

---

## Task 4: Hooks (infinite scroll + notifications)

**Files:**
- Create: `front/src/hooks/useInfiniteScroll.ts`
- Create: `front/src/hooks/useNotifications.ts`

- [ ] **Step 1: Create `front/src/hooks/useInfiniteScroll.ts`**

```ts
import { useRef, useCallback, useState } from 'react'

interface UseInfiniteScrollOptions<T> {
  fetcher: (cursor: T | undefined) => Promise<T[]>
  getNextCursor: (items: T[]) => T | undefined
}

export function useInfiniteScroll<T>({ fetcher, getNextCursor }: UseInfiniteScrollOptions<T>) {
  const [items, setItems] = useState<T[]>([])
  const [loading, setLoading] = useState(false)
  const [hasMore, setHasMore] = useState(true)
  const cursor = useRef<T | undefined>(undefined)
  const observer = useRef<IntersectionObserver | null>(null)

  const loadMore = useCallback(async () => {
    if (loading || !hasMore) return
    setLoading(true)
    try {
      const newItems = await fetcher(cursor.current)
      if (newItems.length === 0) {
        setHasMore(false)
      } else {
        cursor.current = getNextCursor(newItems)
        setItems((prev) => [...prev, ...newItems])
        if (!cursor.current) setHasMore(false)
      }
    } finally {
      setLoading(false)
    }
  }, [fetcher, getNextCursor, loading, hasMore])

  const sentinelRef = useCallback((node: HTMLDivElement | null) => {
    if (observer.current) observer.current.disconnect()
    if (!node) return
    observer.current = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting) loadMore()
    })
    observer.current.observe(node)
  }, [loadMore])

  return { items, loading, hasMore, sentinelRef, reset: () => {
    setItems([]); setHasMore(true); cursor.current = undefined
  }}
}
```

- [ ] **Step 2: Create `front/src/hooks/useNotifications.ts`**

Strategy: poll `/api/friends/requests` (pending requests = unread notifications proxy)
every 30 seconds. `markRead()` resets the local count after the popover fetches notifications.

```ts
import { useEffect, useState, useCallback } from 'react'
import { getFriendRequests } from '../api/friends'

export function useNotifications() {
  const [unreadCount, setUnreadCount] = useState(0)

  const fetchCount = useCallback(async () => {
    try {
      const res = await getFriendRequests()
      setUnreadCount(res.data.length)
    } catch { /* ignore */ }
  }, [])

  useEffect(() => {
    fetchCount()
    const interval = setInterval(fetchCount, 30_000)
    return () => clearInterval(interval)
  }, [fetchCount])

  const markRead = useCallback(() => setUnreadCount(0), [])

  return { unreadCount, markRead, refresh: fetchCount }
}
```

- [ ] **Step 3: Commit**

```bash
git add front/src/hooks/
git commit -m "feat(front): add useInfiniteScroll and useNotifications hooks"
```

---

## Task 5: Layout — Header + AppLayout

**Files:**
- Create: `front/src/components/Layout/Header.tsx`
- Create: `front/src/components/Layout/AppLayout.tsx`
- Create: `front/src/components/NotificationPopover.tsx`

- [ ] **Step 1: Create `front/src/components/NotificationPopover.tsx`**

```tsx
import { useState, useEffect } from 'react'
import {
  Popover, List, ListItem, ListItemText, ListItemAvatar, Avatar,
  Typography, Button, Stack, Divider, Box
} from '@mui/material'
import { getNotifications, type NotificationDto } from '../api/notifications'
import { acceptFriend, removeFriend } from '../api/friends'

interface Props {
  anchorEl: HTMLElement | null
  onClose: () => void
}

export default function NotificationPopover({ anchorEl, onClose }: Props) {
  const open = Boolean(anchorEl)
  const [notifications, setNotifications] = useState<NotificationDto[]>([])

  useEffect(() => {
    if (open) {
      getNotifications().then((res) => setNotifications(res.data))
    }
  }, [open])

  const handleAccept = async (userId: string, notifId: number) => {
    await acceptFriend(userId)
    setNotifications((prev) => prev.filter((n) => n.id !== notifId))
  }

  const handleDecline = async (userId: string, notifId: number) => {
    await removeFriend(userId)
    setNotifications((prev) => prev.filter((n) => n.id !== notifId))
  }

  return (
    <Popover open={open} anchorEl={anchorEl} onClose={onClose}
      anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      transformOrigin={{ vertical: 'top', horizontal: 'right' }}>
      <Box sx={{ width: 320, maxHeight: 400, overflowY: 'auto', p: 1 }}>
        {notifications.length === 0 ? (
          <Typography variant="body2" sx={{ p: 2, textAlign: 'center', color: 'text.secondary' }}>
            Aucune nouvelle notification
          </Typography>
        ) : (
          <List dense>
            {notifications.map((n, i) => (
              <Box key={n.id}>
                {i > 0 && <Divider />}
                <ListItem alignItems="flex-start">
                  <ListItemAvatar>
                    <Avatar src={n.relatedAvatarUrl ?? undefined}>
                      {n.relatedUsername.slice(0, 2).toUpperCase()}
                    </Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary={`${n.relatedUsername} vous a envoyé une demande d'ami`}
                    secondary={
                      <Stack direction="row" spacing={1} mt={0.5}>
                        <Button size="small" variant="contained" color="primary"
                          onClick={() => handleAccept(n.relatedUserId, n.id)}>
                          Accepter
                        </Button>
                        <Button size="small" variant="outlined" color="error"
                          onClick={() => handleDecline(n.relatedUserId, n.id)}>
                          Décliner
                        </Button>
                      </Stack>
                    }
                  />
                </ListItem>
              </Box>
            ))}
          </List>
        )}
      </Box>
    </Popover>
  )
}
```

- [ ] **Step 2: Create `front/src/components/Layout/Header.tsx`**

```tsx
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
```

- [ ] **Step 3: Create `front/src/components/Layout/AppLayout.tsx`**

```tsx
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
```

- [ ] **Step 4: Commit**

```bash
git add front/src/components/
git commit -m "feat(front): add Header, AppLayout, NotificationPopover"
```

---

## Task 6: App.tsx — routes wiring

**Files:**
- Modify: `front/src/App.tsx`

- [ ] **Step 1: Replace `front/src/App.tsx`**

```tsx
import { Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from './contexts/AuthContext'
import ProtectedRoute from './components/ProtectedRoute'
import AppLayout from './components/Layout/AppLayout'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import FeedPage from './pages/FeedPage'
import ExplorePage from './pages/ExplorePage'
import UploadPage from './pages/UploadPage'
import FriendsPage from './pages/FriendsPage'
import ProfilePage from './pages/ProfilePage'

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<AppLayout />}>
            <Route path="/" element={<FeedPage />} />
            <Route path="/explore" element={<ExplorePage />} />
            <Route path="/upload" element={<UploadPage />} />
            <Route path="/friends" element={<FriendsPage />} />
            <Route path="/profile" element={<ProfilePage />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AuthProvider>
  )
}
```

- [ ] **Step 2: Create placeholder pages so the app builds**

Create each of these with a minimal placeholder (fill in for real in later tasks):

`front/src/pages/LoginPage.tsx`:
```tsx
export default function LoginPage() { return <div>Login</div> }
```

Repeat for `RegisterPage.tsx`, `FeedPage.tsx`, `ExplorePage.tsx`, `UploadPage.tsx`, `FriendsPage.tsx`, `ProfilePage.tsx`.

- [ ] **Step 3: Run dev server and verify routes navigate without errors**

```bash
npm run dev
# Visit http://localhost:5173 — should redirect to /login
```

- [ ] **Step 4: Commit**

```bash
git add front/src/App.tsx front/src/pages/
git commit -m "feat(front): wire routes in App.tsx with placeholder pages"
```

---

## Task 7: Login + Register pages

**Files:**
- Modify: `front/src/pages/LoginPage.tsx`
- Modify: `front/src/pages/RegisterPage.tsx`

- [ ] **Step 1: Implement `front/src/pages/LoginPage.tsx`**

```tsx
import { useState } from 'react'
import { Box, Button, TextField, Typography, Paper, Link, Alert } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { login } from '../api/auth'
import { useAuth } from '../contexts/AuthContext'

export default function LoginPage() {
  const [username, setUsername] = useState('')
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
      const res = await login(username, password)
      signIn(res.data.token)
      navigate('/')
    } catch {
      setError('Identifiants incorrects.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center',
      justifyContent: 'center', bgcolor: 'background.default' }}>
      <Paper sx={{ p: 4, width: 360 }} elevation={4}>
        <Typography variant="h5" fontWeight={900} color="primary" mb={3} textAlign="center">
          Monster Hub 🥤
        </Typography>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <TextField label="Nom d'utilisateur" value={username}
            onChange={(e) => setUsername(e.target.value)} required fullWidth />
          <TextField label="Mot de passe" type="password" value={password}
            onChange={(e) => setPassword(e.target.value)} required fullWidth />
          <Button type="submit" variant="contained" fullWidth disabled={loading}>
            {loading ? 'Connexion...' : 'Se connecter'}
          </Button>
        </Box>
        <Typography variant="body2" textAlign="center" mt={2}>
          Pas de compte ?{' '}
          <Link href="/register" underline="hover">Créer un compte</Link>
        </Typography>
      </Paper>
    </Box>
  )
}
```

- [ ] **Step 2: Implement `front/src/pages/RegisterPage.tsx`**

```tsx
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
```

- [ ] **Step 3: Commit**

```bash
git add front/src/pages/LoginPage.tsx front/src/pages/RegisterPage.tsx
git commit -m "feat(front): implement login and register pages"
```

---

## Task 8: PhotoCard component

**Files:**
- Create: `front/src/components/PhotoCard.tsx`

- [ ] **Step 1: Create `front/src/components/PhotoCard.tsx`**

```tsx
import { useState } from 'react'
import { Card, CardHeader, CardMedia, CardActions, Avatar,
  IconButton, Typography, Box } from '@mui/material'
import FavoriteIcon from '@mui/icons-material/Favorite'
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder'
import type { PhotoDto } from '../api/photos'
import { likePhoto, unlikePhoto } from '../api/photos'

interface Props { photo: PhotoDto }

export default function PhotoCard({ photo }: Props) {
  const [liked, setLiked] = useState(photo.likedByMe)
  const [count, setCount] = useState(photo.likesCount)

  const toggleLike = async () => {
    try {
      if (liked) {
        const res = await unlikePhoto(photo.id)
        setLiked(false)
        setCount(res.data.likesCount)
      } else {
        const res = await likePhoto(photo.id)
        setLiked(true)
        setCount(res.data.likesCount)
      }
    } catch { /* ignore */ }
  }

  return (
    <Card sx={{ mb: 2, bgcolor: 'background.paper' }}>
      <CardHeader
        avatar={
          <Avatar src={photo.avatarUrl ?? undefined} sx={{ bgcolor: 'primary.main' }}>
            {photo.username.slice(0, 2).toUpperCase()}
          </Avatar>
        }
        title={photo.username}
        subheader={
          <Box component="span" sx={{ color: 'text.secondary', fontSize: 13 }}>
            {photo.monsterEmoji} {photo.monsterName}
          </Box>
        }
      />
      <CardMedia
        component="img"
        image={photo.imageUrl}
        alt={photo.monsterName}
        sx={{ maxHeight: 480, objectFit: 'contain', bgcolor: '#111' }}
      />
      <CardActions>
        <IconButton onClick={toggleLike} color={liked ? 'error' : 'default'}>
          {liked ? <FavoriteIcon /> : <FavoriteBorderIcon />}
        </IconButton>
        <Typography variant="body2" color="text.secondary">{count}</Typography>
      </CardActions>
    </Card>
  )
}
```

- [ ] **Step 2: Commit**

```bash
git add front/src/components/PhotoCard.tsx
git commit -m "feat(front): add PhotoCard component with like toggle"
```

---

## Task 9: Feed page

**Files:**
- Modify: `front/src/pages/FeedPage.tsx`

- [ ] **Step 1: Implement `front/src/pages/FeedPage.tsx`**

```tsx
import { useCallback } from 'react'
import { Typography, CircularProgress, Box } from '@mui/material'
import { getFeed, type PhotoDto } from '../api/photos'
import PhotoCard from '../components/PhotoCard'
import { useInfiniteScroll } from '../hooks/useInfiniteScroll'

export default function FeedPage() {
  const fetcher = useCallback(async (cursor?: number) => {
    const res = await getFeed(cursor, 10)
    return res.data
  }, [])

  const getNextCursor = useCallback((items: PhotoDto[]) =>
    items.length > 0 ? items[items.length - 1].id : undefined, [])

  const { items, loading, hasMore, sentinelRef } = useInfiniteScroll({ fetcher, getNextCursor })

  return (
    <Box>
      <Typography variant="h6" mb={2} fontWeight={700}>Mon fil</Typography>
      {items.map((photo) => <PhotoCard key={photo.id} photo={photo} />)}
      {items.length === 0 && !loading && (
        <Typography color="text.secondary" textAlign="center" mt={4}>
          Aucune photo pour le moment. Ajoutez des amis pour voir leur fil !
        </Typography>
      )}
      <Box ref={sentinelRef} sx={{ py: 2, textAlign: 'center' }}>
        {loading && <CircularProgress size={24} />}
        {!hasMore && items.length > 0 && (
          <Typography variant="body2" color="text.secondary">C'est tout !</Typography>
        )}
      </Box>
    </Box>
  )
}
```

- [ ] **Step 2: Commit**

```bash
git add front/src/pages/FeedPage.tsx
git commit -m "feat(front): implement feed page with infinite scroll"
```

---

## Task 10: Explore page

**Files:**
- Modify: `front/src/pages/ExplorePage.tsx`
- Create: `front/src/components/PhotoGrid.tsx`

- [ ] **Step 1: Create `front/src/components/PhotoGrid.tsx`**

```tsx
import { ImageList, ImageListItem, Box } from '@mui/material'
import type { PhotoDto } from '../api/photos'
import FavoriteIcon from '@mui/icons-material/Favorite'
import { useState } from 'react'
import { likePhoto, unlikePhoto } from '../api/photos'

interface Props { photos: PhotoDto[] }

export default function PhotoGrid({ photos }: Props) {
  const [likeState, setLikeState] = useState<Record<number, { liked: boolean; count: number }>>(
    () => Object.fromEntries(photos.map((p) => [p.id, { liked: p.likedByMe, count: p.likesCount }]))
  )

  const toggleLike = async (photo: PhotoDto) => {
    const current = likeState[photo.id]
    try {
      if (current?.liked) {
        const res = await unlikePhoto(photo.id)
        setLikeState((s) => ({ ...s, [photo.id]: { liked: false, count: res.data.likesCount } }))
      } else {
        const res = await likePhoto(photo.id)
        setLikeState((s) => ({ ...s, [photo.id]: { liked: true, count: res.data.likesCount } }))
      }
    } catch { /* ignore */ }
  }

  return (
    <ImageList variant="masonry" cols={3} gap={4}>
      {photos.map((photo) => {
        const state = likeState[photo.id] ?? { liked: photo.likedByMe, count: photo.likesCount }
        return (
          <ImageListItem key={photo.id} sx={{ position: 'relative', cursor: 'pointer' }}>
            <img src={photo.imageUrl} alt={photo.monsterName} loading="lazy"
              style={{ borderRadius: 4 }} />
            <Box onClick={() => toggleLike(photo)}
              sx={{ position: 'absolute', bottom: 4, right: 4, display: 'flex',
                alignItems: 'center', gap: 0.5, bgcolor: 'rgba(0,0,0,0.6)',
                borderRadius: 1, px: 0.75, py: 0.25 }}>
              <FavoriteIcon sx={{ fontSize: 14, color: state.liked ? 'error.main' : 'white' }} />
              <Box component="span" sx={{ color: 'white', fontSize: 12 }}>{state.count}</Box>
            </Box>
          </ImageListItem>
        )
      })}
    </ImageList>
  )
}
```

- [ ] **Step 2: Implement `front/src/pages/ExplorePage.tsx`**

```tsx
import { useCallback, useEffect, useRef, useState } from 'react'
import { Box, Typography, CircularProgress, Chip, Stack } from '@mui/material'
import { getExplore, type PhotoDto } from '../api/photos'
import { getMonsters, type MonsterOption } from '../api/monsters'
import PhotoGrid from '../components/PhotoGrid'

export default function ExplorePage() {
  const [photos, setPhotos] = useState<PhotoDto[]>([])
  const [monsters, setMonsters] = useState<MonsterOption[]>([])
  const [selectedMonsters, setSelectedMonsters] = useState<number[]>([])
  const [loading, setLoading] = useState(false)
  const [hasMore, setHasMore] = useState(true)
  const offset = useRef(0)
  const seed = useRef(Math.floor(Math.random() * 999983) + 1)
  const sentinel = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    getMonsters().then((res) => setMonsters(res.data))
  }, [])

  const loadMore = useCallback(async () => {
    if (loading || !hasMore) return
    setLoading(true)
    try {
      const res = await getExplore(offset.current, seed.current, 20,
        selectedMonsters.length > 0 ? selectedMonsters : undefined)
      if (res.data.length === 0) {
        setHasMore(false)
      } else {
        offset.current += res.data.length
        setPhotos((prev) => [...prev, ...res.data])
      }
    } finally {
      setLoading(false)
    }
  }, [loading, hasMore, selectedMonsters])

  // Reset on filter change
  useEffect(() => {
    setPhotos([])
    setHasMore(true)
    offset.current = 0
    seed.current = Math.floor(Math.random() * 999983) + 1
  }, [selectedMonsters])

  // Trigger first load when hasMore resets
  useEffect(() => {
    if (hasMore && photos.length === 0) loadMore()
  }, [hasMore, photos.length])

  // IntersectionObserver
  useEffect(() => {
    const el = sentinel.current
    if (!el) return
    const obs = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting) loadMore()
    })
    obs.observe(el)
    return () => obs.disconnect()
  }, [loadMore])

  const toggleMonster = (id: number) =>
    setSelectedMonsters((prev) =>
      prev.includes(id) ? prev.filter((m) => m !== id) : [...prev, id])

  return (
    <Box>
      <Typography variant="h6" mb={2} fontWeight={700}>Explorer</Typography>
      <Stack direction="row" flexWrap="wrap" gap={1} mb={2}>
        {monsters.map((m) => (
          <Chip key={m.id} label={`${m.emoji} ${m.name}`} size="small"
            color={selectedMonsters.includes(m.id) ? 'primary' : 'default'}
            onClick={() => toggleMonster(m.id)} />
        ))}
      </Stack>
      <PhotoGrid photos={photos} />
      <Box ref={sentinel} sx={{ py: 2, textAlign: 'center' }}>
        {loading && <CircularProgress size={24} />}
        {!hasMore && photos.length > 0 && (
          <Typography variant="body2" color="text.secondary">C'est tout !</Typography>
        )}
      </Box>
    </Box>
  )
}
```

- [ ] **Step 3: Commit**

```bash
git add front/src/pages/ExplorePage.tsx front/src/components/PhotoGrid.tsx
git commit -m "feat(front): implement explore page with masonry grid and monster filters"
```

---

## Task 11: Upload page

**Files:**
- Modify: `front/src/pages/UploadPage.tsx`

- [ ] **Step 1: Implement `front/src/pages/UploadPage.tsx`**

```tsx
import { useState, useRef } from 'react'
import { Box, Button, Typography, Paper, CircularProgress, Alert, Stack } from '@mui/material'
import CameraAltIcon from '@mui/icons-material/CameraAlt'
import { useNavigate } from 'react-router-dom'
import { analyzePhoto, publishPhoto, type AnalyzeResult } from '../api/photos'

type Step = 'select' | 'detected' | 'error'

export default function UploadPage() {
  const [step, setStep] = useState<Step>('select')
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<string | null>(null)
  const [analyzing, setAnalyzing] = useState(false)
  const [publishing, setPublishing] = useState(false)
  const [result, setResult] = useState<AnalyzeResult | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const navigate = useNavigate()

  const handleFile = async (f: File) => {
    setFile(f)
    setPreview(URL.createObjectURL(f))
    setAnalyzing(true)
    setStep('select')
    try {
      const res = await analyzePhoto(f)
      setResult(res.data)
      setStep('detected')
    } catch (err: any) {
      setStep('error')
    } finally {
      setAnalyzing(false)
    }
  }

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0]
    if (f) handleFile(f)
  }

  const handlePublish = async () => {
    if (!file || !result) return
    setPublishing(true)
    try {
      await publishPhoto(file, result.monsterId)
      navigate('/')
    } catch {
      /* show error */
    } finally {
      setPublishing(false)
    }
  }

  const handleRetry = () => {
    setStep('select')
    setFile(null)
    setPreview(null)
    setResult(null)
    if (inputRef.current) inputRef.current.value = ''
  }

  return (
    <Box sx={{ maxWidth: 480, mx: 'auto' }}>
      <Typography variant="h6" mb={2} fontWeight={700}>Publier une photo</Typography>

      <Paper sx={{ p: 3 }}>
        {/* Step 1: Select */}
        {step === 'select' && !analyzing && (
          <Box sx={{ textAlign: 'center' }}>
            <CameraAltIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
            <Typography color="text.secondary" mb={2}>
              Prends une photo de ta canette Monster
            </Typography>
            <Button variant="contained" onClick={() => inputRef.current?.click()}>
              Choisir une photo
            </Button>
            <input ref={inputRef} type="file" accept="image/*"
              style={{ display: 'none' }} onChange={handleInputChange} />
          </Box>
        )}

        {/* Analyzing */}
        {analyzing && (
          <Box sx={{ textAlign: 'center', py: 4 }}>
            {preview && <img src={preview} style={{ maxWidth: '100%', maxHeight: 300,
              borderRadius: 8, marginBottom: 16 }} alt="preview" />}
            <CircularProgress />
            <Typography mt={2} color="text.secondary">Identification en cours...</Typography>
          </Box>
        )}

        {/* Step 2a: Detected */}
        {step === 'detected' && result && (
          <Box sx={{ textAlign: 'center' }}>
            {preview && <img src={preview} style={{ maxWidth: '100%', maxHeight: 300,
              borderRadius: 8, marginBottom: 16 }} alt="preview" />}
            <Alert severity="success" sx={{ mb: 2 }}>
              Monster détecté : <strong>{result.emoji} {result.monsterName}</strong>
            </Alert>
            <Stack direction="row" spacing={2} justifyContent="center">
              <Button variant="outlined" onClick={handleRetry}>Reprendre</Button>
              <Button variant="contained" onClick={handlePublish} disabled={publishing}>
                {publishing ? 'Publication...' : 'Publier'}
              </Button>
            </Stack>
          </Box>
        )}

        {/* Step 2b: Error */}
        {step === 'error' && (
          <Box sx={{ textAlign: 'center' }}>
            {preview && <img src={preview} style={{ maxWidth: '100%', maxHeight: 300,
              borderRadius: 8, marginBottom: 16 }} alt="preview" />}
            <Alert severity="error" sx={{ mb: 2 }}>
              Aucune canette de Monster détectée sur cette photo.
            </Alert>
            <Button variant="outlined" onClick={handleRetry}>Réessayer</Button>
          </Box>
        )}
      </Paper>
    </Box>
  )
}
```

- [ ] **Step 2: Commit**

```bash
git add front/src/pages/UploadPage.tsx
git commit -m "feat(front): implement upload page with analyze → confirm flow"
```

---

## Task 12: Friends page

**Files:**
- Modify: `front/src/pages/FriendsPage.tsx`

- [ ] **Step 1: Implement `front/src/pages/FriendsPage.tsx`**

```tsx
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
```

- [ ] **Step 2: Commit**

```bash
git add front/src/pages/FriendsPage.tsx
git commit -m "feat(front): implement friends page (list, requests, search)"
```

---

## Task 13: Profile page

**Files:**
- Modify: `front/src/pages/ProfilePage.tsx`

- [ ] **Step 1: Implement `front/src/pages/ProfilePage.tsx`**

```tsx
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
        <Box sx={{ position: 'relative' }}>
          <Avatar src={profile?.avatarUrl ?? undefined}
            sx={{ width: 72, height: 72, bgcolor: 'primary.main', fontSize: 28, cursor: 'pointer' }}
            onClick={() => avatarInputRef.current?.click()}>
            {user?.username.slice(0, 2).toUpperCase()}
          </Avatar>
          <input ref={avatarInputRef} type="file" accept="image/*"
            style={{ display: 'none' }} onChange={handleAvatarChange} />
        </Box>
        <Box sx={{ flexGrow: 1 }}>
          <Typography variant="h6" fontWeight={700}>{user?.username}</Typography>
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
```

- [ ] **Step 2: Commit**

```bash
git add front/src/pages/ProfilePage.tsx
git commit -m "feat(front): implement profile page with photo grid and edit dialog"
```

---

## Task 14: Full build verification

- [ ] **Step 1: Build the frontend**

```bash
cd front
npm run build
```
Expected: Build succeeds in `dist/` with no TypeScript errors.

- [ ] **Step 2: Test against the running backend**

```bash
npm run dev
```

Manual test sequence:
1. Register a new account → redirected to feed
2. Upload a photo with a Monster can → analyze succeeds, publish works
3. Explore page shows photos with monster filter chips
4. Friends page: search user, send request, accept in the other account
5. Notifications bell shows and marks as read

---

## Task 15: Dockerfile (frontend)

**Files:**
- Create: `front/Dockerfile`
- Create: `front/nginx.conf`

- [ ] **Step 1: Create `front/nginx.conf`**

```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff2?)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

- [ ] **Step 2: Create `front/Dockerfile`**

```dockerfile
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

- [ ] **Step 3: Add `VITE_API_URL` handling**

In `front/.env.production`:
```
VITE_API_URL=
```
(Empty = relative URL, since Caddy proxies `/api/*` to the backend on the same domain.)

In `front/.env.development`:
```
VITE_API_URL=http://localhost:5000
```

- [ ] **Step 4: Commit**

```bash
git add front/Dockerfile front/nginx.conf front/.env.development front/.env.production
git commit -m "feat(front): add Dockerfile and nginx config for production"
```

---

## Task 16: Docker Compose + Caddyfile

**Files:**
- Create: `docker-compose.yml` (at `WebApps/PortalMonster/`)
- Create: `Caddyfile`

- [ ] **Step 1: Create `docker-compose.yml`**

```yaml
services:
  back:
    build: ./back
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
      - Jwt__Secret=${JWT_SECRET}
      - Mistral__ApiKey=${MISTRAL_API_KEY}
      - Ftp__Host=${FTP_HOST}
      - Ftp__Port=${FTP_PORT:-21}
      - Ftp__UserName=${FTP_USER}
      - Ftp__Password=${FTP_PASSWORD}
      - Ftp__BaseRemotePath=${FTP_BASE_PATH:-/monsterhub}
    restart: unless-stopped

  front:
    build: ./front
    restart: unless-stopped

  caddy:
    image: caddy:2-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile
      - caddy_data:/data
      - caddy_config:/config
    depends_on:
      - back
      - front
    restart: unless-stopped

volumes:
  caddy_data:
  caddy_config:
```

- [ ] **Step 2: Create `.env.example` at project root**

```bash
DB_CONNECTION_STRING=Server=host.docker.internal;Database=MonsterHub;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True
JWT_SECRET=CHANGE_ME_TO_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS
MISTRAL_API_KEY=your_mistral_api_key
FTP_HOST=192.168.1.X
FTP_PORT=21
FTP_USER=ftpuser
FTP_PASSWORD=ftppassword
FTP_BASE_PATH=/monsterhub
```

- [ ] **Step 3: Create `Caddyfile`**

```
your-domain.com {
    request_body max_size 10MB

    reverse_proxy /api/* back:8080  # port 8080 = ASP.NET Core default in Docker (matches Dockerfile EXPOSE 8080)
    reverse_proxy * front:80
}
```

Replace `your-domain.com` with actual domain. For local testing, use `localhost` (no TLS).

- [ ] **Step 4: Add `.gitignore` entry for `.env`**

In root `.gitignore`:
```
.env
```

- [ ] **Step 5: Build and verify Docker Compose**

```bash
cp .env.example .env
# fill in real values
docker compose build
docker compose up
```
Expected: All three containers start. `http://localhost` serves the frontend.

- [ ] **Step 6: Commit**

```bash
git add docker-compose.yml Caddyfile .env.example
git commit -m "feat: add Docker Compose and Caddyfile for production deployment"
```
