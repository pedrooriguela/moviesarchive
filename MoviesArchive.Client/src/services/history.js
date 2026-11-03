import { authFetch } from './api.js'
import { mockGetHistory, mockRecordWatch } from '../mocks/history.js'

// Mesmo interruptor do catálogo (services/catalog.js). Com os filmes de exemplo,
// os ids ("m01"...) não existem no banco, então o histórico fica guardado no navegador.
// Com VITE_USE_MOCKS=false, o histórico é salvo na tabela WatchHistory pela API.
const USE_MOCKS = import.meta.env.VITE_USE_MOCKS !== 'false'

/** POST /api/history — registra que o usuário apertou play no filme */
export function recordWatch(movie, token) {
  if (USE_MOCKS) return mockRecordWatch(movie, token)
  return authFetch('/api/history', token, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ movieId: movie.id }),
  })
}

/** GET /api/history/me?page&pageSize — linha do tempo, do mais recente para o mais antigo */
export function getMyHistory({ page = 1, pageSize = 20 } = {}, token) {
  if (USE_MOCKS) return mockGetHistory({ page, pageSize }, token)
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return authFetch(`/api/history/me?${params}`, token)
}
