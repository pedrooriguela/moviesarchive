import { authFetch } from './api.js'

// Painel do administrador. Só funciona com a API de verdade (não há versão de exemplo):
// os endpoints conferem Users.IsAdmin e respondem 403 para quem não é administrador.

/** GET /api/admin/stats?days= — sem days, desde o início */
export function getPlatformStats(token, days) {
  const params = days ? `?${new URLSearchParams({ days: String(days) })}` : ''
  return authFetch(`/api/admin/stats${params}`, token)
}

/** GET /api/admin/movies — status: 'all' | 'active' | 'removed' */
export function searchAdminMovies(token, { query = '', status = 'all', page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams({ status, page: String(page), pageSize: String(pageSize) })
  if (query.trim()) params.set('query', query.trim())
  return authFetch(`/api/admin/movies?${params}`, token)
}

export function getAdminMovie(token, movieId) {
  return authFetch(`/api/admin/movies/${movieId}`, token)
}

/** POST /api/admin/movies (novo) ou PUT /api/admin/movies/{id} (edição) */
export function saveAdminMovie(token, movieId, movie) {
  return authFetch(movieId ? `/api/admin/movies/${movieId}` : '/api/admin/movies', token, {
    method: movieId ? 'PUT' : 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(movie),
  })
}

/** DELETE /api/admin/movies/{id} — tira do catálogo, mas mantém nos históricos */
export function removeAdminMovie(token, movieId) {
  return authFetch(`/api/admin/movies/${movieId}`, token, { method: 'DELETE' })
}

export function restoreAdminMovie(token, movieId) {
  return authFetch(`/api/admin/movies/${movieId}/restore`, token, { method: 'POST' })
}

/** GET /api/admin/users — sem query, os administradores atuais */
export function searchAdminUsers(token, query = '') {
  const params = query.trim() ? `?${new URLSearchParams({ query: query.trim() })}` : ''
  return authFetch(`/api/admin/users${params}`, token)
}

/** PUT/DELETE /api/admin/users/{id}/admin */
export function setUserAdmin(token, userId, isAdmin) {
  return authFetch(`/api/admin/users/${userId}/admin`, token, { method: isAdmin ? 'PUT' : 'DELETE' })
}
