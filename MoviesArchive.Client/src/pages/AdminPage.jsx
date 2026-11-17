import { useState } from 'react'
import AdminsPanel from '../components/admin/AdminsPanel.jsx'
import MoviesPanel from '../components/admin/MoviesPanel.jsx'
import StatsPanel from '../components/admin/StatsPanel.jsx'
import StatusBar from '../components/StatusBar.jsx'

const TABS = [
  { id: 'stats', label: 'Estatísticas' },
  { id: 'movies', label: 'Filmes' },
  { id: 'admins', label: 'Administradores' },
]

// Painel do administrador (só aparece no perfil de quem tem Users.IsAdmin)
export default function AdminPage({ token, user, onBack }) {
  const [tab, setTab] = useState('stats')

  function handleTabKey(event, index) {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return
    event.preventDefault()
    const next = (index + (event.key === 'ArrowRight' ? 1 : TABS.length - 1)) % TABS.length
    setTab(TABS[next].id)
    document.getElementById(`admin-aba-${TABS[next].id}`)?.focus()
  }

  return (
    <section className="window home-window admin-window" aria-labelledby="admin-titulo">
      <div className="window-title">
        <h1 id="admin-titulo">Administração</h1>
      </div>

      <nav className="menubar" aria-label="Navegação">
        <button type="button" onClick={onBack}>
          ← Voltar ao perfil
        </button>
      </nav>

      <div className="window-body admin-body">
        <div className="tabs" role="tablist" aria-label="Seções do painel">
          {TABS.map((item, index) => (
            <button
              key={item.id}
              id={`admin-aba-${item.id}`}
              type="button"
              role="tab"
              className={tab === item.id ? 'tab is-active' : 'tab'}
              aria-selected={tab === item.id}
              aria-controls="admin-painel"
              tabIndex={tab === item.id ? 0 : -1}
              onClick={() => setTab(item.id)}
              onKeyDown={(event) => handleTabKey(event, index)}
            >
              {item.label}
            </button>
          ))}
        </div>

        <div id="admin-painel" className="tab-panel" role="tabpanel" aria-labelledby={`admin-aba-${tab}`}>
          {tab === 'stats' ? (
            <StatsPanel token={token} />
          ) : tab === 'movies' ? (
            <MoviesPanel token={token} />
          ) : (
            <AdminsPanel token={token} />
          )}
        </div>
      </div>

      <StatusBar items={[`Administrador: ${user.name}`]} />
    </section>
  )
}
