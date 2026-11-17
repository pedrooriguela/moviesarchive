import { useEffect, useState } from 'react'
import Avatar from '../Avatar.jsx'
import { searchAdminUsers, setUserAdmin } from '../../services/admin.js'

// Quem é administrador. Um admin promove outros e tira o acesso deles, mas não o próprio:
// assim sempre sobra pelo menos um.
export default function AdminsPanel({ token }) {
  const [admins, setAdmins] = useState({ status: 'loading', items: [], error: '' })
  const [adminsKey, setAdminsKey] = useState(0)
  const [query, setQuery] = useState('')
  const [results, setResults] = useState({ status: 'idle', items: [], error: '' })
  const [busyId, setBusyId] = useState(null)
  const [notice, setNotice] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    searchAdminUsers(token)
      .then((items) => {
        if (active) setAdmins({ status: 'ready', items, error: '' })
      })
      .catch((requestError) => {
        if (active) setAdmins({ status: 'error', items: [], error: requestError.message })
      })
    return () => {
      active = false
    }
  }, [token, adminsKey])

  // Busca de usuários com atraso, para não chamar a API a cada tecla
  useEffect(() => {
    const term = query.trim()
    if (!term) return

    let active = true
    const timer = setTimeout(() => {
      setResults((current) => ({ ...current, status: 'loading', error: '' }))
      searchAdminUsers(token, term)
        .then((items) => {
          if (active) setResults({ status: 'ready', items, error: '' })
        })
        .catch((requestError) => {
          if (active) setResults({ status: 'error', items: [], error: requestError.message })
        })
    }, 300)
    return () => {
      active = false
      clearTimeout(timer)
    }
  }, [query, token])

  async function change(user, isAdmin) {
    if (!isAdmin && !window.confirm(`Tirar o acesso de administrador de ${user.name}?`)) return

    setBusyId(user.id)
    setNotice('')
    setError('')
    try {
      await setUserAdmin(token, user.id, isAdmin)
      setNotice(isAdmin ? `${user.name} agora é administrador.` : `${user.name} não é mais administrador.`)
      setResults((current) => ({
        ...current,
        items: current.items.map((item) => (item.id === user.id ? { ...item, isAdmin } : item)),
      }))
      setAdminsKey((key) => key + 1)
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setBusyId(null)
    }
  }

  function renderUser(user) {
    return (
      <li key={user.id} className="admin-user">
        <Avatar name={user.name} photoUrl={user.photoUrl} size={36} />
        <span className="admin-user-info">
          <strong>{user.name}</strong>
          {user.isMe && <span className="badge">Você</span>}
          <span className="admin-user-email">{user.email}</span>
        </span>
        {user.isAdmin ? (
          <button
            type="button"
            className="btn-secondary btn-small"
            onClick={() => change(user, false)}
            disabled={user.isMe || busyId === user.id}
            title={user.isMe ? 'Você não pode tirar o seu próprio acesso' : undefined}
          >
            {busyId === user.id ? 'Aguarde…' : 'Tirar acesso'}
          </button>
        ) : (
          <button
            type="button"
            className="btn-primary btn-small"
            onClick={() => change(user, true)}
            disabled={busyId === user.id}
          >
            {busyId === user.id ? 'Aguarde…' : 'Tornar administrador'}
          </button>
        )}
      </li>
    )
  }

  const term = query.trim()

  return (
    <div className="admin-users">
      {notice && (
        <div className="alert alert-success" role="status">
          {notice}
        </div>
      )}
      {error && (
        <div className="alert alert-error" role="alert">
          {error}
        </div>
      )}

      <section className="stats-card" aria-labelledby="admins-atuais">
        <h2 id="admins-atuais" className="stats-card-title">Administradores</h2>
        <p className="stats-card-subtitle">
          Administradores cadastram e removem filmes e veem as estatísticas. Você não pode tirar o seu próprio acesso.
        </p>
        {admins.status === 'loading' ? (
          <p className="field-hint" role="status">
            Carregando...
          </p>
        ) : admins.status === 'error' ? (
          <div className="alert alert-error" role="alert">
            {admins.error}
          </div>
        ) : (
          <ul className="admin-user-list">{admins.items.map(renderUser)}</ul>
        )}
      </section>

      <section className="stats-card" aria-labelledby="admins-adicionar">
        <h2 id="admins-adicionar" className="stats-card-title">Adicionar administrador</h2>
        <div className="field">
          <label htmlFor="admins-busca">Nome de usuário ou e-mail</label>
          <input
            id="admins-busca"
            type="search"
            placeholder="Buscar usuário..."
            value={query}
            onChange={(event) => setQuery(event.target.value)}
          />
        </div>
        {term &&
          (results.status === 'loading' ? (
            <p className="field-hint" role="status">
              Buscando...
            </p>
          ) : results.status === 'error' ? (
            <div className="alert alert-error" role="alert">
              {results.error}
            </div>
          ) : results.status === 'ready' && results.items.length === 0 ? (
            <p className="stats-empty">Nenhum usuário encontrado.</p>
          ) : (
            <ul className="admin-user-list">{results.items.map(renderUser)}</ul>
          ))}
      </section>
    </div>
  )
}
