import MovieArt from './MovieArt.jsx'
import Stars from './Stars.jsx'

export default function MovieCard({ movie, onOpen }) {
  return (
    <article className="movie-card">
      <div className="poster">
        <MovieArt movie={movie} />
        <div className="poster-label">
          <span className="poster-title">{movie.title}</span>
          <span className="poster-year">{movie.releaseYear}</span>
        </div>
      </div>
      <div className="movie-card-body">
        <h3 className="movie-card-title">
          {/* O botão cobre o cartão inteiro (ver .movie-card-open::after) */}
          <button type="button" className="movie-card-open" onClick={() => onOpen?.(movie.id)}>
            {movie.title}
          </button>
        </h3>
        {movie.rating != null && <Stars value={movie.rating} />}
        <p className="movie-card-meta">{[movie.director, movie.language].filter(Boolean).join(', ')}</p>
      </div>
    </article>
  )
}
