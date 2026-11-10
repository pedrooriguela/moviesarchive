import { useEffect, useState } from 'react'
import Hero from '../components/Hero.jsx'
import MovieCard from '../components/MovieCard.jsx'
import Shelf from '../components/Shelf.jsx'
import StatusBar from '../components/StatusBar.jsx'
import { GENRES } from '../constants/genres.js'
import { getHome, searchCatalog } from '../services/catalog.js'

const PAGE_SIZE = 12

// Listas fixas por enquanto. Depois podem vir da API.
const LANGUAGE_OPTIONS = ['Alemão', 'Coreano', 'Francês', 'Inglês', 'Italiano', 'Japonês', 'Português']
const SORT_OPTIONS = [
  { value: 'default', label: 'Título (A a Z)' },
  { value: 'rating-desc', label: 'Mais bem avaliados' },
  { value: 'rating-asc', label: 'Menos bem avaliados' },
  { value: 'year-desc', label: 'Mais recentes' },
  { value: 'year-asc', label: 'Mais antigos' },
]
const DEFAULT_FORM = { query: '', language: '', yearFrom: '', yearTo: '', genres: [], sort: 'default' }

export default function HomePage({ user, token, onOpenLists, onOpenWatchlist, onOpenHistory, onOpenSubscription, onOpenProfile, onLogout, onOpenMovie }) {
  const [form, setForm] = useState(DEFAULT_FORM) // o que está nos campos
  const [applied, setApplied] = useState(null) // o que foi buscado; null = ainda não buscou
  const [page, setPage] = useState(1)
  const [reloadKey, setReloadKey] = useState(0)
  const [result, setResult] = useState({ status: 'loading', data: null, error: '' })
  const [home, setHome] = useState({ status: 'loading', data: null, error: '' })
  const [homeKey, setHomeKey] = useState(0)

  const yearsInvalid = Boolean(
    form.yearFrom && form.yearTo && Number(form.yearFrom) > Number(form.yearTo),
  )
  const browsing = applied === null
  const canClear = !browsing || JSON.stringify(form) !== JSON.stringify(DEFAULT_FORM)

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

  // Resultados: só busca depois que a pessoa aperta "Buscar" (ou muda de página)
  useEffect(() => {
    if (applied === null) return

    let active = true
    setResult((previous) => ({ ...previous, status: 'loading', error: '' }))

    searchCatalog(
      {
        query: applied.query,
        language: applied.language,
        yearFrom: applied.yearFrom ? Number(applied.yearFrom) : undefined,
        yearTo: applied.yearTo ? Number(applied.yearTo) : undefined,
        genres: applied.genres,
        sort: applied.sort,
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
  }, [applied, page, reloadKey, token])

  function updateForm(patch) {
    setForm((current) => ({ ...current, ...patch }))
  }

  // Nenhum gênero marcado = todos. Marcar os nove volta para "Todos", que dá no mesmo.
  function toggleGenre(value) {
    setForm((current) => {
      const genres = current.genres.includes(value)
        ? current.genres.filter((genre) => genre !== value)
        : [...current.genres, value]
      return { ...current, genres: genres.length === GENRES.length ? [] : genres }
    })
  }

  function handleSubmit(event) {
    event.preventDefault()
    if (yearsInvalid) return
    setApplied({ ...form, query: form.query.trim() })
    setPage(1)
  }

  function clearAll() {
    setForm(DEFAULT_FORM)
    setApplied(null)
    setPage(1)
  }

  // Atalho do destaque: já busca pelo título
  function openHomeTitle(title) {
    const next = { ...DEFAULT_FORM, query: title }
    setForm(next)
    setApplied(next)
    setPage(1)
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

    const { items, totalCount, totalPages } = result.data

    if (totalCount === 0) {
      return (
        <div className="results-state">
          <p>Nenhum filme com esses filtros. Remova um filtro para ver mais resultados.</p>
          {applied?.query && (
            <p className="field-hint">
              A busca por texto olha só o título e o diretor. Para gêneros, use as caixas em Filtros.
            </p>
          )}
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
        <button type="button" onClick={onOpenWatchlist}>Watchlist</button>
        <button type="button" onClick={onOpenLists}>Minhas listas</button>
        <button type="button" onClick={onOpenHistory}>Meu histórico</button>
        <button type="button" onClick={onOpenSubscription}>Assinatura</button>
        <button type="button" onClick={onOpenProfile}>Meu perfil</button>
        <button type="button" onClick={onLogout}>Sair</button>
      </nav>

      {browsing && home.status === 'ready' && home.data.featured && (
        <Hero movie={home.data.featured} onSearch={openHomeTitle} onOpen={onOpenMovie} />
      )}

      <div className="window-body">
        <p className="welcome">Olá, {user.name}. Procure um título no acervo.</p>

        <form role="search" onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label htmlFor="busca-texto">Título ou diretor</label>
            <input
              id="busca-texto"
              type="search"
              placeholder="Ex.: Metropolis, Kurosawa, kubrick iluminado"
              value={form.query}
              onChange={(event) => updateForm({ query: event.target.value })}
              autoFocus
            />
          </div>

          <fieldset className="filter-bar">
            <legend>Filtros</legend>

            <div className="field filter-field">
              <label htmlFor="filtro-idioma">Idioma</label>
              <select
                id="filtro-idioma"
                value={form.language}
                onChange={(event) => updateForm({ language: event.target.value })}
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
                value={form.yearFrom}
                onChange={(event) => updateForm({ yearFrom: event.target.value })}
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
                value={form.yearTo}
                onChange={(event) => updateForm({ yearTo: event.target.value })}
                aria-invalid={yearsInvalid}
              />
            </div>

            <div className="field filter-field">
              <label htmlFor="filtro-ordem">Ordenar por</label>
              <select
                id="filtro-ordem"
                value={form.sort}
                onChange={(event) => updateForm({ sort: event.target.value })}
              >
                {SORT_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>

            <fieldset className="genre-filter">
              <legend>Gêneros</legend>
              <div className="genre-options">
                <label className="genre-option genre-option-all">
                  <input
                    type="checkbox"
                    checked={form.genres.length === 0}
                    onChange={() => updateForm({ genres: [] })}
                  />
                  Todos
                </label>
                {GENRES.map((genre) => (
                  <label key={genre.value} className="genre-option">
                    <input
                      type="checkbox"
                      checked={form.genres.includes(genre.value)}
                      onChange={() => toggleGenre(genre.value)}
                    />
                    {genre.label}
                  </label>
                ))}
              </div>
              <p className="genre-hint">Marque um ou mais para ver os filmes que têm pelo menos um deles.</p>
            </fieldset>

            {yearsInvalid && (
              <p className="field-error filter-error" role="alert">
                O ano inicial não pode ser maior que o ano final.
              </p>
            )}

            <div className="filter-actions">
              <button type="button" className="btn-secondary" onClick={clearAll} disabled={!canClear}>
                Limpar filtros
              </button>
              <button type="submit" className="btn-primary" disabled={yearsInvalid}>
                Buscar
              </button>
            </div>
          </fieldset>
        </form>

        {browsing ? (
          renderShelves()
        ) : (
          <div className="results" aria-live="polite">
            {renderResults()}
          </div>
        )}
      </div>

      <StatusBar
        items={[
          `Conectado como ${user.name}`,
          browsing && home.status === 'ready' && `${home.data.shelves.length} prateleiras`,
          !browsing && result.status === 'ready' &&
            `${result.data.totalCount} ${result.data.totalCount === 1 ? 'resultado' : 'resultados'}`,
        ]}
      />
    </section>
  )
}
