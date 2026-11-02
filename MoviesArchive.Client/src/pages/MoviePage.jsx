import { useEffect, useState } from 'react'
import MovieArt from '../components/MovieArt.jsx'
import Stars from '../components/Stars.jsx'
import { getMovie } from '../services/catalog.js'

function valueOrDash(value) {
  return value ? value : 'Não informado'
}

export default function MoviePage({ movieId, token, onBack }) {
  const [state, setState] = useState({ status: 'loading', movie: null, error: '' })
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let active = true
    getMovie(movieId, token)
      .then((movie) => {
        if (active) setState({ status: 'ready', movie, error: '' })
      })
      .catch((requestError) => {
        if (active) setState({ status: 'error', movie: null, error: requestError.message })
      })
    return () => {
      active = false
    }
  }, [movieId, token, reloadKey])

  const { status, movie } = state

  function renderBody() {
    if (status === 'loading') {
      return (
        <div className="window-body">
          <div className="progress" role="status">
            <div className="progress-track" aria-hidden="true">
              <span className="progress-bar" />
            </div>
            <span>Carregando a ficha...</span>
          </div>
        </div>
      )
    }

    if (status === 'error') {
      return (
        <div className="window-body">
          <div className="results-state">
            <div className="alert alert-error" role="alert">
              {state.error}
            </div>
            <button
              type="button"
              className="btn-secondary"
              onClick={() => {
                setState({ status: 'loading', movie: null, error: '' })
                setReloadKey((key) => key + 1)
              }}
            >
              Tentar novamente
            </button>
          </div>
        </div>
      )
    }

    return (
      <>
        <div className="movie-backdrop">
          <MovieArt movie={movie} variant="backdrop" />
        </div>

        <div className="movie-page-body">
          <div className="movie-poster-wrap">
            <div className="poster">
              <MovieArt movie={movie} />
              <div className="poster-label">
                <span className="poster-title">{movie.title}</span>
                <span className="poster-year">{movie.releaseYear}</span>
              </div>
            </div>
          </div>

          <div className="movie-info">
            {movie.rating != null && <Stars value={movie.rating} votes={movie.votes} />}

            <dl className="ficha">
              <div className="ficha-row">
                <dt>Direção</dt>
                <dd>{valueOrDash(movie.director)}</dd>
              </div>
              <div className="ficha-row">
                <dt>Ano de lançamento</dt>
                <dd>{valueOrDash(movie.releaseYear)}</dd>
              </div>
              <div className="ficha-row">
                <dt>Idioma</dt>
                <dd>{valueOrDash(movie.language)}</dd>
              </div>
              <div className="ficha-row">
                <dt>Gêneros</dt>
                <dd>{movie.genres.length ? movie.genres.join(', ') : 'Não informado'}</dd>
              </div>
            </dl>

            <h2 className="movie-section-title">Sinopse</h2>
            <p className="movie-synopsis">{movie.synopsis}</p>
          </div>
        </div>
      </>
    )
  }

  return (
    <section className="window home-window" aria-labelledby="ficha-titulo">
      <div className="window-title">
        <h1 id="ficha-titulo">{movie ? movie.title : 'Ficha do filme'}</h1>
      </div>

      <nav className="menubar" aria-label="Navegação">
        <button type="button" onClick={onBack}>
          ← Voltar ao início
        </button>
      </nav>

      {renderBody()}
    </section>
  )
}
