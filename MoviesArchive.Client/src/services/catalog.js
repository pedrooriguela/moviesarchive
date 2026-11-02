import { authFetch } from './api.js'
import { mockGetHome, mockGetMovie, mockSearchCatalog } from '../mocks/movies.js'

// VITE_USE_MOCKS=false usa a API de verdade. Enquanto o banco não tiver as
// alterações do catálogo (schema_alteracoes_catalogo.sql), deixe como true.
const USE_MOCKS = import.meta.env.VITE_USE_MOCKS !== 'false'

function buildQuery(params) {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  }
  return search.toString()
}

export function searchCatalog(params, token) {
  if (USE_MOCKS) return mockSearchCatalog(params)
  const query = buildQuery({
    query: params.query,
    language: params.language,
    yearFrom: params.yearFrom,
    yearTo: params.yearTo,
    sort: params.sort,
    page: params.page,
    pageSize: params.pageSize,
  })
  return authFetch(`/api/movies/catalog?${query}`, token)
}

export function getHome(token) {
  if (USE_MOCKS) return mockGetHome()
  return authFetch('/api/movies/home', token)
}

export function getMovie(id, token) {
  if (USE_MOCKS) return mockGetMovie(id)
  return authFetch(`/api/movies/${id}`, token)
}
