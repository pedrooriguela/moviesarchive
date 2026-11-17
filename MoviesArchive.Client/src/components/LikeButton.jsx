import { useState } from 'react'

// Coração em pixel art (grade 9 x 8), no mesmo traço das estrelas
const HEART = [
  '.##...##.',
  '####.####',
  '#########',
  '#########',
  '.#######.',
  '..#####..',
  '...###...',
  '....#....',
]

const PIXELS = HEART.flatMap((row, y) =>
  [...row].map((cell, x) => (cell === '#' ? { x, y } : null)).filter(Boolean),
)

function PixelHeart() {
  return (
    <svg className="pixel-heart" viewBox="0 0 9 8" shapeRendering="crispEdges" aria-hidden="true" focusable="false">
      {PIXELS.map(({ x, y }) => (
        <rect key={`${x}-${y}`} x={x} y={y} width="1" height="1" />
      ))}
    </svg>
  )
}

// Curtir/descurtir com resposta imediata na tela; volta atrás se a API recusar.
// onChange(liked) faz o pedido e devolve { likeCount, likedByMe }.
export default function LikeButton({ liked, count, onChange, subject }) {
  const [state, setState] = useState({ liked, count })
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function toggle() {
    if (busy) return
    const previous = state
    const next = { liked: !previous.liked, count: previous.count + (previous.liked ? -1 : 1) }
    setState(next)
    setBusy(true)
    setError('')
    try {
      const result = await onChange(next.liked)
      setState({ liked: result.likedByMe, count: result.likeCount })
    } catch (requestError) {
      setState(previous)
      setError(requestError.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <span className="like">
      <button
        type="button"
        className={state.liked ? 'like-button is-liked' : 'like-button'}
        aria-pressed={state.liked}
        onClick={toggle}
        title={state.liked ? 'Descurtir' : 'Curtir'}
      >
        <PixelHeart />
        <span className="like-count">{state.count}</span>
        <span className="visually-hidden">
          {state.count === 1 ? ' curtida' : ' curtidas'}. Curtir {subject}
        </span>
      </button>
      {error && (
        <span className="like-error" role="alert">
          {error}
        </span>
      )}
    </span>
  )
}
