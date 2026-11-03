// Avaliações e watchlist em memória, para usar sem a API (somem ao recarregar a página).
// Não há avaliações pré-cadastradas: a lista de cada filme começa vazia.
// Os formatos seguem o back-end: ReviewResponseDto, MovieReviewsResponseDto e MovieUserStateDto.
import { MOVIES } from './movies.js'
import { getUserFromToken } from '../services/token.js'

// Começa sem nenhuma avaliação: só aparecem as que o usuário logado publicar nesta sessão.
const reviewsByMovie = new Map() // movieId -> ReviewResponseDto[]
const watchlistByUser = new Map() // userId -> [{ movieId, addedAt }]

function reviewsOf(movieId) {
  if (!reviewsByMovie.has(movieId)) reviewsByMovie.set(movieId, [])
  return reviewsByMovie.get(movieId)
}

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

export function mockGetMovieReviews(movieId, { page = 1, pageSize = 10 } = {}) {
  if (!ensureMovie(movieId)) return fail('Filme não encontrado')

  const all = [...reviewsOf(movieId)].sort((a, b) => b.createdAt.localeCompare(a.createdAt))
  const histogram = new Array(10).fill(0)
  for (const review of all) histogram[review.rating * 2 - 1] += 1
  const total = all.length
  const average = total ? Math.round((all.reduce((sum, r) => sum + r.rating, 0) / total) * 100) / 100 : null
  const start = (page - 1) * pageSize

  return delay({
    items: all.slice(start, start + pageSize),
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
  return delay({ inWatchlist, review: review && { ...review } })
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
    return delay({ ...existing })
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
  return delay({ ...created })
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
  return delay(items.map((item) => MOVIES.find((movie) => movie.id === item.movieId)).filter(Boolean))
}
