import { useEffect, useState } from 'react'
import MovieForm from './MovieForm.jsx'
import { formatNumber } from './format.js'
import { ageRatingLabel } from '../../constants/ageRatings.js'
import { GENRES } from '../../constants/genres.js'
import { removeAdminMovie, restoreAdminMovie, searchAdminMovies } from '../../services/admin.js'

const PAGE_SIZE = 20

const STATUS_OPTIONS = [
  { value: 'all', label: 'Todos' },
  { value: 'active', label: 'No catálogo' },
  { value: 'removed', label: 'Removidos' },
]

const GENRE_LABELS = Object.fromEntries(GENRES.map((genre) => [genre.value, genre.label]))

const removedFormat = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' })

// Catálogo no painel: busca, cadastro, edição, remoção e restauração
export default function MoviesPanel({ token }) {
  const [query, setQuery] = useState('')
  const [appliedQuery, setAppliedQuery] = useState('')
  const [status, setStatus] = useState('all')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState({ status: 'loading', data: null, error: '' })
  const [reloadKey, setReloadKey] = useState(0)
  const [editing, setEditing] = useState(null) // null = lista; 'new' = cadastro; objeto = edição
  const [busyId, setBusyId] = useState(null)
  const [notice, setNotice] = useState('')
  const [error, setError] = useState('')

  // Busca com atraso, para não chamar a API a cada tecla
  useEffect(() => {
    const timer = setTimeout(() => {
      setAppliedQuery(query.trim())
      setPage(1)
    }, 300)
    return () => clearTimeout(timer)
  }, [query])

  useEffect(() => {
    let active = true
    setResult((previous) => ({ ...previous, status: 'loading', error: '' }))
    searchAdminMovies(token, { query: appliedQuery, status, page, pageSize: PAGE_SIZE })
      .then((data) => {
        if (active) setResult({ status: 'ready', data, error: '' })
      })
      .catch((requestError) => {
        if (active) setResult({ status: 'error', data: null, error: requestError.message })
      })
    return () => {
      active = false
    }
  }, [token, appliedQuery, status, page, reloadKey])

  function replaceMovie(updated) {
    setResult((current) => ({
      ...current,
      data: current.data && {
        ...current.data,
        items: current.data.items.map((movie) => (movie.id === updated.id ? updated : movie)),
      },
    }))
  }

  async function handleRemove(movie) {
    const confirmed = window.confirm(
      `Remover “${movie.title}” do catálogo?\n\nEle some da busca, das listas e da ficha para os usuários, ` +
        'mas continua no histórico de quem já assistiu. Dá para restaurar depois.',
    )
    if (!confirmed) return

    setBusyId(movie.id)
    setError('')
    setNotice('')
    try {
      replaceMovie(await removeAdminMovie(token, movie.id))
      setNotice(`“${movie.title}” foi removido do catálogo.`)
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setBusyId(null)
    }
  }

  async function handleRestore(movie) {
    setBusyId(movie.id)
    setError('')
    setNotice('')
    try {
      replaceMovie(await restoreAdminMovie(token, movie.id))
      setNotice(`“${movie.title}” voltou para o catálogo.`)
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setBusyId(null)
    }
  }

  function handleSaved(saved, action) {
    setEditing(null)
    setError('')
    setNotice(action === 'created' ? `“${saved.title}” foi cadastrado.` : `“${saved.title}” foi atualizado.`)
    if (action === 'created') {
      // Mostra o filme novo: busca pelo título dele
      setQuery(saved.title)
      setAppliedQuery(saved.title)
      setStatus('all')
      setPage(1)
    } else {
      replaceMovie(saved)
    }
    setReloadKey((key) => key + 1)
  }

  if (editing) {
    return (
      <MovieForm
        key={editing === 'new' ? 'new' : editing.id}
        token={token}
        movie={editing === 'new' ? null : editing}
        onSaved={handleSaved}
        onCancel={() => setEditing(null)}
      />
    )
  }

  function renderTable() {
    if (!result.data) {
      if (result.status === 'error') {
        return (
          <div className="results-state">
            <div className="alert alert-error" role="alert">
              {result.error}
            </div>
            <button type="button" className="btn-secondary" onClick={() => setReloadKey((key) => key + 1)}>
              Tentar novamente
            </button>
          </div>
        )
      }
      return (
        <p className="field-hint" role="status">
          Carregando filmes...
        </p>
      )
    }

    const { items, totalCount, totalPages } = result.data
    if (totalCount === 0) {
      return <p className="stats-empty">Nenhum filme encontrado.</p>
    }

    return (
      <>
        <div className={result.status === 'loading' ? 'stats-table-wrap is-refreshing' : 'stats-table-wrap'}>
          <table className="stats-table admin-movies-table">
            <thead>
              <tr>
                <th scope="col">Filme</th>
                <th scope="col">Gêneros</th>
                <th scope="col" className="is-center">Classif.</th>
                <th scope="col" className="is-center">Vídeo</th>
                <th scope="col" className="is-number">Acessos</th>
                <th scope="col">Situação</th>
                <th scope="col">
                  <span className="visually-hidden">Ações</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {items.map((movie) => (
                <tr key={movie.id} className={movie.isRemoved ? 'is-removed' : undefined}>
                  <td>
                    <strong>{movie.title}</strong>
                    {movie.releaseYear && <small> ({movie.releaseYear})</small>}
                    {movie.director && <span className="admin-movie-director">{movie.director}</span>}
                  </td>
                  <td>{movie.genres.map((code) => GENRE_LABELS[code] ?? code).join(', ')}</td>
                  <td className="is-center">
                    <span className="age-badge">{ageRatingLabel(movie.ageRating)}</span>
                  </td>
                  <td className="is-center">{movie.videoUrl ? 'Sim' : <span className="field-hint">Não</span>}</td>
                  <td className="is-number">{formatNumber(movie.accessCount)}</td>
                  <td>
                    {movie.isRemoved ? (
                      <span className="badge badge-removed" title={`Removido em ${removedFormat.format(new Date(movie.deletedAt))}`}>
                        Removido
                      </span>
                    ) : (
                      <span className="badge badge-public">No catálogo</span>
                    )}
                  </td>
                  <td>
                    <div className="admin-row-actions">
                      <button
                        type="button"
                        className="btn-secondary btn-small"
                        onClick={() => {
                          setNotice('')
                          setError('')
                          setEditing(movie)
                        }}
                        disabled={busyId === movie.id}
                      >
                        Editar
                      </button>
                      {movie.isRemoved ? (
                        <button
                          type="button"
                          className="btn-secondary btn-small"
                          onClick={() => handleRestore(movie)}
                          disabled={busyId === movie.id}
                        >
                          {busyId === movie.id ? 'Aguarde…' : 'Restaurar'}
                        </button>
                      ) : (
                        <button
                          type="button"
                          className="btn-danger btn-small"
                          onClick={() => handleRemove(movie)}
                          disabled={busyId === movie.id}
                        >
                          {busyId === movie.id ? 'Aguarde…' : 'Remover'}
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {totalPages > 1 && (
          <nav className="pagination" aria-label="Paginação">
            <button
              type="button"
              className="btn-secondary"
              onClick={() => setPage((current) => current - 1)}
              disabled={result.data.page <= 1}
            >
              Anterior
            </button>
            <span>
              Página {result.data.page} de {totalPages}
            </span>
            <button
              type="button"
              className="btn-secondary"
              onClick={() => setPage((current) => current + 1)}
              disabled={result.data.page >= totalPages}
            >
              Próxima
            </button>
          </nav>
        )}
      </>
    )
  }

  const totalCount = result.data?.totalCount

  return (
    <div className="admin-movies">
      <div className="admin-toolbar">
        <div className="field admin-toolbar-search">
          <label htmlFor="admin-busca">Título ou diretor</label>
          <input
            id="admin-busca"
            type="search"
            placeholder="Buscar no catálogo..."
            value={query}
            onChange={(event) => setQuery(event.target.value)}
          />
        </div>
        <div className="field">
          <label htmlFor="admin-situacao">Situação</label>
          <select
            id="admin-situacao"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value)
              setPage(1)
            }}
          >
            {STATUS_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        <button
          type="button"
          className="btn-primary admin-new-movie"
          onClick={() => {
            setNotice('')
            setError('')
            setEditing('new')
          }}
        >
          + Cadastrar filme
        </button>
      </div>

      <p className="field-hint admin-movies-hint">
        Remover tira o filme da busca, das listas e da ficha para os usuários, mas ele continua no histórico de quem já
        assistiu. Um filme removido pode ser restaurado.
        {totalCount != null && ` ${totalCount} ${totalCount === 1 ? 'filme encontrado' : 'filmes encontrados'}.`}
      </p>

      {notice && (
        <div className="alert alert-success" role="status">
          {notice}
        </div>
      )}
      {error && (
        <div className="alert alert-error" role="alert">
          {error}
        </div>
      )}

      {renderTable()}
    </div>
  )
}
