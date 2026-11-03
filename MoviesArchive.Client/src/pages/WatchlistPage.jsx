import { useEffect, useState } from 'react'
import MovieCard from '../components/MovieCard.jsx'
import StatusBar from '../components/StatusBar.jsx'
import { getMyWatchlist } from '../services/reviews.js'

export default function WatchlistPage({ token, onBack, onOpenMovie }) {
  const [state, setState] = useState({ status: 'loading', movies: [], error: '' })
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let active = true
    getMyWatchlist(token)
      .then((movies) => {
        if (active) setState({ status: 'ready', movies, error: '' })
      })
      .catch((requestError) => {
        if (active) setState({ status: 'error', movies: [], error: requestError.message })
      })
    return () => {
      active = false
    }
  }, [token, reloadKey])

  function renderBody() {
    if (state.status === 'loading') {
      return (
        <div className="progress" role="status">
          <div className="progress-track" aria-hidden="true">
            <span className="progress-bar" />
          </div>
          <span>Carregando sua watchlist...</span>
        </div>
      )
    }

    if (state.status === 'error') {
      return (
        <div className="results-state">
          <div className="alert alert-error" role="alert">
            {state.error}
          </div>
          <button
            type="button"
            className="btn-secondary"
            onClick={() => {
              setState({ status: 'loading', movies: [], error: '' })
              setReloadKey((key) => key + 1)
            }}
          >
            Tentar novamente
          </button>
        </div>
      )
    }

    if (state.movies.length === 0) {
      return (
        <div className="results-state">
          <p>
            Sua watchlist está vazia. Abra a ficha de um filme e use “Adicionar à watchlist” para guardá-lo aqui.
          </p>
          <button type="button" className="btn-secondary" onClick={onBack}>
            Explorar o acervo
          </button>
        </div>
      )
    }

    return (
      <div className="results-grid">
        {state.movies.map((movie) => (
          <MovieCard key={movie.id} movie={movie} onOpen={onOpenMovie} />
        ))}
      </div>
    )
  }

  const count = state.movies.length
  return (
    <section className="window home-window" aria-labelledby="watchlist-titulo">
      <div className="window-title">
        <h1 id="watchlist-titulo">Sua watchlist</h1>
      </div>

      <nav className="menubar" aria-label="Navegação">
        <button type="button" onClick={onBack}>
          ← Voltar ao início
        </button>
      </nav>

      <div className="window-body">
        <p className="welcome">Filmes que você guardou para assistir depois, do mais recente ao mais antigo.</p>
        {renderBody()}
      </div>

      <StatusBar items={[state.status === 'ready' && `${count} ${count === 1 ? 'filme' : 'filmes'} para assistir`]} />
    </section>
  )
}
