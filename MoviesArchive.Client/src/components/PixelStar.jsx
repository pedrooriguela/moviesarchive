import { useId } from 'react'

// Estrela em pixel art (grade 11 x 10). "fill" vai de 0 (vazia) a 1 (cheia); 0.5 = meia estrela.
const GRID = [
  '.....#.....',
  '....###....',
  '....###....',
  '###########',
  '.#########.',
  '..#######..',
  '..#######..',
  '.####.####.',
  '.###...###.',
  '##.......##',
]

const PIXELS = GRID.flatMap((row, y) =>
  [...row].map((cell, x) => (cell === '#' ? { x, y } : null)).filter(Boolean),
)

function Pixels({ className }) {
  return (
    <g className={className}>
      {PIXELS.map(({ x, y }) => (
        <rect key={`${x}-${y}`} x={x} y={y} width="1" height="1" />
      ))}
    </g>
  )
}

export default function PixelStar({ fill = 1, className = '' }) {
  const clipId = useId()
  const width = Math.max(0, Math.min(1, fill)) * 11

  return (
    <svg
      className={`pixel-star ${className}`}
      viewBox="0 0 11 10"
      shapeRendering="crispEdges"
      aria-hidden="true"
      focusable="false"
    >
      <Pixels className="pixel-star-empty" />
      {width > 0 && (
        <>
          <clipPath id={clipId}>
            <rect x="0" y="0" width={width} height="10" />
          </clipPath>
          <g clipPath={`url(#${clipId})`}>
            <Pixels className="pixel-star-fill" />
          </g>
        </>
      )}
    </svg>
  )
}

// Cinco estrelas preenchidas até "value" (0 a 5)
export function PixelStarRow({ value, className = '' }) {
  return (
    <span className={`pixel-star-row ${className}`}>
      {[1, 2, 3, 4, 5].map((star) => (
        <PixelStar key={star} fill={Math.max(0, Math.min(1, value - (star - 1)))} />
      ))}
    </span>
  )
}
