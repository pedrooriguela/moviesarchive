// Capa ilustrada original. Se o filme tiver PosterUrl no banco, a imagem real é usada.
const PALETTES = [
  { bg: '#000080', shape: '#f2b630', accent: '#d8d3c0' },
  { bg: '#800000', shape: '#f2b630', accent: '#d8d3c0' },
  { bg: '#006400', shape: '#f2b630', accent: '#d8d3c0' },
  { bg: '#f2b630', shape: '#000000', accent: '#800000' },
  { bg: '#000000', shape: '#f2b630', accent: '#d8d3c0' },
]

function hashOf(text) {
  let hash = 7
  for (const char of text) hash = (hash * 31 + char.charCodeAt(0)) >>> 0
  return hash
}

function Composition({ kind, palette }) {
  if (kind === 0) {
    // Sol sobre o horizonte, com faixas
    return (
      <>
        <circle cx="150" cy="210" r="88" fill={palette.shape} />
        {[0, 1, 2, 3, 4, 5].map((i) => (
          <rect key={i} x="0" y={250 + i * 22} width="300" height={6 + i * 2} fill={palette.bg} />
        ))}
      </>
    )
  }
  if (kind === 1) {
    // Anéis concêntricos, como um disco de vinil ou uma lente
    return (
      <>
        {[30, 60, 90, 120].map((r) => (
          <circle key={r} cx="150" cy="225" r={r} fill="none" stroke={palette.shape} strokeWidth="8" />
        ))}
        <circle cx="150" cy="225" r="14" fill={palette.accent} />
      </>
    )
  }
  if (kind === 2) {
    // Faixas diagonais
    return (
      <>
        <polygon points="0,260 300,90 300,150 0,320" fill={palette.shape} />
        <polygon points="0,340 300,190 300,215 0,365" fill={palette.accent} />
      </>
    )
  }
  // Skyline
  const heights = [160, 230, 120, 280, 190, 250, 140]
  return (
    <>
      {heights.map((height, i) => (
        <rect
          key={i}
          x={10 + i * 41}
          y={450 - height}
          width="34"
          height={height}
          fill={i % 2 ? palette.shape : palette.accent}
        />
      ))}
    </>
  )
}

export default function MovieArt({ movie, variant = 'poster' }) {
  if (movie.posterUrl) return <img className="movie-art" src={movie.posterUrl} alt="" />

  const palette = PALETTES[hashOf(movie.title) % PALETTES.length]
  const kind = hashOf(movie.id) % 4
  const grainId = `grain-${movie.id}-${variant}`

  return (
    <svg
      className="movie-art"
      viewBox="0 0 300 450"
      preserveAspectRatio="xMidYMid slice"
      aria-hidden="true"
      focusable="false"
    >
      <defs>
        <filter id={grainId} x="0" y="0" width="100%" height="100%">
          <feTurbulence type="fractalNoise" baseFrequency="0.85" numOctaves="2" stitchTiles="stitch" />
          <feColorMatrix type="saturate" values="0" />
        </filter>
      </defs>
      <rect width="300" height="450" fill={palette.bg} />
      <Composition kind={kind} palette={palette} />
      <rect width="300" height="450" filter={`url(#${grainId})`} opacity="0.18" />
    </svg>
  )
}
