import { useCallback, useEffect, useState } from 'react'
import MovieArt from '../components/MovieArt.jsx'
import Stars from '../components/Stars.jsx'
import ReviewPanel from '../components/ReviewPanel.jsx'
import ReviewsSection from '../components/ReviewsSection.jsx'
import StatusBar from '../components/StatusBar.jsx'
import { getMovie } from '../services/catalog.js'
import { getMovieReviews, getMyMovieState } from '../services/reviews.js'
import { recordWatch } from '../services/history.js'

const REVIEWS_PAGE_SIZE = 10

function valueOrDash(value) {
  return value ? value : 'Não informado'
}

export default function MoviePage({ movieId, token, user, backLabel, onBack, onPlay, onRequirePlan }) {
  const [state, setState] = useState({ status: 'loading', movie: null, error: '' })
  const [reloadKey, setReloadKey] = useState(0)
  const [mine, setMine] = useState({ status: 'loading', data: null, error: '' })
  const [mineKey, setMineKey] = useState(0)
  const [reviews, setReviews] = useState({ status: 'loading', data: null, error: '' })
  const [reviewsKey, setReviewsKey] = useState(0)
  const [loadingMore, setLoadingMore] = useState(false)
  const [playing, setPlaying] = useState(false)
  const [playError, setPlayError] = useState('')

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

  // Watchlist e avaliação do usuário logado
  useEffect(() => {
    let active = true
    getMyMovieState(movieId, token)
      .then((data) => {
        if (active) setMine({ status: 'ready', data, error: '' })
      })
      .catch((requestError) => {
        if (active) setMine({ status: 'error', data: null, error: requestError.message })
      })
    return () => {
      active = false
    }
  }, [movieId, token, mineKey])

  // Primeira página das avaliações; recarrega depois que o usuário publica ou exclui a dele
  useEffect(() => {
    let active = true
    getMovieReviews(movieId, { page: 1, pageSize: REVIEWS_PAGE_SIZE }, token)
      .then((data) => {
        if (active) setReviews({ status: 'ready', data, error: '' })
      })
      .catch((requestError) => {
        if (active) setReviews({ status: 'error', data: null, error: requestError.message })
      })
    return () => {
      active = false
    }
  }, [movieId, token, reviewsKey])

  const refreshReviews = useCallback(() => {
    setReviews((previous) => ({ ...previous, status: 'loading', error: '' }))
    setReviewsKey((key) => key + 1)
  }, [])

  async function loadMoreReviews() {
    const current = reviews.data
    setLoadingMore(true)
    try {
      const next = await getMovieReviews(
        movieId,
        { page: current.page + 1, pageSize: REVIEWS_PAGE_SIZE },
        token,
      )
      const known = new Set(current.items.map((review) => review.id))
      setReviews({
        status: 'ready',
        error: '',
        data: { ...next, items: [...current.items, ...next.items.filter((review) => !known.has(review.id))] },
      })
    } catch (requestError) {
      setReviews((previous) => ({ ...previous, status: 'error', error: requestError.message }))
    } finally {
      setLoadingMore(false)
    }
  }

  const { status, movie } = state

  // Registra a visualização no histórico e só então abre o player
  async function handlePlay() {
    setPlaying(true)
    setPlayError('')
    try {
      await recordWatch(movie, token)
      onPlay(movie)
    } catch (requestError) {
      setPlaying(false)
      // Conta gratuita: em vez de um erro, a página de planos com o aviso
      if (requestError.code === 'subscription-required' && onRequirePlan) {
        onRequirePlan(movie)
        return
      }
      setPlayError(`Não foi possível abrir o filme: ${requestError.message}`)
    }
  }

  function renderPanel() {
    if (mine.status === 'loading') {
      return (
        <aside className="review-panel" aria-busy="true">
          <h2 className="review-panel-title">Seu registro</h2>
          <p role="status" className="field-hint">
            Carregando...
          </p>
        </aside>
      )
    }

    if (mine.status === 'error') {
      return (
        <aside className="review-panel">
          <h2 className="review-panel-title">Seu registro</h2>
          <div className="alert alert-error" role="alert">
            {mine.error}
          </div>
          <button
            type="button"
            className="btn-secondary"
            onClick={() => {
              setMine({ status: 'loading', data: null, error: '' })
              setMineKey((key) => key + 1)
            }}
          >
            Tentar novamente
          </button>
        </aside>
      )
    }

    return (
      <ReviewPanel
        key={movieId}
        movieId={movieId}
        token={token}
        state={mine.data}
        onStateChange={(data) => setMine({ status: 'ready', data, error: '' })}
        onReviewChanged={refreshReviews}
      />
    )
  }

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

    const facts = [movie.releaseYear, movie.director].filter(Boolean).join(', ')

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
            <div className="movie-heading">
              <h2 className="movie-title">{movie.title}</h2>
              {facts && <p className="movie-facts">{facts}</p>}
            </div>

            {/* Mesma média e contagem da seção "Avaliações dos espectadores" */}
            {reviews.data && (
              <p className="movie-catalog-rating">
                <span>Média dos espectadores</span>
                {reviews.data.totalCount > 0 && reviews.data.averageRating != null ? (
                  <Stars value={reviews.data.averageRating} votes={reviews.data.totalCount} />
                ) : (
                  <span>ainda sem avaliações</span>
                )}
              </p>
            )}

            <div className="movie-actions">
              <button type="button" className="btn-primary movie-play" onClick={handlePlay} disabled={playing}>
                <span aria-hidden="true">▶</span> {playing ? 'Abrindo...' : 'Assistir'}
              </button>
            </div>

            {playError && (
              <div className="alert alert-error" role="alert">
                {playError}
              </div>
            )}

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

            <div>
              <h2 className="movie-section-title">Sinopse</h2>
              <p className="movie-synopsis">{movie.synopsis || 'Sem sinopse cadastrada.'}</p>
            </div>
          </div>

          {renderPanel()}
        </div>

        <div className="window-body">
          <ReviewsSection
            data={reviews.data}
            status={reviews.status}
            error={reviews.error}
            userId={user?.id}
            loadingMore={loadingMore}
            onLoadMore={loadMoreReviews}
            onRetry={refreshReviews}
          />
        </div>
      </>
    )
  }

  const reviewCount = reviews.data?.totalCount
  return (
    <section className="window home-window" aria-labelledby="ficha-titulo">
      <div className="window-title">
        <h1 id="ficha-titulo">Ficha do filme</h1>
      </div>

      <nav className="menubar" aria-label="Navegação">
        <button type="button" onClick={onBack}>
          ← {backLabel ?? 'Voltar ao início'}
        </button>
      </nav>

      {renderBody()}

      <StatusBar
        items={[
          reviewCount != null && `${reviewCount} ${reviewCount === 1 ? 'avaliação' : 'avaliações'}`,
          mine.data && (mine.data.inWatchlist ? 'Na sua watchlist' : 'Fora da sua watchlist'),
          mine.data?.review && 'Você já avaliou',
        ]}
      />
    </section>
  )
}
