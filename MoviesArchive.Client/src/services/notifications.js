import { authFetch } from './api.js'
import { USE_MOCKS } from './catalog.js'
import * as mock from '../mocks/notifications.js'

// Avisos de curtidas e respostas no que o usuário escreveu, e do plano familiar.
// Tipos de avaliação: ReviewLiked, ReplyLiked, ReviewReplied, ReplyReplied (com movieId e reviewId).
// Tipos de plano: PlanInviteReceived, PlanInviteAccepted, PlanInviteDeclined (com inviteId, planName e
// inviteStatus: Pending, Accepted, Declined ou Closed). Responder o convite: services/subscriptions.js.

// Avisos de plano levam à página "Minha assinatura"; os demais, à conversa no filme
export function isPlanNotification(notification) {
  return notification.type.startsWith('PlanInvite')
}

/** GET /api/notifications/me — { items, page, pageSize, totalCount, totalPages, unreadCount } */
export function getMyNotifications({ page = 1, pageSize = 20 } = {}, token) {
  if (USE_MOCKS) return mock.mockGetMyNotifications({ page, pageSize }, token)
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return authFetch(`/api/notifications/me?${params}`, token)
}

/** GET /api/notifications/me/unread-count — só o número do sino */
export async function getUnreadNotificationCount(token) {
  const data = USE_MOCKS ? await mock.mockGetUnreadCount(token) : await authFetch('/api/notifications/me/unread-count', token)
  return data.count
}

/** POST /api/notifications/me/read-all */
export function markAllNotificationsRead(token) {
  if (USE_MOCKS) return mock.mockMarkAllRead(token)
  return authFetch('/api/notifications/me/read-all', token, { method: 'POST' })
}

/** POST /api/notifications/{id}/read */
export function markNotificationRead(notificationId, token) {
  if (USE_MOCKS) return mock.mockMarkRead(notificationId, token)
  return authFetch(`/api/notifications/${notificationId}/read`, token, { method: 'POST' })
}
