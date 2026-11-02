import MovieCard from './MovieCard.jsx'

// Prateleira horizontal de filmes, com aparência de tira de filme
export default function Shelf({ id, title, movies, onOpen }) {
  const headingId = `shelf-${id}`

  return (
    <section className="shelf" aria-labelledby={headingId}>
      <h2 id={headingId} className="shelf-title">
        {title}
      </h2>
      <ul className="shelf-track" tabIndex={0} aria-label={title}>
        {movies.map((movie) => (
          <li key={movie.id} className="shelf-item">
            <MovieCard movie={movie} onOpen={onOpen} />
          </li>
        ))}
      </ul>
    </section>
  )
}
