import client from './client'

export const register = (username: string, email: string, password: string) =>
  client.post<{ token: string }>('/api/auth/register', { username, email, password })

export const login = (username: string, password: string) =>
  client.post<{ token: string }>('/api/auth/login', { username, password })
