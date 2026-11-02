import { useEffect, useState } from 'react'
import Hero from '../components/Hero.jsx'
import MovieCard from '../components/MovieCard.jsx'
import Shelf from '../components/Shelf.jsx'
import { getHome, searchCatalog } from '../services/catalog.js'

const PAGE_SIZE = 12

// Listas fixas por enquanto. Depois podem vir da API.
const LANGUAGE_OPTIONS = ['Alemão', 'Coreano', 'Francês', 'Inglês', 'Italiano', 'Japonês', 'Português']
const SORT_OPTIONS = [
  { value: 'default', label: 'Título (A a Z)' },
  { value: 'year-desc', label: 'Mais recentes' },
  { value: 'year-asc', label: 'Mais antigos' },
]
const DEFAULT_FILTERS = { language: '', yearFrom: '', yearTo: '', sort: 'default' }

export default function HomePage({ user, token, onOpenLists, onOpenProfile, onLogout, onOpenMovie }) {
  const [draft, setDraft] = useState('') // o que está digitado
  const [query, setQuery] = useState('') // o que foi de fato buscado
  const [filters, setFilters] = useState(DEFAULT_FILTERS)
  const [page, setPage] = useState(1)
  const [reloadKey, setReloadKey] = useState(0)
  const [result, setResult] = useState({ status: 'loading', data: null, error: '' })
  const [home, setHome] = useState({ status: 'loading', data: null, error: '' })
  const [homeKey, setHomeKey] = useState(0)

  const yearsInvalid = Boolean(
    filters.yearFrom && filters.yearTo && Number(filters.yearFrom) > Number(filters.yearTo),
  )
  const hasCriteria = Boolean(
    query || filters.language || filters.yearFrom || filters.yearTo,
  )
  const browsing = !hasCriteria

  // Busca por texto com atraso, para não buscar a cada tecla
  useEffect(() => {
    const timer = setTimeout(() => {
      setQuery(draft.trim())
      setPage(1)
    }, 300)
    return () => clearTimeout(timer)
  }, [draft])

  // Página inicial: destaque e prateleiras
  useEffect(() => {
    let active = true
    getHome(token)
      .then((data) => {
        if (active) setHome({ status: 'ready', data, error: '' })
      })
      .catch((requestError) => {
        if (active) setHome({ status: 'error', data: null, error: requestError.message })
      })
    return () => {
      active = false
    }
  }, [homeKey, token])

  // Resultados: só busca quando há algum critério
  useEffect(() => {
    if (!hasCriteria) return
    if (yearsInvalid) {
      setResult({ status: 'invalid', data: null, error: '' })
      return
    }

    let active = true
    setResult((previous) => ({ ...previous, status: 'loading', error: '' }))

    searchCatalog(
      {
      query,
      language: filters.language,
      yearFrom: filters.yearFrom ? Number(filters.yearFrom) : undefined,
      yearTo: filters.yearTo ? Number(filters.yearTo) : undefined,
      sort: filters.sort,
      page,
      pageSize: PAGE_SIZE,
      },
      token,
    )
      .then((data) => {
        if (active) setResult({ status: 'ready', data, error: '' })
      })
      .catch((requestError) => {
        if (active) setResult({ status: 'error', data: null, error: requestError.message })
      })

    return () => {
      active = false
    }
  }, [query, filters, page, reloadKey, yearsInvalid, hasCriteria, token])

  function updateFilter(patch) {
    setFilters((current) => ({ ...current, ...patch }))
    setPage(1)
  }

  function clearAll() {
    setDraft('')
    setQuery('')
    setFilters(DEFAULT_FILTERS)
    setPage(1)
  }

  function openHomeTitle(title) {
    setDraft(title)
  }

  function renderShelves() {
    if (home.status === 'loading') {
      return (
        <div className="progress" role="status">
          <div className="progress-track" aria-hidden="true">
            <span className="progress-bar" />
          </div>
          <span>Carregando o acervo...</span>
        </div>
      )
    }

    if (home.status === 'error') {
      return (
        <div className="results-state">
          <div className="alert alert-error" role="alert">
            {home.error}
          </div>
          <button
            type="button"
            className="btn-secondary"
            onClick={() => {
              setHome({ status: 'loading', data: null, error: '' })
              setHomeKey((key) => key + 1)
            }}
          >
            Tentar novamente
          </button>
        </div>
      )
    }

    return (
      <div className="shelves">
        {home.data.shelves.map((shelf) => (
          <Shelf key={shelf.id} id={shelf.id} title={shelf.title} movies={shelf.movies} onOpen={onOpenMovie} />
        ))}
      </div>
    )
  }

  function renderResults() {
    if (result.status === 'loading') {
      return (
        <div className="progress" role="status">
          <div className="progress-track" aria-hidden="true">
            <span className="progress-bar" />
          </div>
          <span>Buscando no acervo...</span>
        </div>
      )
    }

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

    if (result.status === 'invalid') {
      return (
        <div className="alert alert-error" role="alert">
          O ano inicial não pode ser maior que o ano final.
        </div>
      )
    }

    const { items, totalCount, totalPages } = result.data

    if (totalCount === 0) {
      return (
        <div className="results-state">
          <p>Nenhum filme com esses filtros. Remova um filtro para ver mais resultados.</p>
          <button type="button" className="btn-secondary" onClick={clearAll}>
            Limpar filtros
          </button>
        </div>
      )
    }

    const start = (result.data.page - 1) * PAGE_SIZE + 1
    const end = Math.min(result.data.page * PAGE_SIZE, totalCount)

    return (
      <>
        <p className="results-count">
          {totalCount} {totalCount === 1 ? 'filme encontrado' : 'filmes encontrados'}
        </p>
        <div className="results-grid">
          {items.map((movie) => (
            <MovieCard key={movie.id} movie={movie} onOpen={onOpenMovie} />
          ))}
        </div>
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
            Mostrando {start} a {end} de {totalCount}
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
      </>
    )
  }

  return (
    <section className="window home-window" aria-labelledby="busca-titulo">
      <div className="window-title">
        <h1 id="busca-titulo">Buscar filmes</h1>
      </div>

      <nav className="menubar" aria-label="Menu principal">
        <button type="button" onClick={onOpenLists}>Minhas listas</button>
        <button type="button" onClick={onOpenProfile}>Meu perfil</button>
        <button type="button" onClick={onLogout}>Sair</button>
      </nav>

      {browsing && home.status === 'ready' && home.data.featured && (
        <Hero movie={home.data.featured} onSearch={openHomeTitle} onOpen={onOpenMovie} />
      )}

      <div className="window-body">
        <p className="welcome">Olá, {user.name}. Procure um título no acervo.</p>

        <form role="search" onSubmit={(event) => event.preventDefault()}>
          <div className="field">
            <label htmlFor="busca-texto">Título, diretor, elenco ou gênero</label>
            <input
              id="busca-texto"
              type="search"
              placeholder="Ex.: Metropolis, Kurosawa, terror"
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              autoFocus
            />
          </div>
        </form>

        <fieldset className="filter-bar">
          <legend>Filtros</legend>

          <div className="field filter-field">
            <label htmlFor="filtro-idioma">Idioma</label>
            <select
              id="filtro-idioma"
              value={filters.language}
              onChange={(event) => updateFilter({ language: event.target.value })}
            >
              <option value="">Todos</option>
              {LANGUAGE_OPTIONS.map((language) => (
                <option key={language} value={language}>
                  {language}
                </option>
              ))}
            </select>
          </div>

          <div className="field filter-year">
            <label htmlFor="filtro-ano-inicial">Ano inicial</label>
            <input
              id="filtro-ano-inicial"
              type="number"
              inputMode="numeric"
              min="1880"
              max="2100"
              value={filters.yearFrom}
              onChange={(event) => updateFilter({ yearFrom: event.target.value })}
              aria-invalid={yearsInvalid}
            />
          </div>

          <div className="field filter-year">
            <label htmlFor="filtro-ano-final">Ano final</label>
            <input
              id="filtro-ano-final"
              type="number"
              inputMode="numeric"
              min="1880"
              max="2100"
              value={filters.yearTo}
              onChange={(event) => updateFilter({ yearTo: event.target.value })}
              aria-invalid={yearsInvalid}
            />
          </div>

          <div className="field filter-field">
            <label htmlFor="filtro-ordem">Ordenar por</label>
            <select
              id="filtro-ordem"
              value={filters.sort}
              onChange={(event) => updateFilter({ sort: event.target.value })}
            >
              {SORT_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>

          <button
            type="button"
            className="btn-secondary filter-clear"
            onClick={clearAll}
            disabled={!hasCriteria}
          >
            Limpar filtros
          </button>
        </fieldset>

        {browsing ? (
          renderShelves()
        ) : (
          <div className="results" aria-live="polite">
            {renderResults()}
          </div>
        )}
      </div>
    </section>
  )
}
