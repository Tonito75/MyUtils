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
