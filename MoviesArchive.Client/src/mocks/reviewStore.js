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
