// Nota média em estrelas, com a quantidade de avaliações ao lado
export default function Stars({ value, votes }) {
  const label = value.toFixed(1).replace('.', ',')
  const percent = Math.round((value / 5) * 100)

  return (
    <span className="rating">
      <span className="stars" role="img" aria-label={`Nota média ${label} de 5`}>
        <span className="stars-fill" style={{ width: `${percent}%` }} aria-hidden="true">
          ★★★★★
        </span>
        <span aria-hidden="true">★★★★★</span>
      </span>
      <span>
        {label}
        {votes !== undefined && ` · ${votes} avaliações`}
      </span>
    </span>
  )
}
