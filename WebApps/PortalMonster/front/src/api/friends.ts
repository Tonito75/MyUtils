import client from './client'

export interface FriendDto {
  userId: string; username: string; avatarUrl: string | null
}

export const getFriends = () => client.get<FriendDto[]>('/api/friends')
export const getFriendRequests = () => client.get<FriendDto[]>('/api/friends/requests')
export const sendFriendRequest = (userId: string) => client.post(`/api/friends/${userId}`)
export const acceptFriend = (userId: string) => client.put(`/api/friends/${userId}/accept`)
export const removeFriend = (userId: string) => client.delete(`/api/friends/${userId}`)
