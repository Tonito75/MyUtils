import client from './client'

export interface NotificationDto {
  id: number; type: string; relatedUserId: string
  relatedUsername: string; relatedAvatarUrl: string | null; createdAt: string
}

export const getNotifications = () =>
  client.get<NotificationDto[]>('/api/notifications')
