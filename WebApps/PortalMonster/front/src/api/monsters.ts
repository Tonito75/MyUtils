import client from './client'

export interface MonsterOption { id: number; name: string; emoji: string }

export const getMonsters = () => client.get<MonsterOption[]>('/api/monsters')
