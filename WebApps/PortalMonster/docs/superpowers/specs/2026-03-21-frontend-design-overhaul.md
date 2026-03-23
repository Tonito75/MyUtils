# Frontend Design Overhaul — PortalMonster

**Date:** 2026-03-21
**Scope:** `front/src/` — theme, layout, all pages and shared components

---

## Decisions

| Topic | Choice |
|---|---|
| Theme direction | Violet/indigo → rose/magenta (dark fantasy) |
| Explore grid | Enriched cards (monster info + likes always visible) |
| Mobile nav | Bottom navigation bar (desktop keeps header) |
| Filters | Bottom sheet (slides up from bottom) |
| Explore width | ~900px max |
| CSS strategy | Centralize tokens in `createTheme`; use `sx` only for layout-specific overrides |

---

## 1. Theme (`front/src/main.tsx`)

- **Primary:** `#7c3aed` (violet)
- **Secondary:** `#ec4899` (rose/magenta)
- **Background default:** `#060611`, **paper:** `#0f0f1a`
- **Divider / borders:** `#1a1a2e`
- **Gradient token:** stored as `theme.vars` custom or via `cssVarPrefix` — expose as CSS custom property `--gradient-primary: linear-gradient(135deg, #7c3aed 0%, #ec4899 100%)`
- **Component overrides in theme:**
  - `MuiButton` contained → gradient background, no box-shadow flicker
  - `MuiChip` selected state → gradient background
  - `MuiCard` → subtle violet glow on hover (`box-shadow: 0 0 0 1px #7c3aed40`)
  - `MuiAppBar` → `#07071a` bg with bottom border `#1a1a2e`
  - `MuiBottomNavigation` → `#0a0a1a` bg, active item uses gradient color
  - `MuiPaper` → no background image
  - `MuiTextField` outlined → border color `#2a2a4a`, focused border `primary.main`

---

## 2. Layout (`AppLayout.tsx`)

- **Desktop (≥ sm):** keep `Header`, content area, no bottom nav
- **Mobile (< sm):** hide header nav links (keep logo + avatar + bell), show `BottomNav` component fixed at bottom
- `AppLayout` exposes two max-width slots:
  - Default (feed, profile, friends, upload): `maxWidth: 480px`
  - Wide (explore): `maxWidth: 900px` — page sets via context or prop on `<Outlet>`

---

## 3. Header (`Header.tsx`)

- Background: `#07071a` + `backdrop-filter: blur(12px)` for frosted glass feel
- Logo: gradient text (`background: var(--gradient-primary); -webkit-background-clip: text`)
- Nav links: active state uses gradient underline instead of flat green
- "Publier" button: gradient background (violet → rose)
- Mobile: hide nav links (Fil / Explorer / Amis) — they move to bottom nav

---

## 4. Bottom Navigation (new `BottomNav.tsx`)

- Shown only on `xs` breakpoint
- Fixed at bottom, height 56px, `zIndex: theme.zIndex.appBar`
- Items: Fil (home icon), Explorer (explore icon), Amis (people icon), Profil (person icon)
- Active item: gradient color on icon + label
- Background: `#0a0a1a` with top border `#1a1a2e`

---

## 5. PhotoCard (`PhotoCard.tsx`) — Feed

- Card background: `#0f0f1a` with border `#1a1a2e`
- Avatar ring: 2px gradient border (violet → rose)
- Monster name badge: small pill with gradient background
- Image: `aspectRatio: 4/5`, rounded corners on image itself (not just card)
- Footer: like button with animation; count styled more prominently
- Hover: card lifts slightly (`translateY(-2px)`) with glow

---

## 6. PhotoGrid + GridCard (`PhotoGrid.tsx` + new `GridPhotoCard.tsx`)

- Grid: responsive — 3 cols on desktop, 2 cols on mobile, `gap: 12px`
- Each item: `GridPhotoCard` component (separate from feed `PhotoCard`)
  - Fixed aspect ratio `1/1` for the image
  - Below image: monster emoji + name (1 line), like count with heart icon
  - Gradient overlay on hover with "❤ N" centered
  - Border-radius: 12px, matches theme shape
- Like state managed locally in `PhotoGrid` (current approach, keep)

---

## 7. ExplorePage (`ExplorePage.tsx`)

- Width: `maxWidth: 900px`
- Header row: "Explorer" title left, "Filtres" button right (with filter count badge when active)
- Filters: **Bottom Sheet** using MUI `Drawer` with `anchor="bottom"`:
  - Title "Filtrer par monster", close button
  - Scrollable list of `Chip` items (emoji + name), multi-select
  - "Appliquer" CTA button (gradient), "Réinitialiser" text button
  - `PaperProps` with top border-radius 20px, max-height 60vh
- Grid: uses updated `PhotoGrid` (enriched cards)

---

## 8. CSS Centralization Strategy

- **All colors, gradients, shadows, border-radius** → `createTheme` in `main.tsx`
- **Component-level overrides** → `components` key in theme (not inline `sx`)
- **`sx` props** → only for layout-specific values (margin, padding, flex, grid) that can't be in theme
- **No new `.css` files** — `index.css` kept minimal (font import, body margin reset)
- Custom CSS variable `--gradient-primary` injected via `GlobalStyles` or `CssBaseline` override

---

## Files Touched

| File | Change |
|---|---|
| `front/src/main.tsx` | Full theme rewrite |
| `front/src/components/Layout/AppLayout.tsx` | Add BottomNav, responsive max-width |
| `front/src/components/Layout/Header.tsx` | Gradient logo, responsive hide nav |
| `front/src/components/Layout/BottomNav.tsx` | **New** — mobile bottom navigation |
| `front/src/components/PhotoCard.tsx` | Gradient avatar ring, monster badge, hover glow |
| `front/src/components/PhotoGrid.tsx` | Use GridPhotoCard, responsive cols, gap |
| `front/src/components/GridPhotoCard.tsx` | **New** — enriched grid card |
| `front/src/pages/ExplorePage.tsx` | Bottom sheet filters, wide layout, filter button |
