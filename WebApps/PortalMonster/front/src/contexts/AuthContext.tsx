import React, { createContext, useContext, useState, useEffect } from 'react'
import { jwtDecode } from 'jwt-decode'

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
