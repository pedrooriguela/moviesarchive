// Avaliações e watchlist em memória, para usar sem a API (somem ao recarregar a página).
// Não há avaliações pré-cadastradas: a lista de cada filme começa vazia.
// Os formatos seguem o back-end: ReviewResponseDto, MovieReviewsResponseDto e MovieUserStateDto.
import { MOVIES, withRating } from './movies.js'
import {
  findReply,
  replyLikesOf,
  repliesOf,
  reviewLikesOf,
  reviewsOf,
  withEngagement,
  withReplyLikes,
} from './reviewStore.js'
import { getUserFromToken } from '../services/token.js'

const watchlistByUser = new Map() // userId -> [{ movieId, addedAt }]

function delay(value, ms = 350) {
  return new Promise((resolve) => setTimeout(() => resolve(value), ms))
}

function fail(message, ms = 350) {
  return new Promise((_, reject) => setTimeout(() => reject(new Error(message)), ms))
}

function currentUser(token) {
  return getUserFromToken(token) ?? { id: 'visitante', name: 'você' }
}

function ensureMovie(movieId) {
  return MOVIES.some((movie) => movie.id === movieId)
}

export function mockGetMovieReviews(movieId, { page = 1, pageSize = 10 } = {}, token) {
  if (!ensureMovie(movieId)) return fail('Filme não encontrado')

  const all = [...reviewsOf(movieId)].sort((a, b) => b.createdAt.localeCompare(a.createdAt))
  const histogram = new Array(10).fill(0)
  for (const review of all) histogram[review.rating * 2 - 1] += 1
  const total = all.length
  const average = total ? Math.round((all.reduce((sum, r) => sum + r.rating, 0) / total) * 100) / 100 : null
  const start = (page - 1) * pageSize

  return delay({
    items: all.slice(start, start + pageSize).map((review) => withEngagement(review, currentUser(token).id)),
    page,
    pageSize,
    totalCount: total,
    totalPages: Math.max(1, Math.ceil(total / pageSize)),
    averageRating: average,
    histogram,
  })
}

export function mockGetMyMovieState(movieId, token) {
  if (!ensureMovie(movieId)) return fail('Filme não encontrado')
  const user = currentUser(token)
  const review = reviewsOf(movieId).find((r) => r.userId === user.id) ?? null
  const inWatchlist = (watchlistByUser.get(user.id) ?? []).some((item) => item.movieId === movieId)
  return delay({ inWatchlist, review: review && withEngagement(review, user.id) })
}

export function mockSaveMyReview(movieId, { rating, comment }, token) {
  if (!ensureMovie(movieId)) return fail('Filme não encontrado')
  if (!(rating >= 0.5 && rating <= 5 && Number.isInteger(rating * 2))) {
    return fail('A nota deve ficar entre 0,5 e 5 estrelas, em passos de meia estrela.')
  }
  const text = comment?.trim() || null
  if (text && text.length > 2000) return fail('O comentário deve ter até 2000 caracteres.')

  const user = currentUser(token)
  const reviews = reviewsOf(movieId)
  const existing = reviews.find((r) => r.userId === user.id)

  if (existing) {
    existing.rating = rating
    existing.comment = text
    return delay(withEngagement(existing, user.id))
  }

  const created = {
    id: `r-${movieId}-${user.id}`,
    movieId,
    userId: user.id,
    userName: user.name,
    userPhotoUrl: null,
    rating,
    comment: text,
    createdAt: new Date().toISOString(),
  }
  reviews.push(created)
  return delay(withEngagement(created, user.id))
}

export function mockDeleteMyReview(movieId, token) {
  const user = currentUser(token)
  const reviews = reviewsOf(movieId)
  const index = reviews.findIndex((r) => r.userId === user.id)
  if (index === -1) return fail('Você ainda não avaliou este filme.')
  reviews.splice(index, 1)
  return delay(null)
}

export function mockSetInWatchlist(movieId, inWatchlist, token) {
  if (!ensureMovie(movieId)) return fail('Filme não encontrado')
  const user = currentUser(token)
  const items = (watchlistByUser.get(user.id) ?? []).filter((item) => item.movieId !== movieId)
  if (inWatchlist) items.unshift({ movieId, addedAt: new Date().toISOString() })
  watchlistByUser.set(user.id, items)
  return delay(null, 250)
}

export function mockGetMyWatchlist(token) {
  const user = currentUser(token)
  const items = watchlistByUser.get(user.id) ?? []
  return delay(
    items
      .map((item) => MOVIES.find((movie) => movie.id === item.movieId))
      .filter(Boolean)
      .map(withRating),
  )
}

// ---- Respostas e curtidas ----

function findReview(reviewId) {
  for (const movie of MOVIES) {
    const review = reviewsOf(movie.id).find((item) => item.id === reviewId)
    if (review) return review
  }
  return null
}

export function mockGetReplies(reviewId, token) {
  if (!findReview(reviewId)) return fail('Avaliação não encontrada.')
  const viewerId = currentUser(token).id
  return delay(repliesOf(reviewId).map((reply) => withReplyLikes(reply, viewerId)), 250)
}

export function mockAddReply(reviewId, { comment, parentReplyId = null }, token) {
  if (!findReview(reviewId)) return fail('Avaliação não encontrada.')
  const text = comment?.trim() ?? ''
  if (!text) return fail('Escreva a resposta antes de enviar.')
  if (text.length > 1000) return fail('A resposta deve ter até 1000 caracteres.')
  if (parentReplyId) {
    const parent = findReply(parentReplyId)
    if (!parent || parent.reviewId !== reviewId) return fail('Resposta não encontrada.')
    if (parent.isDeleted) return fail('Essa resposta foi excluída e não pode mais ser respondida.')
  }

  const user = currentUser(token)
  const reply = {
    id: crypto.randomUUID(),
    reviewId,
    parentReplyId,
    userId: user.id,
    userName: user.name,
    userPhotoUrl: null,
    comment: text,
    isDeleted: false,
    createdAt: new Date().toISOString(),
  }
  repliesOf(reviewId).push(reply)
  return delay(withReplyLikes(reply, user.id), 250)
}

export function mockDeleteReply(replyId, token) {
  const reply = findReply(replyId)
  if (!reply || reply.isDeleted) return fail('Resposta não encontrada.')
  if (reply.userId !== currentUser(token).id) return fail('Só quem escreveu a resposta pode excluí-la.')

  const replies = repliesOf(reply.reviewId)
  const hasChildren = (id) => replies.some((item) => item.parentReplyId === id)

  if (hasChildren(reply.id)) {
    reply.isDeleted = true
    reply.comment = ''
    return delay(null, 200)
  }

  // Apaga e sobe a conversa removendo respostas já excluídas que ficaram sem filhos
  let current = reply
  while (current) {
    replies.splice(replies.indexOf(current), 1)
    const parent = current.parentReplyId ? replies.find((item) => item.id === current.parentReplyId) : null
    current = parent && parent.isDeleted && !hasChildren(parent.id) ? parent : null
  }
  return delay(null, 200)
}

export function mockSetReviewLike(reviewId, liked, token) {
  if (!findReview(reviewId)) return fail('Avaliação não encontrada.')
  const likes = reviewLikesOf(reviewId)
  const userId = currentUser(token).id
  if (liked) likes.add(userId)
  else likes.delete(userId)
  return delay({ likeCount: likes.size, likedByMe: liked }, 150)
}

export function mockSetReplyLike(replyId, liked, token) {
  const reply = findReply(replyId)
  if (!reply || reply.isDeleted) return fail('Resposta não encontrada.')
  const likes = replyLikesOf(replyId)
  const userId = currentUser(token).id
  if (liked) likes.add(userId)
  else likes.delete(userId)
  return delay({ likeCount: likes.size, likedByMe: liked }, 150)
}
