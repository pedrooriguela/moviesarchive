// Avaliações fictícias em memória, compartilhadas entre mocks/reviews.js e mocks/movies.js.
// Começa vazio: só existem as avaliações publicadas nesta sessão (somem ao recarregar a página).
const reviewsByMovie = new Map() // movieId -> ReviewResponseDto[]

export function reviewsOf(movieId) {
  if (!reviewsByMovie.has(movieId)) reviewsByMovie.set(movieId, [])
  return reviewsByMovie.get(movieId)
}

// Mesma regra da API: média das notas (2 casas) e quantidade; null quando não há avaliações
export function ratingStatsOf(movieId) {
  const reviews = reviewsOf(movieId)
  if (reviews.length === 0) return null
  const sum = reviews.reduce((total, review) => total + review.rating, 0)
  return { averageRating: Math.round((sum / reviews.length) * 100) / 100, reviewCount: reviews.length }
}

// ---- Respostas e curtidas (mesmas regras da API) ----
const repliesByReview = new Map() // reviewId -> ReplyResponseDto[] (sem likeCount/likedByMe)
const reviewLikes = new Map() // reviewId -> Set(userId)
const replyLikes = new Map() // replyId -> Set(userId)

export function repliesOf(reviewId) {
  if (!repliesByReview.has(reviewId)) repliesByReview.set(reviewId, [])
  return repliesByReview.get(reviewId)
}

export function findReply(replyId) {
  for (const replies of repliesByReview.values()) {
    const reply = replies.find((item) => item.id === replyId)
    if (reply) return reply
  }
  return null
}

function likesIn(map, id) {
  if (!map.has(id)) map.set(id, new Set())
  return map.get(id)
}

export const reviewLikesOf = (reviewId) => likesIn(reviewLikes, reviewId)
export const replyLikesOf = (replyId) => likesIn(replyLikes, replyId)

// Curtidas e respostas de uma avaliação para quem está vendo (como o ReviewEngagement da API)
export function withEngagement(review, viewerId) {
  const likes = reviewLikesOf(review.id)
  return {
    ...review,
    likeCount: likes.size,
    likedByMe: likes.has(viewerId),
    replyCount: repliesOf(review.id).filter((reply) => !reply.isDeleted).length,
  }
}

export function withReplyLikes(reply, viewerId) {
  const likes = replyLikesOf(reply.id)
  return { ...reply, likeCount: likes.size, likedByMe: likes.has(viewerId) }
}
