// Notificações fictícias, em memória (somem ao recarregar a página). Mesmas regras da API:
// curtir ou responder algo de outra pessoa gera um aviso para ela; descurtir ou excluir a
// resposta apaga o aviso. Formato: NotificationResponseDto / NotificationPageDto.
import { MOVIES } from './movies.js'
import { findReply, reviewsOf } from './reviewStore.js'
import { getUserFromToken } from '../services/token.js'

const byUser = new Map() // userId -> notificações, da mais nova para a mais antiga

function listOf(userId) {
  if (!byUser.has(userId)) byUser.set(userId, [])
  return byUser.get(userId)
}

function delay(value, ms = 200) {
  return new Promise((resolve) => setTimeout(() => resolve(value), ms))
}

function viewerId(token) {
  return getUserFromToken(token)?.id ?? 'visitante'
}

function excerptOf(notification) {
  const text =
    notification.type === 'ReviewLiked'
      ? reviewsOf(notification.movieId).find((review) => review.id === notification.reviewId)?.comment
      : notification.replyId
        ? findReply(notification.replyId)?.comment
        : null
  if (!text) return null
  const singleLine = text.replace(/\s+/g, ' ').trim()
  return singleLine.length <= 120 ? singleLine : `${singleLine.slice(0, 120).trimEnd()}…`
}

// Chamado pelos mocks de curtida e resposta. Ninguém é avisado das próprias ações.
export function pushNotification({ recipientId, actor, type, movieId, reviewId, replyId = null }) {
  if (!recipientId || recipientId === actor.id) return
  listOf(recipientId).unshift({
    id: crypto.randomUUID(),
    type,
    actorId: actor.id,
    actorName: actor.name,
    actorPhotoUrl: null,
    movieId,
    movieTitle: MOVIES.find((movie) => movie.id === movieId)?.title ?? '',
    reviewId,
    replyId,
    createdAt: new Date().toISOString(),
    readAt: null,
  })
}

export function removeLikeNotification({ actorId, type, reviewId, replyId = null }) {
  for (const [userId, items] of byUser) {
    byUser.set(
      userId,
      items.filter(
        (item) => !(item.actorId === actorId && item.type === type && item.reviewId === reviewId && item.replyId === replyId),
      ),
    )
  }
}

export function removeNotificationsForReply(replyId) {
  for (const [userId, items] of byUser) byUser.set(userId, items.filter((item) => item.replyId !== replyId))
}

function toResponse(item) {
  // eslint-disable-next-line no-unused-vars
  const { readAt, ...fields } = item
  return { ...fields, excerpt: excerptOf(item), isRead: readAt != null }
}

export function mockGetMyNotifications({ page = 1, pageSize = 20 } = {}, token) {
  const items = listOf(viewerId(token))
  const start = (page - 1) * pageSize
  return delay({
    items: items.slice(start, start + pageSize).map(toResponse),
    page,
    pageSize,
    totalCount: items.length,
    totalPages: Math.max(1, Math.ceil(items.length / pageSize)),
    unreadCount: items.filter((item) => item.readAt == null).length,
  })
}

export function mockGetUnreadCount(token) {
  return delay({ count: listOf(viewerId(token)).filter((item) => item.readAt == null).length }, 100)
}

export function mockMarkAllRead(token) {
  const now = new Date().toISOString()
  for (const item of listOf(viewerId(token))) item.readAt ??= now
  return delay(null, 100)
}

export function mockMarkRead(notificationId, token) {
  const item = listOf(viewerId(token)).find((entry) => entry.id === notificationId)
  if (!item) return Promise.reject(new Error('Notificação não encontrada.'))
  item.readAt ??= new Date().toISOString()
  return delay(null, 100)
}
