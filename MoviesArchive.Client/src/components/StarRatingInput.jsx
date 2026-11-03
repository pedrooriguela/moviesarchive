import { useState } from 'react'
import PixelStar from './PixelStar.jsx'
import { formatRating } from '../services/reviews.js'

const STARS = [1, 2, 3, 4, 5]

function clamp(value) {
  return Math.min(5, Math.max(0.5, value))
}

// Seletor de nota de 0,5 a 5 em passos de meia estrela.
// Mouse: metade esquerda da estrela = meia estrela. Clicar de novo na nota atual limpa.
// Teclado: setas mudam 0,5; Home/End vão aos extremos; 1 a 5 escolhem a nota cheia; Delete limpa.
export default function StarRatingInput({ id, value, onChange, disabled = false, labelledBy, describedBy, invalid = false }) {
  const [hover, setHover] = useState(null)
  const shown = hover ?? value ?? 0

  function valueAt(event, star) {
    const rect = event.currentTarget.getBoundingClientRect()
    return event.clientX - rect.left < rect.width / 2 ? star - 0.5 : star
  }

  function handleKeyDown(event) {
    if (disabled) return
    const current = value ?? 0
    let next

    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowUp':
        next = clamp(current + 0.5)
        break
      case 'ArrowLeft':
      case 'ArrowDown':
        next = clamp(current - 0.5)
        break
      case 'Home':
        next = 0.5
        break
      case 'End':
        next = 5
        break
      case 'Delete':
      case 'Backspace':
        next = null
        break
      default:
        if (/^[1-5]$/.test(event.key)) next = Number(event.key)
        else return
    }

    event.preventDefault()
    onChange(next)
  }

  const label = hover ?? value

  return (
    <div className="star-input-wrap">
      <div
        id={id}
        className="star-input"
        role="slider"
        tabIndex={disabled ? -1 : 0}
        aria-label={labelledBy ? undefined : 'Sua nota'}
        aria-labelledby={labelledBy}
        aria-valuemin={0.5}
        aria-valuemax={5}
        aria-valuenow={value ?? undefined}
        aria-valuetext={value ? `${formatRating(value)} de 5 estrelas` : 'Sem nota'}
        aria-describedby={describedBy}
        aria-invalid={invalid || undefined}
        aria-disabled={disabled || undefined}
        onKeyDown={handleKeyDown}
        onMouseLeave={() => setHover(null)}
      >
        {STARS.map((star) => (
          <span
            key={star}
            className="star-input-slot"
            onMouseMove={(event) => !disabled && setHover(valueAt(event, star))}
            onClick={(event) => {
              if (disabled) return
              const next = valueAt(event, star)
              onChange(next === value ? null : next)
              setHover(null)
            }}
          >
            <PixelStar fill={Math.max(0, Math.min(1, shown - (star - 1)))} />
          </span>
        ))}
      </div>
      <span className={hover != null ? 'star-input-value is-preview' : 'star-input-value'} aria-hidden="true">
        {label ? formatRating(label) : '–'}
      </span>
    </div>
  )
}
