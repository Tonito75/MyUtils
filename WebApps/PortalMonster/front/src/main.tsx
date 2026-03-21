import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.tsx'
import { BrowserRouter } from 'react-router-dom'
import { CssBaseline, ThemeProvider, createTheme } from '@mui/material'

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
