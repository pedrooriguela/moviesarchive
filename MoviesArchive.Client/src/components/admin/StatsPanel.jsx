import { useEffect, useState } from 'react'
import { BarList, ColumnChart } from './charts.jsx'
import { formatNumber, formatPointDate, plural } from './format.js'
import { getPlatformStats } from '../../services/admin.js'
import { formatRating } from '../../services/reviews.js'

// Períodos do painel: o filtro vale para todos os números de acesso da tela
const PERIODS = [
  { days: 7, label: '7 dias' },
  { days: 30, label: '30 dias' },
  { days: 90, label: '90 dias' },
  { days: null, label: 'Todo o período' },
]

const TOP_MOVIES = 10

function StatTile({ label, value, detail }) {
  return (
    <div className="stat-tile">
      <span className="stat-tile-label">{label}</span>
      <span className="stat-tile-value">{value}</span>
      {detail && <span className="stat-tile-detail">{detail}</span>}
    </div>
  )
}

export default function StatsPanel({ token }) {
  const [days, setDays] = useState(30)
  const [state, setState] = useState({ status: 'loading', data: null, error: '' })
  const [reloadKey, setReloadKey] = useState(0)
  const [showAllMovies, setShowAllMovies] = useState(false)

  // Ao trocar o período, os números antigos continuam na tela (apagados) até os novos chegarem
  useEffect(() => {
    let active = true
    setState((previous) => ({ ...previous, status: 'loading', error: '' }))
    getPlatformStats(token, days)
      .then((data) => {
        if (active) setState({ status: 'ready', data, error: '' })
      })
      .catch((requestError) => {
        if (active) setState((previous) => ({ ...previous, status: 'error', error: requestError.message }))
      })
    return () => {
      active = false
    }
  }, [token, days, reloadKey])

  const { data } = state
  const periodLabel = days ? `nos últimos ${days} dias` : 'desde o início'

  function renderBody() {
    if (!data) {
      if (state.status === 'error') return null
      return (
        <div className="progress" role="status">
          <div className="progress-track" aria-hidden="true">
            <span className="progress-bar" />
          </div>
          <span>Calculando as estatísticas...</span>
        </div>
      )
    }

    const genreTotal = data.accessesByGenre.reduce((sum, row) => sum + row.count, 0)
    const movies = showAllMovies ? data.accessesByMovie : data.accessesByMovie.slice(0, TOP_MOVIES)
    const movieMax = Math.max(1, ...data.accessesByMovie.map((row) => row.count))
    const chartTitle = data.timelineUnit === 'month' ? 'Acessos por mês' : 'Acessos por dia'

    return (
      <div className={state.status === 'loading' ? 'stats-body is-refreshing' : 'stats-body'} aria-busy={state.status === 'loading'}>
        <div className="stat-tiles">
          <StatTile
            label={`Acessos ${periodLabel}`}
            value={formatNumber(data.accesses)}
            detail={days ? `${formatNumber(data.allTimeAccesses)} desde o início` : 'cliques em ▶ Assistir'}
          />
          <StatTile
            label="Usuários"
            value={formatNumber(data.totalUsers)}
            detail={`${plural(data.subscribers, 'assinante', 'assinantes')} · ${plural(data.adminUsers, 'admin', 'admins')}`}
          />
          <StatTile
            label={`Quem assistiu ${periodLabel}`}
            value={formatNumber(data.activeViewers)}
            detail={data.totalUsers > 0 ? `${Math.round((data.activeViewers / data.totalUsers) * 100)}% dos usuários` : null}
          />
          <StatTile
            label="Filmes no catálogo"
            value={formatNumber(data.activeMovies)}
            detail={plural(data.removedMovies, 'removido', 'removidos')}
          />
          <StatTile
            label="Avaliações"
            value={formatNumber(data.totalReviews)}
            detail={data.averageRating != null ? `média ${formatRating(Math.round(data.averageRating * 10) / 10)} de 5` : 'nenhuma ainda'}
          />
        </div>

        <section className="stats-card" aria-labelledby="stats-timeline">
          <h2 id="stats-timeline" className="stats-card-title">{chartTitle}</h2>
          <p className="stats-card-subtitle">
            Cada vez que alguém aperta ▶ Assistir conta um acesso. Datas no horário de Brasília.
          </p>
          {data.accesses === 0 ? (
            <p className="stats-empty">Nenhum acesso {periodLabel}.</p>
          ) : (
            <ColumnChart points={data.timeline} unit={data.timelineUnit} label={chartTitle} />
          )}
          <details className="stats-table-toggle">
            <summary>Ver como tabela</summary>
            <div className="stats-table-wrap">
              <table className="stats-table">
                <thead>
                  <tr>
                    <th scope="col">{data.timelineUnit === 'month' ? 'Mês' : 'Dia'}</th>
                    <th scope="col" className="is-number">Acessos</th>
                  </tr>
                </thead>
                <tbody>
                  {data.timeline.map((point) => (
                    <tr key={point.date}>
                      <td>{formatPointDate(point.date, data.timelineUnit, true)}</td>
                      <td className="is-number">{formatNumber(point.count)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </details>
        </section>

        <div className="stats-grid">
          <section className="stats-card" aria-labelledby="stats-genres">
            <h2 id="stats-genres" className="stats-card-title">Acessos por categoria</h2>
            <p className="stats-card-subtitle">Um filme com dois gêneros conta nos dois.</p>
            <BarList
              total={genreTotal}
              rows={data.accessesByGenre.map((row) => ({ key: row.genre, label: row.label, value: row.count }))}
            />
          </section>

          <div className="stats-stack">
            <section className="stats-card" aria-labelledby="stats-plans">
              <h2 id="stats-plans" className="stats-card-title">Assinaturas ativas</h2>
              <div className="stats-table-wrap">
                <table className="stats-table">
                  <thead>
                    <tr>
                      <th scope="col">Plano</th>
                      <th scope="col" className="is-number">Assinaturas</th>
                      <th scope="col" className="is-number">Pessoas</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.subscriptionsByPlan.map((plan) => (
                      <tr key={plan.planName}>
                        <td>{plan.planName}</td>
                        <td className="is-number">{formatNumber(plan.subscriptions)}</td>
                        <td className="is-number">{formatNumber(plan.members)}</td>
                      </tr>
                    ))}
                    <tr className="stats-table-total">
                      <td>Sem plano (conta gratuita)</td>
                      <td className="is-number">–</td>
                      <td className="is-number">{formatNumber(Math.max(0, data.totalUsers - data.subscribers))}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </section>

            <section className="stats-card" aria-labelledby="stats-watchlist">
              <h2 id="stats-watchlist" className="stats-card-title">Mais guardados nas watchlists</h2>
              {data.topWatchlisted.length === 0 ? (
                <p className="stats-empty">Nenhum filme nas watchlists ainda.</p>
              ) : (
                <ol className="stats-ranking">
                  {data.topWatchlisted.map((movie) => (
                    <li key={movie.movieId}>
                      <span className="stats-ranking-title">
                        {movie.title}
                        {movie.releaseYear && <small> ({movie.releaseYear})</small>}
                        {movie.isRemoved && <span className="badge badge-removed">Removido</span>}
                      </span>
                      <span className="stats-ranking-value">{plural(movie.count, 'watchlist', 'watchlists')}</span>
                    </li>
                  ))}
                </ol>
              )}
            </section>
          </div>
        </div>

        <section className="stats-card" aria-labelledby="stats-movies">
          <h2 id="stats-movies" className="stats-card-title">Acessos por filme</h2>
          <p className="stats-card-subtitle">
            Do mais assistido ao menos assistido {periodLabel}. Filmes removidos aparecem enquanto tiverem acessos no
            período.
          </p>
          <div className="stats-table-wrap">
            <table className="stats-table stats-movies-table">
              <thead>
                <tr>
                  <th scope="col" className="is-number">#</th>
                  <th scope="col">Filme</th>
                  <th scope="col">Acessos</th>
                  <th scope="col" className="is-number">Pessoas</th>
                </tr>
              </thead>
              <tbody>
                {movies.map((movie, index) => (
                  <tr key={movie.movieId} className={movie.isRemoved ? 'is-removed' : undefined}>
                    <td className="is-number">{index + 1}</td>
                    <td>
                      {movie.title}
                      {movie.releaseYear && <small> ({movie.releaseYear})</small>}
                      {movie.isRemoved && <span className="badge badge-removed">Removido</span>}
                    </td>
                    <td>
                      <span className="inline-bar">
                        {movie.count > 0 && (
                          <span className="inline-bar-fill" style={{ '--ratio': movie.count / movieMax }} />
                        )}
                        <span className="inline-bar-value">{formatNumber(movie.count)}</span>
                      </span>
                    </td>
                    <td className="is-number">{formatNumber(movie.viewers)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {data.accessesByMovie.length > TOP_MOVIES && (
            <button type="button" className="btn-secondary btn-small" onClick={() => setShowAllMovies((value) => !value)}>
              {showAllMovies ? `Mostrar só os ${TOP_MOVIES} primeiros` : `Mostrar todos (${data.accessesByMovie.length})`}
            </button>
          )}
        </section>
      </div>
    )
  }

  return (
    <div className="stats-panel">
      <div className="stats-filters" role="group" aria-label="Período das estatísticas">
        <span className="stats-filters-label">Período:</span>
        {PERIODS.map((period) => (
          <button
            key={period.label}
            type="button"
            className={days === period.days ? 'btn-secondary btn-small is-pressed' : 'btn-secondary btn-small'}
            aria-pressed={days === period.days}
            onClick={() => setDays(period.days)}
          >
            {period.label}
          </button>
        ))}
        {state.status === 'loading' && data && <span className="field-hint" role="status">Atualizando…</span>}
      </div>

      {state.status === 'error' && (
        <div className="results-state">
          <div className="alert alert-error" role="alert">
            {state.error}
          </div>
          <button type="button" className="btn-secondary" onClick={() => setReloadKey((key) => key + 1)}>
            Tentar novamente
          </button>
        </div>
      )}

      {renderBody()}
    </div>
  )
}
