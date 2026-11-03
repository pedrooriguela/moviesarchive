// Histórico de mentira para o modo com filmes de exemplo (VITE_USE_MOCKS).
// Fica no localStorage, separado por usuário, e devolve o mesmo formato da API:
//   POST /api/history     -> { id, movieId, movieTitle, releaseYear, posterUrl, watchedAt }
//   GET  /api/history/me  -> { items, page, pageSize, totalCount, totalPages }
import { getUserFromToken } from '../services/token.js'

function storageKey(token) {
  return `moviesarchive.mock-history.${getUserFromToken(token)?.id ?? 'anonimo'}`
}

function readEntries(token) {
  try {
    return JSON.parse(localStorage.getItem(storageKey(token))) ?? []
  } catch {
    return []
  }
}

function writeEntries(token, entries) {
  try {
    localStorage.setItem(storageKey(token), JSON.stringify(entries))
  } catch {
    // Sem localStorage (modo privado, por exemplo): o histórico só não é guardado
  }
}

export function mockRecordWatch(movie, token) {
  return new Promise((resolve) => {
    setTimeout(() => {
      const entry = {
        id: crypto.randomUUID(),
        movieId: movie.id,
        movieTitle: movie.title,
        releaseYear: movie.releaseYear ?? null,
        posterUrl: movie.posterUrl ?? null,
        watchedAt: new Date().toISOString(),
      }
      writeEntries(token, [entry, ...readEntries(token)])
      resolve(entry)
    }, 300)
  })
}

export function mockGetHistory({ page = 1, pageSize = 20 } = {}, token) {
  return new Promise((resolve) => {
    setTimeout(() => {
      const entries = readEntries(token)
      const start = (page - 1) * pageSize
      resolve({
        items: entries.slice(start, start + pageSize),
        page,
        pageSize,
        totalCount: entries.length,
        totalPages: Math.max(1, Math.ceil(entries.length / pageSize)),
      })
    }, 400)
  })
}
