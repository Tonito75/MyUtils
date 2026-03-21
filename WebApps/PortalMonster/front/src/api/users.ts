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
