import MovieArt from './MovieArt.jsx'
import Stars from './Stars.jsx'

// Banner de destaque no topo da página inicial
export default function Hero({ movie, onSearch, onOpen }) {
  return (
    <section className="hero" aria-labelledby="hero-titulo">
      <div className="hero-art">
        <MovieArt movie={movie} variant="backdrop" />
      </div>
      <div className="hero-content">
        <h2 id="hero-titulo" className="hero-title">
          {movie.title}
        </h2>
        <p className="hero-meta">
          {movie.releaseYear} · {movie.director} · {movie.language}
        </p>
        {movie.rating != null && <Stars value={movie.rating} votes={movie.votes} />}
        <p className="hero-synopsis">{movie.synopsis}</p>
        <div className="hero-actions">
          <button type="button" className="btn-primary hero-action" onClick={() => onOpen(movie.id)}>
            Ver ficha
          </button>
          <button type="button" className="btn-secondary hero-action" onClick={() => onSearch(movie.title)}>
            Buscar este título
          </button>
        </div>
      </div>
    </section>
  )
}
