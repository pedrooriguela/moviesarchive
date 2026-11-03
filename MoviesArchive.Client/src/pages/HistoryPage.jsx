import { useEffect, useState } from 'react'
import MovieArt from '../components/MovieArt.jsx'
import { getMyHistory } from '../services/history.js'

const PAGE_SIZE = 20

const dayFormat = new Intl.DateTimeFormat('pt-BR', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
  year: 'numeric',
})
const timeFormat = new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' })

function dayKey(date) {
  return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`
}

function dayLabel(date) {
  const today = new Date()
  const yesterday = new Date(today)
  yesterday.setDate(today.getDate() - 1)

  if (dayKey(date) === dayKey(today)) return 'Hoje'
  if (dayKey(date) === dayKey(yesterday)) return 'Ontem'

  const label = dayFormat.format(date)
  return label.charAt(0).toUpperCase() + label.slice(1)
}

// As entradas já chegam da mais recente para a mais antiga; aqui só são separadas por dia
function groupByDay(entries) {
  const groups = []
  for (const entry of entries) {
    const date = new Date(entry.watchedAt)
    const key = dayKey(date)
    const current = groups[groups.length - 1]

    if (current?.key === key) current.entries.push({ ...entry, date })
    else groups.push({ key, label: dayLabel(date), entries: [{ ...entry, date }] })
  }
  return groups
}

export default function HistoryPage({ token, onBack, onOpenMovie }) {
  const [entries, setEntries] = useState([])
  const [page, setPage] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [status, setStatus] = useState('loading') // 'loading' | 'ready' | 'error'
  const [error, setError] = useState('')
  const [loadingMore, setLoadingMore] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let active = true
    getMyHistory({ page: 1, pageSize: PAGE_SIZE }, token)
      .then((data) => {
        if (!active) return
        setEntries(data.items)
        setPage(1)
        setTotalCount(data.totalCount)
        setStatus('ready')
      })
      .catch((requestError) => {
        if (!active) return
        setError(requestError.message)
        setStatus('error')
      })
    return () => {
      active = false
    }
  }, [token, reloadKey])

  function reload() {
    setError('')
    setStatus('loading')
    setReloadKey((key) => key + 1)
  }

  async function loadMore() {
    setLoadingMore(true)
    setError('')
    try {
      const data = await getMyHistory({ page: page + 1, pageSize: PAGE_SIZE }, token)
      // Evita repetir uma entrada caso o histórico tenha mudado entre uma página e outra
      setEntries((current) => {
        const known = new Set(current.map((entry) => entry.id))
        return [...current, ...data.items.filter((entry) => !known.has(entry.id))]
      })
      setPage(data.page)
      setTotalCount(data.totalCount)
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setLoadingMore(false)
    }
  }

  function renderBody() {
    if (status === 'loading') {
      return (
        <div className="progress" role="status">
          <div className="progress-track" aria-hidden="true">
            <span className="progress-bar" />
          </div>
          <span>Carregando o histórico...</span>
        </div>
      )
    }

    if (status === 'error') {
      return (
        <div className="results-state">
          <div className="alert alert-error" role="alert">
            {error}
          </div>
          <button type="button" className="btn-secondary" onClick={reload}>
            Tentar novamente
          </button>
        </div>
      )
    }

    if (entries.length === 0) {
      return (
        <div className="results-state">
          <p>Você ainda não assistiu nenhum filme. Abra a ficha de um filme e aperte ▶ Assistir.</p>
          <button type="button" className="btn-secondary" onClick={onBack}>
            Procurar filmes
          </button>
        </div>
      )
    }

    const hasMore = entries.length < totalCount

    return (
      <>
        <p className="results-count">
          {totalCount} {totalCount === 1 ? 'visualização' : 'visualizações'}
        </p>

        <ol className="timeline">
          {groupByDay(entries).map((group) => (
            <li key={group.key} className="timeline-day">
              <h2 className="timeline-day-title">{group.label}</h2>
              <ol className="timeline-entries">
                {group.entries.map((entry) => (
                  <li key={entry.id} className="timeline-entry">
                    <time className="timeline-time" dateTime={entry.watchedAt}>
                      {timeFormat.format(entry.date)}
                    </time>
                    <div className="timeline-thumb">
                      <MovieArt
                        movie={{ id: entry.movieId, title: entry.movieTitle, posterUrl: entry.posterUrl }}
                        variant="thumb"
                      />
                    </div>
                    <div className="timeline-info">
                      <button
                        type="button"
                        className="link timeline-movie"
                        onClick={() => onOpenMovie(entry.movieId)}
                      >
                        {entry.movieTitle}
                      </button>
                      {entry.releaseYear && <span className="timeline-year">{entry.releaseYear}</span>}
                    </div>
                  </li>
                ))}
              </ol>
            </li>
          ))}
        </ol>

        {error && (
          <div className="alert alert-error" role="alert">
            {error}
          </div>
        )}

        {hasMore && (
          <button type="button" className="btn-secondary timeline-more" onClick={loadMore} disabled={loadingMore}>
            {loadingMore ? 'Carregando...' : 'Carregar mais'}
          </button>
        )}
      </>
    )
  }

  return (
    <section className="window home-window" aria-labelledby="historico-titulo">
      <div className="window-title">
        <h1 id="historico-titulo">Meu histórico</h1>
      </div>

      <nav className="menubar" aria-label="Navegação">
        <button type="button" onClick={onBack}>
          ← Voltar ao início
        </button>
      </nav>

      <div className="window-body history-body">{renderBody()}</div>
    </section>
  )
}
