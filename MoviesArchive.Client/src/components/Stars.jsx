import { PixelStarRow } from './PixelStar.jsx'

// Nota média em estrelas, com a quantidade de avaliações ao lado
export default function Stars({ value, votes }) {
  const label = value.toFixed(1).replace('.', ',')

  return (
    <span className="rating">
      <span className="stars" role="img" aria-label={`Nota média ${label} de 5`}>
        <PixelStarRow value={value} />
      </span>
      <span>
        {label}
        {votes !== undefined && ` · ${votes} ${votes === 1 ? 'avaliação' : 'avaliações'}`}
      </span>
    </span>
  )
}
