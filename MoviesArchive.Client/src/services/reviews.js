import { authFetch } from './api.js'
import { USE_MOCKS } from './catalog.js'
import * as mock from '../mocks/reviews.js'

// Avaliações (nota de 0,5 a 5 + comentário opcional) e watchlist.
// Com VITE_USE_MOCKS diferente de 'false', tudo roda em memória (ver mocks/reviews.js).

export const RATING_STEPS = [0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5]
export const MAX_COMMENT_LENGTH = 2000

/** GET /api/movies/{id}/reviews — página de avaliações + média e histograma */
export function getMovieReviews(movieId, { page = 1, pageSize = 10 } = {}, token) {
  if (USE_MOCKS) return mock.mockGetMovieReviews(movieId, { page, pageSize })
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return authFetch(`/api/movies/${movieId}/reviews?${params}`, token)
}

/** GET /api/movies/{id}/me — { inWatchlist, review } do usuário logado */
export function getMyMovieState(movieId, token) {
  if (USE_MOCKS) return mock.mockGetMyMovieState(movieId, token)
  return authFetch(`/api/movies/${movieId}/me`, token)
}

/** PUT /api/movies/{id}/review — cria ou substitui a avaliação do usuário logado */
export function saveMyReview(movieId, { rating, comment }, token) {
  if (USE_MOCKS) return mock.mockSaveMyReview(movieId, { rating, comment }, token)
  return authFetch(`/api/movies/${movieId}/review`, token, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ rating, comment: comment?.trim() || null }),
  })
}

/** DELETE /api/movies/{id}/review */
export function deleteMyReview(movieId, token) {
  if (USE_MOCKS) return mock.mockDeleteMyReview(movieId, token)
  return authFetch(`/api/movies/${movieId}/review`, token, { method: 'DELETE' })
}

/** GET /api/watchlist/me — fichas resumidas, da mais recente à mais antiga */
export function getMyWatchlist(token) {
  if (USE_MOCKS) return mock.mockGetMyWatchlist(token)
  return authFetch('/api/watchlist/me', token)
}

/** PUT/DELETE /api/watchlist/{id} — idempotentes */
export function setInWatchlist(movieId, inWatchlist, token) {
  if (USE_MOCKS) return mock.mockSetInWatchlist(movieId, inWatchlist, token)
  return authFetch(`/api/watchlist/${movieId}`, token, { method: inWatchlist ? 'PUT' : 'DELETE' })
}

/** 3.5 -> "3,5" */
export function formatRating(value) {
  return Number.isInteger(value) ? String(value) : value.toFixed(1).replace('.', ',')
}
