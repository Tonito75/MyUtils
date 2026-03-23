# Frontend Design Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Overhaul the PortalMonster frontend with a dark fantasy violet/rose gradient theme, enriched photo grid, bottom sheet monster filters, and mobile bottom navigation.

**Architecture:** All design tokens (colors, gradients, shadows, radii, component overrides) are centralized in `createTheme` in `main.tsx`. Component `sx` props are reserved for layout-only overrides (margins, padding, flex/grid). Two new components are introduced: `BottomNav` (mobile nav) and `GridPhotoCard` (enriched explore grid card).

**Tech Stack:** React 18, MUI v5 (Material UI), React Router v6, TypeScript, Vite

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `front/src/main.tsx` | Modify | Theme: palette, gradients, all component overrides |
| `front/src/components/Layout/AppLayout.tsx` | Modify | Responsive layout: narrow vs wide slots, BottomNav mount |
| `front/src/components/Layout/Header.tsx` | Modify | Gradient logo + button, hide nav links on mobile |
| `front/src/components/Layout/BottomNav.tsx` | **Create** | Mobile bottom navigation (xs only) |
| `front/src/components/GridPhotoCard.tsx` | **Create** | Enriched explore grid card (image + monster info + likes) |
| `front/src/components/PhotoGrid.tsx` | Modify | Use GridPhotoCard, responsive 2/3 cols, 12px gap |
| `front/src/components/PhotoCard.tsx` | Modify | Feed card: gradient avatar ring, monster pill, hover lift |
| `front/src/pages/ExplorePage.tsx` | Modify | Bottom sheet filters, wide layout, filter badge button |

---

## Task 1: Rewrite the MUI Theme

**Files:**
- Modify: `front/src/main.tsx`

This is the foundation. All subsequent tasks inherit from it.

- [ ] **Step 1: Replace `createTheme` call with the new theme**

Replace the entire `createTheme({...})` block in `front/src/main.tsx` with:

```typescript
const gradientPrimary = 'linear-gradient(135deg, #7c3aed 0%, #ec4899 100%)'

const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: { main: '#7c3aed', light: '#9d63f5', dark: '#5b21b6' },
    secondary: { main: '#ec4899', light: '#f472b6', dark: '#be185d' },
    background: { default: '#060611', paper: '#0f0f1a' },
    text: { primary: '#f0f0f0', secondary: '#8888aa' },
    divider: '#1a1a2e',
    error: { main: '#f43f5e' },
  },
  typography: {
    fontFamily: '"Inter", "Roboto", sans-serif',
    fontWeightMedium: 500,
  },
  shape: { borderRadius: 12 },
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        ':root': {
          '--gradient-primary': gradientPrimary,
        },
        body: {
          scrollbarWidth: 'thin',
          scrollbarColor: '#1a1a2e transparent',
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          background: 'rgba(6, 6, 17, 0.85)',
          backdropFilter: 'blur(12px)',
          borderBottom: '1px solid #1a1a2e',
          boxShadow: 'none',
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        containedPrimary: {
          background: gradientPrimary,
          color: '#fff',
          fontWeight: 700,
          '&:hover': { opacity: 0.88, background: gradientPrimary },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          border: '1px solid #1a1a2e',
          transition: 'box-shadow 0.2s ease, transform 0.2s ease',
          '&:hover': {
            boxShadow: '0 0 0 1px #7c3aed40, 0 8px 24px rgba(124,58,237,0.15)',
            transform: 'translateY(-2px)',
          },
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: { backgroundImage: 'none' },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          borderRadius: 20,
          fontWeight: 500,
        },
        colorPrimary: {
          background: gradientPrimary,
          color: '#fff',
          border: 'none',
        },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          '& .MuiOutlinedInput-notchedOutline': { borderColor: '#2a2a4a' },
          '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: '#7c3aed80' },
          '&.Mui-focused .MuiOutlinedInput-notchedOutline': { borderColor: '#7c3aed' },
        },
      },
    },
    MuiBottomNavigation: {
      styleOverrides: {
        root: {
          background: '#0a0a1a',
          borderTop: '1px solid #1a1a2e',
          height: 56,
        },
      },
    },
    MuiBottomNavigationAction: {
      styleOverrides: {
        root: {
          color: '#8888aa',
          '&.Mui-selected': {
            background: gradientPrimary,
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            backgroundClip: 'text',
          },
          '&.Mui-selected .MuiBottomNavigationAction-label': {
            fontSize: '0.7rem',
          },
          '& .MuiSvgIcon-root': {
            fontSize: 24,
          },
        },
      },
    },
    MuiDrawer: {
      styleOverrides: {
        paper: {
          background: '#0f0f1a',
          borderTop: '1px solid #1a1a2e',
        },
      },
    },
    MuiDialog: {
      styleOverrides: {
        paper: {
          background: '#0f0f1a',
          border: '1px solid #1a1a2e',
        },
      },
    },
    MuiBadge: {
      styleOverrides: {
        badge: {
          background: gradientPrimary,
          color: '#fff',
          fontWeight: 700,
          fontSize: 10,
          minWidth: 16,
          height: 16,
        },
      },
    },
  },
})
```

- [ ] **Step 2: Verify the app still compiles**

```bash
cd front && npm run build 2>&1 | tail -20
```

Expected: no TypeScript errors, build succeeds.

- [ ] **Step 3: Start dev server and visually verify base colors changed**

```bash
cd front && npm run dev
```

Open browser. Background should be near-black with purple tint (`#060611`). No green elements.

- [ ] **Step 4: Commit**

```bash
git add front/src/main.tsx
git commit -m "feat(theme): violet/rose dark fantasy gradient theme"
```

---

## Task 2: Create BottomNav Component

**Files:**
- Create: `front/src/components/Layout/BottomNav.tsx`

- [ ] **Step 1: Create the file**

```typescript
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
        value={currentValue === -1 ? false : currentValue}
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
```

- [ ] **Step 2: Verify it compiles**

```bash
cd front && npx tsc --noEmit 2>&1 | head -30
```

Expected: no errors on this file.

- [ ] **Step 3: Commit**

```bash
git add front/src/components/Layout/BottomNav.tsx
git commit -m "feat(layout): add mobile bottom navigation component"
```

---

## Task 3: Update AppLayout for Responsive Layout

**Files:**
- Modify: `front/src/components/Layout/AppLayout.tsx`

AppLayout needs to:
1. Show `BottomNav` on mobile (xs) and `Header` on all sizes (but Header hides nav links on xs)
2. Add bottom padding on mobile to avoid content going under BottomNav
3. Support a wide slot for `/explore` (900px) vs default 480px

- [ ] **Step 1: Replace AppLayout content**

```typescript
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
```

- [ ] **Step 2: Verify in browser**

On mobile viewport (< 600px): bottom nav visible at bottom, content not clipped under it.
On desktop: no bottom nav, content at max 480px or 900px depending on route.

- [ ] **Step 3: Commit**

```bash
git add front/src/components/Layout/AppLayout.tsx
git commit -m "feat(layout): responsive layout with wide slot and mobile bottom padding"
```

---

## Task 4: Update Header

**Files:**
- Modify: `front/src/components/Layout/Header.tsx`

Changes:
- Logo uses CSS gradient text
- "Publier" button uses gradient (via `variant="contained"` + theme override)
- Nav links hidden on mobile (moved to BottomNav)
- Active underline uses gradient

- [ ] **Step 1: Replace Header content**

```typescript
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
                    '&:hover': { color: '#bbb', WebkitTextFillColor: 'unset' },
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
                padding: '2px',
                '& img': { borderRadius: '50%' },
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
```

- [ ] **Step 2: Verify in browser**

Desktop: gradient logo, gradient active nav link, gradient "Publier" button.
Mobile: logo visible, nav links hidden, only avatar + bell + Publier button visible.

- [ ] **Step 3: Commit**

```bash
git add front/src/components/Layout/Header.tsx
git commit -m "feat(header): gradient logo, responsive nav, gradient publish button"
```

---

## Task 5: Create GridPhotoCard Component

**Files:**
- Create: `front/src/components/GridPhotoCard.tsx`

This is the enriched card for the Explore grid: image (square) + monster info + like count always visible below.

- [ ] **Step 1: Create the file**

```typescript
import { Box, Typography, IconButton } from '@mui/material'
import FavoriteIcon from '@mui/icons-material/Favorite'
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder'
import type { PhotoDto } from '../api/photos'

interface Props {
  photo: PhotoDto
  liked: boolean
  count: number
  onToggleLike: () => void
}

export default function GridPhotoCard({ photo, liked, count, onToggleLike }: Props) {
  return (
    <Box
      sx={{
        borderRadius: 2,
        overflow: 'hidden',
        bgcolor: 'background.paper',
        border: '1px solid',
        borderColor: 'divider',
        display: 'flex',
        flexDirection: 'column',
        transition: 'box-shadow 0.2s ease, transform 0.2s ease',
        '&:hover': {
          boxShadow: '0 0 0 1px #7c3aed40, 0 8px 24px rgba(124,58,237,0.15)',
          transform: 'translateY(-2px)',
        },
        '&:hover .img-overlay': { opacity: 1 },
      }}
    >
      {/* Image */}
      <Box sx={{ position: 'relative', width: '100%', aspectRatio: '1/1', overflow: 'hidden', bgcolor: '#0a0a18' }}>
        <img
          src={photo.imageUrl}
          alt={photo.monsterName}
          loading="lazy"
          style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }}
        />
        {/* Hover overlay */}
        <Box
          className="img-overlay"
          sx={{
            position: 'absolute',
            inset: 0,
            background: 'linear-gradient(135deg, rgba(124,58,237,0.5) 0%, rgba(236,72,153,0.5) 100%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: 0.5,
            opacity: 0,
            transition: 'opacity 0.18s ease',
          }}
        >
          <FavoriteIcon sx={{ fontSize: 20, color: liked ? '#ff3b5c' : 'white' }} />
          <Box component="span" sx={{ color: 'white', fontWeight: 700, fontSize: 14 }}>
            {count}
          </Box>
        </Box>
      </Box>

      {/* Footer: monster info + like */}
      <Box
        sx={{
          px: 1,
          py: 0.75,
          display: 'flex',
          alignItems: 'center',
          gap: 0.5,
          minWidth: 0,
        }}
      >
        {/* Monster badge */}
        <Box
          sx={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 0.25,
            px: 0.75,
            py: 0.25,
            borderRadius: '20px',
            background: 'var(--gradient-primary)',
            flexShrink: 0,
          }}
        >
          <Typography sx={{ fontSize: 11, lineHeight: 1 }}>{photo.monsterEmoji}</Typography>
          <Typography sx={{ fontSize: 10, fontWeight: 700, color: '#fff', lineHeight: 1, whiteSpace: 'nowrap' }}>
            {photo.monsterName}
          </Typography>
        </Box>

        <Box sx={{ flexGrow: 1 }} />

        {/* Like button */}
        <IconButton
          size="small"
          onClick={(e) => { e.stopPropagation(); onToggleLike() }}
          sx={{
            color: liked ? '#ec4899' : '#8888aa',
            p: 0.25,
            '&:hover': { color: liked ? '#f472b6' : '#f0f0f0' },
          }}
        >
          {liked
            ? <FavoriteIcon sx={{ fontSize: 16 }} />
            : <FavoriteBorderIcon sx={{ fontSize: 16 }} />
          }
        </IconButton>
        <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 600, minWidth: 16, textAlign: 'right' }}>
          {count}
        </Typography>
      </Box>
    </Box>
  )
}
```

- [ ] **Step 2: Verify it compiles**

```bash
cd front && npx tsc --noEmit 2>&1 | head -30
```

Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add front/src/components/GridPhotoCard.tsx
git commit -m "feat(components): add GridPhotoCard for explore grid"
```

---

## Task 6: Update PhotoGrid to Use GridPhotoCard

**Files:**
- Modify: `front/src/components/PhotoGrid.tsx`

Replace the raw image grid with `GridPhotoCard` instances. Make columns responsive (2 on mobile, 3 on desktop).

- [ ] **Step 1: Replace PhotoGrid content**

```typescript
import { Box } from '@mui/material'
import { useState } from 'react'
import type { PhotoDto } from '../api/photos'
import { likePhoto, unlikePhoto } from '../api/photos'
import GridPhotoCard from './GridPhotoCard'

interface Props { photos: PhotoDto[] }

export default function PhotoGrid({ photos }: Props) {
  const [likeState, setLikeState] = useState<Record<number, { liked: boolean; count: number }>>(
    () => Object.fromEntries(photos.map((p) => [p.id, { liked: p.likedByMe, count: p.likesCount }]))
  )

  // Sync new photos into likeState without overwriting existing
  const mergedState = (photo: PhotoDto) =>
    likeState[photo.id] ?? { liked: photo.likedByMe, count: photo.likesCount }

  const toggleLike = async (photo: PhotoDto) => {
    const current = mergedState(photo)
    try {
      if (current.liked) {
        const res = await unlikePhoto(photo.id)
        setLikeState((s) => ({ ...s, [photo.id]: { liked: false, count: res.data.likesCount } }))
      } else {
        const res = await likePhoto(photo.id)
        setLikeState((s) => ({ ...s, [photo.id]: { liked: true, count: res.data.likesCount } }))
      }
    } catch { /* ignore */ }
  }

  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: { xs: 'repeat(2, 1fr)', sm: 'repeat(3, 1fr)' },
        gap: '12px',
      }}
    >
      {photos.map((photo) => {
        const state = mergedState(photo)
        return (
          <GridPhotoCard
            key={photo.id}
            photo={photo}
            liked={state.liked}
            count={state.count}
            onToggleLike={() => toggleLike(photo)}
          />
        )
      })}
    </Box>
  )
}
```

- [ ] **Step 2: Verify in browser**

Navigate to `/explore` or `/profile`. Grid should show enriched cards with monster badge and like button below each image. 2 columns on mobile, 3 on desktop.

- [ ] **Step 3: Commit**

```bash
git add front/src/components/PhotoGrid.tsx
git commit -m "feat(grid): responsive enriched photo grid with GridPhotoCard"
```

---

## Task 7: Update PhotoCard (Feed)

**Files:**
- Modify: `front/src/components/PhotoCard.tsx`

Feed card redesign: gradient avatar ring, monster pill badge, hover lift (via theme card override already set in Task 1).

- [ ] **Step 1: Replace PhotoCard content**

```typescript
import { useState } from 'react'
import { Box, Avatar, Typography, IconButton } from '@mui/material'
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
    <Box
      sx={{
        mb: '12px',
        bgcolor: 'background.paper',
        border: '1px solid',
        borderColor: 'divider',
        borderRadius: 2,
        overflow: 'hidden',
        transition: 'box-shadow 0.2s ease, transform 0.2s ease',
        '&:hover': {
          boxShadow: '0 0 0 1px #7c3aed40, 0 8px 24px rgba(124,58,237,0.15)',
          transform: 'translateY(-2px)',
        },
      }}
    >
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', px: 1.5, py: 1.25, gap: 1.25 }}>
        {/* Avatar with gradient ring */}
        <Box
          sx={{
            p: '2px',
            borderRadius: '50%',
            background: 'var(--gradient-primary)',
            flexShrink: 0,
          }}
        >
          <Avatar
            src={photo.avatarUrl ?? undefined}
            sx={{
              width: 36,
              height: 36,
              bgcolor: 'background.paper',
              fontSize: 13,
              fontWeight: 700,
              border: '2px solid',
              borderColor: 'background.paper',
            }}
          >
            {photo.username?.slice(0, 2).toUpperCase()}
          </Avatar>
        </Box>

        <Box sx={{ flexGrow: 1, minWidth: 0 }}>
          <Typography variant="body2" fontWeight={700} noWrap sx={{ color: 'text.primary', lineHeight: 1.3 }}>
            {photo.username}
          </Typography>
          {/* Monster pill */}
          <Box
            sx={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: 0.25,
              px: 0.75,
              py: 0.125,
              borderRadius: '20px',
              background: 'var(--gradient-primary)',
              mt: 0.25,
            }}
          >
            <Typography sx={{ fontSize: 11, lineHeight: 1 }}>{photo.monsterEmoji}</Typography>
            <Typography sx={{ fontSize: 10, fontWeight: 700, color: '#fff', lineHeight: 1 }}>
              {photo.monsterName}
            </Typography>
          </Box>
        </Box>
      </Box>

      {/* Image */}
      <Box sx={{ width: '100%', aspectRatio: '4/5', overflow: 'hidden', bgcolor: '#0a0a18' }}>
        <img
          src={photo.imageUrl}
          alt={photo.monsterName}
          style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }}
        />
      </Box>

      {/* Footer */}
      <Box sx={{ px: 0.5, py: 0.25, display: 'flex', alignItems: 'center', gap: 0.5, borderTop: '1px solid', borderColor: 'divider' }}>
        <IconButton
          onClick={toggleLike}
          size="small"
          sx={{ color: liked ? '#ec4899' : '#8888aa', '&:hover': { color: liked ? '#f472b6' : '#f0f0f0' } }}
        >
          {liked ? <FavoriteIcon sx={{ fontSize: 22 }} /> : <FavoriteBorderIcon sx={{ fontSize: 22 }} />}
        </IconButton>
        <Typography variant="body2" sx={{ color: 'text.secondary', fontSize: 13, fontWeight: 600 }}>
          {count > 0 ? `${count} j'aime` : 'Aucun like'}
        </Typography>
      </Box>
    </Box>
  )
}
```

- [ ] **Step 2: Verify in browser**

Navigate to `/` (feed). Cards show gradient avatar ring, monster pill badge below username. Like button turns pink/rose when liked.

- [ ] **Step 3: Commit**

```bash
git add front/src/components/PhotoCard.tsx
git commit -m "feat(card): gradient avatar ring, monster pill badge, rose like button"
```

---

## Task 8: Update ExplorePage with Bottom Sheet Filters

**Files:**
- Modify: `front/src/pages/ExplorePage.tsx`

Replace inline Chip row with a bottom sheet Drawer. Add filter count badge on the trigger button.

- [ ] **Step 1: Replace ExplorePage content**

```typescript
import { useCallback, useEffect, useRef, useState } from 'react'
import {
  Box, Typography, CircularProgress, Chip, Stack,
  Drawer, IconButton, Button, Badge,
} from '@mui/material'
import FilterListIcon from '@mui/icons-material/FilterList'
import CloseIcon from '@mui/icons-material/Close'
import { getExplore, type PhotoDto } from '../api/photos'
import { getMonsters, type MonsterOption } from '../api/monsters'
import PhotoGrid from '../components/PhotoGrid'

export default function ExplorePage() {
  const [photos, setPhotos] = useState<PhotoDto[]>([])
  const [monsters, setMonsters] = useState<MonsterOption[]>([])
  const [selectedMonsters, setSelectedMonsters] = useState<number[]>([])
  const [pendingMonsters, setPendingMonsters] = useState<number[]>([])
  const [loading, setLoading] = useState(false)
  const [hasMore, setHasMore] = useState(true)
  const [filterOpen, setFilterOpen] = useState(false)
  const offset = useRef(0)
  const seed = useRef(Math.floor(Math.random() * 999983) + 1)
  const sentinel = useRef<HTMLDivElement | null>(null)
  const loadingRef = useRef(false)
  const hasMoreRef = useRef(true)

  useEffect(() => {
    getMonsters().then((res) => setMonsters(res.data))
  }, [])

  const loadMore = useCallback(async () => {
    if (loadingRef.current || !hasMoreRef.current) return
    loadingRef.current = true
    setLoading(true)
    try {
      const res = await getExplore(
        offset.current, seed.current, 20,
        selectedMonsters.length > 0 ? selectedMonsters : undefined
      )
      if (res.data.length === 0) {
        hasMoreRef.current = false
        setHasMore(false)
      } else {
        offset.current += res.data.length
        setPhotos((prev) => [...prev, ...res.data])
      }
    } finally {
      loadingRef.current = false
      setLoading(false)
    }
  }, [selectedMonsters])

  useEffect(() => {
    setPhotos([])
    setHasMore(true)
    hasMoreRef.current = true
    offset.current = 0
    seed.current = Math.floor(Math.random() * 999983) + 1
  }, [selectedMonsters])

  useEffect(() => {
    if (hasMore && photos.length === 0) loadMore()
  }, [hasMore, photos.length, loadMore])

  useEffect(() => {
    const el = sentinel.current
    if (!el) return
    const obs = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting) loadMore()
    })
    obs.observe(el)
    return () => obs.disconnect()
  }, [loadMore])

  const togglePending = (id: number) =>
    setPendingMonsters((prev) =>
      prev.includes(id) ? prev.filter((m) => m !== id) : [...prev, id])

  const openFilter = () => {
    setPendingMonsters(selectedMonsters)
    setFilterOpen(true)
  }

  const applyFilter = () => {
    setSelectedMonsters(pendingMonsters)
    setFilterOpen(false)
  }

  const resetFilter = () => {
    setPendingMonsters([])
  }

  return (
    <Box>
      {/* Header row */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2, px: { xs: 2, sm: 0 } }}>
        <Typography variant="h6" fontWeight={700} sx={{ flexGrow: 1 }}>
          Explorer
        </Typography>
        <IconButton
          onClick={openFilter}
          sx={{
            color: selectedMonsters.length > 0 ? 'primary.main' : 'text.secondary',
            border: '1px solid',
            borderColor: selectedMonsters.length > 0 ? 'primary.main' : 'divider',
            borderRadius: 2,
            px: 1.5,
            gap: 0.5,
            fontSize: 14,
            fontWeight: 600,
          }}
        >
          <Badge badgeContent={selectedMonsters.length || null} color="primary">
            <FilterListIcon sx={{ fontSize: 20 }} />
          </Badge>
          <Box component="span" sx={{ ml: 0.5, fontSize: 13, display: { xs: 'none', sm: 'inline' } }}>
            Filtres
          </Box>
        </IconButton>
      </Box>

      {/* Active filter chips (summary) */}
      {selectedMonsters.length > 0 && (
        <Stack direction="row" flexWrap="wrap" gap={0.75} mb={2} px={{ xs: 2, sm: 0 }}>
          {monsters.filter((m) => selectedMonsters.includes(m.id)).map((m) => (
            <Chip
              key={m.id}
              label={`${m.emoji} ${m.name}`}
              size="small"
              color="primary"
              onDelete={() => setSelectedMonsters((prev) => prev.filter((id) => id !== m.id))}
            />
          ))}
        </Stack>
      )}

      <PhotoGrid photos={photos} />

      <Box ref={sentinel} sx={{ py: 2, textAlign: 'center' }}>
        {loading && <CircularProgress size={24} sx={{ color: 'primary.main' }} />}
        {!hasMore && photos.length > 0 && (
          <Typography variant="body2" color="text.secondary">C'est tout !</Typography>
        )}
      </Box>

      {/* Bottom sheet filter drawer */}
      <Drawer
        anchor="bottom"
        open={filterOpen}
        onClose={() => setFilterOpen(false)}
        PaperProps={{
          sx: {
            borderTopLeftRadius: 20,
            borderTopRightRadius: 20,
            maxHeight: '60vh',
            display: 'flex',
            flexDirection: 'column',
          },
        }}
      >
        {/* Sheet header */}
        <Box sx={{ display: 'flex', alignItems: 'center', px: 2.5, pt: 2, pb: 1, flexShrink: 0 }}>
          <Typography variant="subtitle1" fontWeight={700} sx={{ flexGrow: 1 }}>
            Filtrer par monster
          </Typography>
          <IconButton size="small" onClick={() => setFilterOpen(false)} sx={{ color: 'text.secondary' }}>
            <CloseIcon />
          </IconButton>
        </Box>

        {/* Draggable handle visual */}
        <Box sx={{ width: 40, height: 4, borderRadius: 2, bgcolor: '#2a2a4a', mx: 'auto', mb: 1, flexShrink: 0 }} />

        {/* Monster chips */}
        <Box sx={{ overflowY: 'auto', px: 2.5, pb: 1, flexGrow: 1 }}>
          <Stack direction="row" flexWrap="wrap" gap={1}>
            {monsters.map((m) => (
              <Chip
                key={m.id}
                label={`${m.emoji} ${m.name}`}
                onClick={() => togglePending(m.id)}
                color={pendingMonsters.includes(m.id) ? 'primary' : 'default'}
                sx={{ cursor: 'pointer' }}
              />
            ))}
          </Stack>
        </Box>

        {/* Actions */}
        <Box
          sx={{
            display: 'flex',
            gap: 1.5,
            px: 2.5,
            py: 2,
            borderTop: '1px solid',
            borderColor: 'divider',
            flexShrink: 0,
          }}
        >
          <Button
            variant="text"
            onClick={resetFilter}
            disabled={pendingMonsters.length === 0}
            sx={{ color: 'text.secondary', fontWeight: 600 }}
          >
            Réinitialiser
          </Button>
          <Button
            variant="contained"
            onClick={applyFilter}
            fullWidth
            sx={{ borderRadius: '20px', fontWeight: 700 }}
          >
            Appliquer
            {pendingMonsters.length > 0 && ` (${pendingMonsters.length})`}
          </Button>
        </Box>
      </Drawer>
    </Box>
  )
}
```

- [ ] **Step 2: Verify in browser**

Navigate to `/explore`. Header row shows title + filter button. Clicking filter button opens bottom sheet from bottom. Chips are selectable. "Appliquer" closes sheet and reloads grid. Active filters shown as chips above grid with × to remove individually.

- [ ] **Step 3: Verify mobile**

On narrow viewport (< 600px): bottom sheet fills most of screen, handle visible, scrollable chip list, actions pinned at bottom. BottomNav sits below sheet.

- [ ] **Step 4: Commit**

```bash
git add front/src/pages/ExplorePage.tsx
git commit -m "feat(explore): bottom sheet monster filters, active chip summary, filter badge"
```

---

## Task 9: Final Verification

- [ ] **Step 1: Full build check**

```bash
cd front && npm run build 2>&1 | tail -30
```

Expected: build succeeds with no TypeScript errors.

- [ ] **Step 2: Manual smoke test — Desktop**

1. `/` — Feed: gradient logo in header, gradient nav active state, gradient avatar rings on cards, monster pills, rose likes
2. `/explore` — Wide layout (900px), filter button top right, bottom sheet opens on click, grid enriched cards
3. `/profile` — PhotoGrid with enriched cards
4. `/friends` — No visual regressions
5. `/upload` — No visual regressions

- [ ] **Step 3: Manual smoke test — Mobile (< 600px)**

1. Header: logo + avatar + bell + Publier only (no nav links)
2. BottomNav visible at bottom: Fil / Explorer / Amis / Profil
3. `/explore` filter sheet: opens from bottom, scrollable, actions pinned
4. Content not clipped under BottomNav (72px bottom padding)

- [ ] **Step 4: Final commit**

```bash
git add front/src/ docs/
git commit -m "chore: frontend design overhaul complete"
```
