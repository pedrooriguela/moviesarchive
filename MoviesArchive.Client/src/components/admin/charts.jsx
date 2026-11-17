import { useState } from 'react'
import { formatNumber, formatPointDate, plural } from './format.js'

// Gráficos do painel de estatísticas, feitos com HTML e CSS (sem biblioteca).
// Todos têm uma série só, numa cor (--chart-series); quem precisa dos números exatos
// tem a tabela logo abaixo de cada gráfico.

// Teto "redondo" do eixo e marcas a cada passo de 1, 2 ou 5 × 10^n (no máximo 4 intervalos)
function niceScale(max) {
  if (max <= 0) return { top: 4, step: 1 }
  const rough = max / 4
  const magnitude = 10 ** Math.floor(Math.log10(rough))
  const step = [1, 2, 5, 10].map((factor) => factor * magnitude).find((candidate) => candidate >= rough)
  const wholeStep = Math.max(1, step)
  return { top: Math.ceil(max / wholeStep) * wholeStep, step: wholeStep }
}

// Colunas por dia (ou por mês). O gráfico inteiro é um alvo só de foco: as setas mudam
// o dia destacado, e o mouse destaca a coluna sob ele. O maior valor leva rótulo fixo.
export function ColumnChart({ points, unit, label }) {
  const [active, setActive] = useState(null)
  const max = Math.max(0, ...points.map((point) => point.count))
  const { top, step } = niceScale(max)
  const ticks = []
  for (let value = 0; value <= top; value += step) ticks.push(value)

  const peakIndex = max > 0 ? points.findIndex((point) => point.count === max) : -1
  // No máximo ~8 rótulos no eixo de baixo, sempre incluindo o último ponto
  const every = Math.max(1, Math.ceil(points.length / 8))
  const showLabel = (index) => (points.length - 1 - index) % every === 0
  // Um rótulo sim, outro não, fica "menor": some na tela de celular (ver App.css)
  const isMinorLabel = (index) => ((points.length - 1 - index) / every) % 2 === 1

  function handleKeyDown(event) {
    if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return
    event.preventDefault()
    setActive((current) => {
      const start = current ?? points.length - 1
      if (event.key === 'Home') return 0
      if (event.key === 'End') return points.length - 1
      const next = start + (event.key === 'ArrowLeft' ? -1 : 1)
      return Math.min(points.length - 1, Math.max(0, next))
    })
  }

  const activePoint = active != null ? points[active] : null

  return (
    <div className="column-chart">
      <div className="column-chart-axis" aria-hidden="true">
        {ticks
          .slice()
          .reverse()
          .map((tick) => (
            <span key={tick}>{formatNumber(tick)}</span>
          ))}
      </div>

      <div
        className="column-chart-plot"
        tabIndex={0}
        role="group"
        aria-label={`${label}. Use as setas para percorrer os ${unit === 'month' ? 'meses' : 'dias'}.`}
        onKeyDown={handleKeyDown}
        onBlur={() => setActive(null)}
        onPointerLeave={() => setActive(null)}
      >
        {ticks.map((tick) => (
          <span key={tick} className="column-chart-grid" style={{ bottom: `${(tick / top) * 100}%` }} aria-hidden="true" />
        ))}

        <div className="column-chart-bars">
          {points.map((point, index) => (
            <div
              key={point.date}
              className={index === active ? 'column-chart-slot is-active' : 'column-chart-slot'}
              onPointerEnter={() => setActive(index)}
            >
              {point.count > 0 && (
                <span className="column-chart-bar" style={{ height: `${(point.count / top) * 100}%` }}>
                  {index === peakIndex && active == null && (
                    <span className="column-chart-peak">{formatNumber(point.count)}</span>
                  )}
                </span>
              )}
            </div>
          ))}
        </div>

        {activePoint && (
          <div
            className="chart-tooltip column-chart-tooltip"
            style={{ left: `${((active + 0.5) / points.length) * 100}%` }}
            role="status"
          >
            <strong>{plural(activePoint.count, 'acesso', 'acessos')}</strong>
            <span>{formatPointDate(activePoint.date, unit, true)}</span>
          </div>
        )}
      </div>

      <div className="column-chart-labels" aria-hidden="true">
        {points.map((point, index) => (
          <span key={point.date} className={showLabel(index) && isMinorLabel(index) ? 'is-minor' : undefined}>
            {showLabel(index) ? formatPointDate(point.date, unit) : ''}
          </span>
        ))}
      </div>
    </div>
  )
}

// Barras horizontais em ordem (maior primeiro), com o valor na ponta.
// rows: [{ key, label, value, muted?, note? }]
export function BarList({ rows, total, valueLabel = (value) => formatNumber(value) }) {
  const max = Math.max(0, ...rows.map((row) => row.value))

  return (
    <ol className="bar-list">
      {rows.map((row) => {
        const share = total > 0 ? Math.round((row.value / total) * 100) : 0
        return (
          <li
            key={row.key}
            className={row.muted ? 'bar-list-row is-muted' : 'bar-list-row'}
            title={total > 0 ? `${row.label}: ${valueLabel(row.value)} (${share}% do total)` : undefined}
          >
            <span className="bar-list-label">
              {row.label}
              {row.note}
            </span>
            <span className="bar-list-track">
              {row.value > 0 && max > 0 && (
                <span className="bar-list-bar" style={{ '--ratio': row.value / max }} />
              )}
              <span className="bar-list-value">{valueLabel(row.value)}</span>
            </span>
          </li>
        )
      })}
    </ol>
  )
}
