import { useState, useEffect } from 'react'
import {
  Popover, List, ListItem, ListItemText, ListItemAvatar, Avatar,
  Typography, Button, Stack, Divider, Box
} from '@mui/material'
import { getNotifications, type NotificationDto } from '../api/notifications'
import { acceptFriend, removeFriend } from '../api/friends'

interface Props {
  anchorEl: HTMLElement | null
  onClose: () => void
}

export default function NotificationPopover({ anchorEl, onClose }: Props) {
  const open = Boolean(anchorEl)
  const [notifications, setNotifications] = useState<NotificationDto[]>([])

  useEffect(() => {
    if (open) {
      getNotifications().then((res) => setNotifications(res.data))
    }
  }, [open])

  const handleAccept = async (userId: string, notifId: number) => {
    await acceptFriend(userId)
    setNotifications((prev) => prev.filter((n) => n.id !== notifId))
  }

  const handleDecline = async (userId: string, notifId: number) => {
    await removeFriend(userId)
    setNotifications((prev) => prev.filter((n) => n.id !== notifId))
  }

  return (
    <Popover open={open} anchorEl={anchorEl} onClose={onClose}
      anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      transformOrigin={{ vertical: 'top', horizontal: 'right' }}>
      <Box sx={{ width: 320, maxHeight: 400, overflowY: 'auto', p: 1 }}>
        {notifications.length === 0 ? (
          <Typography variant="body2" sx={{ p: 2, textAlign: 'center', color: 'text.secondary' }}>
            Aucune nouvelle notification
          </Typography>
        ) : (
          <List dense>
            {notifications.map((n, i) => (
              <Box key={n.id}>
                {i > 0 && <Divider />}
                <ListItem alignItems="flex-start">
                  <ListItemAvatar>
                    <Avatar src={n.relatedAvatarUrl ?? undefined}>
                      {n.relatedUsername.slice(0, 2).toUpperCase()}
                    </Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary={`${n.relatedUsername} vous a envoyé une demande d'ami`}
                    secondary={
                      <Stack direction="row" spacing={1} mt={0.5}>
                        <Button size="small" variant="contained" color="primary"
                          onClick={() => handleAccept(n.relatedUserId, n.id)}>
                          Accepter
                        </Button>
                        <Button size="small" variant="outlined" color="error"
                          onClick={() => handleDecline(n.relatedUserId, n.id)}>
                          Décliner
                        </Button>
                      </Stack>
                    }
                  />
                </ListItem>
              </Box>
            ))}
          </List>
        )}
      </Box>
    </Popover>
  )
}
